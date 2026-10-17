using NUnit.Framework;
using Rubik.Core;
using Rubik.Core.Notation;

namespace Rubik.Test.Core;

[TestFixture]
internal sealed class MoveParserTests
{
    private readonly MoveParser _parser = new();

    [Test]
    public void Parse_ReadsAllEighteenFaceTurns()
    {
        var moves = _parser.Parse("F R U B L D F' R' U' B' L' D' F2 R2 U2 B2 L2 D2");

        Assert.That(moves.Count, Is.EqualTo(18));
        Assert.That(moves[0], Is.EqualTo(new Move(Face.F, Turn.Clockwise)));
        Assert.That(moves[11], Is.EqualTo(new Move(Face.D, Turn.AntiClockwise)));
        Assert.That(moves[14], Is.EqualTo(new Move(Face.U, Turn.Half)));
    }

    [Test]
    public void Parse_RoundTripsThroughFormat()
    {
        const string notation = "F R' U B' L D' F2";

        Assert.That(Move.Format(_parser.Parse(notation)), Is.EqualTo(notation));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\n")]
    public void Parse_TreatsBlankInputAsNoMoves(string notation)
    {
        Assert.That(_parser.Parse(notation), Is.Empty);
    }

    [Test]
    public void Parse_IgnoresExtraWhitespace()
    {
        var moves = _parser.Parse("  F \t R'\n\nU2  ");

        Assert.That(Move.Format(moves), Is.EqualTo("F R' U2"));
    }

    [TestCase("F X U", "X", 2, 2)]
    [TestCase("f", "f", 0, 1)]
    [TestCase("F R'' U", "R''", 2, 2)]
    [TestCase("F R3", "R3", 2, 2)]
    [TestCase("FR", "FR", 0, 1)]
    [TestCase("F R U M", "M", 6, 4)]
    [TestCase("F2'", "F2'", 0, 1)]
    public void Parse_RejectsInvalidTokensWithTokenAndPosition(string notation, string token, int position, int tokenNumber)
    {
        var error = Assert.Throws<MoveParseException>(() => _parser.Parse(notation));

        Assert.That(error.Token, Is.EqualTo(token));
        Assert.That(error.Position, Is.EqualTo(position));
        Assert.That(error.TokenNumber, Is.EqualTo(tokenNumber));
        Assert.That(error.Message, Does.Contain($"'{token}'"));
        Assert.That(error.Message, Does.Contain($"position {position}"));
    }
}
