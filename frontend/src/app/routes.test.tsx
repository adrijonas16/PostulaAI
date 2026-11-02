import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RouterProvider, createMemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import { routes } from './routes'

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  render(<RouterProvider router={router} />)
  return router
}

describe('routes', () => {
  it('renders the landing page with the transparency message', () => {
    renderAt('/')

    expect(screen.getByRole('heading', { level: 1, name: /compara tu perfil/i })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /sin inventar nada/i })).toBeInTheDocument()
  })

  it('navigates from the landing page to registration', async () => {
    const user = userEvent.setup()
    const router = renderAt('/')

    await user.click(screen.getAllByRole('link', { name: /crear cuenta/i })[0] ?? document.body)

    expect(router.state.location.pathname).toBe('/registro')
    expect(screen.getByRole('heading', { level: 1, name: /crear cuenta/i })).toBeInTheDocument()
  })

  it.each([
    ['/iniciar-sesion', /iniciar sesión/i],
    ['/app', /inicio/i],
    ['/app/perfil', /perfil profesional/i],
    ['/app/vacantes/nueva', /vacante/i],
    ['/app/vacantes/123/editar', /vacante/i],
    ['/app/analisis/nuevo', /nuevo análisis/i],
    ['/app/analisis', /historial de análisis/i],
    ['/app/analisis/abc', /resultado del análisis/i],
    ['/app/configuracion', /configuración/i],
  ])('renders exactly one h1 at %s', (path, name) => {
    renderAt(path)

    const headings = screen.getAllByRole('heading', { level: 1 })
    expect(headings).toHaveLength(1)
    expect(headings[0]).toHaveAccessibleName(name)
  })

  it('shows a not-found page for unknown routes', () => {
    renderAt('/no-existe')

    expect(screen.getByRole('heading', { level: 1, name: /página no encontrada/i })).toBeInTheDocument()
  })

  it('marks the current section in the app navigation', () => {
    renderAt('/app/perfil')

    const navigation = screen.getByRole('navigation', { name: /principal/i })
    const current = navigation.querySelector('[aria-current="page"]')
    expect(current).toHaveTextContent('Perfil profesional')
  })
})
