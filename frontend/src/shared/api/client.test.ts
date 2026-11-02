import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { authResponse } from '../../test/fixtures'
import { api, problem, server } from '../../test/server'
import { ApiError } from './ApiError'
import { apiRequest, refreshSession } from './client'
import { session } from './session'

describe('refreshSession', () => {
  it('shares a single in-flight request between concurrent callers', async () => {
    let calls = 0
    server.use(
      http.post(api('/auth/refresh'), async () => {
        calls += 1
        await new Promise((resolve) => setTimeout(resolve, 10))
        return HttpResponse.json(authResponse('rotated'))
      }),
    )

    const [first, second] = await Promise.all([refreshSession(), refreshSession()])

    expect(calls).toBe(1)
    expect(first?.accessToken).toBe('rotated')
    expect(second).toBe(first)
    expect(session.accessToken()).toBe('rotated')
  })

  it('clears the session when the refresh token is rejected', async () => {
    session.set(authResponse('stale'))

    const result = await refreshSession()

    expect(result).toBeNull()
    expect(session.get()).toBeNull()
  })
})

describe('apiRequest', () => {
  it('sends the access token and retries once with a refreshed token after a 401', async () => {
    session.set(authResponse('expired'))
    const seenTokens: (string | null)[] = []
    server.use(
      http.get(api('/auth/me'), ({ request }) => {
        const header = request.headers.get('Authorization')
        seenTokens.push(header)
        return header === 'Bearer fresh' ? HttpResponse.json({ ok: true }) : problem(401, 'unauthorized', 'No autorizado')
      }),
      http.post(api('/auth/refresh'), () => HttpResponse.json(authResponse('fresh'))),
    )

    const result = await apiRequest<{ ok: boolean }>('/auth/me')

    expect(result).toEqual({ ok: true })
    expect(seenTokens).toEqual(['Bearer expired', 'Bearer fresh'])
  })

  it('turns ProblemDetails into an ApiError with code and field errors', async () => {
    server.use(
      http.put(api('/profiles/me'), () =>
        problem(400, 'validation_failed', 'Revisa los datos enviados.', { 'experiences[0].endDate': ['Fecha inválida.'] }),
      ),
    )

    const error = await apiRequest('/profiles/me', { method: 'PUT', body: {} }).catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(ApiError)
    const apiError = error as ApiError
    expect(apiError.status).toBe(400)
    expect(apiError.errorCode).toBe('validation_failed')
    expect(apiError.message).toBe('Revisa los datos enviados.')
    expect(apiError.fieldErrors['experiences[0].endDate']).toEqual(['Fecha inválida.'])
  })

  it('reports network failures with a friendly message', async () => {
    server.use(http.get(api('/auth/me'), () => HttpResponse.error()))

    const error = await apiRequest('/auth/me', { auth: false }).catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).errorCode).toBe('network_error')
  })

  it('never stores the access token in web storage', async () => {
    server.use(http.post(api('/auth/refresh'), () => HttpResponse.json(authResponse('in-memory-only'))))

    await refreshSession()

    expect(JSON.stringify({ ...localStorage })).not.toContain('in-memory-only')
    expect(JSON.stringify({ ...sessionStorage })).not.toContain('in-memory-only')
  })
})
