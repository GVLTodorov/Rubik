import { vi } from 'vitest';
import {
  ApiError,
  type CubeApi,
  type SessionDto,
  type SessionUpdate,
  type Solution,
} from '../api/client';
import { applyMoves, solvedState } from '../cube/model';
import { formatMove, invertMove, parseMove, parseMoves, tokenize } from '../cube/notation';
import type { Colour, CubeState, Face, Move } from '../cube/types';

interface FakeSession {
  state: CubeState;
  history: Move[];
  redoStack: Move[];
}

function invalidMove(text: string, position: number): ApiError {
  const detail = `Invalid move '${text}' at position ${position}.`;
  return new ApiError(detail, 400, { status: 400, detail, token: text, position });
}

function parseSequence(sequence: string): Move[] {
  return tokenize(sequence).map(({ text, position }) => {
    const move = parseMove(text);
    if (!move) throw invalidMove(text, position);
    return move;
  });
}

/**
 * An in-memory stand-in for the backend, with spies on every call. Sessions behave like
 * `/api/cube/{id}`: unknown ids are 404, undo/redo with nothing to do are 409.
 */
export function createFakeApi(scramble = "R U2 F'") {
  const sessions = new Map<string, FakeSession>();
  let nextId = 1;

  const find = (id: string): FakeSession => {
    const session = sessions.get(id);
    if (!session) {
      throw new ApiError(`Session '${id}' was not found.`, 404, { status: 404 });
    }
    return session;
  };

  const dto = (id: string): SessionDto => {
    const { state, history, redoStack } = find(id);
    return {
      id,
      state,
      history: history.map(formatMove),
      redoStack: redoStack.map(formatMove),
    };
  };

  const turn = (id: string, moves: Move[], change: (s: FakeSession) => void): SessionUpdate => {
    const session = find(id);
    change(session);
    session.state = applyMoves(session.state, moves);
    return { session: dto(id), appliedMoves: moves.map(formatMove) };
  };

  const api = {
    getScramble: vi.fn(async () => scramble),

    createSession: vi.fn(async () => {
      const id = `session-${nextId++}`;
      sessions.set(id, { state: solvedState(), history: [], redoStack: [] });
      return dto(id);
    }),
    getSession: vi.fn(async (id: string) => dto(id)),
    sessionMoves: vi.fn(async (id: string, sequence: string) => {
      find(id);
      const moves = parseSequence(sequence);
      return turn(id, moves, (s) => {
        s.history = [...s.history, ...moves];
        if (moves.length > 0) s.redoStack = [];
      });
    }),
    sessionUndo: vi.fn(async (id: string) => {
      const last = find(id).history.at(-1);
      if (!last) throw new ApiError('There is no move to undo.', 409, { status: 409 });
      return turn(id, [invertMove(last)], (s) => {
        s.history = s.history.slice(0, -1);
        s.redoStack = [...s.redoStack, last];
      });
    }),
    sessionRedo: vi.fn(async (id: string) => {
      const next = find(id).redoStack.at(-1);
      if (!next) throw new ApiError('There is no move to redo.', 409, { status: 409 });
      return turn(id, [next], (s) => {
        s.history = [...s.history, next];
        s.redoStack = s.redoStack.slice(0, -1);
      });
    }),
    sessionReset: vi.fn(async (id: string) => {
      const session = find(id);
      session.state = solvedState();
      session.history = [];
      session.redoStack = [];
      return { session: dto(id), appliedMoves: [] };
    }),
    // Not a real solver: undoing the history solves any session, split over two stages.
    solveSession: vi.fn(async (id: string): Promise<Solution> => {
      const moves = [...find(id).history].reverse().map(invertMove).map(formatMove);
      const half = Math.ceil(moves.length / 2);
      const step = (stage: string, title: string, stepMoves: string[]) => ({
        stage,
        title,
        description: `${title} description`,
        moves: stepMoves,
      });
      return {
        moves: moves.join(' '),
        moveCount: moves.length,
        steps: [
          step('Cross', 'White cross', moves.slice(0, half)),
          step('MiddleLayer', 'Middle layer', []),
          step('LastLayerEdges', 'Yellow edges', moves.slice(half)),
        ],
      };
    }),
  } satisfies CubeApi;

  return Object.assign(api, {
    /** Seeds a session directly, e.g. to test opening a shared link. */
    seedSession(id: string, sequence: string) {
      const history = parseMoves(sequence);
      sessions.set(id, { state: applyMoves(solvedState(), history), history, redoStack: [] });
    },
    /** Forgets a session, as if it expired or the API restarted. */
    expireSession(id: string) {
      sessions.delete(id);
    },
  });
}

/** Parses "R O G B W W B B B" into a face's colour list. */
export function codes(text: string): Colour[] {
  return text.split(' ') as Colour[];
}

/** The brief's expected faces after F R' U B' L D'. */
export const ACCEPTANCE_FACES: Record<Face, string> = {
  U: 'R O G B W W B B B',
  L: 'G Y Y O O G B G O',
  F: 'O R R O G W W W W',
  R: 'Y B O R R W O Y R',
  B: 'Y B W O B Y Y Y W',
  D: 'G G B R Y R R G G',
};
