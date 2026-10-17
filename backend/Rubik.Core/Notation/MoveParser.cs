namespace Rubik.Core.Notation;

/// <summary>Turns move notation such as <c>F R' U2</c> into <see cref="Move"/> objects.</summary>
public interface IMoveParser
{
    /// <summary>
    /// Parses a whitespace-separated move sequence. An empty or blank string is an empty sequence.
    /// </summary>
    /// <exception cref="MoveParseException">A token is not a valid move.</exception>
    IReadOnlyList<Move> Parse(string notation);
}

/// <summary>
/// Case-sensitive parser for face-turn notation: a face letter (<c>F R U B L D</c>) optionally
/// followed by <c>'</c> (anti-clockwise) or <c>2</c> (half turn).
/// </summary>
public sealed class MoveParser : IMoveParser
{
    private const char PrimeSuffix = '\'';
    private const char HalfTurnSuffix = '2';

    /// <inheritdoc />
    public IReadOnlyList<Move> Parse(string notation)
    {
        ArgumentNullException.ThrowIfNull(notation);

        var moves = new List<Move>();
        var position = 0;
        while (position < notation.Length)
        {
            if (char.IsWhiteSpace(notation[position]))
            {
                position++;
                continue;
            }

            var start = position;
            while (position < notation.Length && !char.IsWhiteSpace(notation[position]))
            {
                position++;
            }

            var token = notation[start..position];
            moves.Add(ParseToken(token, start, moves.Count + 1));
        }

        return moves;
    }

    private static Move ParseToken(string token, int position, int tokenNumber)
    {
        if (token.Length is < 1 or > 2 || !Faces.TryParse(token[0], out var face))
        {
            throw new MoveParseException(token, position, tokenNumber);
        }

        if (token.Length == 1)
        {
            return new Move(face, Turn.Clockwise);
        }

        return token[1] switch
        {
            PrimeSuffix => new Move(face, Turn.AntiClockwise),
            HalfTurnSuffix => new Move(face, Turn.Half),
            _ => throw new MoveParseException(token, position, tokenNumber),
        };
    }
}
