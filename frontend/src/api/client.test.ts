import { describe, expect, it, vi } from 'vitest';
import { solvedState } from '../cube/model';
import { ApiError, createCubeApi } from './client';

function jsonResponse(body: unknown, status = 200, contentType = 'application/json') {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } });
}

describe('createCubeApi', () => {
  it('passes scramble length and seed as query parameters', async () => {
    const fetchMock = vi.fn(async () => jsonResponse({ moves: "R U2 F'" }));

    await expect(createCubeApi('', fetchMock).getScramble(3, 42)).resolves.toBe("R U2 F'");
    expect(fetchMock).toHaveBeenCalledWith('/api/cube/scramble?length=3&seed=42', undefined);
  });

  it('calls the session endpoints with the session id', async () => {
    const session = { id: 'abc', state: solvedState(), history: [], redoStack: [] };
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, _init?: RequestInit) =>
      jsonResponse({ session, appliedMoves: [] }),
    );
    const api = createCubeApi('', fetchMock);

    await api.createSession();
    await api.getSession('abc');
    await api.sessionMoves('abc', "F R'");
    await api.sessionUndo('abc');
    await api.sessionRedo('abc');
    await api.sessionReset('abc');

    expect(fetchMock.mock.calls.map(([url, init]) => `${init?.method ?? 'GET'} ${url}`)).toEqual([
      'POST /api/cube',
      'GET /api/cube/abc',
      'POST /api/cube/abc/moves',
      'POST /api/cube/abc/undo',
      'POST /api/cube/abc/redo',
      'POST /api/cube/abc/reset',
    ]);
    expect(JSON.parse(fetchMock.mock.calls[2]![1]?.body as string)).toEqual({ moves: "F R'" });
  });

  it("asks the solver for a session's solution", async () => {
    const solution = { moves: 'R', moveCount: 1, steps: [] };
    const fetchMock = vi.fn(async () => jsonResponse(solution));

    expect(await createCubeApi('', fetchMock).solveSession('a b')).toEqual(solution);
    expect(fetchMock).toHaveBeenCalledWith('/api/cube/a%20b/solution', undefined);
  });

  it('flags a 404 (e.g. an expired session) as not found', async () => {
    const fetchMock = vi.fn(async () =>
      jsonResponse({ status: 404, detail: 'Session not found.' }, 404),
    );

    const error = await createCubeApi('', fetchMock)
      .getSession('gone')
      .catch((e: unknown) => e);

    expect((error as ApiError).isNotFound).toBe(true);
    expect((error as ApiError).message).toBe('Session not found.');
  });

  it('turns problem details into an ApiError with the offending token', async () => {
    const problem = {
      status: 400,
      title: 'Invalid move',
      detail: "Invalid move 'X'",
      token: 'X',
      position: 2,
    };
    const fetchMock = vi.fn(async () => jsonResponse(problem, 400, 'application/problem+json'));

    const error = await createCubeApi('', fetchMock)
      .sessionMoves('abc', 'F X')
      .catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 400, message: "Invalid move 'X'", problem });
    expect((error as ApiError).isValidationError).toBe(true);
  });

  it('uses the first validation error when there is no detail', async () => {
    const problem = {
      status: 400,
      title: 'One or more validation errors occurred.',
      errors: { length: ['Too long.'] },
    };
    const fetchMock = vi.fn(async () => jsonResponse(problem, 400));

    await expect(createCubeApi('', fetchMock).getScramble(500)).rejects.toThrow('Too long.');
  });

  it('reports an unreachable API as a network error', async () => {
    const fetchMock = vi.fn(async () => {
      throw new TypeError('Failed to fetch');
    });

    const error = await createCubeApi('http://localhost:5080', fetchMock)
      .createSession()
      .catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).isNetworkError).toBe(true);
    expect((error as ApiError).message).toContain('http://localhost:5080');
  });
});
