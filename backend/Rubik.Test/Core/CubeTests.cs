using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using Rubik.Core;
using Rubik.Core.Notation;

namespace Rubik.Test.Core;

[TestFixture]
internal sealed class CubeTests
{
    private static readonly MoveParser Parser = new();

    private static Cube Apply(string notation, Cube? start = null) =>
        (start ?? Cube.Solved()).Apply(Parser.Parse(notation));

    [Test]
    public void Solved_HasUniformFacesInTheBriefsOrientation()
    {
        var cube = Cube.Solved();

        Assert.That(cube.IsSolved, Is.True);
        Assert.That(cube.Codes(Face.U), Is.EqualTo("W W W W W W W W W"));
        Assert.That(cube.Codes(Face.F), Is.EqualTo("G G G G G G G G G"));
        Assert.That(cube.Codes(Face.R), Is.EqualTo("R R R R R R R R R"));
        Assert.That(cube.Codes(Face.B), Is.EqualTo("B B B B B B B B B"));
        Assert.That(cube.Codes(Face.L), Is.EqualTo("O O O O O O O O O"));
        Assert.That(cube.Codes(Face.D), Is.EqualTo("Y Y Y Y Y Y Y Y Y"));
    }

    [Test]
    public void Solved_HasNineStickersOfEachColour()
    {
        AssertNineOfEachColour(Cube.Solved());
    }

    [Test]
    public void AcceptanceSequence_ProducesTheBriefsExpectedFaces()
    {
        var cube = Apply(CubeTestData.AcceptanceSequence);

        foreach (var (face, expected) in CubeTestData.AcceptanceFaces)
        {
            Assert.That(cube.Codes(face), Is.EqualTo(expected));
        }
    }

    [TestCaseSource(typeof(CubeTestData), nameof(CubeTestData.AllFaces))]
    public void FourQuarterTurns_AreIdentity(Face face)
    {
        var start = Apply("F R' U B' L D'");
        var move = new Move(face, Turn.Clockwise);

        Assert.That(start.Apply([move, move, move, move]), Is.EqualTo(start));
    }

    [TestCaseSource(typeof(CubeTestData), nameof(CubeTestData.AllFaces))]
    public void TurnThenInverse_IsIdentity(Face face)
    {
        var start = Apply("R U2 F' L D B2");

        Assert.That(start.Apply([new Move(face, Turn.Clockwise), new Move(face, Turn.AntiClockwise)]), Is.EqualTo(start));
        Assert.That(start.Apply([new Move(face, Turn.AntiClockwise), new Move(face, Turn.Clockwise)]), Is.EqualTo(start));
        Assert.That(start.Apply([new Move(face, Turn.Half), new Move(face, Turn.Half)]), Is.EqualTo(start));
    }

    [TestCaseSource(typeof(CubeTestData), nameof(CubeTestData.AllFaces))]
    public void HalfTurn_EqualsTwoQuarterTurns(Face face)
    {
        var start = Apply("U F2 R' D L B'");
        var quarter = new Move(face, Turn.Clockwise);

        Assert.That(start.Apply(new Move(face, Turn.Half)), Is.EqualTo(start.Apply([quarter, quarter])));
    }

    [TestCaseSource(typeof(CubeTestData), nameof(CubeTestData.AllFaces))]
    public void AntiClockwise_EqualsThreeQuarterTurns(Face face)
    {
        var start = Apply("D' B R2 U' F L2");
        var quarter = new Move(face, Turn.Clockwise);

        Assert.That(start.Apply(new Move(face, Turn.AntiClockwise)), Is.EqualTo(start.Apply([quarter, quarter, quarter])));
    }

    [TestCaseSource(typeof(CubeTestData), nameof(CubeTestData.AllFaces))]
    public void ClockwiseTurn_MatchesARealRotationInSpace(Face face)
    {
        // Checks the FaceTurn table against an independent 3D model, from a scrambled start so
        // every sticker is distinguishable by position.
        var start = Apply("R U2 F' L D B2 U' R2 F");

        Assert.That(start.Apply(new Move(face, Turn.Clockwise)), Is.EqualTo(GeometricCube.TurnClockwise(start, face)));
    }

    [TestCaseSource(nameof(AllMoves))]
    public void EveryMove_PreservesColourCountsAndCentres(Move move)
    {
        var start = Apply("F R' U B' L D'");
        var cube = start.Apply(move);

        AssertNineOfEachColour(cube);
        foreach (var face in Faces.All)
        {
            Assert.That(cube[face, Cube.CentreIndex], Is.EqualTo(start[face, Cube.CentreIndex]));
        }
    }

    [TestCase("F", Face.U, new[] { 6, 7, 8 }, "O O O")]
    [TestCase("F", Face.R, new[] { 0, 3, 6 }, "W W W")]
    [TestCase("F", Face.D, new[] { 0, 1, 2 }, "R R R")]
    [TestCase("F", Face.L, new[] { 2, 5, 8 }, "Y Y Y")]
    [TestCase("R", Face.U, new[] { 2, 5, 8 }, "G G G")]
    [TestCase("R", Face.B, new[] { 0, 3, 6 }, "W W W")]
    [TestCase("R", Face.D, new[] { 2, 5, 8 }, "B B B")]
    [TestCase("R", Face.F, new[] { 2, 5, 8 }, "Y Y Y")]
    [TestCase("U", Face.F, new[] { 0, 1, 2 }, "R R R")]
    [TestCase("U", Face.L, new[] { 0, 1, 2 }, "G G G")]
    [TestCase("U", Face.B, new[] { 0, 1, 2 }, "O O O")]
    [TestCase("U", Face.R, new[] { 0, 1, 2 }, "B B B")]
    [TestCase("B", Face.U, new[] { 0, 1, 2 }, "R R R")]
    [TestCase("B", Face.L, new[] { 0, 3, 6 }, "W W W")]
    [TestCase("B", Face.D, new[] { 6, 7, 8 }, "O O O")]
    [TestCase("B", Face.R, new[] { 2, 5, 8 }, "Y Y Y")]
    [TestCase("L", Face.F, new[] { 0, 3, 6 }, "W W W")]
    [TestCase("L", Face.D, new[] { 0, 3, 6 }, "G G G")]
    [TestCase("L", Face.B, new[] { 2, 5, 8 }, "Y Y Y")]
    [TestCase("L", Face.U, new[] { 0, 3, 6 }, "B B B")]
    [TestCase("D", Face.R, new[] { 6, 7, 8 }, "G G G")]
    [TestCase("D", Face.B, new[] { 6, 7, 8 }, "R R R")]
    [TestCase("D", Face.L, new[] { 6, 7, 8 }, "B B B")]
    [TestCase("D", Face.F, new[] { 6, 7, 8 }, "O O O")]
    public void SingleMove_FromSolved_MovesTheExpectedStrips(string move, Face face, int[] indices, string expected)
    {
        Assert.That(Apply(move).Codes(face, indices), Is.EqualTo(expected));
    }

    [Test]
    public void SingleMove_TurnsOnlyTheFaceAndItsNeighbours()
    {
        var cube = Apply("F");

        Assert.That(cube.Codes(Face.F), Is.EqualTo("G G G G G G G G G"));
        Assert.That(cube.Codes(Face.B), Is.EqualTo("B B B B B B B B B"));
        Assert.That(cube.Codes(Face.U, 0, 1, 2, 3, 4, 5), Is.EqualTo("W W W W W W"));
    }

    [Test]
    public void SexyMoveSixTimes_IsIdentity()
    {
        var start = Apply("F R' U B' L D'");

        Assert.That(Apply(string.Concat(Enumerable.Repeat("R U R' U' ", 6)), start), Is.EqualTo(start));
    }

    [Test]
    public void Apply_DoesNotChangeTheOriginalCube()
    {
        var solved = Cube.Solved();

        _ = solved.Apply(new Move(Face.F, Turn.Clockwise));

        Assert.That(solved.IsSolved, Is.True);
    }

    [Test]
    public void FromFaces_RoundTripsAState()
    {
        var cube = Apply(CubeTestData.AcceptanceSequence);
        var faces = Faces.All.ToDictionary(face => face, cube.GetFace);

        Assert.That(Cube.FromFaces(faces), Is.EqualTo(cube));
    }

    [Test]
    public void FromFaces_RejectsAMissingFace()
    {
        var faces = SolvedFaces();
        faces.Remove(Face.D);

        var error = Assert.Throws<InvalidCubeStateException>(() => Cube.FromFaces(faces));
        Assert.That(error.Message, Does.Contain("'D'"));
    }

    [Test]
    public void FromFaces_RejectsTheWrongNumberOfStickers()
    {
        var faces = SolvedFaces();
        faces[Face.F] = [.. faces[Face.F].Take(8)];

        var error = Assert.Throws<InvalidCubeStateException>(() => Cube.FromFaces(faces));
        Assert.That(error.Message, Does.Contain("8 stickers"));
    }

    [Test]
    public void FromFaces_RejectsWrongColourCounts()
    {
        var faces = SolvedFaces();
        faces[Face.F] = [Colour.White, .. faces[Face.F].Skip(1)];

        var error = Assert.Throws<InvalidCubeStateException>(() => Cube.FromFaces(faces));
        Assert.That(error.Message, Does.Contain("appears 10 times"));
    }

    [TestCase(-1)]
    [TestCase(9)]
    public void Indexer_RejectsOutOfRangeIndices(int index)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Cube.Solved()[Face.F, index]);
    }

    [Test]
    public void Equality_ComparesStickersNotReferences()
    {
        var first = Apply("R U");
        var second = Apply("R U");
        var different = Apply("U R");

        Assert.That(first.Equals((object)second), Is.True);
        Assert.That(second.GetHashCode(), Is.EqualTo(first.GetHashCode()));
        Assert.That(first.Equals((object)different), Is.False);
        Assert.That(first.Equals((object)"R U"), Is.False);
        Assert.That(first.Equals(null), Is.False);
    }

    [Test]
    public void ToString_ListsEachFaceAsColourCodes()
    {
        var lines = Cube.Solved().ToString().Split(Environment.NewLine);

        Assert.That(lines.Length, Is.EqualTo(6));
        Assert.That(lines[0], Is.EqualTo("U: W W W W W W W W W"));
        Assert.That(lines[5], Is.EqualTo("D: Y Y Y Y Y Y Y Y Y"));
    }

    [Test]
    public void MoveInverse_UndoesTheMove()
    {
        Assert.That(new Move(Face.F, Turn.Clockwise).Inverse().ToString(), Is.EqualTo("F'"));
        Assert.That(new Move(Face.F, Turn.AntiClockwise).Inverse().ToString(), Is.EqualTo("F"));
        Assert.That(new Move(Face.F, Turn.Half).Inverse().ToString(), Is.EqualTo("F2"));
    }

    [Test]
    public void DebuggerView_ShowsEachFaceAndTheNet()
    {
        var cube = Cube.Solved().Apply(new MoveParser().Parse(CubeTestData.AcceptanceSequence));
        var proxyType = typeof(Cube).GetCustomAttribute<DebuggerTypeProxyAttribute>()!.ProxyTypeName;
        var view = Activator.CreateInstance(Type.GetType(proxyType)!, cube)!;

        string Read(string property) => (string)view.GetType().GetProperty(property)!.GetValue(view)!;

        foreach (var face in Faces.All)
        {
            Assert.That(Read(face.ToString()), Is.EqualTo(CubeTestData.AcceptanceFaces[face]));
        }

        Assert.That(Read("Net"), Is.EqualTo(new Rubik.Core.Rendering.NetRenderer().Render(cube)));
    }

    private static IEnumerable<Move> AllMoves() =>
        Faces.All.SelectMany(face => Enum.GetValues<Turn>().Select(turn => new Move(face, turn)));

    private static Dictionary<Face, IReadOnlyList<Colour>> SolvedFaces() =>
        Faces.All.ToDictionary(face => face, Cube.Solved().GetFace);

    private static void AssertNineOfEachColour(Cube cube)
    {
        var counts = Faces.All.SelectMany(cube.GetFace).GroupBy(colour => colour).ToDictionary(g => g.Key, g => g.Count());

        Assert.That(counts.Count, Is.EqualTo(ColourCodes.All.Count));
        Assert.That(counts.Values, Is.All.EqualTo(Cube.StickersPerFace));
    }
}
