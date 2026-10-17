namespace Rubik.Core;

/// <summary>
/// The six faces of the cube, in net order (U, then L F R B, then D). The numeric value is also the
/// face's slot in a <see cref="Cube"/>'s sticker array.
/// </summary>
public enum Face
{
    /// <summary>Up (white when solved).</summary>
    U = 0,

    /// <summary>Left (orange when solved).</summary>
    L = 1,

    /// <summary>Front (green when solved).</summary>
    F = 2,

    /// <summary>Right (red when solved).</summary>
    R = 3,

    /// <summary>Back (blue when solved).</summary>
    B = 4,

    /// <summary>Down (yellow when solved).</summary>
    D = 5,
}

/// <summary>Helpers for <see cref="Face"/>.</summary>
public static class Faces
{
    /// <summary>Number of faces on a cube.</summary>
    public const int Count = 6;

    /// <summary>All faces in net order: U, L, F, R, B, D.</summary>
    public static IReadOnlyList<Face> All { get; } = [Face.U, Face.L, Face.F, Face.R, Face.B, Face.D];

    /// <summary>The single-letter notation for a face, e.g. <c>'F'</c>.</summary>
    public static char ToLetter(this Face face) => face.ToString()[0];

    /// <summary>Parses a face letter (case-sensitive). Returns false for anything else.</summary>
    public static bool TryParse(char letter, out Face face)
    {
        foreach (var candidate in All)
        {
            if (candidate.ToLetter() == letter)
            {
                face = candidate;
                return true;
            }
        }

        face = default;
        return false;
    }

    /// <summary>The colour a face has on a solved cube (Green front, Red right, White up).</summary>
    public static Colour SolvedColour(this Face face) => face switch
    {
        Face.U => Colour.White,
        Face.L => Colour.Orange,
        Face.F => Colour.Green,
        Face.R => Colour.Red,
        Face.B => Colour.Blue,
        Face.D => Colour.Yellow,
        _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
    };
}
