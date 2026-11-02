import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { postingSummary, sampleAnalysis, sampleProfile } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { api, problem, server, withAuthenticatedSession } from '../../test/server'

function withProfileAndPostings() {
  withAuthenticatedSession()
  server.use(
    http.get(api('/profiles/me'), () => HttpResponse.json(sampleProfile)),
    http.get(api('/job-postings'), () => HttpResponse.json({ items: [postingSummary(1), postingSummary(2)], page: 1, pageSize: 50, totalCount: 2 })),
  )
}

describe('run analysis', () => {
  it('asks for a profile first when there is none', async () => {
    withAuthenticatedSession()
    renderApp('/app/analisis/nuevo')

    expect(await screen.findByRole('link', { name: /completar mi perfil/i })).toHaveAttribute('href', '/app/perfil')
    expect(screen.queryByRole('button', { name: /analizar compatibilidad/i })).not.toBeInTheDocument()
  })

  it('preselects the posting from the URL, sends it and opens the result', async () => {
    const user = userEvent.setup()
    withProfileAndPostings()
    let body: unknown = null
    server.use(
      http.post(api('/analyses'), async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(sampleAnalysis, { status: 201 })
      }),
      http.get(api(`/analyses/${sampleAnalysis.id}`), () => HttpResponse.json(sampleAnalysis)),
    )
    const { router } = renderApp(`/app/analisis/nuevo?vacante=${postingSummary(2).id}`)

    expect(await screen.findByLabelText('Vacante')).toHaveValue(postingSummary(2).id)
    expect(await screen.findByText(/te quedan/i)).toHaveTextContent('Te quedan 10 de 10 análisis')
    await user.click(screen.getByRole('button', { name: /analizar compatibilidad/i }))

    await waitFor(() => expect(router.state.location.pathname).toBe(`/app/analisis/${sampleAnalysis.id}`))
    expect(body).toEqual({ jobPostingId: postingSummary(2).id, language: 'es' })
  })

  it('explains a missing API key in plain words', async () => {
    const user = userEvent.setup()
    withProfileAndPostings()
    server.use(
      http.post(api('/analyses'), () =>
        problem(503, 'ai_not_configured', 'El análisis con IA no está configurado en este servidor. Falta la API key de Anthropic.'),
      ),
    )
    renderApp('/app/analisis/nuevo')

    await user.click(await screen.findByRole('button', { name: /analizar compatibilidad/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/falta la api key de anthropic/i)
  })

  it('disables the button when the usage limit is reached', async () => {
    withProfileAndPostings()
    server.use(http.get(api('/usage/me'), () => HttpResponse.json({ limit: 3, used: 3, remaining: 0, windowHours: 24, nextSlotAt: '2026-11-03T10:00:00Z' })))
    renderApp('/app/analisis/nuevo')

    expect(await screen.findByText(/alcanzaste el límite/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /analizar compatibilidad/i })).toBeDisabled()
  })
})

describe('analysis result', () => {
  it('shows the band with requirement counts, never a bare percentage, plus every section and the disclaimer', async () => {
    withAuthenticatedSession()
    server.use(http.get(api(`/analyses/${sampleAnalysis.id}`), () => HttpResponse.json(sampleAnalysis)))
    renderApp(`/app/analisis/${sampleAnalysis.id}`)

    const score = await screen.findByRole('region', { name: 'Compatibilidad alta' })
    expect(within(score).getByText(/78\/100/)).toBeInTheDocument()
    expect(within(score).getByText(/es una estimación/i)).toBeInTheDocument()
    expect(within(score).getByText('1 cumplidos · 0 por trabajar')).toBeInTheDocument()

    const strengths = screen.getByRole('region', { name: /fortalezas/i })
    expect(within(strengths).getByText(/evidencia en tu perfil: desarrolladora backend/i)).toBeInTheDocument()
    expect(within(strengths).getByText('Obligatorio')).toBeInTheDocument()

    const gaps = screen.getByRole('region', { name: /brechas/i })
    expect(within(gaps).getByText('Habilidad faltante')).toBeInTheDocument()
    expect(within(gaps).getByText('Deseable')).toBeInTheDocument()

    expect(within(screen.getByRole('list', { name: 'Palabras clave' })).getAllByRole('listitem')).toHaveLength(2)
    expect(screen.getByRole('button', { name: /copiar resumen profesional/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /copiar carta de presentación/i })).toBeInTheDocument()
    expect(screen.getByRole('complementary', { name: /aviso de transparencia/i })).toHaveTextContent(/no garantiza contratación/i)
  })

  it('explains a failed analysis and offers a retry for the same posting', async () => {
    withAuthenticatedSession()
    const failed = { ...sampleAnalysis, status: 'failed' as const, result: null, score: null, errorCode: 'ai_invalid_output' }
    server.use(http.get(api(`/analyses/${failed.id}`), () => HttpResponse.json(failed)))
    renderApp(`/app/analisis/${failed.id}`)

    expect(await screen.findByRole('alert')).toHaveTextContent(/no guardamos nada incompleto/i)
    expect(screen.getByRole('link', { name: /intentar de nuevo/i })).toHaveAttribute('href', `/app/analisis/nuevo?vacante=${sampleAnalysis.jobPostingId}`)
    expect(screen.queryByRole('region', { name: /fortalezas/i })).not.toBeInTheDocument()
  })

  it('shows not found for analyses of other users', async () => {
    withAuthenticatedSession()
    server.use(http.get(api('/analyses/ajeno'), () => problem(404, 'analysis_not_found', 'No encontramos ese análisis.')))
    renderApp('/app/analisis/ajeno')

    expect(await screen.findByRole('alert')).toHaveTextContent('No encontramos ese análisis.')
  })
})

describe('analysis history', () => {
  it('lists analyses with band or failure and deletes one', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let deleted = false
    server.use(
      http.get(api('/analyses'), () =>
        HttpResponse.json({
          items: deleted
            ? []
            : [
                { id: 'a1', status: 'completed', jobPostingId: null, jobTitle: 'Backend .NET', jobCompany: 'Empresa 1', compatibilityScore: 78, scoreBand: 'high', errorCode: null, createdAt: '2026-11-02T20:00:00Z' },
                { id: 'a2', status: 'failed', jobPostingId: null, jobTitle: 'Analista', jobCompany: null, compatibilityScore: null, scoreBand: null, errorCode: 'ai_unavailable', createdAt: '2026-11-01T20:00:00Z' },
              ],
          page: 1,
          pageSize: 10,
          totalCount: deleted ? 0 : 2,
        }),
      ),
      http.delete(api('/analyses/a1'), () => {
        deleted = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    renderApp('/app/analisis')

    const list = await screen.findByRole('list', { name: /análisis realizados/i })
    expect(within(list).getByText(/compatibilidad alta \(78\/100\)/i)).toBeInTheDocument()
    expect(within(list).getByText(/no se completó/i)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /eliminar análisis de backend .net/i }))

    expect(await screen.findByText('Análisis eliminado.')).toBeInTheDocument()
    expect(await screen.findByText(/aún no has hecho análisis/i)).toBeInTheDocument()
  })
})
