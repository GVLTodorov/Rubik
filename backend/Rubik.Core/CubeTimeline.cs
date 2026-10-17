namespace Rubik.Core;

/// <summary>
/// A cube together with its move history and redo stack. Starts solved. Not thread-safe: the
/// caller locks it when several threads share one timeline.
/// </summary>
public sealed class CubeTimeline
{
    private readonly List<Move> _history = [];
    private readonly List<Move> _redoStack = [];

    /// <summary>The current cube.</summary>
    public Cube Cube { get; private set; } = Cube.Solved();

    /// <summary>Moves applied so far, oldest first.</summary>
    public IReadOnlyList<Move> History => _history;

    /// <summary>Undone moves that can be redone; the last entry is redone first.</summary>
    public IReadOnlyList<Move> RedoStack => _redoStack;

    /// <summary>True when there is a move to undo.</summary>
    public bool CanUndo => _history.Count > 0;

    /// <summary>True when there is an undone move to redo.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Turns the moves, appends them to the history and clears the redo stack. An empty list
    /// changes nothing (the redo stack is kept).
    /// </summary>
    public void Apply(IReadOnlyList<Move> moves)
    {
        ArgumentNullException.ThrowIfNull(moves);
        if (moves.Count == 0)
        {
            return;
        }

        foreach (var move in moves)
        {
            Cube = Cube.Apply(move);
            _history.Add(move);
        }

        _redoStack.Clear();
    }

    /// <summary>Turns the inverse of the last move and puts that move on the redo stack.</summary>
    /// <returns>The move actually turned (the inverse of the undone move).</returns>
    /// <exception cref="InvalidOperationException">There is nothing to undo.</exception>
    public Move Undo()
    {
        if (!CanUndo)
        {
            throw new InvalidOperationException("There is no move to undo.");
        }

        var last = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        _redoStack.Add(last);

        var turned = last.Inverse();
        Cube = Cube.Apply(turned);
        return turned;
    }

    /// <summary>Turns the most recently undone move again and puts it back in the history.</summary>
    /// <returns>The move turned.</returns>
    /// <exception cref="InvalidOperationException">There is nothing to redo.</exception>
    public Move Redo()
    {
        if (!CanRedo)
        {
            throw new InvalidOperationException("There is no move to redo.");
        }

        var move = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        _history.Add(move);

        Cube = Cube.Apply(move);
        return move;
    }

    /// <summary>Returns to a solved cube and clears the history and redo stack.</summary>
    public void Reset()
    {
        Cube = Cube.Solved();
        _history.Clear();
        _redoStack.Clear();
    }
}
