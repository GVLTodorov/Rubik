using NUnit.Framework;
using Rubik.Core;
using Rubik.Core.Notation;
using Rubik.Core.Solving;

namespace Rubik.Test.Core;

[TestFixture]
internal sealed class BeginnerSolverTests
{
    private static readonly MoveParser Parser = new();
    private static readonly ScrambleGenerator Scrambler = new();
    private static readonly BeginnerSolver Solver = new();
    private static readonly Face[] Sides = [Face.F, Face.R, Face.B, Face.L];

    private static IEnumerable<int> Seeds => Enumerable.Range(1, 300);

    [Test]
    public void SolvedCube_NeedsNoMovesButListsEveryStage()
    {
        var solution = Solver.Solve(Cube.Solved());

        Assert.That(solution.Moves, Is.Empty);
        Assert.That(solution.Steps.Select(step => step.Stage), Is.EqualTo(Enum.GetValues<SolveStage>()));
        Assert.That(solution.Steps.Select(step => step.Moves), Is.All.Empty);
    }

    [Test]
    public void Titles_UseTheCubesColours()
    {
        var titles = Solver.Solve(Cube.Solved()).Steps.Select(step => step.Title);

        Assert.That(titles, Is.EqualTo(new[] { "White cross", "White corners", "Middle layer", "Yellow cross", "Yellow face", "Yellow corners", "Yellow edges" }));
    }

    [Test]
    public void AcceptanceState_IsSolved()
    {
        var cube = Cube.Solved().Apply(Parser.Parse(CubeTestData.AcceptanceSequence));

        Assert.That(cube.Apply(Solver.Solve(cube).Moves).IsSolved, Is.True);
    }

    [TestCaseSource(nameof(Seeds))]
    public void RandomScramble_IsSolved(int seed)
    {
        var cube = Cube.Solved().Apply(Scrambler.Generate(25, seed));

        var solution = Solver.Solve(cube);

        Assert.That(cube.Apply(solution.Moves).IsSolved, Is.True);
        Assert.That(solution.Moves.Count, Is.InRange(1, 250));
    }

    [TestCase(11)]
    [TestCase(42)]
    [TestCase(2026)]
    public void EachStage_LeavesItsLayerSolved(int seed)
    {
        var cube = Cube.Solved().Apply(Scrambler.Generate(25, seed));
        var steps = Solver.Solve(cube).Steps;

        cube = cube.Apply(steps[0].Moves);
        Assert.That(cube.Codes(Face.U, 1, 3, 5, 7), Is.EqualTo("W W W W"));
        Assert.That(Sides.Select(side => cube[side, 1]), Is.EqualTo(Sides.Select(side => side.SolvedColour())));

        cube = cube.Apply(steps[1].Moves);
        Assert.That(cube.Codes(Face.U), Is.EqualTo("W W W W W W W W W"));
        Assert.That(Sides.Select(side => RowsSolved(cube, side, rows: 1)), Is.All.True);

        cube = cube.Apply(steps[2].Moves);
        Assert.That(Sides.Select(side => RowsSolved(cube, side, rows: 2)), Is.All.True);

        cube = cube.Apply(steps[3].Moves);
        Assert.That(cube.Codes(Face.D, 1, 3, 5, 7), Is.EqualTo("Y Y Y Y"));

        cube = cube.Apply(steps[4].Moves);
        Assert.That(cube.GetFace(Face.D), Is.All.EqualTo(Colour.Yellow));

        cube = cube.Apply(steps[5].Moves);
        Assert.That(
            Sides.Select(side => (cube[side, 6], cube[side, 8])),
            Is.EqualTo(Sides.Select(side => (side.SolvedColour(), side.SolvedColour()))));

        cube = cube.Apply(steps[6].Moves);
        Assert.That(cube.IsSolved, Is.True);
    }

    [Test]
    public void Steps_NeverTurnTheSameFaceTwiceInARow()
    {
        var cube = Cube.Solved().Apply(Scrambler.Generate(25, seed: 7));

        foreach (var step in Solver.Solve(cube).Steps)
        {
            Assert.That(step.Moves.Zip(step.Moves.Skip(1)).Where(pair => pair.First.Face == pair.Second.Face), Is.Empty);
        }
    }

    [Test]
    public void TwistedCorner_IsUnsolvable()
    {
        var faces = Stickers(Cube.Solved());
        // Twist the U-F-R corner (U 8, F 2, R 0) once clockwise.
        (faces[Face.U][8], faces[Face.F][2], faces[Face.R][0]) = (faces[Face.F][2], faces[Face.R][0], faces[Face.U][8]);

        Assert.Throws<UnsolvableCubeException>(() => Solver.Solve(Cube.FromFaces(ReadOnly(faces))));
    }

    [Test]
    public void FlippedEdge_IsUnsolvable()
    {
        var faces = Stickers(Cube.Solved().Apply(Parser.Parse("R U F")));
        (faces[Face.U][7], faces[Face.F][1]) = (faces[Face.F][1], faces[Face.U][7]);

        Assert.Throws<UnsolvableCubeException>(() => Solver.Solve(Cube.FromFaces(ReadOnly(faces))));
    }

    [Test]
    public void TwoSwappedEdges_AreUnsolvable()
    {
        var faces = Stickers(Cube.Solved());
        // Swap the U-F and U-R edges, keeping each one's U sticker on U.
        (faces[Face.F][1], faces[Face.R][1]) = (faces[Face.R][1], faces[Face.F][1]);

        Assert.Throws<UnsolvableCubeException>(() => Solver.Solve(Cube.FromFaces(ReadOnly(faces))));
    }

    [Test]
    public void StickersThatFormNoRealPiece_AreUnsolvable()
    {
        var faces = Stickers(Cube.Solved());
        // Swapping these makes a white/yellow edge and a green/green one, which no cube has.
        (faces[Face.F][1], faces[Face.D][1]) = (faces[Face.D][1], faces[Face.F][1]);

        var error = Assert.Throws<UnsolvableCubeException>(() => Solver.Solve(Cube.FromFaces(ReadOnly(faces))));
        Assert.That(error.Message, Does.Contain("No piece has the colours W/G"));
    }

    private static bool RowsSolved(Cube cube, Face side, int rows) =>
        Enumerable.Range(0, rows * 3).All(index => cube[side, index] == side.SolvedColour());

    private static Dictionary<Face, Colour[]> Stickers(Cube cube) =>
        Faces.All.ToDictionary(face => face, face => cube.GetFace(face).ToArray());

    private static Dictionary<Face, IReadOnlyList<Colour>> ReadOnly(Dictionary<Face, Colour[]> faces) =>
        faces.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Colour>)pair.Value);
}
