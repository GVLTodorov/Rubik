import { formatMove } from '../cube/notation';
import type { SolutionProgress } from '../session/CubeSession';

interface SolutionPanelProps {
  solution: SolutionProgress;
  onClose: () => void;
}

type StepState = 'done' | 'active' | 'pending';

/**
 * The suggested solution, stage by stage, following the move that is playing. It never scrolls
 * the page: the list has no height limit, so the current stage is always on screen with the rest.
 */
export function SolutionPanel({ solution, onClose }: SolutionPanelProps) {
  const { steps, totalMoves, current, done } = solution;
  const stepState = (index: number): StepState => {
    if (done || (current !== null && index < current.step)) return 'done';
    return current?.step === index ? 'active' : 'pending';
  };

  return (
    <section className="solution" aria-labelledby="solution-heading">
      <div className="solution-header">
        <h2 id="solution-heading">
          Solution <span className="history-count">({countMoves(totalMoves)})</span>
        </h2>
        <button
          type="button"
          className="icon-button"
          aria-label="Close solution"
          title="Close solution"
          onClick={onClose}
        >
          ×
        </button>
      </div>
      <p className="solution-status" role="status">
        {describeStatus(solution)}
      </p>
      <ol className="solution-steps">
        {steps.map((step, index) => {
          const state = stepState(index);
          return (
            <li
              key={index}
              className={`solution-step solution-step-${state}`}
              aria-current={state === 'active' ? 'step' : undefined}
            >
              <div className="solution-step-title">
                <span className="solution-step-mark" aria-hidden="true">
                  {state === 'done' ? '✓' : index + 1}
                </span>
                <span>{step.title}</span>
                <span className="history-count">
                  {step.moves.length === 0 ? 'already done' : countMoves(step.moves.length)}
                </span>
              </div>
              {state === 'active' ? (
                <p className="solution-step-description">{step.description}</p>
              ) : null}
              {step.moves.length > 0 ? (
                <code className="solution-moves">
                  {step.moves.map((move, m) => (
                    <span key={m} className={moveClass(state, current?.move ?? -1, m)}>
                      {formatMove(move)}
                    </span>
                  ))}
                </code>
              ) : null}
            </li>
          );
        })}
      </ol>
    </section>
  );
}

function describeStatus({ steps, totalMoves, current, done }: SolutionProgress): string {
  if (totalMoves === 0) return 'The cube is already solved.';
  if (done) return `Solved in ${countMoves(totalMoves)}.`;
  if (current === null) return 'Starting…';
  return `Stage ${current.step + 1} of ${steps.length}: ${steps[current.step]?.title ?? ''}`;
}

function moveClass(state: StepState, currentMove: number, index: number): string {
  if (state === 'done' || (state === 'active' && index < currentMove))
    return 'solution-move-played';
  if (state === 'active' && index === currentMove) return 'solution-move-current';
  return 'solution-move';
}

function countMoves(count: number): string {
  return `${count} ${count === 1 ? 'move' : 'moves'}`;
}
