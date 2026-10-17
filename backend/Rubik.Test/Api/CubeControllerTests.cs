using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NUnit.Framework;
using Rubik.Api.Contracts;
using Rubik.Api.Controllers;
using Rubik.Core;
using Rubik.Core.Notation;

namespace Rubik.Test.Api;

[TestFixture]
internal sealed class CubeControllerTests
{
    private static readonly MoveParser Parser = new();

    private CubeController _controller = null!;

    [SetUp]
    public void CreateController()
    {
        _controller = TestControllers.Create();
    }

    private Guid CreateSession() =>
        ((SessionDto)((CreatedAtActionResult)_controller.CreateSession().Result!).Value!).Id;

    private SessionUpdateResponse Moves(Guid id, string moves) =>
        _controller.ApplySessionMoves(id, new SessionMovesRequest { Moves = moves }).Value!;

    private static ProblemDetails Problem(IConvertToActionResult result, int status)
    {
        var objectResult = (ObjectResult)result.Convert();
        Assert.That(objectResult.StatusCode, Is.EqualTo(status));
        return (ProblemDetails)objectResult.Value!;
    }

    [Test]
    public void CreateSession_Returns201PointingAtGetSession()
    {
        var result = _controller.CreateSession().Result;
        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result!;
        Assert.That(created.Value, Is.TypeOf<SessionDto>());
        var session = (SessionDto)created.Value!;

        Assert.That(created.ActionName, Is.EqualTo(nameof(CubeController.GetSession)));
        Assert.That(created.RouteValues!["id"], Is.EqualTo(session.Id));
        Assert.That(session.History, Is.Empty);
        Assert.That(session.RedoStack, Is.Empty);
        Assert.That(session.State.Faces, Is.EqualTo(CubeStateMapper.ToDto(Cube.Solved()).Faces));
    }

    [Test]
    public void CreateSession_GivesEverySessionItsOwnId()
    {
        var first = CreateSession();
        var second = CreateSession();

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void ApplySessionMoves_AppendsToHistory()
    {
        var id = CreateSession();

        Moves(id, "F R'");
        var response = Moves(id, "U");

        Assert.That(response.AppliedMoves, Is.EqualTo(new[] { "U" }));
        Assert.That(response.Session.History, Is.EqualTo(new[] { "F", "R'", "U" }));
        Assert.That(_controller.GetSession(id).Value!.History, Is.EqualTo(response.Session.History));
    }

    [Test]
    public void ApplySessionMoves_WithAnInvalidToken_LeavesTheSessionUnchanged()
    {
        var id = CreateSession();

        Assert.Throws<MoveParseException>(() => Moves(id, "F X"));

        Assert.That(_controller.GetSession(id).Value!.History, Is.Empty);
    }

    [Test]
    public void Sessions_AreIndependent()
    {
        var changed = CreateSession();
        var untouched = CreateSession();

        Moves(changed, "F");

        Assert.That(_controller.GetSession(untouched).Value!.History, Is.Empty);
    }

    [Test]
    public void UndoThenRedo_TurnTheInverseThenTheMoveAgain()
    {
        var id = CreateSession();
        Moves(id, "R");

        var undone = _controller.Undo(id).Value!;
        Assert.That(undone.AppliedMoves, Is.EqualTo(new[] { "R'" }));
        Assert.That(undone.Session.History, Is.Empty);
        Assert.That(undone.Session.RedoStack, Is.EqualTo(new[] { "R" }));

        var redone = _controller.Redo(id).Value!;
        Assert.That(redone.AppliedMoves, Is.EqualTo(new[] { "R" }));
        Assert.That(redone.Session.History, Is.EqualTo(new[] { "R" }));
        Assert.That(redone.Session.RedoStack, Is.Empty);
    }

    [Test]
    public void Undo_WithNoHistory_Is409()
    {
        var id = CreateSession();

        var problem = Problem(_controller.Undo(id), StatusCodes.Status409Conflict);

        Assert.That(problem.Title, Is.EqualTo("Session conflict"));
        Assert.That(problem.Detail, Is.EqualTo("There is no move to undo."));
    }

    [Test]
    public void Redo_WithNothingUndone_Is409()
    {
        var id = CreateSession();
        Moves(id, "R");

        var problem = Problem(_controller.Redo(id), StatusCodes.Status409Conflict);

        Assert.That(problem.Detail, Is.EqualTo("There is no move to redo."));
    }

    [Test]
    public void Reset_ReturnsToSolvedAndClearsHistory()
    {
        var id = CreateSession();
        Moves(id, "F R U");
        _controller.Undo(id);

        var reset = _controller.Reset(id).Value!;

        Assert.That(reset.AppliedMoves, Is.Empty);
        Assert.That(reset.Session.History, Is.Empty);
        Assert.That(reset.Session.RedoStack, Is.Empty);
        Assert.That(reset.Session.State.Faces["F"], Is.EqualTo(Enumerable.Repeat("G", 9)));
    }

    [Test]
    public void SolveSession_SuggestsMovesThatSolveTheSessionsCube_WithoutChangingIt()
    {
        var id = CreateSession();
        Moves(id, "F R' U B' L D'");
        var cube = Cube.Solved().Apply(Parser.Parse("F R' U B' L D'"));

        var solution = _controller.SolveSession(id).Value!;

        Assert.That(_controller.GetSession(id).Value!.State.Faces, Is.EqualTo(CubeStateMapper.ToDto(cube).Faces));
        Assert.That(cube.IsSolved, Is.False);
        Assert.That(cube.Apply(Parser.Parse(solution.Moves)).IsSolved, Is.True);
        Assert.That(solution.Steps.SelectMany(step => step.Moves), Is.EqualTo(Parser.Parse(solution.Moves).Select(move => move.ToString())));
    }

    [Test]
    public void SolveSession_ForANewSession_ListsEveryStageWithNoMoves()
    {
        var solution = _controller.SolveSession(CreateSession()).Value!;

        Assert.That(solution.Moves, Is.EqualTo(""));
        Assert.That(solution.MoveCount, Is.EqualTo(0));
        Assert.That(solution.Steps.Select(step => step.Stage), Is.EqualTo(new[] { "Cross", "FirstLayerCorners", "MiddleLayer", "LastLayerCross", "LastLayerFace", "LastLayerCorners", "LastLayerEdges" }));
        Assert.That(solution.Steps.Select(step => step.Moves), Is.All.Empty);
        Assert.That(solution.Steps[0].Title, Is.EqualTo("White cross"));
    }

    [Test]
    public void EveryUnknownSessionEndpoint_Is404()
    {
        var id = Guid.NewGuid();

        Assert.That(Problem(_controller.GetSession(id), StatusCodes.Status404NotFound).Title, Is.EqualTo("Session not found"));
        Assert.That(Problem(_controller.ApplySessionMoves(id, new SessionMovesRequest { Moves = "F" }), StatusCodes.Status404NotFound).Detail, Does.Contain(id.ToString()));
        Problem(_controller.Undo(id), StatusCodes.Status404NotFound);
        Problem(_controller.Redo(id), StatusCodes.Status404NotFound);
        Problem(_controller.Reset(id), StatusCodes.Status404NotFound);
        Problem(_controller.SolveSession(id), StatusCodes.Status404NotFound);
    }

    [Test]
    public void Session_ExpiresAfterTheSlidingWindowWithoutUse()
    {
        var clock = new ManualClock();
        _controller = TestControllers.Create(clock, TimeSpan.FromMinutes(10));
        var id = CreateSession();

        clock.Advance(TimeSpan.FromMinutes(11));

        Problem(_controller.GetSession(id), StatusCodes.Status404NotFound);
    }

    [Test]
    public void Session_StaysAliveWhileItKeepsBeingUsed()
    {
        var clock = new ManualClock();
        _controller = TestControllers.Create(clock, TimeSpan.FromMinutes(10));
        var id = CreateSession();

        for (var i = 0; i < 3; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(8));
            Assert.That(_controller.GetSession(id).Value, Is.Not.Null);
        }
    }

    [Test]
    public async Task ConcurrentMoves_ToOneSession_AreAllApplied()
    {
        var id = CreateSession();

        var tasks = new List<Task>();
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => Moves(id, "F")));
        }

        await Task.WhenAll(tasks);

        Assert.That(_controller.GetSession(id).Value!.History, Has.Count.EqualTo(100));
    }

    [Test]
    public void GetScramble_IsRepeatableForASeed()
    {
        var first = _controller.GetScramble(length: 25, seed: 7).Value;
        var second = _controller.GetScramble(length: 25, seed: 7).Value;

        Assert.That(first, Is.Not.Null);
        Assert.That(first.Moves.Split(' ').Length, Is.EqualTo(25));
        Assert.That(second, Is.EqualTo(first));
    }
}
