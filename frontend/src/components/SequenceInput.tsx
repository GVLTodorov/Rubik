import { useId } from 'react';
import { tokenize } from '../cube/notation';

export interface SequenceError {
  message: string;
  token?: string;
  position?: number;
}

interface SequenceInputProps {
  value: string;
  error: SequenceError | null;
  disabled: boolean;
  onChange: (value: string) => void;
  onApply: () => void;
  onPlay: () => void;
}

/** Move-sequence field with ✓ (apply at once) and ▶ (play move by move), as on ruwix. */
export function SequenceInput({
  value,
  error,
  disabled,
  onChange,
  onApply,
  onPlay,
}: SequenceInputProps) {
  const inputId = useId();
  const errorId = useId();
  const empty = value.trim() === '';

  return (
    <form
      className="sequence"
      onSubmit={(event) => {
        event.preventDefault();
        if (!empty) onApply();
      }}
    >
      <label htmlFor={inputId} className="visually-hidden">
        Move sequence
      </label>
      <div className="sequence-row">
        <input
          id={inputId}
          className="sequence-input"
          type="text"
          value={value}
          placeholder="e.g. F R' U B' L D'"
          spellCheck={false}
          autoComplete="off"
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          onChange={(event) => onChange(event.target.value)}
        />
        <button
          type="submit"
          className="icon-button"
          aria-label="Apply sequence"
          title="Apply sequence"
          disabled={disabled || empty}
        >
          ✓
        </button>
        <button
          type="button"
          className="icon-button"
          aria-label="Play sequence step by step"
          title="Play sequence step by step"
          disabled={disabled || empty}
          onClick={onPlay}
        >
          ▶
        </button>
      </div>
      {error ? (
        <p id={errorId} className="sequence-error" role="alert">
          <HighlightedSequence value={value} position={error.position} />
          <span>{error.message}</span>
        </p>
      ) : null}
    </form>
  );
}

/** Shows the sequence with the offending token marked. */
function HighlightedSequence({ value, position }: { value: string; position?: number }) {
  if (position === undefined) return null;
  const tokens = tokenize(value);
  if (!tokens.some((token) => token.position === position)) return null;

  return (
    <code className="sequence-preview">
      {tokens.map((token, i) => (
        <span key={token.position}>
          {i > 0 ? ' ' : ''}
          {token.position === position ? <mark>{token.text}</mark> : token.text}
        </span>
      ))}
    </code>
  );
}
