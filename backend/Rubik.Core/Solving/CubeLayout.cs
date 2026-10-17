namespace Rubik.Core.Solving;

/// <summary>
/// The cube's 12 edge and 8 corner slots, each listed as the sticker numbers that sit on it.
/// A sticker number is <c>face * 9 + index</c> with faces in U L F R B D order (see <see cref="StickerState"/>):
/// <code>
/// U = 0..8   L = 9..17   F = 18..26   R = 27..35   B = 36..44   D = 45..53
/// </code>
/// So <c>[7, 19]</c> is U's sticker 7 and F's sticker 1: the up-front edge.
/// </summary>
internal static class CubeLayout
{
    /// <summary>The 12 edge slots (2 stickers each).</summary>
    public static readonly int[][] Edges =
    [
        [1, 37],  // U1 B1  up-back
        [3, 10],  // U3 L1  up-left
        [5, 28],  // U5 R1  up-right
        [7, 19],  // U7 F1  up-front
        [12, 41], // L3 B5  left-back
        [14, 21], // L5 F3  left-front
        [16, 48], // L7 D3  left-down
        [23, 30], // F5 R3  front-right
        [25, 46], // F7 D1  front-down
        [32, 39], // R5 B3  right-back
        [34, 50], // R7 D5  right-down
        [43, 52], // B7 D7  back-down
    ];

    /// <summary>The 8 corner slots (3 stickers each).</summary>
    public static readonly int[][] Corners =
    [
        [0, 9, 38],   // U0 L0 B2  up-left-back
        [2, 29, 36],  // U2 R2 B0  up-right-back
        [6, 11, 18],  // U6 L2 F0  up-left-front
        [8, 20, 27],  // U8 F2 R0  up-front-right
        [15, 44, 51], // L6 B8 D6  down-left-back
        [17, 24, 45], // L8 F6 D0  down-left-front
        [26, 33, 47], // F8 R6 D2  down-front-right
        [35, 42, 53], // R8 B6 D8  down-right-back
    ];

    /// <summary>Every slot: the 12 edges, then the 8 corners.</summary>
    public static readonly int[][] Slots = [.. Edges, .. Corners];

    /// <summary>The slot where exactly these faces meet, e.g. (U, F) is the up-front edge.</summary>
    public static int[] Slot(params Face[] faces)
    {
        foreach (var slot in Slots)
        {
            if (slot.Length != faces.Length)
            {
                continue;
            }

            var allOnTheseFaces = true;
            foreach (var sticker in slot)
            {
                if (Array.IndexOf(faces, StickerState.FaceOf(sticker)) < 0)
                {
                    allOnTheseFaces = false;
                    break;
                }
            }

            if (allOnTheseFaces)
            {
                return slot;
            }
        }

        throw new ArgumentException($"No slot joins the faces {string.Join(", ", faces)}.", nameof(faces));
    }

    /// <summary>True when the slot holds its own piece, turned the right way.</summary>
    public static bool IsSolved(byte[] state, int[] slot)
    {
        foreach (var sticker in slot)
        {
            if (state[sticker] != StickerState.Centre(state, StickerState.FaceOf(sticker)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when every one of <paramref name="slots"/> is solved.</summary>
    public static bool AllSolved(byte[] state, IReadOnlyList<int[]> slots)
    {
        foreach (var slot in slots)
        {
            if (!IsSolved(state, slot))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The colours of the piece that belongs in <paramref name="slot"/>: its faces' centres.</summary>
    public static byte[] HomeColours(byte[] state, int[] slot)
    {
        var colours = new byte[slot.Length];
        for (var i = 0; i < slot.Length; i++)
        {
            colours[i] = StickerState.Centre(state, StickerState.FaceOf(slot[i]));
        }

        return colours;
    }

    /// <summary>The slot currently holding the piece with these colours (in any order).</summary>
    /// <exception cref="UnsolvableCubeException">No piece has these colours.</exception>
    public static int[] Locate(byte[] state, byte[] colours)
    {
        foreach (var slot in Slots)
        {
            if (slot.Length == colours.Length && HasExactlyTheseColours(state, slot, colours))
            {
                return slot;
            }
        }

        var codes = new List<char>();
        foreach (var colour in colours)
        {
            codes.Add(((Colour)colour).ToCode());
        }

        throw new UnsolvableCubeException(
            $"No piece has the colours {string.Join('/', codes)}, so the stickers do not form a real cube.");
    }

    /// <summary>True when <paramref name="slot"/> has a sticker on <paramref name="face"/>.</summary>
    public static bool Touches(int[] slot, Face face)
    {
        foreach (var sticker in slot)
        {
            if (StickerState.FaceOf(sticker) == face)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The slots that have a sticker on <paramref name="face"/>.</summary>
    public static List<int[]> SlotsTouching(int[][] slots, Face face)
    {
        var result = new List<int[]>();
        foreach (var slot in slots)
        {
            if (Touches(slot, face))
            {
                result.Add(slot);
            }
        }

        return result;
    }

    /// <summary>The slots that have no sticker on <paramref name="face"/>.</summary>
    public static List<int[]> SlotsNotTouching(int[][] slots, Face face)
    {
        var result = new List<int[]>();
        foreach (var slot in slots)
        {
            if (!Touches(slot, face))
            {
                result.Add(slot);
            }
        }

        return result;
    }

    /// <summary>True when the slot's stickers are exactly <paramref name="colours"/>, in any order.</summary>
    private static bool HasExactlyTheseColours(byte[] state, int[] slot, byte[] colours)
    {
        // Every sticker's colour is one of the wanted colours...
        foreach (var sticker in slot)
        {
            if (Array.IndexOf(colours, state[sticker]) < 0)
            {
                return false;
            }
        }

        // ...and every wanted colour is on one of the stickers.
        foreach (var colour in colours)
        {
            var found = false;
            foreach (var sticker in slot)
            {
                if (state[sticker] == colour)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }
}
