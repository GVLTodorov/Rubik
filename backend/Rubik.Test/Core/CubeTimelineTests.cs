using NUnit.Framework;
using Rubik.Core;
using Rubik.Core.Notation;

namespace Rubik.Test.Core;

[TestFixture]
internal sealed class CubeTimelineTests
{
    private static readonly MoveParser Parser = new();

    [Test]
    public void New_StartsSolvedWithNoHistory()
    {
        var timeline = new CubeTimeline();

        Assert.That(timeline.Cube.IsSolved, Is.True);
        Assert.That(timeline.History, Is.Empty);
        Assert.That(timeline.RedoStack, Is.Empty);
        Assert.That(timeline.CanUndo, Is.False);
        Assert.That(timeline.CanRedo, Is.False);
    }

    [Test]
    public void Apply_TurnsTheCubeAndRecordsTheMoves()
    {
        var timeline = new CubeTimeline();

        timeline.Apply(Parser.Parse(CubeTestData.AcceptanceSequence));

        Assert.That(Move.Format(timeline.History), Is.EqualTo(CubeTestData.AcceptanceSequence));
        Assert.That(timeline.Cube.Codes(Face.U), Is.EqualTo(CubeTestData.AcceptanceFaces[Face.U]));
        Assert.That(timeline.CanUndo, Is.True);
    }

    [Test]
    public void Undo_TurnsTheInverseAndAllowsRedo()
    {
        var timeline = new CubeTimeline();
        timeline.Apply(Parser.Parse("F R'"));

        var turned = timeline.Undo();

        Assert.That(turned.ToString(), Is.EqualTo("R"));
        Assert.That(Move.Format(timeline.History), Is.EqualTo("F"));
        Assert.That(Move.Format(timeline.RedoStack), Is.EqualTo("R'"));
        Assert.That(timeline.Cube, Is.EqualTo(Cube.Solved().Apply(Parser.Parse("F"))));
        Assert.That(timeline.CanRedo, Is.True);
    }

    [Test]
    public void Redo_ReappliesTheUndoneMovesInOrder()
    {
        var timeline = new CubeTimeline();
        timeline.Apply(Parser.Parse("F R' U"));
        var before = timeline.Cube;
        timeline.Undo();
        timeline.Undo();

        var first = timeline.Redo();
        var second = timeline.Redo();

        Assert.That(first.ToString(), Is.EqualTo("R'"));
        Assert.That(second.ToString(), Is.EqualTo("U"));
        Assert.That(timeline.Cube, Is.EqualTo(before));
        Assert.That(Move.Format(timeline.History), Is.EqualTo("F R' U"));
        Assert.That(timeline.CanRedo, Is.False);
    }

    [Test]
    public void Apply_ClearsTheRedoStack()
    {
        var timeline = new CubeTimeline();
        timeline.Apply(Parser.Parse("F R"));
        timeline.Undo();

        timeline.Apply(Parser.Parse("U"));

        Assert.That(timeline.CanRedo, Is.False);
        Assert.That(Move.Format(timeline.History), Is.EqualTo("F U"));
    }

    [Test]
    public void Apply_WithNoMoves_KeepsTheRedoStack()
    {
        var timeline = new CubeTimeline();
        timeline.Apply(Parser.Parse("F"));
        timeline.Undo();

        timeline.Apply([]);

        Assert.That(Move.Format(timeline.RedoStack), Is.EqualTo("F"));
        Assert.That(timeline.Cube.IsSolved, Is.True);
    }

    [Test]
    public void Apply_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CubeTimeline().Apply(null!));
    }

    [Test]
    public void Reset_ReturnsToSolvedAndClearsEverything()
    {
        var timeline = new CubeTimeline();
        timeline.Apply(Parser.Parse("F R U"));
        timeline.Undo();

        timeline.Reset();

        Assert.That(timeline.Cube.IsSolved, Is.True);
        Assert.That(timeline.History, Is.Empty);
        Assert.That(timeline.RedoStack, Is.Empty);
    }

    [Test]
    public void UndoAndRedo_WhenEmpty_Throw()
    {
        var timeline = new CubeTimeline();

        Assert.Throws<InvalidOperationException>(() => timeline.Undo());
        Assert.Throws<InvalidOperationException>(() => timeline.Redo());
    }
}
