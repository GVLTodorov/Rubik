namespace Rubik.Core.Solving;

/// <summary>
/// Solves the cube with the layer-by-layer (beginner's) method, so every stage is easy to follow:
/// the U layer first (cross, then corners), then the middle layer, then the D layer (cross, face,
/// corners, edges). Each stage picks textbook algorithms by a small bounded search whose goal is
/// "the next piece is solved and nothing solved earlier was disturbed", so no case tables are
/// needed. Solutions are not the shortest (typically 80–150 moves), but every move has a purpose.
/// </summary>
public sealed class BeginnerSolver : ISolver
{
    // Algorithms are written for "last layer on top, front facing you" (see ViewFrame).

    /// <summary>Inserts from top-front-right into front-right-bottom (the real U layer), for each way the corner can face.</summary>
    private const string CornerInsertFromRight = "R U R'";

    private const string CornerInsertFromFront = "F' U' F";

    private const string CornerInsertFromTop = "R U2 R' U' R U R'";

    /// <summary>Also takes a corner out of its slot, or twists it in place.</summary>
    private const string CornerInsertAndTwist = "R U R' U'";

    /// <summary>Inserts the top-front edge into the front-right slot.</summary>
    private const string EdgeInsertRight = "U R U' R' U' F' U F";

    /// <summary>Inserts the top-front edge into the front-left slot.</summary>
    private const string EdgeInsertLeft = "U' L' U L U F U' F'";

    /// <summary>Flips top edges: dot → L shape → line → cross.</summary>
    private const string EdgeFlip = "F R U R' U' F'";

    private const string EdgeFlipAlternative = "F U R U' R' F'";

    /// <summary>Twists three top corners.</summary>
    private const string Sune = "R U R' U R U2 R'";

    private const string AntiSune = "R U2 R' U' R U' R'";

    /// <summary>Cycles three top corners, keeping the front-left one (A permutation).</summary>
    private const string CornerCycle = "R' F R' B2 R F' R' B2 R2";

    private const string CornerCycleReverse = "R2 B2 R F R' B2 R F' R";

    /// <summary>Cycles three top edges, keeping the back one (U permutation).</summary>
    private const string EdgeCycle = "R U' R U R U R U' R' U' R2";

    private const string EdgeCycleReverse = "R2 U R U R' U' R' U' R' U R'";

    /// <summary>The three turns of a face, in the order the search tries them.</summary>
    private static readonly Turn[] Turns = [Turn.Clockwise, Turn.Half, Turn.AntiClockwise];

    /// <summary>All 18 single face turns: used for the cross, which needs free moves.</summary>
    private static readonly Macro[] SingleTurns = BuildSingleTurns();

    private static readonly Macro[] CornerMacros = BuildStageMacros(CornerInsertFromRight, CornerInsertFromFront, CornerInsertFromTop, CornerInsertAndTwist);

    private static readonly Macro[] MiddleEdgeMacros = BuildStageMacros(EdgeInsertRight, EdgeInsertLeft);

    private static readonly Macro[] EdgeFlipMacros = BuildStageMacros(EdgeFlip, EdgeFlipAlternative);

    private static readonly Macro[] CornerTwistMacros = BuildStageMacros(Sune, AntiSune);

    private static readonly Macro[] CornerCycleMacros = BuildStageMacros(CornerCycle, CornerCycleReverse);

    private static readonly Macro[] EdgeCycleMacros = BuildStageMacros(EdgeCycle, EdgeCycleReverse);

    /// <summary>The four U edges (the cross).</summary>
    private static readonly List<int[]> UpEdges = CubeLayout.SlotsTouching(CubeLayout.Edges, Face.U);

    /// <summary>Every U-layer slot: the cross and the four U corners.</summary>
    private static readonly List<int[]> FirstLayer = CubeLayout.SlotsTouching(CubeLayout.Slots, Face.U);

    /// <summary>Every slot not on D: the U layer and the middle layer.</summary>
    private static readonly List<int[]> FirstTwoLayers = CubeLayout.SlotsNotTouching(CubeLayout.Slots, Face.D);

    /// <summary>The four D edges.</summary>
    private static readonly List<int[]> LastLayerEdges = CubeLayout.SlotsTouching(CubeLayout.Edges, Face.D);

    /// <inheritdoc />
    public Solution Solve(Cube cube)
    {
        ArgumentNullException.ThrowIfNull(cube);

        var run = new Run(StickerState.From(cube));
        var up = (Colour)StickerState.Centre(run.State, Face.U);
        var down = (Colour)StickerState.Centre(run.State, Face.D);
        var steps = new List<SolutionStep>();

        SolveCross(run);
        steps.Add(run.EndStage(SolveStage.Cross, $"{up} cross", $"Bring the four {up} edges to the U face, each lined up with its side centre."));

        SolveFirstLayerCorners(run);
        steps.Add(run.EndStage(SolveStage.FirstLayerCorners, $"{up} corners", $"Insert the four {up} corners into the U layer, completing the first layer."));

        SolveMiddleLayer(run);
        steps.Add(run.EndStage(SolveStage.MiddleLayer, "Middle layer", "Insert the four middle-layer edges from the D layer, completing the first two layers."));

        SolveLastLayerCross(run);
        steps.Add(run.EndStage(SolveStage.LastLayerCross, $"{down} cross", $"Flip the D-layer edges until they make a {down} cross on the D face."));

        SolveLastLayerFace(run);
        steps.Add(run.EndStage(SolveStage.LastLayerFace, $"{down} face", $"Twist the D-layer corners until the whole D face is {down}."));

        SolveLastLayerCorners(run);
        steps.Add(run.EndStage(SolveStage.LastLayerCorners, $"{down} corners", "Cycle the D-layer corners into their places."));

        SolveLastLayerEdges(run);
        steps.Add(run.EndStage(SolveStage.LastLayerEdges, $"{down} edges", "Cycle the D-layer edges into their places: the cube is solved."));

        // The last stage's goal is "every slot solved", so reaching here means the cube is solved.
        return new Solution(steps);
    }

    /// <summary>Each U edge: bring it to the D layer, then up into its slot, keeping earlier edges.</summary>
    private static void SolveCross(Run run)
    {
        var solved = new List<int[]>();
        foreach (var side in ViewFrame.Sides)
        {
            var slot = CubeLayout.Slot(Face.U, side);
            if (!CubeLayout.IsSolved(run.State, slot))
            {
                var colours = CubeLayout.HomeColours(run.State, slot);
                run.Reach(SingleTurns, state => CubeLayout.AllSolved(state, solved) && CubeLayout.Touches(CubeLayout.Locate(state, colours), Face.D), maxDepth: 3);
                run.Reach(SingleTurns, state => CubeLayout.AllSolved(state, solved) && CubeLayout.IsSolved(state, slot), maxDepth: 5);
            }

            solved.Add(slot);
        }
    }

    /// <summary>
    /// Each U corner, keeping the cross and earlier corners: at most take it out of a wrong slot,
    /// turn D to bring it below its own slot, and insert it, so 3 search steps always suffice.
    /// </summary>
    private static void SolveFirstLayerCorners(Run run)
    {
        var solved = new List<int[]>(UpEdges);
        foreach (var front in ViewFrame.Sides)
        {
            var slot = CubeLayout.Slot(Face.U, front, ViewFrame.LeftOf(front));
            run.Reach(CornerMacros, state => CubeLayout.AllSolved(state, solved) && CubeLayout.IsSolved(state, slot), maxDepth: 3);
            solved.Add(slot);
        }
    }

    /// <summary>Each middle edge, keeping the first layer and earlier middle edges.</summary>
    private static void SolveMiddleLayer(Run run)
    {
        var solved = new List<int[]>(FirstLayer);
        foreach (var front in ViewFrame.Sides)
        {
            var slot = CubeLayout.Slot(front, ViewFrame.LeftOf(front));
            run.Reach(MiddleEdgeMacros, state => CubeLayout.AllSolved(state, solved) && CubeLayout.IsSolved(state, slot), maxDepth: 4);
            solved.Add(slot);
        }
    }

    private static void SolveLastLayerCross(Run run) =>
        run.Reach(EdgeFlipMacros, state => CubeLayout.AllSolved(state, FirstTwoLayers) && LastLayerEdgesFaceDown(state), maxDepth: 4);

    private static void SolveLastLayerFace(Run run) =>
        run.Reach(CornerTwistMacros, state => CubeLayout.AllSolved(state, FirstTwoLayers) && FaceIsUniform(state, Face.D), maxDepth: 4);

    private static void SolveLastLayerCorners(Run run) =>
        run.Reach(
            CornerCycleMacros,
            state => CubeLayout.AllSolved(state, FirstTwoLayers) && CubeLayout.AllSolved(state, CubeLayout.Corners) && LastLayerEdgesFaceDown(state),
            maxDepth: 4);

    private static void SolveLastLayerEdges(Run run) =>
        run.Reach(EdgeCycleMacros, state => CubeLayout.AllSolved(state, CubeLayout.Slots), maxDepth: 3);

    /// <summary>True when every D edge shows the D colour on the D face.</summary>
    private static bool LastLayerEdgesFaceDown(byte[] state)
    {
        var down = StickerState.Centre(state, Face.D);
        foreach (var slot in LastLayerEdges)
        {
            var facesDown = false;
            foreach (var sticker in slot)
            {
                if (StickerState.FaceOf(sticker) == Face.D && state[sticker] == down)
                {
                    facesDown = true;
                }
            }

            if (!facesDown)
            {
                return false;
            }
        }

        return true;
    }

    private static bool FaceIsUniform(byte[] state, Face face)
    {
        var offset = (int)face * Cube.StickersPerFace;
        for (var i = 0; i < Cube.StickersPerFace; i++)
        {
            if (state[offset + i] != state[offset + Cube.CentreIndex])
            {
                return false;
            }
        }

        return true;
    }

    private static Macro[] BuildSingleTurns()
    {
        var macros = new List<Macro>();
        foreach (var face in Faces.All)
        {
            foreach (var turn in Turns)
            {
                macros.Add(Macro.Turn(new Move(face, turn)));
            }
        }

        return [.. macros];
    }

    /// <summary>
    /// The macros for a last-layer stage: the three D turns (to line pieces up), then each algorithm
    /// held with each of the four sides in front.
    /// </summary>
    private static Macro[] BuildStageMacros(params string[] algorithms)
    {
        var macros = new List<Macro>();
        foreach (var turn in Turns)
        {
            macros.Add(Macro.Turn(new Move(Face.D, turn)));
        }

        foreach (var algorithm in algorithms)
        {
            foreach (var front in ViewFrame.Sides)
            {
                macros.Add(Macro.Algorithm(ViewFrame.Translate(algorithm, front)));
            }
        }

        return [.. macros];
    }

    /// <summary>The cube being solved, and the moves of the stage in progress.</summary>
    private sealed class Run(byte[] state)
    {
        private readonly byte[] _scratch = new byte[StickerState.Count];
        private readonly List<Move> _stageMoves = [];

        public byte[] State { get; } = state;

        /// <summary>Searches for macros that reach <paramref name="goal"/> and applies them.</summary>
        /// <exception cref="UnsolvableCubeException">No macros reach the goal: the cube cannot be solved.</exception>
        public void Reach(IReadOnlyList<Macro> macros, Func<byte[], bool> goal, int maxDepth)
        {
            var found = MacroSearch.Find(State, macros, goal, maxDepth);
            if (found is null)
            {
                throw new UnsolvableCubeException(
                    "The cube cannot be solved: a piece is twisted, flipped or swapped in a way face turns cannot produce.");
            }

            foreach (var macro in found)
            {
                foreach (var move in macro.Moves)
                {
                    StickerState.Apply(State, move, _scratch);
                    _stageMoves.Add(move);
                }
            }
        }

        /// <summary>Finishes the current stage: returns its (simplified) moves and starts a new stage.</summary>
        public SolutionStep EndStage(SolveStage stage, string title, string description)
        {
            var moves = Simplify(_stageMoves);
            _stageMoves.Clear();
            return new SolutionStep(stage, title, description, moves);
        }

        /// <summary>Merges neighbouring turns of the same face (<c>R R</c> → <c>R2</c>, <c>R R'</c> → nothing).</summary>
        private static List<Move> Simplify(List<Move> moves)
        {
            var result = new List<Move>();
            foreach (var move in moves)
            {
                if (result.Count > 0 && result[^1].Face == move.Face)
                {
                    var quarterTurns = (result[^1].QuarterTurns + move.QuarterTurns) % 4;
                    result.RemoveAt(result.Count - 1);
                    if (quarterTurns != 0)
                    {
                        result.Add(new Move(move.Face, (Turn)quarterTurns));
                    }
                }
                else
                {
                    result.Add(move);
                }
            }

            return result;
        }
    }
}
