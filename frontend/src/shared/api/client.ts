import { ApiError, networkError } from './ApiError'
import { session } from './session'
import type { AuthResponse } from './types'

export const API_BASE = '/api/v1'

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  /** Attach the access token and transparently refresh it once on 401. Defaults to true. */
  auth?: boolean
}

let refreshInFlight: Promise<AuthResponse | null> | null = null

/**
 * Rotates the refresh-token cookie and stores the new access token.
 * Single-flight: concurrent callers share one request, because the backend treats a replayed
 * (already rotated) refresh token as theft and revokes the whole session.
 */
export function refreshSession(): Promise<AuthResponse | null> {
  refreshInFlight ??= (async () => {
    try {
      const response = await send(`${API_BASE}/auth/refresh`, { method: 'POST' }, null)
      if (!response.ok) {
        session.set(null)
        return null
      }
      const auth = (await response.json()) as AuthResponse
      session.set(auth)
      return auth
    } catch {
      session.set(null)
      return null
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = true } = options
  const init: RequestInit = { method }
  if (body !== undefined) {
    init.body = JSON.stringify(body)
  }

  let response = await sendOrThrow(`${API_BASE}${path}`, init, auth ? session.accessToken() : null)

  if (response.status === 401 && auth) {
    const refreshed = await refreshSession()
    if (refreshed !== null) {
      response = await sendOrThrow(`${API_BASE}${path}`, init, refreshed.accessToken)
    }
  }

  if (!response.ok) {
    throw await ApiError.fromResponse(response)
  }

  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}

async function sendOrThrow(url: string, init: RequestInit, accessToken: string | null): Promise<Response> {
  try {
    return await send(url, init, accessToken)
  } catch {
    throw networkError()
  }
}

function send(url: string, init: RequestInit, accessToken: string | null): Promise<Response> {
  const headers = new Headers({ Accept: 'application/json' })
  if (init.body !== undefined) {
    headers.set('Content-Type', 'application/json')
  }
  if (accessToken !== null) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }
  // Absolute same-origin URL; credentials 'same-origin' sends the refresh cookie only to our own API.
  return fetch(new URL(url, window.location.origin), { ...init, headers, credentials: 'same-origin' })
}
