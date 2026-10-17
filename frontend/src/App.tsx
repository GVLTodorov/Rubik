import { useCallback, useEffect, useRef, useState } from 'react';
import { createCubeApi, type CubeApi } from './api/client';
import { Cube3D, type Cube3DHandle } from './components/Cube3D';
import { CubeNet } from './components/CubeNet';
import { ErrorBanner } from './components/ErrorBanner';
import { MoveButtons } from './components/MoveButtons';
import { MoveHistory } from './components/MoveHistory';
import { SequenceInput, type SequenceError } from './components/SequenceInput';
import { ShortcutHelp } from './components/ShortcutHelp';
import { SolutionPanel } from './components/SolutionPanel';
import { SpeedControl } from './components/SpeedControl';
import { Toolbar } from './components/Toolbar';
import { ViewControls } from './components/ViewControls';
import type { Move } from './cube/types';
import { useKeyboardShortcuts } from './hooks/useKeyboardShortcuts';
import { CubeSession } from './session/CubeSession';
import { useCubeSession } from './session/useCubeSession';
import { readUrlState, writeUrlState } from './shareUrl';

type View = '3d' | 'net';

const VIEWS: { id: View; label: string }[] = [
  { id: '3d', label: '3D' },
  { id: 'net', label: '2D net' },
];

interface AppProps {
  api?: CubeApi;
  /** Animation length per quarter turn; tests pass 0. */
  animationMs?: number;
}

export default function App({ api, animationMs }: AppProps) {
  const [session] = useState(
    () => new CubeSession(api ?? createCubeApi(), { durationMs: animationMs }),
  );
  const snapshot = useCubeSession(session);
  const [initialUrl] = useState(() => readUrlState());
  const [view, setView] = useState<View>(initialUrl.view === 'net' ? 'net' : '3d');
  const [speed, setSpeed] = useState(session.animationMs);
  const [sequence, setSequence] = useState('');
  const [sequenceError, setSequenceError] = useState<SequenceError | null>(null);
  const cube3d = useRef<Cube3DHandle>(null);

  useEffect(() => {
    // Opens the ?session= from a shared link, or creates a new session. Safe to call twice.
    void session.start(initialUrl.session);
    return () => session.dispose();
  }, [session, initialUrl]);

  useEffect(() => {
    writeUrlState({ session: snapshot.sessionId, view: view === 'net' ? view : null });
  }, [snapshot.sessionId, view]);

  const turn = useCallback((move: Move) => void session.turn(move), [session]);
  const undo = useCallback(() => void session.undo(), [session]);
  const redo = useCallback(() => void session.redo(), [session]);
  useKeyboardShortcuts({ onMove: turn, onUndo: undo, onRedo: redo });

  async function runSequence(animate: boolean) {
    const outcome = await session.applySequence(sequence, animate);
    setSequenceError(outcome.ok ? null : outcome);
  }

  async function scramble() {
    setSequenceError(null);
    const moves = await session.scramble();
    if (moves !== undefined) setSequence(moves);
  }

  function solve() {
    setSequenceError(null);
    void session.solve();
  }

  function changeSpeed(ms: number) {
    setSpeed(ms);
    session.setAnimationMs(ms);
  }

  return (
    <div className="app">
      <header className="app-header">
        <h1>Rubik's Cube Simulator</h1>
        <div className="tabs" role="tablist" aria-label="View">
          {VIEWS.map(({ id, label }) => (
            <button
              key={id}
              id={`tab-${id}`}
              type="button"
              role="tab"
              aria-selected={view === id}
              aria-controls={`panel-${id}`}
              className="tab"
              onClick={() => setView(id)}
            >
              {label}
            </button>
          ))}
        </div>
        <span className="busy" aria-live="polite">
          {snapshot.busy ? <span className="spinner" role="status" aria-label="Working" /> : null}
        </span>
        <ShortcutHelp />
      </header>

      {snapshot.error ? (
        <ErrorBanner message={snapshot.error} onDismiss={() => session.dismissError()} />
      ) : null}

      {/* data-animating lets the demo recorder (backend/Rubik.Demo) wait for the cube to settle. */}
      <main
        className="stage"
        id={`panel-${view}`}
        role="tabpanel"
        aria-labelledby={`tab-${view}`}
        data-animating={snapshot.animation !== null}
      >
        {view === '3d' ? (
          <>
            <Cube3D ref={cube3d} state={snapshot.display} animation={snapshot.animation} />
            <ViewControls
              onRotate={(direction) => cube3d.current?.rotateView(direction)}
              onReset={() => cube3d.current?.resetView()}
            />
          </>
        ) : (
          <CubeNet state={snapshot.display} />
        )}
      </main>

      <section className="controls" aria-label="Controls">
        <MoveButtons onMove={turn} />
        <SequenceInput
          value={sequence}
          error={sequenceError}
          disabled={snapshot.busy}
          onChange={(value) => {
            setSequence(value);
            setSequenceError(null);
          }}
          onApply={() => void runSequence(false)}
          onPlay={() => void runSequence(true)}
        />
        <Toolbar
          busy={snapshot.busy}
          canUndo={snapshot.history.length > 0}
          canRedo={snapshot.redoStack.length > 0}
          solving={snapshot.solution !== null && !snapshot.solution.done}
          onReset={() => {
            setSequenceError(null);
            void session.reset();
          }}
          onScramble={() => void scramble()}
          onSolve={solve}
          onUndo={undo}
          onRedo={redo}
        />
        <SpeedControl value={speed} onChange={changeSpeed} />
        {snapshot.solution ? (
          <SolutionPanel solution={snapshot.solution} onClose={() => session.dismissSolution()} />
        ) : null}
        <MoveHistory moves={snapshot.history} />
      </section>
    </div>
  );
}
