namespace Rubik.Core.Notation;

/// <summary>Produces random move sequences.</summary>
public interface IScrambleGenerator
{
    /// <summary>
    /// Generates <paramref name="length"/> random moves that never turn the same face twice in a row.
    /// The same <paramref name="seed"/> always produces the same sequence.
    /// </summary>
    IReadOnlyList<Move> Generate(int length = ScrambleGenerator.DefaultLength, int? seed = null);
}

/// <summary>Random scrambles over the 18 face turns (quarter and half turns of all six faces).</summary>
public sealed class ScrambleGenerator : IScrambleGenerator
{
    /// <summary>The scramble length used when none is given.</summary>
    public const int DefaultLength = 20;

    private static readonly Turn[] Turns = [Turn.Clockwise, Turn.AntiClockwise, Turn.Half];

    /// <inheritdoc />
    public IReadOnlyList<Move> Generate(int length = DefaultLength, int? seed = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        var random = seed is { } value ? new Random(value) : Random.Shared;
        var moves = new List<Move>(length);
        Face? previous = null;
        while (moves.Count < length)
        {
            var face = Faces.All[random.Next(Faces.Count)];
            if (face == previous)
            {
                continue;
            }

            moves.Add(new Move(face, Turns[random.Next(Turns.Length)]));
            previous = face;
        }

        return moves;
    }
}
