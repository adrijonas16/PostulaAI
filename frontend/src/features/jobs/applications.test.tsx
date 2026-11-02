import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import type { ApplicationRequest, JobPostingResponse } from '../../shared/api/types'
import { postingSummary, sampleAnalysis, samplePosting } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { api, server, withAuthenticatedSession } from '../../test/server'
import { todayIso } from './applicationLabels'

function withPostingApplicationEndpoint(onSave: (request: ApplicationRequest) => void) {
  let posting: JobPostingResponse = samplePosting
  server.use(
    http.get(api(`/job-postings/${samplePosting.id}`), () => HttpResponse.json(posting)),
    http.put(api(`/job-postings/${samplePosting.id}/application`), async ({ request }) => {
      const body = (await request.json()) as ApplicationRequest
      onSave(body)
      posting = { ...posting, application: { ...body, updatedAt: '2026-11-02T20:00:00Z' } }
      return HttpResponse.json(posting)
    }),
  )
}

describe('application tracking', () => {
  it('records the application from the result page with the texts exactly as the user edited them', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let saved: ApplicationRequest | null = null
    withPostingApplicationEndpoint((request) => {
      saved = request
    })
    server.use(http.get(api(`/analyses/${sampleAnalysis.id}`), () => HttpResponse.json(sampleAnalysis)))
    renderApp(`/app/analisis/${sampleAnalysis.id}`)

    const letter = await screen.findByRole('region', { name: /carta de presentación/i })
    await user.type(within(letter).getByRole('textbox'), ' Saludos.')

    const panel = await screen.findByRole('region', { name: 'Tu postulación' })
    expect(within(panel).getByRole('link', { name: /abrir la oferta original/i })).toHaveAttribute('href', samplePosting.sourceUrl)
    await user.click(within(panel).getByRole('radio', { name: 'Postulé' }))
    expect(within(panel).getByLabelText(/fecha de postulación/i)).toHaveValue(todayIso())
    await user.type(within(panel).getByLabelText(/notas/i), 'Por LinkedIn')
    await user.click(within(panel).getByRole('button', { name: /guardar postulación/i }))

    expect(await within(panel).findByText('Postulación guardada.')).toBeInTheDocument()
    expect(saved).toEqual({
      status: 'applied',
      appliedOn: todayIso(),
      notes: 'Por LinkedIn',
      submittedSummary: sampleAnalysis.result?.adaptedProfessionalSummary,
      submittedCoverLetter: `${sampleAnalysis.result?.coverLetterDraft ?? ''} Saludos.`,
    })
    expect(within(panel).getByText('Lo que enviaste')).toBeInTheDocument()
  })

  it('filters the list by status and marks a saved posting as applied without losing its notes', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let saved: ApplicationRequest | null = null
    let requestedStatus: string | null = 'unset'
    withPostingApplicationEndpoint((request) => {
      saved = request
    })
    server.use(
      http.get(api(`/job-postings/${samplePosting.id}`), () =>
        HttpResponse.json({ ...samplePosting, application: { ...samplePosting.application, notes: 'Referido por Ana' } }),
      ),
      http.get(api('/job-postings'), ({ request }) => {
        requestedStatus = new URL(request.url).searchParams.get('status')
        const item = { ...postingSummary(1), id: samplePosting.id, sourceUrl: samplePosting.sourceUrl }
        return HttpResponse.json({ items: [item], page: 1, pageSize: 10, totalCount: 1 })
      }),
    )
    const { router } = renderApp('/app/vacantes')

    const list = await screen.findByRole('list', { name: /vacantes guardadas/i })
    expect(within(list).getByText('Sin postular')).toBeInTheDocument()
    expect(within(list).getByRole('link', { name: /ver oferta original/i })).toHaveAttribute('href', samplePosting.sourceUrl)

    await user.click(within(list).getByRole('button', { name: /marcar vacante 1 como postulada/i }))
    expect(await screen.findByText(/marcaste «desarrolladora backend \.net» como postulada/i)).toBeInTheDocument()
    expect(saved).toMatchObject({ status: 'applied', appliedOn: todayIso(), notes: 'Referido por Ana' })

    await user.click(screen.getByRole('radio', { name: 'Entrevista' }))
    await waitFor(() => expect(requestedStatus).toBe('interview'))
    expect(router.state.location.search).toBe('?estado=interview')
  })
})
