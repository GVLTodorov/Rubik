import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { applyMoves, solvedState } from '../cube/model';
import { parseMoves } from '../cube/notation';
import { FACES, FACE_NAMES, type Face } from '../cube/types';
import { ACCEPTANCE_FACES } from '../test/fakeApi';
import { CubeNet } from './CubeNet';

function faceColours(face: Face): string {
  const group = screen.getByRole('group', { name: `${FACE_NAMES[face]} face` });
  return within(group)
    .getAllByRole('img')
    .map((sticker) => sticker.dataset.colour)
    .join(' ');
}

describe('CubeNet', () => {
  it("renders the acceptance state exactly as the brief's net", () => {
    render(<CubeNet state={applyMoves(solvedState(), parseMoves("F R' U B' L D'"))} />);

    for (const face of FACES) {
      expect(faceColours(face)).toBe(ACCEPTANCE_FACES[face]);
    }
  });

  it('lays faces out as a cross: U above F, L F R B in a row, D below F', () => {
    render(<CubeNet state={solvedState()} />);

    const position = (face: Face) => {
      const style = screen.getByRole('group', { name: `${FACE_NAMES[face]} face` }).style;
      return `${style.gridRow}/${style.gridColumn}`;
    };
    expect(FACES.map(position)).toEqual(['1/2', '2/1', '2/2', '2/3', '2/4', '3/2']);
  });

  it('exposes colour names to screen readers', () => {
    render(<CubeNet state={solvedState()} />);

    expect(screen.getByRole('img', { name: 'Front 1: Green' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Up 9: White' })).toBeInTheDocument();
  });
});
