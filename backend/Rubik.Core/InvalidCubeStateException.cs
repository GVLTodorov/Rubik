namespace Rubik.Core;

/// <summary>Thrown when a cube state is malformed (missing faces, wrong sticker counts, unknown colours).</summary>
public sealed class InvalidCubeStateException : Exception
{
    /// <summary>Creates the exception with a message describing what is wrong with the state.</summary>
    public InvalidCubeStateException(string message)
        : base(message)
    {
    }
}
