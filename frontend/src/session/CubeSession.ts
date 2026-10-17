import { ApiError, type CubeApi, type SessionDto, type SessionUpdate } from '../api/client';
import { applyMove, solvedState, statesEqual } from '../cube/model';
import { formatMove, formatMoves, parseMoves } from '../cube/notation';
import type { CubeState, Move } from '../cube/types';

export const DEFAULT_ANIMATION_MS = 250;

export const EXPIRED_SESSION_MESSAGE =
  'Your session has expired or the API restarted, so a new session was started.';

export interface Animation {
  id: number;
  move: Move;
  /** `performance.now()`-style timestamp the animation started at. */
  startedAt: number;
  durationMs: number;
}

/** One stage of a suggested solution, with its moves parsed. */
export interface SolutionStepView {
  title: string;
  description: string;
  moves: readonly Move[];
}

/** A solution being played (or just played) on the cube. */
export interface SolutionProgress {
  steps: readonly SolutionStepView[];
  totalMoves: number;
  /** The stage and move on screen now; null before the first move starts and after the last. */
  current: { step: number; move: number } | null;
  /** True once every move has played, or straight away when there was nothing to do. */
  done: boolean;
}

export interface SessionSnapshot {
  /** The server session id, once one exists. */
  sessionId: string | null;
  /** The state to draw. While `animation` runs, this is the state *before* its move. */
  display: CubeState;
  animation: Animation | null;
  /** The server's history and redo stack, as of the last response. */
  history: readonly Move[];
  redoStack: readonly Move[];
  /** True while any API request is in flight. */
  busy: boolean;
  error: string | null;
  /** The last solution from `solve()`, until another action replaces or clears it. */
  solution: SolutionProgress | null;
}

export type SequenceOutcome =
  { ok: true; moves: Move[] } | { ok: false; message: string; token?: string; position?: number };

export interface Clock {
  now(): number;
  setTimeout(callback: () => void, ms: number): ReturnType<typeof setTimeout>;
  clearTimeout(handle: ReturnType<typeof setTimeout>): void;
}

const browserClock: Clock = {
  now: () => performance.now(),
  setTimeout: (callback, ms) => setTimeout(callback, ms),
  clearTimeout: (handle) => clearTimeout(handle),
};

/** Ties a queued move to its place in a solution, so the UI can follow along. */
interface SolutionMarker {
  solution: number;
  step: number;
  move: number;
  last: boolean;
}

type QueueItem =
  | { kind: 'move'; move: Move; sync?: CubeState; marker?: SolutionMarker }
  | { kind: 'set'; state: CubeState };

/**
 * Owns the cube as the UI sees it. The cube and its undo/redo history live in a server session
 * (`/api/cube/{id}`); the browser only keeps the id. Requests are sent one at a time, the moves
 * each response reports are animated locally, and the view re-syncs to the server's state at the
 * end of each batch.
 */
export class CubeSession {
  private snapshot: SessionSnapshot;
  private readonly listeners = new Set<() => void>();
  private chain: Promise<unknown> = Promise.resolve();
  private pending = 0;
  private queue: QueueItem[] = [];
  private timer: ReturnType<typeof setTimeout> | null = null;
  private nextAnimationId = 1;
  /** Bumped by every solve and every other action, so markers of an old solution are ignored. */
  private solutionId = 0;
  private durationMs: number;
  private readonly api: CubeApi;
  private readonly clock: Clock;

  constructor(api: CubeApi, options: { durationMs?: number; clock?: Clock } = {}) {
    this.api = api;
    this.clock = options.clock ?? browserClock;
    this.durationMs = options.durationMs ?? DEFAULT_ANIMATION_MS;
    this.snapshot = {
      sessionId: null,
      display: solvedState(),
      animation: null,
      history: [],
      redoStack: [],
      busy: false,
      error: null,
      solution: null,
    };
  }

  readonly subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  readonly getSnapshot = (): SessionSnapshot => this.snapshot;

  get animationMs(): number {
    return this.durationMs;
  }

  setAnimationMs(ms: number): void {
    this.durationMs = Math.max(0, ms);
  }

  /**
   * Opens the given session (e.g. from a shared link), or creates a new one. An unknown or expired
   * id falls back to a new session. Calling it again once a session is open does nothing.
   */
  start(sessionId?: string | null): Promise<void> {
    return this.run(async () => {
      if (this.snapshot.sessionId) return;
      if (sessionId) {
        try {
          this.adopt(await this.api.getSession(sessionId));
          return;
        } catch (error) {
          if (!(error instanceof ApiError && error.isNotFound)) throw error;
          this.update({ error: EXPIRED_SESSION_MESSAGE });
        }
      }
      this.adopt(await this.api.createSession());
    });
  }

  turn(move: Move): Promise<void> {
    this.clearSolution();
    return this.run(async () => {
      const id = await this.ensureSession();
      this.applyUpdate(await this.api.sessionMoves(id, formatMove(move)), true);
    });
  }

  undo(): Promise<void> {
    this.clearSolution();
    return this.run(async () => {
      if (this.snapshot.history.length === 0) return;
      const id = await this.ensureSession();
      this.applyUpdate(await this.api.sessionUndo(id), true);
    });
  }

  redo(): Promise<void> {
    this.clearSolution();
    return this.run(async () => {
      if (this.snapshot.redoStack.length === 0) return;
      const id = await this.ensureSession();
      this.applyUpdate(await this.api.sessionRedo(id), true);
    });
  }

  /**
   * Applies a typed sequence. With `animate` each move plays in turn; without it the cube jumps
   * straight to the result. Invalid input is returned (with the offending token), not raised.
   */
  async applySequence(sequence: string, animate: boolean): Promise<SequenceOutcome> {
    this.clearSolution();
    const outcome = await this.run(async (): Promise<SequenceOutcome> => {
      const id = await this.ensureSession();
      try {
        return {
          ok: true,
          moves: this.applyUpdate(await this.api.sessionMoves(id, sequence), animate),
        };
      } catch (error) {
        if (error instanceof ApiError && error.isValidationError) {
          return {
            ok: false,
            message: error.message,
            token: error.problem?.token,
            position: error.problem?.position,
          };
        }
        throw error;
      }
    });
    return outcome ?? { ok: false, message: this.snapshot.error ?? 'The request failed.' };
  }

  /** Fetches a scramble from the API, animates it, and returns its notation. */
  async scramble(): Promise<string | undefined> {
    this.clearSolution();
    return this.run(async () => {
      const id = await this.ensureSession();
      const moves = await this.api.getScramble();
      return formatMoves(this.applyUpdate(await this.api.sessionMoves(id, moves), true));
    });
  }

  /** Back to solved, with history cleared and any queued animations dropped. */
  reset(): Promise<void> {
    this.clearSolution();
    return this.run(async () => {
      const id = await this.ensureSession();
      const update = await this.api.sessionReset(id);
      this.clearQueue();
      this.applyUpdate(update, false);
    });
  }

  /**
   * Asks the API for the layer-by-layer solution of the session's cube, applies it to the session
   * (so undo still works move by move), and plays it one move at a time at the current turn speed.
   * `solution` in the snapshot tracks which stage and move is on screen.
   */
  solve(): Promise<void> {
    this.clearSolution();
    const solutionId = this.solutionId;
    return this.run(async () => {
      const id = await this.ensureSession();
      const suggested = await this.api.solveSession(id);
      const steps: SolutionStepView[] = suggested.steps.map((step) => ({
        title: step.title,
        description: step.description,
        moves: parseMoves(step.moves),
      }));
      const totalMoves = steps.reduce((sum, step) => sum + step.moves.length, 0);
      if (solutionId !== this.solutionId) return;
      if (totalMoves === 0) {
        this.update({ solution: { steps, totalMoves, current: null, done: true } });
        return;
      }

      const update = await this.api.sessionMoves(id, suggested.moves);
      let index = 0;
      const markers = steps.flatMap((step, s) =>
        step.moves.map((_, m) => ({
          solution: solutionId,
          step: s,
          move: m,
          last: ++index === totalMoves,
        })),
      );
      this.update({ solution: { steps, totalMoves, current: null, done: false } });
      this.applyUpdate(update, true, markers);
    });
  }

  /** Hides the solution panel; any moves still playing carry on. */
  dismissSolution(): void {
    this.clearSolution();
  }

  dismissError(): void {
    this.update({ error: null });
  }

  /** Stops pending animations. Subscribers stay attached (React StrictMode re-runs effects). */
  dispose(): void {
    this.clearQueue();
    this.update({ animation: null });
  }

  private async ensureSession(): Promise<string> {
    if (this.snapshot.sessionId) return this.snapshot.sessionId;
    const session = await this.api.createSession();
    this.adopt(session);
    return session.id;
  }

  /** Shows a session as-is: its state, history and redo stack, without animation. */
  private adopt(session: SessionDto): void {
    this.clearQueue();
    this.update({
      sessionId: session.id,
      display: session.state,
      animation: null,
      history: parseMoves(session.history),
      redoStack: parseMoves(session.redoStack),
    });
  }

  /** Takes the server's history and queues the turned moves (or a jump) to reach its state. */
  private applyUpdate(
    update: SessionUpdate,
    animate: boolean,
    markers?: readonly SolutionMarker[],
  ): Move[] {
    const moves = parseMoves(update.appliedMoves);
    this.update({
      history: parseMoves(update.session.history),
      redoStack: parseMoves(update.session.redoStack),
    });
    if (animate) {
      this.enqueueMoves(moves, update.session.state, markers);
    } else {
      this.enqueueSet(update.session.state);
    }
    return moves;
  }

  /** Runs API work strictly one at a time; failures go to the error banner. */
  private run<T>(work: () => Promise<T>): Promise<T | undefined> {
    this.setPending(1);
    const result = this.chain
      .then(work)
      .catch(async (error: unknown) => {
        if (error instanceof ApiError && error.isNotFound && this.snapshot.sessionId) {
          await this.replaceExpiredSession();
        } else {
          this.update({ error: describeError(error) });
        }
        return undefined;
      })
      .finally(() => this.setPending(-1));
    this.chain = result;
    return result;
  }

  /** The server forgot the session (expiry or restart): start a fresh one and say so. */
  private async replaceExpiredSession(): Promise<void> {
    try {
      this.adopt(await this.api.createSession());
      this.update({ error: EXPIRED_SESSION_MESSAGE });
    } catch (error) {
      this.update({ sessionId: null, error: describeError(error) });
    }
  }

  private setPending(delta: number): void {
    this.pending += delta;
    const busy = this.pending > 0;
    if (busy !== this.snapshot.busy) {
      // A new action clears the previous error; this action's own error is set afterwards.
      this.update({ busy, ...(busy ? { error: null } : {}) });
    }
  }

  private enqueueMoves(
    moves: readonly Move[],
    finalState: CubeState,
    markers?: readonly SolutionMarker[],
  ): void {
    if (moves.length === 0) {
      this.enqueueSet(finalState);
      return;
    }
    moves.forEach((move, i) => {
      this.queue.push({
        kind: 'move',
        move,
        sync: i === moves.length - 1 ? finalState : undefined,
        marker: markers?.[i],
      });
    });
    this.pump();
  }

  private enqueueSet(state: CubeState): void {
    this.queue.push({ kind: 'set', state });
    this.pump();
  }

  /** Starts the next queued animation, applying any queued instant state changes first. */
  private pump(): void {
    if (this.snapshot.animation) return;

    let display = this.snapshot.display;
    let head = this.queue[0];
    while (head?.kind === 'set') {
      display = head.state;
      this.queue.shift();
      head = this.queue[0];
    }

    if (!head) {
      this.update({ display, animation: null });
      return;
    }

    const animation: Animation = {
      id: this.nextAnimationId++,
      move: head.move,
      startedAt: this.clock.now(),
      durationMs: this.durationMs,
    };
    this.update({ display, animation, solution: this.solutionAt(head.marker, false) });
    this.timer = this.clock.setTimeout(() => this.finishAnimation(), this.durationMs);
  }

  private finishAnimation(): void {
    this.timer = null;
    const head = this.queue.shift();
    if (head?.kind !== 'move') return;

    let display = applyMove(this.snapshot.display, head.move);
    if (head.sync) {
      if (!statesEqual(display, head.sync)) {
        console.warn('Local cube model disagreed with the server; re-synced to the server state.');
      }
      display = head.sync;
    }
    const solution = head.marker?.last
      ? this.solutionAt(head.marker, true)
      : this.snapshot.solution;
    this.snapshot = { ...this.snapshot, display, animation: null, solution };
    this.pump();
  }

  /** The solution with `marker`'s move on screen (or finished), if the marker is still current. */
  private solutionAt(
    marker: SolutionMarker | undefined,
    finished: boolean,
  ): SolutionProgress | null {
    const solution = this.snapshot.solution;
    if (!marker || !solution || marker.solution !== this.solutionId) return solution;
    return finished
      ? { ...solution, current: null, done: true }
      : { ...solution, current: { step: marker.step, move: marker.move } };
  }

  private clearSolution(): void {
    this.solutionId++;
    if (this.snapshot.solution) this.update({ solution: null });
  }

  private clearQueue(): void {
    if (this.timer !== null) {
      this.clock.clearTimeout(this.timer);
      this.timer = null;
    }
    this.queue = [];
    if (this.snapshot.animation) {
      this.snapshot = { ...this.snapshot, animation: null };
    }
  }

  private update(patch: Partial<SessionSnapshot>): void {
    this.snapshot = { ...this.snapshot, ...patch };
    this.listeners.forEach((listener) => listener());
  }
}

function describeError(error: unknown): string {
  if (error instanceof Error) return error.message;
  return 'Something went wrong.';
}
