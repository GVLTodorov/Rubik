namespace Rubik.Core;

/// <summary>
/// The whole move mechanism: what one clockwise quarter turn of a face does to the stickers.
/// <para>
/// A sticker is addressed as (face, index). The index is row-major, as seen when looking straight
/// at that face from outside:
/// <code>
/// 0 1 2
/// 3 4 5
/// 6 7 8
/// </code>
/// </para>
/// <para>
/// A clockwise turn is just five 4-cycles. A cycle <c>A → B → C → D</c> means the sticker at A moves
/// to B, B moves to C, C moves to D and D moves back to A:
/// </para>
/// <list type="number">
/// <item>The turned face's corners: <c>0 → 2 → 8 → 6</c>.</item>
/// <item>The turned face's edges: <c>1 → 5 → 7 → 3</c>. The centre (4) never moves.</item>
/// <item>Three cycles through the <see cref="Rings">ring</see>: the 12 stickers on the four neighbouring
/// faces that touch the turned face. The k-th sticker of each strip cycles to the k-th sticker of
/// the next strip.</item>
/// </list>
/// Anti-clockwise is three clockwise turns and a half turn is two (see <see cref="Cube.Apply(Move)"/>).
/// </summary>
internal static class FaceTurn
{
    /// <summary>
    /// For each face, the four strips of 3 neighbouring stickers that a clockwise turn carries round:
    /// strip 0 → strip 1 → strip 2 → strip 3 → strip 0. Stickers within a strip are listed so that
    /// position k of one strip lands on position k of the next.
    /// </summary>
    /// <example>
    /// F: U's bottom row (6 7 8) moves onto R's left column (0 3 6), which moves onto D's top row read
    /// right to left (2 1 0), which moves onto L's right column read bottom to top (8 5 2), which moves
    /// back onto U's bottom row.
    /// </example>
    private static readonly Dictionary<Face, (Face Face, int[] Stickers)[]> Rings = new()
    {
        //          strip 0              strip 1              strip 2              strip 3
        [Face.U] = [(Face.F, [0, 1, 2]), (Face.L, [0, 1, 2]), (Face.B, [0, 1, 2]), (Face.R, [0, 1, 2])],
        [Face.L] = [(Face.U, [0, 3, 6]), (Face.F, [0, 3, 6]), (Face.D, [0, 3, 6]), (Face.B, [8, 5, 2])],
        [Face.F] = [(Face.U, [6, 7, 8]), (Face.R, [0, 3, 6]), (Face.D, [2, 1, 0]), (Face.L, [8, 5, 2])],
        [Face.R] = [(Face.F, [2, 5, 8]), (Face.U, [2, 5, 8]), (Face.B, [6, 3, 0]), (Face.D, [2, 5, 8])],
        [Face.B] = [(Face.U, [0, 1, 2]), (Face.L, [6, 3, 0]), (Face.D, [8, 7, 6]), (Face.R, [2, 5, 8])],
        [Face.D] = [(Face.F, [6, 7, 8]), (Face.R, [6, 7, 8]), (Face.B, [6, 7, 8]), (Face.L, [6, 7, 8])],
    };

    /// <summary>
    /// Turns <paramref name="face"/> a clockwise quarter turn, in place. <paramref name="faces"/> holds
    /// one 9-element array per face, indexed by <see cref="Face"/>. Generic so the solver can turn an
    /// array of sticker numbers and read off where each one went.
    /// </summary>
    public static void Clockwise<T>(T[][] faces, Face face)
    {
        // Step 1 and 2: the turned face spins on its own.
        T[] own = faces[(int)face];
        Cycle(own, 0, own, 2, own, 8, own, 6); // corners
        Cycle(own, 1, own, 5, own, 7, own, 3); // edges

        // Step 3: the ring of 12 neighbouring stickers shifts one strip along.
        (Face Face, int[] Stickers)[] ring = Rings[face];
        T[] face0 = faces[(int)ring[0].Face];
        T[] face1 = faces[(int)ring[1].Face];
        T[] face2 = faces[(int)ring[2].Face];
        T[] face3 = faces[(int)ring[3].Face];

        for (int k = 0; k < 3; k++)
        {
            Cycle(
                face0, ring[0].Stickers[k],
                face1, ring[1].Stickers[k],
                face2, ring[2].Stickers[k],
                face3, ring[3].Stickers[k]);
        }
    }

    /// <summary>
    /// Moves the sticker at a[ia] to b[ib], b[ib] to c[ic], c[ic] to d[id], and d[id] back to a[ia].
    /// </summary>
    private static void Cycle<T>(T[] a, int ia, T[] b, int ib, T[] c, int ic, T[] d, int id)
    {
        T temp = d[id]; // d is about to be overwritten, so keep it
        d[id] = c[ic];  // c → d
        c[ic] = b[ib];  // b → c
        b[ib] = a[ia];  // a → b
        a[ia] = temp;   // d → a
    }
}
