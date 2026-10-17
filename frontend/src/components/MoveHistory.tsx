import { formatMoves } from '../cube/notation';
import type { Move } from '../cube/types';

export function MoveHistory({ moves }: { moves: readonly Move[] }) {
  return (
    <section className="history" aria-labelledby="history-heading">
      <h2 id="history-heading">
        Moves <span className="history-count">({moves.length})</span>
      </h2>
      <output className="history-moves" aria-live="polite">
        {moves.length > 0 ? formatMoves(moves) : 'No moves yet'}
      </output>
    </section>
  );
}
