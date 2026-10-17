import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';
import { ACCEPTANCE_FACES, createFakeApi } from './test/fakeApi';

// WebGL is not available in jsdom; the 3D view is covered by the shared model and session tests.
vi.mock('./components/Cube3D', () => ({
  Cube3D: () => <div data-testid="cube3d" />,
}));

function faceColours(name: string): string {
  const group = screen.getByRole('group', { name: `${name} face` });
  return within(group)
    .getAllByRole('img')
    .map((sticker) => sticker.dataset.colour)
    .join(' ');
}

describe('App', () => {
  let api: ReturnType<typeof createFakeApi>;

  beforeEach(() => {
    api = createFakeApi();
  });

  async function renderOnNet() {
    const user = userEvent.setup();
    render(<App api={api} animationMs={0} />);
    await user.click(screen.getByRole('tab', { name: '2D net' }));
    await waitFor(() => expect(api.createSession).toHaveBeenCalled());
    return user;
  }

  it('opens on the 3D view with twelve labelled face-turn buttons', () => {
    render(<App api={api} animationMs={0} />);

    expect(screen.getByTestId('cube3d')).toBeInTheDocument();
    const buttons = within(screen.getByRole('group', { name: 'Face turns' })).getAllByRole(
      'button',
    );
    expect(buttons.map((b) => b.textContent)).toEqual([
      'F',
      'R',
      'U',
      'B',
      'L',
      'D',
      "F'",
      "R'",
      "U'",
      "B'",
      "L'",
      "D'",
    ]);
    expect(screen.getByRole('button', { name: 'Rotate front face clockwise' })).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Rotate down face anti-clockwise' }),
    ).toBeInTheDocument();
  });

  it('clicking a move button calls the session API and updates the net', async () => {
    const user = await renderOnNet();

    await user.click(screen.getByRole('button', { name: 'Rotate front face clockwise' }));

    await waitFor(() => expect(faceColours('Up')).toBe('W W W W W W O O O'));
    expect(api.sessionMoves).toHaveBeenCalledWith('session-1', 'F');
    expect(
      within(screen.getByRole('region', { name: /moves/i })).getByText('F'),
    ).toBeInTheDocument();
  });

  it("applies a typed sequence and shows the brief's acceptance net", async () => {
    const user = await renderOnNet();

    await user.type(screen.getByRole('textbox', { name: 'Move sequence' }), "F R' U B' L D'");
    await user.click(screen.getByRole('button', { name: 'Apply sequence' }));

    await waitFor(() => expect(faceColours('Up')).toBe(ACCEPTANCE_FACES.U));
    expect(faceColours('Left')).toBe(ACCEPTANCE_FACES.L);
    expect(faceColours('Front')).toBe(ACCEPTANCE_FACES.F);
    expect(faceColours('Right')).toBe(ACCEPTANCE_FACES.R);
    expect(faceColours('Back')).toBe(ACCEPTANCE_FACES.B);
    expect(faceColours('Down')).toBe(ACCEPTANCE_FACES.D);
  });

  it('highlights an invalid token with the API error', async () => {
    const user = await renderOnNet();
    const input = screen.getByRole('textbox', { name: 'Move sequence' });

    await user.type(input, 'F X U');
    await user.click(screen.getByRole('button', { name: 'Apply sequence' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent("Invalid move 'X' at position 2.");
    expect(within(alert).getByText('X', { selector: 'mark' })).toBeInTheDocument();
    expect(input).toHaveAttribute('aria-invalid', 'true');
  });

  it('turns faces from the keyboard and undoes with Ctrl+Z', async () => {
    const user = await renderOnNet();

    await user.keyboard('{Shift>}r{/Shift}');
    await waitFor(() => expect(api.sessionMoves).toHaveBeenLastCalledWith('session-1', "R'"));

    await user.keyboard('{Control>}z{/Control}');
    await waitFor(() => expect(api.sessionUndo).toHaveBeenCalledWith('session-1'));
    await waitFor(() => expect(faceColours('Up')).toBe('W W W W W W W W W'));
  });

  it('Solve plays the suggested solution and lists its stages', async () => {
    const user = await renderOnNet();
    await user.type(screen.getByRole('textbox', { name: 'Move sequence' }), "F R'");
    await user.click(screen.getByRole('button', { name: 'Apply sequence' }));
    await waitFor(() => expect(faceColours('Up')).not.toBe('W W W W W W W W W'));

    await user.click(screen.getByRole('button', { name: 'Solve' }));

    const panel = await screen.findByRole('region', { name: /solution/i });
    await waitFor(() =>
      expect(within(panel).getByRole('status')).toHaveTextContent('Solved in 2 moves.'),
    );
    expect(faceColours('Up')).toBe('W W W W W W W W W');
    expect(api.solveSession).toHaveBeenCalledWith('session-1');
    expect(
      within(panel)
        .getAllByRole('listitem')
        .map((item) => item.textContent),
    ).toEqual(['✓White cross1 moveR', '✓Middle layeralready done', "✓Yellow edges1 moveF'"]);

    await user.click(within(panel).getByRole('button', { name: 'Close solution' }));
    expect(screen.queryByRole('region', { name: /solution/i })).not.toBeInTheDocument();
  });

  it('Solve on a solved cube says so', async () => {
    const user = await renderOnNet();

    await user.click(screen.getByRole('button', { name: 'Solve' }));

    expect(await screen.findByText('The cube is already solved.')).toBeInTheDocument();
  });

  it('scramble fills the sequence field', async () => {
    const user = await renderOnNet();

    await user.click(screen.getByRole('button', { name: 'Scramble' }));

    await waitFor(() =>
      expect(screen.getByRole('textbox', { name: 'Move sequence' })).toHaveValue("R U2 F'"),
    );
    await waitFor(() =>
      expect(screen.getByRole('tabpanel')).toHaveAttribute('data-animating', 'false'),
    );
  });

  it('puts only the session id in the URL, however many moves are made', async () => {
    const user = await renderOnNet();

    for (let i = 0; i < 8; i++) {
      await user.click(screen.getByRole('button', { name: 'Rotate right face clockwise' }));
    }
    await waitFor(() => expect(api.sessionMoves).toHaveBeenCalledTimes(8));

    expect(window.location.search).toBe('?session=session-1&view=net');
  });

  it('opens a shared ?session= link with its cube and history', async () => {
    api.seedSession('shared-id', "F R' U B' L D'");
    window.history.replaceState(null, '', '/?session=shared-id&view=net');
    render(<App api={api} animationMs={0} />);

    await waitFor(() => expect(faceColours('Up')).toBe(ACCEPTANCE_FACES.U));
    expect(api.getSession).toHaveBeenCalledWith('shared-id');
    expect(api.createSession).not.toHaveBeenCalled();
    expect(screen.getByRole('tab', { name: '2D net' })).toHaveAttribute('aria-selected', 'true');
    expect(
      within(screen.getByRole('region', { name: /moves/i })).getByText("F R' U B' L D'"),
    ).toBeInTheDocument();
  });

  it('replaces an expired shared session with a new one and says so', async () => {
    window.history.replaceState(null, '', '/?session=expired-id');
    render(<App api={api} animationMs={0} />);

    expect(await screen.findByRole('alert')).toHaveTextContent('expired');
    await waitFor(() =>
      expect(new URLSearchParams(window.location.search).get('session')).toBe('session-1'),
    );
  });

  it('shows a dismissible banner when the API is unreachable', async () => {
    api.createSession.mockRejectedValueOnce(
      new Error('Cannot reach the API. Is the backend running?'),
    );
    const user = userEvent.setup();
    render(<App api={api} animationMs={0} />);

    const banner = await screen.findByRole('alert');
    expect(banner).toHaveTextContent('Cannot reach the API');

    await user.click(within(banner).getByRole('button', { name: 'Dismiss error' }));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});
