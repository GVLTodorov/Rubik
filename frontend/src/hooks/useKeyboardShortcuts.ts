import { useEffect, useRef } from 'react';
import { faceForKey } from '../cube/notation';
import type { Move } from '../cube/types';

interface ShortcutHandlers {
  onMove: (move: Move) => void;
  onUndo: () => void;
  onRedo: () => void;
}

/** F R U B L D turn clockwise, Shift + key anti-clockwise; Ctrl+Z / Ctrl+Y undo and redo. */
export function useKeyboardShortcuts(handlers: ShortcutHandlers): void {
  const latest = useRef(handlers);
  useEffect(() => {
    latest.current = handlers;
  });

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.defaultPrevented || isTypingTarget(event.target)) return;
      const key = event.key.toLowerCase();

      if (event.ctrlKey || event.metaKey) {
        if (key === 'z' && !event.shiftKey) {
          event.preventDefault();
          latest.current.onUndo();
        } else if (key === 'y' || (key === 'z' && event.shiftKey)) {
          event.preventDefault();
          latest.current.onRedo();
        }
        return;
      }

      const face = event.altKey ? null : faceForKey(event.key);
      if (face) {
        event.preventDefault();
        latest.current.onMove({ face, turn: event.shiftKey ? 'antiClockwise' : 'clockwise' });
      }
    }

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, []);
}

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  return (
    target.isContentEditable ||
    target instanceof HTMLInputElement ||
    target instanceof HTMLTextAreaElement ||
    target instanceof HTMLSelectElement
  );
}
