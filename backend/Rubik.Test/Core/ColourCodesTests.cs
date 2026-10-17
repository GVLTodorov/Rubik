using NUnit.Framework;
using Rubik.Core;

namespace Rubik.Test.Core;

[TestFixture]
internal sealed class ColourCodesTests
{
    [TestCase("W", Colour.White)]
    [TestCase("O", Colour.Orange)]
    [TestCase("G", Colour.Green)]
    [TestCase("R", Colour.Red)]
    [TestCase("B", Colour.Blue)]
    [TestCase("Y", Colour.Yellow)]
    public void ToCode_GivesEachColoursLetter(string code, Colour colour)
    {
        Assert.That(colour.ToCode(), Is.EqualTo(code[0]));
    }

    [TestCaseSource(typeof(CubeTestData), nameof(CubeTestData.AllFaces))]
    public void SolvedColour_MatchesTheSolvedCubesCentre(Face face)
    {
        Assert.That(face.SolvedColour(), Is.EqualTo(Cube.Solved()[face, 4]));
    }
}
