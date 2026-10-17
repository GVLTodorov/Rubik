import { describeMove, formatMove } from '../cube/notation';
import type { Face, Move, Turn } from '../cube/types';

/** Button order on ruwix: F R U B L D. */
const BUTTON_FACES: Face[] = ['F', 'R', 'U', 'B', 'L', 'D'];
const ROWS: Turn[] = ['clockwise', 'antiClockwise'];

/** The 12 face-turn buttons: clockwise on top, anti-clockwise below. */
export function MoveButtons({ onMove }: { onMove: (move: Move) => void }) {
  return (
    <div className="move-buttons" role="group" aria-label="Face turns">
      {ROWS.flatMap((turn) =>
        BUTTON_FACES.map((face) => {
          const move: Move = { face, turn };
          const label = describeMove(move);
          return (
            <button
              key={formatMove(move)}
              type="button"
              className="move-button"
              aria-label={label}
              title={label}
              onClick={() => onMove(move)}
            >
              {formatMove(move)}
            </button>
          );
        }),
      )}
    </div>
  );
}
