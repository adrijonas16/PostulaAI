import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import type { JobPostingRequest } from '../../shared/api/types'
import { postingSummary, samplePosting } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { api, problem, server, withAuthenticatedSession } from '../../test/server'

const longDescription = 'Buscamos analista de datos con SQL avanzado, Python y experiencia en tableros de Power BI.'

describe('job posting editor', () => {
  it('creates a posting, sends the API contract and returns to the list with a notice', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let sent: JobPostingRequest | null = null
    server.use(
      http.post(api('/job-postings'), async ({ request }) => {
        sent = (await request.json()) as JobPostingRequest
        return HttpResponse.json({ ...samplePosting, ...sent }, { status: 201 })
      }),
    )
    const { router } = renderApp('/app/vacantes/nueva')

    await user.type(await screen.findByLabelText(/título del cargo/i), 'Analista de datos')
    await user.selectOptions(screen.getByLabelText(/modalidad/i), 'remote')
    await user.type(screen.getByLabelText(/descripción de la vacante/i), longDescription)
    await user.click(screen.getByRole('button', { name: /guardar vacante/i }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/app/vacantes'))
    expect(sent).toEqual({ title: 'Analista de datos', company: null, location: null, workMode: 'remote', description: longDescription, sourceUrl: null })
    expect(await screen.findByText('Vacante guardada.')).toBeInTheDocument()
  })

  it('validates before sending: required title, minimum description, http(s) link', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let calls = 0
    server.use(http.post(api('/job-postings'), () => { calls += 1; return HttpResponse.json(samplePosting, { status: 201 }) }))
    renderApp('/app/vacantes/nueva')

    await user.type(await screen.findByLabelText(/descripción de la vacante/i), 'Muy corta')
    await user.type(screen.getByLabelText(/enlace a la oferta/i), 'javascript:alert(1)')
    await user.click(screen.getByRole('button', { name: /guardar vacante/i }))

    expect(await screen.findByText('Ingresa el título de la vacante.')).toBeInTheDocument()
    expect(screen.getByText(/al menos 30 caracteres/i)).toBeInTheDocument()
    expect(screen.getByText(/empiece por http/i)).toBeInTheDocument()
    expect(calls).toBe(0)
  })

  it('loads an existing posting for editing and saves changes with PUT', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let method = ''
    server.use(
      http.get(api(`/job-postings/${samplePosting.id}`), () => HttpResponse.json(samplePosting)),
      http.put(api(`/job-postings/${samplePosting.id}`), async ({ request }) => {
        method = request.method
        return HttpResponse.json({ ...samplePosting, ...((await request.json()) as JobPostingRequest) })
      }),
    )
    renderApp(`/app/vacantes/${samplePosting.id}/editar`)

    const title = await screen.findByDisplayValue('Desarrolladora backend .NET')
    expect(screen.getByLabelText(/modalidad/i)).toHaveValue('hybrid')
    await user.clear(title)
    await user.type(title, 'Ingeniera backend')
    await user.click(screen.getByRole('button', { name: /guardar vacante/i }))

    expect(await screen.findByText('Vacante actualizada.')).toBeInTheDocument()
    expect(method).toBe('PUT')
  })

  it('shows a clear message when the posting does not exist (or belongs to someone else)', async () => {
    withAuthenticatedSession()
    server.use(http.get(api('/job-postings/desconocida'), () => problem(404, 'job_posting_not_found', 'No encontramos esa vacante.')))
    renderApp('/app/vacantes/desconocida/editar')

    expect(await screen.findByRole('alert')).toHaveTextContent('No encontramos esa vacante.')
    expect(screen.getByRole('link', { name: /volver a mis vacantes/i })).toBeInTheDocument()
  })
})

describe('job postings list', () => {
  it('shows an empty state with a call to action', async () => {
    withAuthenticatedSession()
    renderApp('/app/vacantes')

    expect(await screen.findByText(/aún no has guardado vacantes/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /agrega la primera/i })).toHaveAttribute('href', '/app/vacantes/nueva')
  })

  it('lists postings, paginates and searches through the URL', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    const requests: string[] = []
    server.use(
      http.get(api('/job-postings'), ({ request }) => {
        const url = new URL(request.url)
        requests.push(url.search)
        const page = Number(url.searchParams.get('page'))
        const items = url.searchParams.has('search') ? [postingSummary(7)] : page === 1 ? [postingSummary(1), postingSummary(2)] : [postingSummary(3)]
        return HttpResponse.json({ items, page, pageSize: 10, totalCount: url.searchParams.has('search') ? 1 : 12 })
      }),
    )
    const { router } = renderApp('/app/vacantes')

    const list = await screen.findByRole('list', { name: /vacantes guardadas/i })
    expect(within(list).getByText('Vacante 1')).toBeInTheDocument()
    expect(within(list).getByText(/empresa 1, bogotá, remota/i)).toBeInTheDocument()
    expect(screen.getByText(/página 1 de 2 \(12 vacantes\)/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Siguiente' }))
    expect(await screen.findByText('Vacante 3')).toBeInTheDocument()
    expect(router.state.location.search).toBe('?page=2')

    await user.type(screen.getByRole('searchbox', { name: /buscar por cargo o empresa/i }), 'datos')
    await user.click(screen.getByRole('button', { name: 'Buscar' }))
    expect(await screen.findByText('Vacante 7')).toBeInTheDocument()
    expect(router.state.location.search).toBe('?q=datos')
    expect(requests.at(-1)).toBe('?page=1&pageSize=10&search=datos')
  })

  it('asks for confirmation before deleting and refreshes the list', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let deleted = false
    server.use(
      http.get(api('/job-postings'), () =>
        HttpResponse.json({ items: deleted ? [] : [postingSummary(1)], page: 1, pageSize: 10, totalCount: deleted ? 0 : 1 }),
      ),
      http.delete(api(`/job-postings/${postingSummary(1).id}`), () => {
        deleted = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    renderApp('/app/vacantes')

    await user.click(await screen.findByRole('button', { name: 'Eliminar Vacante 1' }))
    expect(deleted).toBe(false)
    const confirmation = screen.getByRole('group', { name: /confirmar eliminación de vacante 1/i })
    await user.click(within(confirmation).getByRole('button', { name: 'Cancelar' }))
    expect(screen.queryByRole('group', { name: /confirmar eliminación/i })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Eliminar Vacante 1' }))
    await user.click(screen.getByRole('button', { name: /sí, eliminar/i }))

    expect(await screen.findByText('Vacante eliminada.')).toBeInTheDocument()
    expect(await screen.findByText(/aún no has guardado vacantes/i)).toBeInTheDocument()
    expect(deleted).toBe(true)
  })
})
