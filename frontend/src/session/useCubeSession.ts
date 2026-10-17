import { useSyncExternalStore } from 'react';
import type { CubeSession, SessionSnapshot } from './CubeSession';

export function useCubeSession(session: CubeSession): SessionSnapshot {
  return useSyncExternalStore(session.subscribe, session.getSnapshot);
}
