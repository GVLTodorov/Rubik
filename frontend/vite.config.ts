/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react';
import { defineConfig, loadEnv } from 'vite';

// The backend's fixed dev URL (Rubik.Api launchSettings.json).
const DEFAULT_API_URL = 'http://localhost:5080';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiTarget = env.VITE_API_BASE_URL || DEFAULT_API_URL;

  return {
    plugins: [react()],
    // three.js alone is ~1 MB minified (~300 kB gzipped); that is expected for a 3D app.
    build: { chunkSizeWarningLimit: 1500 },
    server: {
      port: 5173,
      strictPort: true,
      // With no VITE_API_BASE_URL the app calls relative URLs and Vite forwards them here.
      proxy: {
        '/api': apiTarget,
        '/health': apiTarget,
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/test/setup.ts'],
      css: false,
      // `npm run test:coverage`; CI turns coverage/cobertura-coverage.xml into the README badges.
      // Cube3D is the WebGL scene: jsdom can't render it (App tests mock it), and its maths is in
      // cube/geometry.ts, which is covered.
      coverage: {
        provider: 'v8',
        include: ['src/**/*.{ts,tsx}'],
        exclude: [
          'src/**/*.test.{ts,tsx}',
          'src/test/**',
          'src/main.tsx',
          'src/vite-env.d.ts',
          'src/components/Cube3D.tsx',
        ],
        reporter: ['text-summary', 'html', 'cobertura'],
      },
    },
  };
});
