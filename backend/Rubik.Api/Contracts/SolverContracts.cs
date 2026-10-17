using Rubik.Core;
using Rubik.Core.Solving;

namespace Rubik.Api.Contracts;

/// <summary>A suggested solution: every stage of the layer-by-layer method with its moves.</summary>
public sealed record SolutionResponse
{
    /// <summary>All moves of all stages, space-separated, ready for <c>/api/cube/{id}/moves</c>. Empty when already solved.</summary>
    /// <example>F' D R2 U2 ...</example>
    public required string Moves { get; init; }

    /// <summary>Total number of moves.</summary>
    public required int MoveCount { get; init; }

    /// <summary>The stages in solving order. Stages already done have no moves.</summary>
    public required IReadOnlyList<SolutionStepDto> Steps { get; init; }

    public static SolutionResponse From(Solution solution)
    {
        var steps = new List<SolutionStepDto>();
        foreach (var step in solution.Steps)
        {
            steps.Add(SolutionStepDto.From(step));
        }

        return new SolutionResponse
        {
            Moves = Move.Format(solution.Moves),
            MoveCount = solution.Moves.Count,
            Steps = steps,
        };
    }
}

/// <summary>One stage of a solution.</summary>
public sealed record SolutionStepDto
{
    /// <summary>
    /// The stage: <c>Cross</c>, <c>FirstLayerCorners</c>, <c>MiddleLayer</c>, <c>LastLayerCross</c>,
    /// <c>LastLayerFace</c>, <c>LastLayerCorners</c> or <c>LastLayerEdges</c>.
    /// </summary>
    /// <example>Cross</example>
    public required string Stage { get; init; }

    /// <summary>A short name using the cube's colours.</summary>
    /// <example>White cross</example>
    public required string Title { get; init; }

    /// <summary>What the stage's moves achieve.</summary>
    public required string Description { get; init; }

    /// <summary>The stage's moves in order; empty when the stage was already done.</summary>
    public required IReadOnlyList<string> Moves { get; init; }

    public static SolutionStepDto From(SolutionStep step) => new()
    {
        Stage = step.Stage.ToString(),
        Title = step.Title,
        Description = step.Description,
        Moves = CubeStateMapper.ToNotation(step.Moves),
    };
}
