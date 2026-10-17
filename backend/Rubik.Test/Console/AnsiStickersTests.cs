using NUnit.Framework;
using Rubik.Console;
using Rubik.Core;

namespace Rubik.Test.Console;

[TestFixture]
internal sealed class AnsiStickersTests
{
    [TestCase(true, false, null)]
    [TestCase(false, true, null)]
    [TestCase(false, false, "1")]
    public void Choose_UsesPlainLettersWhenColoursAreOff(bool forcePlain, bool outputRedirected, string? noColor)
    {
        Assert.That(AnsiStickers.Choose(forcePlain, outputRedirected, noColor), Is.Null);
    }

    [Test]
    public void Choose_WithForcePlain_IsAlwaysPlain()
    {
        Assert.That(AnsiStickers.Choose(forcePlain: true), Is.Null);
    }

    [TestCase(Colour.White, "30;48;2;255;255;255")]
    [TestCase(Colour.Orange, "30;48;2;255;140;0")]
    [TestCase(Colour.Green, "30;48;2;0;192;64")]
    [TestCase(Colour.Red, "97;48;2;224;0;27")]
    [TestCase(Colour.Blue, "97;48;2;0;80;200")]
    [TestCase(Colour.Yellow, "30;48;2;255;224;0")]
    public void Format_WrapsTheLetterInItsBackgroundColour(Colour colour, string style)
    {
        var format = AnsiStickers.Choose(forcePlain: false, outputRedirected: false, noColor: "");

        Assert.That(format, Is.Not.Null);
        Assert.That(format(colour), Is.EqualTo($"\u001b[{style}m{colour.ToCode()}\u001b[0m"));
    }
}
