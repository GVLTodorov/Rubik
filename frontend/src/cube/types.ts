/** Faces in net order: U, then L F R B, then D (same as the API contract). */
export const FACES = ['U', 'L', 'F', 'R', 'B', 'D'] as const;
export type Face = (typeof FACES)[number];

/** Colour codes: White, Orange, Green, Red, Blue, Yellow. */
export const COLOURS = ['W', 'O', 'G', 'R', 'B', 'Y'] as const;
export type Colour = (typeof COLOURS)[number];

export const STICKERS_PER_FACE = 9;

/**
 * The API's state contract: each face holds 9 colour codes, row-major, as seen from outside that
 * face. Row 0 of U is next to B, row 0 of D is next to F, row 0 of the side faces is next to U.
 */
export interface CubeState {
  faces: Record<Face, readonly Colour[]>;
}

export type Turn = 'clockwise' | 'antiClockwise' | 'half';

export interface Move {
  face: Face;
  turn: Turn;
}

export const FACE_NAMES: Record<Face, string> = {
  U: 'Up',
  L: 'Left',
  F: 'Front',
  R: 'Right',
  B: 'Back',
  D: 'Down',
};

export const COLOUR_NAMES: Record<Colour, string> = {
  W: 'White',
  O: 'Orange',
  G: 'Green',
  R: 'Red',
  B: 'Blue',
  Y: 'Yellow',
};

/** Sticker colours, close to ruwix.com's palette. */
export const COLOUR_HEX: Record<Colour, string> = {
  W: '#ffffff',
  O: '#ff8c00',
  G: '#00c040',
  R: '#e0001b',
  B: '#0050c8',
  Y: '#ffe000',
};

export const SOLVED_COLOUR: Record<Face, Colour> = {
  U: 'W',
  L: 'O',
  F: 'G',
  R: 'R',
  B: 'B',
  D: 'Y',
};
