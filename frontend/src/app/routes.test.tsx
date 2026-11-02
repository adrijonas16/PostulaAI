import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http } from 'msw'
import { describe, expect, it } from 'vitest'
import { renderApp } from '../test/renderApp'
import { api, problem, server, withAuthenticatedSession } from '../test/server'

describe('public routes', () => {
  it('renders the landing page with the transparency message', async () => {
    renderApp('/')

    expect(await screen.findByRole('heading', { level: 1, name: /compara tu perfil/i })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /sin inventar nada/i })).toBeInTheDocument()
  })

  it('navigates from the landing page to registration', async () => {
    const user = userEvent.setup()
    const { router } = renderApp('/')

    const [firstLink] = await screen.findAllByRole('link', { name: /crear cuenta/i })
    if (firstLink === undefined) throw new Error('Missing "Crear cuenta" link')
    await user.click(firstLink)

    expect(router.state.location.pathname).toBe('/registro')
    expect(await screen.findByRole('heading', { level: 1, name: /crear cuenta/i })).toBeInTheDocument()
  })

  it('shows a not-found page for unknown routes', async () => {
    renderApp('/no-existe')

    expect(await screen.findByRole('heading', { level: 1, name: /página no encontrada/i })).toBeInTheDocument()
  })
})

describe('authenticated routes', () => {
  it.each([
    ['/app', /inicio/i],
    ['/app/vacantes/nueva', /vacante/i],
    ['/app/vacantes/123/editar', /vacante/i],
    ['/app/analisis/nuevo', /nuevo análisis/i],
    ['/app/analisis', /historial de análisis/i],
    ['/app/analisis/abc', /resultado del análisis/i],
    ['/app/configuracion', /configuración/i],
  ])('renders exactly one h1 at %s', async (path, name) => {
    withAuthenticatedSession()
    renderApp(path)

    const heading = await screen.findByRole('heading', { level: 1, name })
    expect(screen.getAllByRole('heading', { level: 1 })).toEqual([heading])
  })

  it('marks the current section and shows the signed-in user', async () => {
    withAuthenticatedSession()
    server.use(http.get(api('/profiles/me'), () => problem(404, 'profile_not_found', 'Sin perfil')))
    renderApp('/app/perfil')

    const navigation = await screen.findByRole('navigation', { name: /principal/i })
    expect(within(navigation).getByRole('link', { name: 'Perfil profesional' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByText('laura@example.test')).toBeInTheDocument()
  })
})
