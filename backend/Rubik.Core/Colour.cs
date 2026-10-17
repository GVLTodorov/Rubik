namespace Rubik.Core;

/// <summary>A sticker colour.</summary>
public enum Colour
{
    /// <summary>White, code <c>W</c>.</summary>
    White,

    /// <summary>Orange, code <c>O</c>.</summary>
    Orange,

    /// <summary>Green, code <c>G</c>.</summary>
    Green,

    /// <summary>Red, code <c>R</c>.</summary>
    Red,

    /// <summary>Blue, code <c>B</c>.</summary>
    Blue,

    /// <summary>Yellow, code <c>Y</c>.</summary>
    Yellow,
}

/// <summary>Conversions between <see cref="Colour"/> and its single-letter code (W O G R B Y).</summary>
public static class ColourCodes
{
    /// <summary>All colours, in the order W O G R B Y.</summary>
    public static IReadOnlyList<Colour> All { get; } =
        [Colour.White, Colour.Orange, Colour.Green, Colour.Red, Colour.Blue, Colour.Yellow];

    /// <summary>The single-letter code for a colour, e.g. <c>'W'</c>.</summary>
    public static char ToCode(this Colour colour) => colour switch
    {
        Colour.White => 'W',
        Colour.Orange => 'O',
        Colour.Green => 'G',
        Colour.Red => 'R',
        Colour.Blue => 'B',
        Colour.Yellow => 'Y',
        _ => throw new ArgumentOutOfRangeException(nameof(colour), colour, null),
    };
}
