import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../api/client';
import { applyMoves, solvedState, statesEqual } from '../cube/model';
import { formatMoves, parseMoves } from '../cube/notation';
import { createFakeApi } from '../test/fakeApi';
import { CubeSession, EXPIRED_SESSION_MESSAGE } from './CubeSession';

/** Waits until queued requests and zero-length animations have all run. */
async function settle(session: CubeSession) {
  await vi.waitFor(() => {
    const { busy, animation } = session.getSnapshot();
    expect(busy || animation !== null).toBe(false);
  });
}

const after = (sequence: string) => applyMoves(solvedState(), parseMoves(sequence));

describe('CubeSession', () => {
  it('creates a server session on start, only once', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });

    void session.start();
    await session.start();

    expect(api.createSession).toHaveBeenCalledTimes(1);
    expect(session.getSnapshot().sessionId).toBe('session-1');
  });

  it('opens an existing session with its state and history', async () => {
    const api = createFakeApi();
    api.seedSession('shared', "F R'");
    const session = new CubeSession(api, { durationMs: 0 });

    await session.start('shared');

    const snapshot = session.getSnapshot();
    expect(snapshot.sessionId).toBe('shared');
    expect(formatMoves(snapshot.history)).toBe("F R'");
    expect(statesEqual(snapshot.display, after("F R'"))).toBe(true);
    expect(api.createSession).not.toHaveBeenCalled();
  });

  it('falls back to a new session when the shared one has expired', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });

    await session.start('gone');

    expect(session.getSnapshot().sessionId).toBe('session-1');
    expect(session.getSnapshot().error).toBe(EXPIRED_SESSION_MESSAGE);
  });

  it('sends each move to the session and takes the history from the server', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });
    await session.start();

    void session.turn({ face: 'F', turn: 'clockwise' });
    void session.turn({ face: 'R', turn: 'antiClockwise' });
    await settle(session);

    expect(api.sessionMoves.mock.calls).toEqual([
      ['session-1', 'F'],
      ['session-1', "R'"],
    ]);
    expect(formatMoves(session.getSnapshot().history)).toBe("F R'");
    expect(statesEqual(session.getSnapshot().display, after("F R'"))).toBe(true);
  });

  it('creates a session on demand if start has not run', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });

    await session.turn({ face: 'U', turn: 'clockwise' });

    expect(api.createSession).toHaveBeenCalledTimes(1);
    expect(api.sessionMoves).toHaveBeenCalledWith('session-1', 'U');
  });

  it('animates the moves the server reports, one at a time, in order', async () => {
    const session = new CubeSession(createFakeApi(), { durationMs: 20 });
    await session.start();
    const seen: string[] = [];
    session.subscribe(() => {
      const move = session.getSnapshot().animation?.move;
      if (move && seen.at(-1) !== `${move.face}${move.turn}`) seen.push(`${move.face}${move.turn}`);
    });

    void session.applySequence("U B' L", true);
    await settle(session);

    expect(seen).toEqual(['Uclockwise', 'BantiClockwise', 'Lclockwise']);
  });

  it('re-syncs the view to the server state after a batch', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });
    await session.start();
    const serverState = after('D2');
    api.sessionMoves.mockResolvedValueOnce({
      session: { id: 'session-1', state: serverState, history: ['F'], redoStack: [] },
      appliedMoves: ['F'],
    });
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});

    void session.turn({ face: 'F', turn: 'clockwise' });
    await settle(session);

    expect(statesEqual(session.getSnapshot().display, serverState)).toBe(true);
    expect(warn).toHaveBeenCalled();
    warn.mockRestore();
  });

  it('undoes and redoes on the server', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });
    await session.start();

    await session.turn({ face: 'F', turn: 'clockwise' });
    await session.undo();
    await settle(session);
    expect(api.sessionUndo).toHaveBeenCalledWith('session-1');
    expect(session.getSnapshot().history).toHaveLength(0);
    expect(formatMoves(session.getSnapshot().redoStack)).toBe('F');
    expect(statesEqual(session.getSnapshot().display, solvedState())).toBe(true);

    await session.redo();
    await settle(session);
    expect(formatMoves(session.getSnapshot().history)).toBe('F');
    expect(session.getSnapshot().redoStack).toHaveLength(0);
  });

  it('does not call undo or redo when there is nothing to do', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });
    await session.start();

    await session.undo();
    await session.redo();

    expect(api.sessionUndo).not.toHaveBeenCalled();
    expect(api.sessionRedo).not.toHaveBeenCalled();
    expect(session.getSnapshot().error).toBeNull();
  });

  it('returns invalid tokens from applySequence instead of raising a banner', async () => {
    const session = new CubeSession(createFakeApi(), { durationMs: 0 });
    await session.start();

    const outcome = await session.applySequence('F X U', false);

    expect(outcome).toMatchObject({ ok: false, token: 'X', position: 2 });
    expect(session.getSnapshot().error).toBeNull();
    expect(session.getSnapshot().history).toHaveLength(0);
  });

  it('starts a new session when the current one expires mid-use', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });
    await session.start();
    await session.turn({ face: 'F', turn: 'clockwise' });
    api.expireSession('session-1');

    await session.turn({ face: 'R', turn: 'clockwise' });

    const snapshot = session.getSnapshot();
    expect(snapshot.sessionId).toBe('session-2');
    expect(snapshot.error).toBe(EXPIRED_SESSION_MESSAGE);
    expect(snapshot.history).toHaveLength(0);
    expect(statesEqual(snapshot.display, solvedState())).toBe(true);
  });

  it('shows an error when the API is unreachable and keeps working afterwards', async () => {
    const api = createFakeApi();
    const session = new CubeSession(api, { durationMs: 0 });
    await session.start();
    api.sessionMoves.mockRejectedValueOnce(new ApiError('Cannot reach the API.', 0));

    await session.turn({ face: 'F', turn: 'clockwise' });
    expect(session.getSnapshot().error).toBe('Cannot reach the API.');
    expect(session.getSnapshot().history).toHaveLength(0);

    await session.turn({ face: 'R', turn: 'clockwise' });
    await settle(session);
    expect(session.getSnapshot().error).toBeNull();
    expect(formatMoves(session.getSnapshot().history)).toBe('R');
  });

  it('reset returns to solved and clears history', async () => {
    const session = new CubeSession(createFakeApi(), { durationMs: 0 });
    await session.start();
    await session.applySequence("F R' U", false);

    await session.reset();
    await settle(session);

    const snapshot = session.getSnapshot();
    expect(statesEqual(snapshot.display, solvedState())).toBe(true);
    expect(snapshot.history).toHaveLength(0);
    expect(snapshot.redoStack).toHaveLength(0);
  });

  it('scramble applies the API scramble to the session and returns its notation', async () => {
    const api = createFakeApi("R U2 F'");
    const session = new CubeSession(api, { durationMs: 0 });
    await session.start();

    await expect(session.scramble()).resolves.toBe("R U2 F'");
    await settle(session);
    expect(api.sessionMoves).toHaveBeenCalledWith('session-1', "R U2 F'");
    expect(formatMoves(session.getSnapshot().history)).toBe("R U2 F'");
  });

  describe('solve', () => {
    it('applies the suggested moves to the session and plays them stage by stage', async () => {
      const api = createFakeApi();
      api.seedSession('scrambled', "F R' U B'");
      const session = new CubeSession(api, { durationMs: 5 });
      await session.start('scrambled');
      const seen: string[] = [];
      session.subscribe(() => {
        const current = session.getSnapshot().solution?.current;
        const label = current ? `${current.step}.${current.move}` : null;
        if (label && seen.at(-1) !== label) seen.push(label);
      });

      await session.solve();
      expect(session.getSnapshot().solution?.done).toBe(false);
      await settle(session);

      const snapshot = session.getSnapshot();
      expect(api.solveSession).toHaveBeenCalledWith('scrambled');
      expect(api.sessionMoves).toHaveBeenCalledWith('scrambled', "B U' R F'");
      // Two moves in stage 0, nothing in stage 1, two in stage 2.
      expect(seen).toEqual(['0.0', '0.1', '2.0', '2.1']);
      expect(snapshot.solution).toMatchObject({ totalMoves: 4, current: null, done: true });
      expect(statesEqual(snapshot.display, solvedState())).toBe(true);
      expect(formatMoves(snapshot.history)).toBe("F R' U B' B U' R F'");
    });

    it('reports an already solved cube without sending moves', async () => {
      const api = createFakeApi();
      const session = new CubeSession(api, { durationMs: 0 });
      await session.start();

      await session.solve();

      expect(session.getSnapshot().solution).toMatchObject({ totalMoves: 0, done: true });
      expect(api.sessionMoves).not.toHaveBeenCalled();
    });

    it('is cleared by the next action, and an action queued behind it cancels it', async () => {
      const api = createFakeApi();
      api.seedSession('scrambled', 'R');
      const session = new CubeSession(api, { durationMs: 0 });
      await session.start('scrambled');

      await session.solve();
      await settle(session);
      void session.turn({ face: 'F', turn: 'clockwise' });
      expect(session.getSnapshot().solution).toBeNull();
      await settle(session);

      void session.solve();
      await session.turn({ face: 'U', turn: 'clockwise' });
      await settle(session);
      expect(session.getSnapshot().solution).toBeNull();
      expect(formatMoves(session.getSnapshot().history)).toBe("R R' F U");
    });

    it('can be dismissed', async () => {
      const api = createFakeApi();
      const session = new CubeSession(api, { durationMs: 0 });
      await session.start();
      await session.solve();

      session.dismissSolution();

      expect(session.getSnapshot().solution).toBeNull();
    });
  });
});
