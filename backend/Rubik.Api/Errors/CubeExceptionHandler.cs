using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rubik.Api.Configuration;
using Rubik.Core;
using Rubik.Core.Notation;
using Rubik.Core.Solving;

namespace Rubik.Api.Errors;

/// <summary>
/// Turns exceptions into RFC 7807 problem details: domain validation errors become 400 and
/// unsolvable cubes 422, each with a helpful message; anything else becomes a generic 500. No stack
/// trace ever reaches the client. (Unknown sessions and undo/redo conflicts are returned directly
/// by <see cref="Controllers.CubeController"/>.)
/// </summary>
public sealed partial class CubeExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<CubeExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            MoveParseException parse => InvalidMove(parse),
            InvalidCubeStateException state => BadRequest("Invalid cube state", state.Message),
            UnsolvableCubeException unsolvable => Problem(StatusCodes.Status422UnprocessableEntity, "Unsolvable cube", unsolvable.Message),
            _ => null,
        };

        if (problem is null)
        {
            LogUnhandled(exception, httpContext.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            };
        }
        else
        {
            LogRejected(httpContext.Request.Path, problem.Detail);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails InvalidMove(MoveParseException error)
    {
        var problem = BadRequest("Invalid move", error.Message);
        problem.Extensions[RubikConstants.ProblemExtension.Token] = error.Token;
        problem.Extensions[RubikConstants.ProblemExtension.Position] = error.Position;
        problem.Extensions[RubikConstants.ProblemExtension.TokenNumber] = error.TokenNumber;
        return problem;
    }

    private static ProblemDetails BadRequest(string title, string detail) =>
        Problem(StatusCodes.Status400BadRequest, title, detail);

    private static ProblemDetails Problem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected request to {Path}: {Detail}")]
    private partial void LogRejected(string path, string? detail);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Path}")]
    private partial void LogUnhandled(Exception exception, string path);
}
