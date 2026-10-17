import { FACES, FACE_NAMES, type Face, type Move, type Turn } from './types';

const SUFFIX: Record<Turn, string> = { clockwise: '', antiClockwise: "'", half: '2' };

const TURN_LABEL: Record<Turn, string> = {
  clockwise: 'clockwise',
  antiClockwise: 'anti-clockwise',
  half: '180 degrees',
};

const INVERSE: Record<Turn, Turn> = {
  clockwise: 'antiClockwise',
  antiClockwise: 'clockwise',
  half: 'half',
};

/** Standard notation for a move: `F`, `F'` or `F2`. */
export function formatMove(move: Move): string {
  return move.face + SUFFIX[move.turn];
}

export function formatMoves(moves: readonly Move[]): string {
  return moves.map(formatMove).join(' ');
}

/** A spoken description, e.g. "Rotate front face clockwise". */
export function describeMove(move: Move): string {
  return `Rotate ${FACE_NAMES[move.face].toLowerCase()} face ${TURN_LABEL[move.turn]}`;
}

export function invertMove(move: Move): Move {
  return { face: move.face, turn: INVERSE[move.turn] };
}

function isFace(letter: string): letter is Face {
  return (FACES as readonly string[]).includes(letter);
}

/** Parses one token (case-sensitive). Returns null when it is not a valid move. */
export function parseMove(token: string): Move | null {
  const face = token[0];
  if (face === undefined || !isFace(face) || token.length > 2) {
    return null;
  }
  const suffix = token.slice(1);
  const turn = (Object.keys(SUFFIX) as Turn[]).find((t) => SUFFIX[t] === suffix);
  return turn ? { face, turn } : null;
}

export interface Token {
  text: string;
  /** Zero-based character offset in the input, matching the API's `position`. */
  position: number;
}

/** Splits a sequence on whitespace, keeping each token's position. */
export function tokenize(sequence: string): Token[] {
  return [...sequence.matchAll(/\S+/g)].map((match) => ({
    text: match[0],
    position: match.index,
  }));
}

/** Parses moves the API has already validated (e.g. `appliedMoves`). Throws on invalid input. */
export function parseMoves(sequence: string | readonly string[]): Move[] {
  const tokens = typeof sequence === 'string' ? tokenize(sequence).map((t) => t.text) : sequence;
  return tokens.map((token) => {
    const move = parseMove(token);
    if (!move) {
      throw new Error(`Invalid move '${token}'.`);
    }
    return move;
  });
}

/** Keyboard key → face, for the F R U B L D shortcuts. */
export function faceForKey(key: string): Face | null {
  const letter = key.toUpperCase();
  return isFace(letter) ? letter : null;
}
