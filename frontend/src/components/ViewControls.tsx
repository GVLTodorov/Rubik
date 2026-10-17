import type { ViewDirection } from './Cube3D';

const BUTTONS: { direction: ViewDirection; symbol: string; label: string }[] = [
  { direction: 'left', symbol: '⟲', label: 'Rotate view left' },
  { direction: 'right', symbol: '⟳', label: 'Rotate view right' },
  { direction: 'up', symbol: '⤒', label: 'Tilt view to see the bottom' },
  { direction: 'down', symbol: '⤓', label: 'Tilt view to see the top' },
];

/** Curved-arrow buttons that orbit the camera; the cube's state never changes. */
export function ViewControls({
  onRotate,
  onReset,
}: {
  onRotate: (direction: ViewDirection) => void;
  onReset: () => void;
}) {
  return (
    <div className="view-controls" role="group" aria-label="Camera">
      {BUTTONS.map(({ direction, symbol, label }) => (
        <button
          key={direction}
          type="button"
          className="icon-button"
          aria-label={label}
          title={label}
          onClick={() => onRotate(direction)}
        >
          {symbol}
        </button>
      ))}
      <button
        type="button"
        className="icon-button"
        aria-label="Reset view"
        title="Reset view"
        onClick={onReset}
      >
        ⌂
      </button>
    </div>
  );
}
