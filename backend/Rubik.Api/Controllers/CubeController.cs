using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Rubik.Api.Configuration;
using Rubik.Api.Contracts;
using Rubik.Core;
using Rubik.Core.Notation;
using Rubik.Core.Solving;

namespace Rubik.Api.Controllers;

/// <summary>
/// Every cube endpoint. The API keeps each cube and its undo/redo history in memory under a session
/// id (<c>api/cube/{id}</c>), with a sliding expiry. Scrambles and solutions only suggest moves; the
/// client sends them back to <c>{id}/moves</c> to turn them.
/// </summary>
[ApiController]
[Route(RubikConstants.Route.CubeController)]
public sealed partial class CubeController(
    IMoveParser parser,
    IScrambleGenerator scrambler,
    ISolver solver,
    IMemoryCache sessions,
    IRubikConfiguration config,
    ILogger<CubeController> logger) : ControllerBase
{
    private const string SessionKeyPrefix = "cube-session:";

    /// <summary>Starts a new session with a solved cube.</summary>
    /// <response code="201">The new session; its URL is in the <c>Location</c> header.</response>
    [HttpPost]
    [ProducesResponseType<SessionDto>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    public ActionResult<SessionDto> CreateSession()
    {
        var id = Guid.NewGuid();
        var timeline = new CubeTimeline();
        sessions.Set(SessionKey(id), timeline, new MemoryCacheEntryOptions
        {
            SlidingExpiration = config.SessionSlidingExpiration,
        });

        LogSessionCreated(id);
        var session = SessionDto.From(id, timeline);

        return CreatedAtAction(nameof(GetSession), new { id }, session);
    }

    /// <summary>Returns a session's current cube and history.</summary>
    /// <response code="200">The session.</response>
    /// <response code="404">No such session, or it has expired.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public ActionResult<SessionDto> GetSession(Guid id)
    {
        var timeline = FindSession(id);
        if (timeline is null)
        {
            return SessionNotFound(id);
        }

        lock (timeline)
        {
            var session = SessionDto.From(id, timeline);

            return session;
        }
    }

    /// <summary>Applies moves to a session's cube and appends them to its history (clears redo).</summary>
    /// <response code="200">The updated session and the moves turned.</response>
    /// <response code="400">An invalid move token (with <c>token</c> and <c>position</c>).</response>
    /// <response code="404">No such session, or it has expired.</response>
    [HttpPost("{id:guid}/moves")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<SessionUpdateResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public ActionResult<SessionUpdateResponse> ApplySessionMoves(Guid id, SessionMovesRequest request)
    {
        var timeline = FindSession(id);
        if (timeline is null)
        {
            return SessionNotFound(id);
        }

        // Parse before touching the session, so an invalid move leaves it unchanged.
        var moves = parser.Parse(request.Moves);

        // One request at a time per session, so concurrent requests can't lose moves.
        lock (timeline)
        {
            timeline.Apply(moves);
            var update = SessionUpdate(id, timeline, moves);

            return update;
        }
    }

    /// <summary>Undoes the last move by turning its inverse.</summary>
    /// <response code="200">The updated session; <c>appliedMoves</c> holds the inverse move turned.</response>
    /// <response code="404">No such session, or it has expired.</response>
    /// <response code="409">There is no move to undo.</response>
    [HttpPost("{id:guid}/undo")]
    [ProducesResponseType<SessionUpdateResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public ActionResult<SessionUpdateResponse> Undo(Guid id)
    {
        var timeline = FindSession(id);
        if (timeline is null)
        {
            return SessionNotFound(id);
        }

        lock (timeline)
        {
            if (!timeline.CanUndo)
            {
                return SessionConflict("There is no move to undo.");
            }

            var turned = timeline.Undo();
            var update = SessionUpdate(id, timeline, [turned]);

            return update;
        }
    }

    /// <summary>Turns the most recently undone move again.</summary>
    /// <response code="200">The updated session; <c>appliedMoves</c> holds the move turned.</response>
    /// <response code="404">No such session, or it has expired.</response>
    /// <response code="409">There is no move to redo.</response>
    [HttpPost("{id:guid}/redo")]
    [ProducesResponseType<SessionUpdateResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public ActionResult<SessionUpdateResponse> Redo(Guid id)
    {
        var timeline = FindSession(id);
        if (timeline is null)
        {
            return SessionNotFound(id);
        }

        lock (timeline)
        {
            if (!timeline.CanRedo)
            {
                return SessionConflict("There is no move to redo.");
            }

            var turned = timeline.Redo();
            var update = SessionUpdate(id, timeline, [turned]);

            return update;
        }
    }

    /// <summary>Returns the session's cube to solved and clears its history.</summary>
    /// <response code="200">The reset session.</response>
    /// <response code="404">No such session, or it has expired.</response>
    [HttpPost("{id:guid}/reset")]
    [ProducesResponseType<SessionUpdateResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public ActionResult<SessionUpdateResponse> Reset(Guid id)
    {
        var timeline = FindSession(id);
        if (timeline is null)
        {
            return SessionNotFound(id);
        }

        lock (timeline)
        {
            timeline.Reset();
            var update = SessionUpdate(id, timeline, []);

            return update;
        }
    }

    /// <summary>Suggests the steps that solve a session's current cube. The session is not changed.</summary>
    /// <remarks>Send the returned <c>moves</c> to <c>{id}/moves</c> to solve the cube.</remarks>
    /// <response code="200">The solution, stage by stage.</response>
    /// <response code="404">No such session, or it has expired.</response>
    [HttpGet("{id:guid}/solution")]
    [ProducesResponseType<SolutionResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public ActionResult<SolutionResponse> SolveSession(Guid id)
    {
        var timeline = FindSession(id);
        if (timeline is null)
        {
            return SessionNotFound(id);
        }

        Cube cube;
        lock (timeline)
        {
            cube = timeline.Cube;
        }

        var solution = solver.Solve(cube);
        LogSolved(solution.Moves.Count);
        var response = SolutionResponse.From(solution);

        return response;
    }

    /// <summary>Generates a random scramble that never turns the same face twice in a row.</summary>
    /// <param name="length">Number of moves (1–100).</param>
    /// <param name="seed">Optional seed; the same seed always gives the same scramble.</param>
    /// <response code="200">The scramble.</response>
    /// <response code="400">The length is out of range.</response>
    [HttpGet("scramble")]
    [ProducesResponseType<ScrambleResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public ActionResult<ScrambleResponse> GetScramble(
        [FromQuery, Range(RubikConstants.Scramble.MinLength, RubikConstants.Scramble.MaxLength)]
        int length = ScrambleGenerator.DefaultLength,
        [FromQuery] int? seed = null) =>
        new ScrambleResponse { Moves = Move.Format(scrambler.Generate(length, seed)) };

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static string SessionKey(Guid id) => SessionKeyPrefix + id.ToString("N");

    /// <summary>The session's timeline, or null when the id is unknown or has expired.</summary>
    private CubeTimeline? FindSession(Guid id)
    {
        if (sessions.TryGetValue(SessionKey(id), out CubeTimeline? timeline))
        {
            return timeline;
        }

        return null;
    }

    private static SessionUpdateResponse SessionUpdate(Guid id, CubeTimeline timeline, IReadOnlyList<Move> turned) => new()
    {
        Session = SessionDto.From(id, timeline),
        AppliedMoves = CubeStateMapper.ToNotation(turned),
    };

    private ObjectResult SessionNotFound(Guid id) => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Session not found",
        detail: $"Session '{id}' was not found. It may have expired or the API may have restarted.");

    private ObjectResult SessionConflict(string detail) => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Session conflict",
        detail: detail);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created cube session {SessionId}")]
    private partial void LogSessionCreated(Guid sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Suggested a {MoveCount}-move solution")]
    private partial void LogSolved(int moveCount);
}
