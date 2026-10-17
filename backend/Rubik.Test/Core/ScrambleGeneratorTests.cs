using NUnit.Framework;
using Rubik.Core.Notation;

namespace Rubik.Test.Core;

[TestFixture]
internal sealed class ScrambleGeneratorTests
{
    private readonly ScrambleGenerator _generator = new();

    [Test]
    public void Generate_DefaultsToTwentyMoves()
    {
        Assert.That(_generator.Generate().Count, Is.EqualTo(ScrambleGenerator.DefaultLength));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(25)]
    [TestCase(100)]
    public void Generate_ReturnsTheRequestedLength(int length)
    {
        Assert.That(_generator.Generate(length).Count, Is.EqualTo(length));
    }

    [Test]
    public void Generate_NeverTurnsTheSameFaceTwiceInARow()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var moves = _generator.Generate(200, seed);

            Assert.That(moves.Zip(moves.Skip(1)).Where(pair => pair.First.Face == pair.Second.Face), Is.Empty);
        }
    }

    [Test]
    public void Generate_IsDeterministicForASeed()
    {
        var first = _generator.Generate(30, 123);
        var again = _generator.Generate(30, 123);
        var other = _generator.Generate(30, 124);

        Assert.That(again, Is.EqualTo(first));
        Assert.That(other, Is.Not.EqualTo(first));
    }

    [Test]
    public void Generate_RejectsANegativeLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.Generate(-1));
    }
}
