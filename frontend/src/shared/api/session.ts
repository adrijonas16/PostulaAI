import type { AuthResponse } from './types'

/**
 * In-memory session. The access token is never written to localStorage/sessionStorage;
 * the refresh token lives only in an HttpOnly cookie the browser manages.
 */
type Listener = (session: AuthResponse | null) => void

let current: AuthResponse | null = null
const listeners = new Set<Listener>()

export const session = {
  get: (): AuthResponse | null => current,
  accessToken: (): string | null => current?.accessToken ?? null,
  set(next: AuthResponse | null): void {
    current = next
    for (const listener of listeners) {
      listener(current)
    }
  },
  subscribe(listener: Listener): () => void {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  },
}
