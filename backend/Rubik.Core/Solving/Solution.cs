namespace Rubik.Core.Solving;

/// <summary>Finds a move sequence that returns a cube to solved.</summary>
public interface ISolver
{
    /// <summary>Solves <paramref name="cube"/>, stage by stage. A solved cube gives empty stages.</summary>
    /// <exception cref="UnsolvableCubeException">
    /// The state cannot be reached by turning faces (e.g. a twisted corner or a flipped edge).
    /// </exception>
    Solution Solve(Cube cube);
}

/// <summary>The stages of the layer-by-layer (beginner's) method, in the order they are solved.</summary>
public enum SolveStage
{
    /// <summary>The four U-layer edges.</summary>
    Cross,

    /// <summary>The four U-layer corners, completing the first layer.</summary>
    FirstLayerCorners,

    /// <summary>The four middle-layer edges, completing the first two layers.</summary>
    MiddleLayer,

    /// <summary>The D-layer edges turned so their D colour faces down (a cross on D).</summary>
    LastLayerCross,

    /// <summary>The D-layer corners turned so the whole D face is one colour.</summary>
    LastLayerFace,

    /// <summary>The D-layer corners moved to their places.</summary>
    LastLayerCorners,

    /// <summary>The D-layer edges moved to their places: the cube is solved.</summary>
    LastLayerEdges,
}

/// <summary>One stage of a solution: what it achieves and the moves that do it (possibly none).</summary>
/// <param name="Stage">The stage.</param>
/// <param name="Title">A short name, using the cube's own colours, e.g. "White cross".</param>
/// <param name="Description">What the stage's moves achieve.</param>
/// <param name="Moves">The moves for this stage, in order; empty when it was already done.</param>
public sealed record SolutionStep(SolveStage Stage, string Title, string Description, IReadOnlyList<Move> Moves);

/// <summary>A full solution: every stage of the method, in order.</summary>
public sealed class Solution
{
    /// <summary>Creates a solution from its stages.</summary>
    public Solution(IReadOnlyList<SolutionStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Steps = steps;

        var moves = new List<Move>();
        foreach (var step in steps)
        {
            moves.AddRange(step.Moves);
        }

        Moves = moves;
    }

    /// <summary>Every stage, in order, including stages with no moves.</summary>
    public IReadOnlyList<SolutionStep> Steps { get; }

    /// <summary>All moves of all stages, in order.</summary>
    public IReadOnlyList<Move> Moves { get; }
}

/// <summary>The cube cannot be solved by face turns: it was assembled or coloured wrongly.</summary>
public sealed class UnsolvableCubeException(string message) : Exception(message);
