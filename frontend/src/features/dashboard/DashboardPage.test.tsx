import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { postingSummary, sampleProfile } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { api, server, withAuthenticatedSession } from '../../test/server'

describe('DashboardPage', () => {
  it('invites to complete the profile when it does not exist yet', async () => {
    withAuthenticatedSession()
    renderApp('/app')

    expect(await screen.findByRole('link', { name: /completar mi perfil/i })).toHaveAttribute('href', '/app/perfil')
    expect(await screen.findByText(/aún no has guardado vacantes/i)).toBeInTheDocument()
  })

  it('summarizes the profile and lists recent postings', async () => {
    withAuthenticatedSession()
    server.use(
      http.get(api('/profiles/me'), () => HttpResponse.json(sampleProfile)),
      http.get(api('/job-postings'), () =>
        HttpResponse.json({ items: [postingSummary(1), postingSummary(2)], page: 1, pageSize: 5, totalCount: 8 }),
      ),
    )
    renderApp('/app')

    expect(await screen.findByRole('link', { name: 'Vacante 1' })).toHaveAttribute('href', `/app/vacantes/${postingSummary(1).id}/editar`)
    expect(screen.getByText('Empresa no indicada')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /ver las 8 vacantes/i })).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: /editar perfil/i })).toBeInTheDocument()
    expect(await screen.findByText(/te quedan 10 de 10 análisis/i)).toBeInTheDocument()
    const analysisCard = screen.getByRole('article', { name: 'Análisis' })
    expect(within(analysisCard).getByRole('link', { name: 'Nuevo análisis' })).toHaveAttribute('href', '/app/analisis/nuevo')
  })
})
