import {
  FACE_FRAMES,
  isInLayer,
  quarterTurns,
  rotateClockwise,
  stickerAt,
  stickerPosition,
} from './geometry';
import {
  FACES,
  SOLVED_COLOUR,
  STICKERS_PER_FACE,
  type Colour,
  type CubeState,
  type Face,
  type Move,
} from './types';

/**
 * A local copy of the cube engine, used only to drive animations between server responses. The
 * backend stays the source of truth: the app re-syncs to the state it returns.
 */
export function solvedState(): CubeState {
  return mapFaces((face) => Array<Colour>(STICKERS_PER_FACE).fill(SOLVED_COLOUR[face]));
}

export function applyMove(state: CubeState, move: Move): CubeState {
  let current = state;
  for (let i = 0; i < quarterTurns(move.turn); i++) {
    current = turnClockwise(current, move.face);
  }
  return current;
}

export function applyMoves(state: CubeState, moves: readonly Move[]): CubeState {
  return moves.reduce(applyMove, state);
}

export function statesEqual(a: CubeState, b: CubeState): boolean {
  return FACES.every((face) => a.faces[face].every((colour, i) => colour === b.faces[face][i]));
}

function turnClockwise(state: CubeState, turned: Face): CubeState {
  const axis = FACE_FRAMES[turned].normal;
  const next = mapFaces((face) => [...state.faces[face]]);

  for (const face of FACES) {
    for (let index = 0; index < STICKERS_PER_FACE; index++) {
      const position = stickerPosition(face, index);
      if (!isInLayer(position, turned)) continue;

      const target = stickerAt(
        rotateClockwise(position, axis),
        rotateClockwise(FACE_FRAMES[face].normal, axis),
      );
      next.faces[target.face][target.index] = state.faces[face][index];
    }
  }
  return next;
}

function mapFaces(build: (face: Face) => Colour[]): { faces: Record<Face, Colour[]> } {
  return {
    faces: Object.fromEntries(FACES.map((face) => [face, build(face)])) as Record<Face, Colour[]>,
  };
}
