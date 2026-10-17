interface ToolbarProps {
  busy: boolean;
  canUndo: boolean;
  canRedo: boolean;
  /** True while a solution is playing. */
  solving: boolean;
  onReset: () => void;
  onScramble: () => void;
  onSolve: () => void;
  onUndo: () => void;
  onRedo: () => void;
}

export function Toolbar({
  busy,
  canUndo,
  canRedo,
  solving,
  onReset,
  onScramble,
  onSolve,
  onUndo,
  onRedo,
}: ToolbarProps) {
  return (
    <div className="toolbar" role="toolbar" aria-label="Cube actions">
      <button type="button" onClick={onReset} disabled={busy} title="Back to solved">
        Reset
      </button>
      <button type="button" onClick={onScramble} disabled={busy} title="Random 20-move scramble">
        Scramble
      </button>
      <button
        type="button"
        className="primary"
        onClick={onSolve}
        disabled={busy || solving}
        title="Solve step by step (layer-by-layer method), at the turn speed below"
      >
        {solving ? 'Solving…' : 'Solve'}
      </button>
      <button type="button" onClick={onUndo} disabled={!canUndo} title="Undo (Ctrl+Z)">
        ↶ Undo
      </button>
      <button type="button" onClick={onRedo} disabled={!canRedo} title="Redo (Ctrl+Y)">
        Redo ↷
      </button>
    </div>
  );
}
