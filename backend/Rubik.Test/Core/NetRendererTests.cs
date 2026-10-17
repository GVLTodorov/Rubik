using NUnit.Framework;
using Rubik.Core;
using Rubik.Core.Notation;
using Rubik.Core.Rendering;

namespace Rubik.Test.Core;

[TestFixture]
internal sealed class NetRendererTests
{
    private readonly NetRenderer _renderer = new();

    [Test]
    public void Render_SolvedCube_LaysOutTheCross()
    {
        const string expected =
            "        W W W\n" +
            "        W W W\n" +
            "        W W W\n" +
            "\n" +
            "O O O   G G G   R R R   B B B\n" +
            "O O O   G G G   R R R   B B B\n" +
            "O O O   G G G   R R R   B B B\n" +
            "\n" +
            "        Y Y Y\n" +
            "        Y Y Y\n" +
            "        Y Y Y";

        Assert.That(_renderer.Render(Cube.Solved()), Is.EqualTo(expected));
    }

    [Test]
    public void Render_AcceptanceSequence_MatchesTheBrief()
    {
        var cube = Cube.Solved().Apply(new MoveParser().Parse(CubeTestData.AcceptanceSequence));

        Assert.That(_renderer.Render(cube), Is.EqualTo(CubeTestData.AcceptanceNet));
    }

    [Test]
    public void Render_UsesTheStickerFormatter()
    {
        var net = _renderer.Render(Cube.Solved(), colour => colour == Colour.White ? "#" : ".");

        Assert.That(net, Does.StartWith("        # # #\n"));
        Assert.That(net, Does.Contain(". . .   . . ."));
    }
}
