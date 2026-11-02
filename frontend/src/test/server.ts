import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { authResponse } from './fixtures'

export const api = (path: string) => `/api/v1${path}`

export const problem = (status: number, errorCode: string, title: string, errors?: Record<string, string[]>) =>
  HttpResponse.json({ status, title, errorCode, ...(errors === undefined ? {} : { errors }) }, {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })

/** Default: no session cookie, so the user starts anonymous. */
export const server = setupServer(
  http.post(api('/auth/refresh'), () => problem(401, 'invalid_refresh_token', 'Tu sesión expiró.')),
  http.post(api('/auth/logout'), () => new HttpResponse(null, { status: 204 })),
  http.get(api('/profiles/me'), () => problem(404, 'profile_not_found', 'Todavía no has creado tu perfil profesional.')),
  http.get(api('/job-postings'), ({ request }) => {
    const url = new URL(request.url)
    return HttpResponse.json({ items: [], page: Number(url.searchParams.get('page') ?? '1'), pageSize: Number(url.searchParams.get('pageSize') ?? '10'), totalCount: 0 })
  }),
)

/** Simulates a browser that already has a valid refresh cookie. */
export function withAuthenticatedSession(): void {
  server.use(http.post(api('/auth/refresh'), () => HttpResponse.json(authResponse())))
}
