/**
 * Shareable links: `?session=<id>&view=net` reopens a server-side session (its cube and history)
 * in that view. The moves themselves stay on the server, so the URL never grows.
 */
export interface UrlState {
  session: string | null;
  view: string | null;
}

const SESSION_PARAM = 'session';
const VIEW_PARAM = 'view';

export function readUrlState(search: string = window.location.search): UrlState {
  const params = new URLSearchParams(search);
  return { session: params.get(SESSION_PARAM)?.trim() || null, view: params.get(VIEW_PARAM) };
}

/** Updates the address bar without adding a browser history entry. */
export function writeUrlState({ session, view }: UrlState): void {
  const url = new URL(window.location.href);
  setOrDelete(url.searchParams, SESSION_PARAM, session);
  setOrDelete(url.searchParams, VIEW_PARAM, view);
  if (url.href !== window.location.href) {
    window.history.replaceState(null, '', url);
  }
}

function setOrDelete(params: URLSearchParams, key: string, value: string | null): void {
  if (value) {
    params.set(key, value);
  } else {
    params.delete(key);
  }
}
