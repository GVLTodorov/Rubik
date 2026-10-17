namespace Rubik.Core.Solving;

/// <summary>
/// A search step: a single turn, or a whole algorithm. <paramref name="Key"/> is the face a single
/// turn moves (null for an algorithm); it lets the search skip redundant orders such as <c>R R'</c>.
/// </summary>
internal sealed record Macro(Move[] Moves, Face? Key)
{
    public static Macro Turn(Move move) => new([move], move.Face);

    public static Macro Algorithm(Move[] moves) => new(moves, null);
}

/// <summary>
/// Iterative-deepening search over macros: tries every list of 1 macro, then every list of 2, and so
/// on up to a depth limit, and returns the first list that takes the start state to one where the
/// goal holds. Each solver stage uses it with a goal like "this piece is solved and every piece
/// solved earlier still is".
/// </summary>
internal sealed class MacroSearch
{
    private readonly IReadOnlyList<Macro> _macros;
    private readonly Func<byte[], bool> _goal;

    /// <summary><c>_states[level]</c> is the state after the first <c>level</c> macros of <c>_path</c>.</summary>
    private readonly byte[][] _states;

    /// <summary>The macro being tried at each level.</summary>
    private readonly Macro[] _path;

    private readonly byte[] _scratch = new byte[StickerState.Count];

    private MacroSearch(byte[] start, IReadOnlyList<Macro> macros, Func<byte[], bool> goal, int maxDepth)
    {
        _macros = macros;
        _goal = goal;
        _path = new Macro[maxDepth];
        _states = new byte[maxDepth + 1][];
        for (var level = 0; level <= maxDepth; level++)
        {
            _states[level] = new byte[StickerState.Count];
        }

        Array.Copy(start, _states[0], StickerState.Count);
    }

    /// <returns>The macros to apply, empty if the goal already holds, or null if none was found.</returns>
    public static List<Macro>? Find(byte[] start, IReadOnlyList<Macro> macros, Func<byte[], bool> goal, int maxDepth)
    {
        if (goal(start))
        {
            return [];
        }

        var search = new MacroSearch(start, macros, goal, maxDepth);
        for (var depth = 1; depth <= maxDepth; depth++)
        {
            if (search.Search(level: 0, depth, previous: null))
            {
                var found = new List<Macro>();
                for (var i = 0; i < depth; i++)
                {
                    found.Add(search._path[i]);
                }

                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries every macro at <paramref name="level"/>. On the last level it checks the goal, otherwise
    /// it goes one level deeper. Returns true as soon as the goal is reached.
    /// </summary>
    private bool Search(int level, int depth, Face? previous)
    {
        foreach (var macro in _macros)
        {
            if (IsRedundant(macro.Key, previous))
            {
                continue;
            }

            // Start from this level's state and turn the macro's moves.
            var next = _states[level + 1];
            Array.Copy(_states[level], next, StickerState.Count);
            foreach (var move in macro.Moves)
            {
                StickerState.Apply(next, move, _scratch);
            }

            _path[level] = macro;

            bool found;
            if (level + 1 == depth)
            {
                found = _goal(next);
            }
            else
            {
                found = Search(level + 1, depth, macro.Key);
            }

            if (found)
            {
                return true;
            }
        }

        return false;
    }

    // Turning the same face twice in a row is never shorter than one turn, and turns of opposite
    // faces commute, so only one order of each opposite pair is tried.
    private static bool IsRedundant(Face? key, Face? previous)
    {
        if (key is not { } face || previous is not { } last)
        {
            return false;
        }

        if (face == last)
        {
            return true;
        }

        return ViewFrame.Opposite(face) == last && face < last;
    }
}
