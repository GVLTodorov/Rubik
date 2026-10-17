using Rubik.Core;

namespace Rubik.Console;

/// <summary>
/// Formats stickers as their letter on a coloured background when the terminal can show ANSI
/// colours, and as the plain letter otherwise (redirected output, <c>NO_COLOR</c>, or <c>--plain</c>).
/// </summary>
internal static class AnsiStickers
{
    private const string Escape = "\u001b[";
    private const string Reset = Escape + "0m";
    private const string DarkText = "30";
    private const string LightText = "97";
    private const string NoColorVariable = "NO_COLOR";

    /// <summary>Returns an ANSI formatter, or null (plain letters) when colours should not be used.</summary>
    public static Func<Colour, string>? Choose(bool forcePlain) =>
        Choose(forcePlain, System.Console.IsOutputRedirected, Environment.GetEnvironmentVariable(NoColorVariable));

    /// <summary>The same choice with the terminal and environment passed in, so it can be unit tested.</summary>
    internal static Func<Colour, string>? Choose(bool forcePlain, bool outputRedirected, string? noColor)
    {
        var supported = !forcePlain && !outputRedirected && string.IsNullOrEmpty(noColor);
        return supported ? Format : null;
    }

    private static string Format(Colour colour) => $"{Style(colour)}{colour.ToCode()}{Reset}";

    // 24-bit background with dark or light text, matching the UI palette.
    private static string Style(Colour colour) => colour switch
    {
        Colour.White => Background(DarkText, 255, 255, 255),
        Colour.Orange => Background(DarkText, 255, 140, 0),
        Colour.Green => Background(DarkText, 0, 192, 64),
        Colour.Red => Background(LightText, 224, 0, 27),
        Colour.Blue => Background(LightText, 0, 80, 200),
        Colour.Yellow => Background(DarkText, 255, 224, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(colour), colour, null),
    };

    private static string Background(string text, int red, int green, int blue) =>
        $"{Escape}{text};48;2;{red};{green};{blue}m";
}
