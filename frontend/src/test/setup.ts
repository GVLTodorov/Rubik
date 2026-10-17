import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

afterEach(() => {
  cleanup();
  // The app mirrors its moves into the URL; start every test from a clean address.
  window.history.replaceState(null, '', '/');
});
