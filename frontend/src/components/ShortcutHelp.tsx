const SHORTCUTS: [keys: string, action: string][] = [
  ['F R U B L D', 'Turn that face clockwise'],
  ['Shift + F R U B L D', 'Turn that face anti-clockwise'],
  ['Ctrl + Z', 'Undo'],
  ['Ctrl + Y', 'Redo'],
  ['Drag', 'Look around the cube (3D view)'],
];

/** A "?" disclosure listing the keyboard shortcuts. */
export function ShortcutHelp() {
  return (
    <details className="shortcuts">
      <summary aria-label="Keyboard shortcuts" title="Keyboard shortcuts">
        ?
      </summary>
      <div className="shortcuts-panel">
        <h2>Keyboard shortcuts</h2>
        <dl>
          {SHORTCUTS.map(([keys, action]) => (
            <div key={keys}>
              <dt>
                <kbd>{keys}</kbd>
              </dt>
              <dd>{action}</dd>
            </div>
          ))}
        </dl>
      </div>
    </details>
  );
}
