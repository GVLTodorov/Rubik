import { FACES, type Face, type Turn } from './types';

/** Integer vector. Axes: x points to R, y to U, z to F. */
export type Vec3 = readonly [number, number, number];

interface FaceFrame {
  /** Outward normal. */
  normal: Vec3;
  /** Direction of increasing column, as seen from outside the face. */
  right: Vec3;
  /** Direction of increasing row, as seen from outside the face. */
  down: Vec3;
}

/** How each face's 3×3 sticker grid sits in space (matches the backend's sticker indexing). */
export const FACE_FRAMES: Record<Face, FaceFrame> = {
  U: { normal: [0, 1, 0], right: [1, 0, 0], down: [0, 0, 1] },
  D: { normal: [0, -1, 0], right: [1, 0, 0], down: [0, 0, -1] },
  F: { normal: [0, 0, 1], right: [1, 0, 0], down: [0, -1, 0] },
  B: { normal: [0, 0, -1], right: [-1, 0, 0], down: [0, -1, 0] },
  R: { normal: [1, 0, 0], right: [0, 0, -1], down: [0, -1, 0] },
  L: { normal: [-1, 0, 0], right: [0, 0, 1], down: [0, -1, 0] },
};

const add = (a: Vec3, b: Vec3): Vec3 => [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
const scale = (a: Vec3, k: number): Vec3 => [a[0] * k, a[1] * k, a[2] * k];
export const dot = (a: Vec3, b: Vec3): number => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
const cross = (a: Vec3, b: Vec3): Vec3 => [
  a[1] * b[2] - a[2] * b[1],
  a[2] * b[0] - a[0] * b[2],
  a[0] * b[1] - a[1] * b[0],
];
const equal = (a: Vec3, b: Vec3): boolean => a[0] === b[0] && a[1] === b[1] && a[2] === b[2];

/** The cubie position (each coordinate −1, 0 or 1) a sticker sits on. */
export function stickerPosition(face: Face, index: number): Vec3 {
  const { normal, right, down } = FACE_FRAMES[face];
  const row = Math.floor(index / 3);
  const column = index % 3;
  return add(normal, add(scale(right, column - 1), scale(down, row - 1)));
}

/** The sticker on the given cubie that faces the given direction. */
export function stickerAt(position: Vec3, normal: Vec3): { face: Face; index: number } {
  const face = FACES.find((f) => equal(FACE_FRAMES[f].normal, normal));
  if (!face) {
    throw new Error(`No face has normal ${normal.join(',')}.`);
  }
  const { right, down } = FACE_FRAMES[face];
  return { face, index: (dot(position, down) + 1) * 3 + dot(position, right) + 1 };
}

/** Clockwise (seen from outside the face) quarter turn = −90° about the outward normal. */
export function rotateClockwise(v: Vec3, axis: Vec3): Vec3 {
  return add(scale(axis, dot(axis, v)), scale(cross(axis, v), -1));
}

/** True when a cubie is in the layer turned by the face. */
export function isInLayer(position: Vec3, face: Face): boolean {
  return dot(position, FACE_FRAMES[face].normal) === 1;
}

const QUARTER_TURNS: Record<Turn, number> = { clockwise: 1, half: 2, antiClockwise: 3 };

export function quarterTurns(turn: Turn): number {
  return QUARTER_TURNS[turn];
}

/** The rotation angle (radians, right-handed about the face normal) a move ends at. */
export function turnAngle(turn: Turn): number {
  const QUARTER = Math.PI / 2;
  if (turn === 'clockwise') return -QUARTER;
  if (turn === 'antiClockwise') return QUARTER;
  return -2 * QUARTER;
}
