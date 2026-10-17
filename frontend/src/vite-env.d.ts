/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the Rubik API. Unset in development: relative URLs go through the Vite proxy. */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
