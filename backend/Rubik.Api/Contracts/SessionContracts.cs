using System.ComponentModel.DataAnnotations;
using Rubik.Core;

namespace Rubik.Api.Contracts;

/// <summary>A server-side session: the current cube and its undo/redo history.</summary>
public sealed record SessionDto
{
    /// <summary>The session id, used in <c>/api/cube/{id}</c> and the app's <c>?session=</c> link.</summary>
    public required Guid Id { get; init; }

    /// <summary>The current cube state.</summary>
    public required CubeStateDto State { get; init; }

    /// <summary>Moves applied so far, oldest first.</summary>
    public required IReadOnlyList<string> History { get; init; }

    /// <summary>Undone moves that redo will turn again; the last entry is redone first.</summary>
    public required IReadOnlyList<string> RedoStack { get; init; }

    public static SessionDto From(Guid id, CubeTimeline timeline) => new()
    {
        Id = id,
        State = CubeStateMapper.ToDto(timeline.Cube),
        History = CubeStateMapper.ToNotation(timeline.History),
        RedoStack = CubeStateMapper.ToNotation(timeline.RedoStack),
    };
}

/// <summary>Moves to apply to a session's cube.</summary>
public sealed record SessionMovesRequest
{
    /// <summary>Whitespace-separated moves, e.g. <c>F R' U B' L D'</c>. <c>'</c> = anti-clockwise, <c>2</c> = half turn.</summary>
    /// <example>F R' U B' L D'</example>
    [Required(AllowEmptyStrings = true)]
    public required string Moves { get; init; }
}

/// <summary>A session after a change, with the moves that were turned (for animation).</summary>
public sealed record SessionUpdateResponse
{
    /// <summary>The session after the change.</summary>
    public required SessionDto Session { get; init; }

    /// <summary>
    /// The moves actually turned, in order: the applied moves, the inverse of an undone move, the
    /// redone move, or nothing for a reset.
    /// </summary>
    public required IReadOnlyList<string> AppliedMoves { get; init; }
}
