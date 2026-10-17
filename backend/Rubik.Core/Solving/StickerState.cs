namespace Rubik.Core.Solving;

/// <summary>
/// A fast, mutable view of a cube for searching: 54 colour bytes in <see cref="Cube"/>'s layout
/// (face-major in U L F R B D order, then row-major within a face). Moves are applied through
/// permutation tables built by turning an array of indices with <see cref="FaceTurn"/>, the same turn
/// algorithm <see cref="Cube"/> uses, so the two can never disagree.
/// </summary>
internal static class StickerState
{
    public const int Count = Faces.Count * Cube.StickersPerFace;

    private const int TurnsPerFace = 3;

    /// <summary><c>Sources[move][i]</c> is the sticker that lands on <c>i</c> when <c>move</c> is turned.</summary>
    private static readonly int[][] Sources = BuildSources();

    public static byte[] From(Cube cube)
    {
        var state = new byte[Count];
        foreach (var face in Faces.All)
        {
            var colours = cube.GetFace(face);
            for (var i = 0; i < Cube.StickersPerFace; i++)
            {
                state[Offset(face) + i] = (byte)colours[i];
            }
        }

        return state;
    }

    /// <summary>Turns <paramref name="move"/> on <paramref name="state"/> in place.</summary>
    public static void Apply(byte[] state, Move move, byte[] scratch)
    {
        var source = Sources[((int)move.Face * TurnsPerFace) + move.QuarterTurns - 1];
        Array.Copy(state, scratch, Count);
        for (var i = 0; i < Count; i++)
        {
            state[i] = scratch[source[i]];
        }
    }

    public static Face FaceOf(int sticker) => (Face)(sticker / Cube.StickersPerFace);

    /// <summary>The colour of a face's centre, which no face turn moves.</summary>
    public static byte Centre(byte[] state, Face face) => state[Offset(face) + Cube.CentreIndex];

    private static int Offset(Face face) => (int)face * Cube.StickersPerFace;

    private static int[][] BuildSources()
    {
        var sources = new int[Faces.Count * TurnsPerFace][];
        foreach (var face in Faces.All)
        {
            // Number every sticker 0–53, turn the numbers, and read off where each one came from.
            var indices = new int[Faces.Count][];
            for (var f = 0; f < Faces.Count; f++)
            {
                indices[f] = new int[Cube.StickersPerFace];
                for (var i = 0; i < Cube.StickersPerFace; i++)
                {
                    indices[f][i] = (f * Cube.StickersPerFace) + i;
                }
            }

            for (var turns = 1; turns <= TurnsPerFace; turns++)
            {
                FaceTurn.Clockwise(indices, face);

                var source = new int[Count];
                for (var f = 0; f < Faces.Count; f++)
                {
                    Array.Copy(indices[f], 0, source, f * Cube.StickersPerFace, Cube.StickersPerFace);
                }

                sources[((int)face * TurnsPerFace) + turns - 1] = source;
            }
        }

        return sources;
    }
}
