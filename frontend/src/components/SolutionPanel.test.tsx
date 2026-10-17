import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { parseMoves } from '../cube/notation';
import type { SolutionProgress } from '../session/CubeSession';
import { SolutionPanel } from './SolutionPanel';

const steps = [
  { title: 'White cross', description: 'Cross description', moves: parseMoves("F R'") },
  { title: 'White corners', description: 'Corners description', moves: parseMoves("R U R' U'") },
  { title: 'Middle layer', description: 'Middle description', moves: [] },
];

function renderPanel(progress: Partial<SolutionProgress>) {
  const onClose = vi.fn();
  render(
    <SolutionPanel
      solution={{ steps, totalMoves: 6, current: null, done: false, ...progress }}
      onClose={onClose}
    />,
  );
  return { onClose, items: screen.getAllByRole('listitem') };
}

describe('SolutionPanel', () => {
  it('follows the move on screen: earlier stages done, the current move highlighted', () => {
    const { items } = renderPanel({ current: { step: 1, move: 2 } });

    expect(screen.getByRole('status')).toHaveTextContent('Stage 2 of 3: White corners');
    expect(items[0]).toHaveClass('solution-step-done');
    expect(items[1]).toHaveAttribute('aria-current', 'step');
    expect(within(items[1]!).getByText('Corners description')).toBeInTheDocument();
    expect(within(items[1]!).getByText("R'")).toHaveClass('solution-move-current');
    expect(within(items[1]!).getByText('U')).toHaveClass('solution-move-played');
    expect(within(items[1]!).getByText("U'")).toHaveClass('solution-move');
    expect(items[2]).toHaveClass('solution-step-pending');
    expect(items[2]).toHaveTextContent('already done');
  });

  it('says it is starting before the first move plays', () => {
    renderPanel({});

    expect(screen.getByRole('status')).toHaveTextContent('Starting…');
    expect(screen.getByRole('heading')).toHaveTextContent('Solution (6 moves)');
  });

  it('closes', async () => {
    const { onClose } = renderPanel({ done: true });

    await userEvent.click(screen.getByRole('button', { name: 'Close solution' }));

    expect(screen.getByRole('status')).toHaveTextContent('Solved in 6 moves.');
    expect(onClose).toHaveBeenCalled();
  });
});
