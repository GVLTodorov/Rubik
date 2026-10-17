import type { CSSProperties } from 'react';
import {
  COLOUR_HEX,
  COLOUR_NAMES,
  FACE_NAMES,
  type Colour,
  type CubeState,
  type Face,
} from '../cube/types';

/** Exploded cross: U on top, L F R B across the middle, D at the bottom (same as the console). */
const LAYOUT: { face: Face; row: number; column: number }[] = [
  { face: 'U', row: 1, column: 2 },
  { face: 'L', row: 2, column: 1 },
  { face: 'F', row: 2, column: 2 },
  { face: 'R', row: 2, column: 3 },
  { face: 'B', row: 2, column: 4 },
  { face: 'D', row: 3, column: 2 },
];

export function CubeNet({ state }: { state: CubeState }) {
  return (
    <div className="net" role="group" aria-label="Cube net">
      {LAYOUT.map(({ face, row, column }) => (
        <NetFace
          key={face}
          face={face}
          colours={state.faces[face]}
          style={{ gridRow: row, gridColumn: column }}
        />
      ))}
    </div>
  );
}

function NetFace({
  face,
  colours,
  style,
}: {
  face: Face;
  colours: readonly Colour[];
  style: CSSProperties;
}) {
  return (
    <div
      className="net-face"
      style={style}
      role="group"
      aria-label={`${FACE_NAMES[face]} face`}
      data-face={face}
    >
      {colours.map((colour, index) => (
        <div
          key={index}
          className="sticker"
          style={{ background: COLOUR_HEX[colour] }}
          role="img"
          aria-label={`${FACE_NAMES[face]} ${index + 1}: ${COLOUR_NAMES[colour]}`}
          title={COLOUR_NAMES[colour]}
          data-colour={colour}
        >
          {index === 4 ? <span className="sticker-label">{face}</span> : null}
        </div>
      ))}
    </div>
  );
}
