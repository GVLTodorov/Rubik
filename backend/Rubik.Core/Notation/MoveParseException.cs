namespace Rubik.Core.Notation;

/// <summary>Thrown when move notation contains a token that is not a valid move.</summary>
public sealed class MoveParseException : Exception
{
    /// <summary>Creates the exception for an invalid token.</summary>
    /// <param name="token">The offending token, exactly as written.</param>
    /// <param name="position">Zero-based character offset of the token in the input.</param>
    /// <param name="tokenNumber">One-based index of the token in the sequence.</param>
    public MoveParseException(string token, int position, int tokenNumber)
        : base(
            $"Invalid move '{token}' at position {position} (move #{tokenNumber}). " +
            "Expected one of F R U B L D, optionally followed by ' (anti-clockwise) or 2 (half turn).")
    {
        Token = token;
        Position = position;
        TokenNumber = tokenNumber;
    }

    /// <summary>The offending token, exactly as written.</summary>
    public string Token { get; }

    /// <summary>Zero-based character offset of the token in the input.</summary>
    public int Position { get; }

    /// <summary>One-based index of the token in the sequence.</summary>
    public int TokenNumber { get; }
}
