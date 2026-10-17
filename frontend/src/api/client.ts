import type { CubeState } from '../cube/types';

/** RFC 7807 problem details, as returned by the API for 4xx/5xx responses. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Invalid move token (POST /api/cube/{id}/moves). */
  token?: string;
  /** Zero-based character offset of `token` in the submitted sequence. */
  position?: number;
  /** Model-validation errors, keyed by field. */
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | undefined;

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }

  /** True for a 400: the input was wrong, not the server. */
  get isValidationError(): boolean {
    return this.status === 400;
  }

  /** True for a 404, e.g. an expired session. */
  get isNotFound(): boolean {
    return this.status === 404;
  }

  /** True when no response arrived at all. */
  get isNetworkError(): boolean {
    return this.status === 0;
  }
}

/** A server-side session: the cube and its undo/redo history, kept in the API's memory. */
export interface SessionDto {
  id: string;
  state: CubeState;
  history: string[];
  /** Undone moves; the last entry is redone first. */
  redoStack: string[];
}

export interface SessionUpdate {
  session: SessionDto;
  /** The moves actually turned (the inverse for an undo, nothing for a reset). */
  appliedMoves: string[];
}

/** One stage of the layer-by-layer method, as suggested by `/api/cube/{id}/solution`. */
export interface SolutionStep {
  /** e.g. `Cross`, `FirstLayerCorners`, … `LastLayerEdges`. */
  stage: string;
  /** A short name using the cube's colours, e.g. "White cross". */
  title: string;
  description: string;
  /** The stage's moves; empty when the stage was already done. */
  moves: string[];
}

export interface Solution {
  /** Every stage's moves, space-separated; empty when the cube is already solved. */
  moves: string;
  moveCount: number;
  steps: SolutionStep[];
}

export interface CubeApi {
  // Server-side sessions (/api/cube/{id}).
  createSession(): Promise<SessionDto>;
  getSession(id: string): Promise<SessionDto>;
  sessionMoves(id: string, moves: string): Promise<SessionUpdate>;
  sessionUndo(id: string): Promise<SessionUpdate>;
  sessionRedo(id: string): Promise<SessionUpdate>;
  sessionReset(id: string): Promise<SessionUpdate>;

  // Suggest moves, change nothing.
  getScramble(length?: number, seed?: number): Promise<string>;
  solveSession(id: string): Promise<Solution>;
}

type Fetch = typeof fetch;

/** Default base URL: `VITE_API_BASE_URL`, or same-origin (the Vite dev proxy) when unset. */
export const DEFAULT_API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? '';

export function createCubeApi(
  baseUrl: string = DEFAULT_API_BASE_URL,
  fetchImpl: Fetch = (...args) => fetch(...args),
): CubeApi {
  const root = baseUrl.replace(/\/+$/, '');

  async function send(path: string, init?: RequestInit): Promise<Response> {
    let response: Response;
    try {
      response = await fetchImpl(`${root}${path}`, init);
    } catch {
      throw new ApiError(
        `Cannot reach the API${root ? ` at ${root}` : ''}. Is the backend running?`,
        0,
      );
    }
    if (!response.ok) {
      const problem = await readProblem(response);
      throw new ApiError(describeProblem(problem, response), response.status, problem);
    }
    return response;
  }

  const postJson = (path: string, body: unknown) =>
    send(path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });

  const post = (path: string) => send(path, { method: 'POST' });
  const sessionPath = (id: string, action = '') =>
    `/api/cube/${encodeURIComponent(id)}${action ? `/${action}` : ''}`;

  return {
    async createSession() {
      return (await post('/api/cube')).json();
    },
    async getSession(id) {
      return (await send(sessionPath(id))).json();
    },
    async sessionMoves(id, moves) {
      return (await postJson(sessionPath(id, 'moves'), { moves })).json();
    },
    async sessionUndo(id) {
      return (await post(sessionPath(id, 'undo'))).json();
    },
    async sessionRedo(id) {
      return (await post(sessionPath(id, 'redo'))).json();
    },
    async sessionReset(id) {
      return (await post(sessionPath(id, 'reset'))).json();
    },
    async getScramble(length, seed) {
      const query = new URLSearchParams();
      if (length !== undefined) query.set('length', String(length));
      if (seed !== undefined) query.set('seed', String(seed));
      const suffix = query.size > 0 ? `?${query}` : '';
      const body: { moves: string } = await (await send(`/api/cube/scramble${suffix}`)).json();
      return body.moves;
    },
    async solveSession(id) {
      return (await send(sessionPath(id, 'solution'))).json();
    },
  };
}

async function readProblem(response: Response): Promise<ProblemDetails | undefined> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return undefined;
  }
}

function describeProblem(problem: ProblemDetails | undefined, response: Response): string {
  const firstFieldError = problem?.errors && Object.values(problem.errors).flat()[0];
  return (
    problem?.detail ??
    firstFieldError ??
    problem?.title ??
    `The API returned ${response.status} ${response.statusText}`.trim()
  );
}
