import { useId } from 'react';

export const MIN_ANIMATION_MS = 50;
export const MAX_ANIMATION_MS = 1000;

export function SpeedControl({
  value,
  onChange,
}: {
  value: number;
  onChange: (ms: number) => void;
}) {
  const id = useId();
  return (
    <div className="speed">
      <label htmlFor={id}>Turn speed</label>
      <input
        id={id}
        type="range"
        min={MIN_ANIMATION_MS}
        max={MAX_ANIMATION_MS}
        step={50}
        value={value}
        aria-valuetext={`${value} milliseconds per turn`}
        onChange={(event) => onChange(Number(event.target.value))}
      />
      <span className="speed-value">{value} ms</span>
    </div>
  );
}
