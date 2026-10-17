namespace Rubik.Core;

/// <summary>How far a face is turned, as seen when looking straight at that face.</summary>
public enum Turn
{
    /// <summary>90° clockwise, written <c>F</c>.</summary>
    Clockwise = 1,

    /// <summary>180°, written <c>F2</c>.</summary>
    Half = 2,

    /// <summary>90° anti-clockwise, written <c>F'</c>.</summary>
    AntiClockwise = 3,
}

/// <summary>A single face turn such as <c>F</c>, <c>R'</c> or <c>U2</c>.</summary>
/// <param name="Face">The face being turned.</param>
/// <param name="Turn">The direction and amount of the turn.</param>
public readonly record struct Move(Face Face, Turn Turn)
{
    /// <summary>The number of clockwise quarter turns this move is equivalent to (1, 2 or 3).</summary>
    public int QuarterTurns => (int)Turn;

    /// <summary>The move that undoes this one (<c>F</c> ↔ <c>F'</c>; <c>F2</c> is its own inverse).</summary>
    public Move Inverse() => this with { Turn = (Turn)(4 - QuarterTurns) };

    /// <summary>Standard notation, e.g. <c>F</c>, <c>R'</c> or <c>U2</c>.</summary>
    public override string ToString() => Turn switch
    {
        Turn.Clockwise => $"{Face.ToLetter()}",
        Turn.AntiClockwise => $"{Face.ToLetter()}'",
        Turn.Half => $"{Face.ToLetter()}2",
        _ => throw new InvalidOperationException($"Unknown turn {Turn}."),
    };

    /// <summary>Formats a sequence of moves as space-separated notation.</summary>
    public static string Format(IEnumerable<Move> moves) => string.Join(' ', moves);
}
