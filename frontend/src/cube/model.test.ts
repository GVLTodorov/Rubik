import { describe, expect, it } from 'vitest';
import { ACCEPTANCE_FACES, codes } from '../test/fakeApi';
import { applyMove, applyMoves, solvedState, statesEqual } from './model';
import { parseMoves } from './notation';
import { FACES, type Face } from './types';

describe('local cube model', () => {
  it('starts solved: Green front, Red right, White up', () => {
    const state = solvedState();
    expect(state.faces.F).toEqual(codes('G G G G G G G G G'));
    expect(state.faces.R).toEqual(codes('R R R R R R R R R'));
    expect(state.faces.U).toEqual(codes('W W W W W W W W W'));
  });

  it("matches the brief's acceptance result for F R' U B' L D'", () => {
    const state = applyMoves(solvedState(), parseMoves("F R' U B' L D'"));
    for (const face of FACES) {
      expect(state.faces[face].join(' ')).toBe(ACCEPTANCE_FACES[face]);
    }
  });

  it.each<[string, Face, number[], string]>([
    ['F', 'U', [6, 7, 8], 'O O O'],
    ['F', 'R', [0, 3, 6], 'W W W'],
    ['R', 'U', [2, 5, 8], 'G G G'],
    ['U', 'F', [0, 1, 2], 'R R R'],
    ['B', 'L', [0, 3, 6], 'W W W'],
    ['L', 'F', [0, 3, 6], 'W W W'],
    ['D', 'R', [6, 7, 8], 'G G G'],
  ])('after %s, %s stickers %j are %s', (move, face, indices, expected) => {
    const state = applyMoves(solvedState(), parseMoves(move));
    expect(indices.map((i) => state.faces[face][i]).join(' ')).toBe(expected);
  });

  it.each(FACES)("%s four times, X X', and X2 X2 are identity", (face) => {
    const start = applyMoves(solvedState(), parseMoves("R U2 F' L D B2"));
    expect(
      statesEqual(applyMoves(start, parseMoves(`${face} ${face} ${face} ${face}`)), start),
    ).toBe(true);
    expect(statesEqual(applyMoves(start, parseMoves(`${face} ${face}'`)), start)).toBe(true);
    expect(statesEqual(applyMoves(start, parseMoves(`${face}2 ${face}2`)), start)).toBe(true);
  });

  it("(R U R' U') × 6 is identity", () => {
    const start = applyMoves(solvedState(), parseMoves("F R' U B' L D'"));
    const sexy = parseMoves("R U R' U' ".repeat(6));
    expect(statesEqual(applyMoves(start, sexy), start)).toBe(true);
  });

  it('does not mutate its input', () => {
    const start = solvedState();
    applyMove(start, { face: 'F', turn: 'clockwise' });
    expect(statesEqual(start, solvedState())).toBe(true);
  });
});
