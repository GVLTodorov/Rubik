import { describe, expect, it } from 'vitest';
import {
  describeMove,
  faceForKey,
  formatMove,
  formatMoves,
  invertMove,
  parseMove,
  parseMoves,
  tokenize,
} from './notation';

describe('notation', () => {
  it('formats the three turn kinds', () => {
    expect(formatMove({ face: 'F', turn: 'clockwise' })).toBe('F');
    expect(formatMove({ face: 'R', turn: 'antiClockwise' })).toBe("R'");
    expect(formatMove({ face: 'U', turn: 'half' })).toBe('U2');
  });

  it('parses valid tokens and rejects invalid ones (case-sensitive)', () => {
    expect(parseMove("B'")).toEqual({ face: 'B', turn: 'antiClockwise' });
    expect(parseMove('D2')).toEqual({ face: 'D', turn: 'half' });
    for (const bad of ['', 'f', 'X', "R''", 'R3', 'FR', "F2'"]) {
      expect(parseMove(bad)).toBeNull();
    }
  });

  it('round-trips a sequence and ignores extra whitespace', () => {
    expect(formatMoves(parseMoves("  F  R'\tU2 "))).toBe("F R' U2");
    expect(formatMoves(parseMoves(['L', "D'"]))).toBe("L D'");
  });

  it('throws on an invalid sequence', () => {
    expect(() => parseMoves('F X')).toThrow("Invalid move 'X'");
  });

  it('tokenizes with character positions matching the API', () => {
    expect(tokenize(' F  X U')).toEqual([
      { text: 'F', position: 1 },
      { text: 'X', position: 4 },
      { text: 'U', position: 6 },
    ]);
  });

  it('inverts moves', () => {
    expect(invertMove({ face: 'F', turn: 'clockwise' }).turn).toBe('antiClockwise');
    expect(invertMove({ face: 'F', turn: 'antiClockwise' }).turn).toBe('clockwise');
    expect(invertMove({ face: 'F', turn: 'half' }).turn).toBe('half');
  });

  it('describes moves for screen readers', () => {
    expect(describeMove({ face: 'F', turn: 'clockwise' })).toBe('Rotate front face clockwise');
    expect(describeMove({ face: 'D', turn: 'antiClockwise' })).toBe(
      'Rotate down face anti-clockwise',
    );
  });

  it('maps keys to faces in either case', () => {
    expect(faceForKey('r')).toBe('R');
    expect(faceForKey('R')).toBe('R');
    expect(faceForKey('x')).toBeNull();
  });
});
