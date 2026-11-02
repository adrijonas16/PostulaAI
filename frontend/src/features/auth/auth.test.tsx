import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { authResponse } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { api, problem, server, withAuthenticatedSession } from '../../test/server'
import { session } from '../../shared/api/session'

describe('route protection', () => {
  it('sends anonymous users from /app to the login page', async () => {
    const { router } = renderApp('/app/perfil')

    expect(await screen.findByRole('heading', { level: 1, name: /iniciar sesión/i })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/iniciar-sesion')
  })

  it('sends authenticated users away from the login page', async () => {
    withAuthenticatedSession()
    server.use(http.get(api('/profiles/me'), () => problem(404, 'profile_not_found', 'Sin perfil')))

    const { router } = renderApp('/iniciar-sesion')

    await waitFor(() => expect(router.state.location.pathname).toBe('/app'))
  })
})

describe('login', () => {
  it('validates required fields before calling the API', async () => {
    const user = userEvent.setup()
    renderApp('/iniciar-sesion')

    await user.click(await screen.findByRole('button', { name: /^iniciar sesión$/i }))

    expect(await screen.findByText('Ingresa tu correo electrónico.')).toBeInTheDocument()
    expect(screen.getByText('Ingresa tu contraseña.')).toBeInTheDocument()
    expect(screen.getByLabelText(/correo electrónico/i)).toHaveAttribute('aria-invalid', 'true')
  })

  it('shows the generic server error for wrong credentials', async () => {
    const user = userEvent.setup()
    server.use(http.post(api('/auth/login'), () => problem(401, 'invalid_credentials', 'Correo o contraseña incorrectos.')))
    renderApp('/iniciar-sesion')

    await user.type(await screen.findByLabelText(/correo electrónico/i), 'laura@example.test')
    await user.type(screen.getByLabelText(/^contraseña/i), 'incorrecta-123')
    await user.click(screen.getByRole('button', { name: /^iniciar sesión$/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Correo o contraseña incorrectos.')
    expect(session.get()).toBeNull()
  })

  it('logs in and returns to the page that was requested', async () => {
    const user = userEvent.setup()
    let body: unknown = null
    server.use(
      http.post(api('/auth/login'), async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(authResponse())
      }),
      http.get(api('/profiles/me'), () => problem(404, 'profile_not_found', 'Sin perfil')),
    )
    const { router } = renderApp('/app/perfil')

    await user.type(await screen.findByLabelText(/correo electrónico/i), 'laura@example.test')
    await user.type(screen.getByLabelText(/^contraseña/i), 'contraseña-larga')
    await user.click(screen.getByRole('button', { name: /^iniciar sesión$/i }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/app/perfil'))
    expect(body).toEqual({ email: 'laura@example.test', password: 'contraseña-larga' })
    expect(session.accessToken()).toBe('access-token-1')
  })
})

describe('registration', () => {
  it('checks password length and confirmation on the client', async () => {
    const user = userEvent.setup()
    renderApp('/registro')

    await user.type(await screen.findByLabelText(/^nombre/i), 'Laura')
    await user.type(screen.getByLabelText(/correo electrónico/i), 'laura@example.test')
    await user.type(screen.getByLabelText(/^contraseña/i), 'corta')
    await user.type(screen.getByLabelText(/confirmar contraseña/i), 'distinta')
    await user.click(screen.getByRole('button', { name: /crear cuenta/i }))

    expect(await screen.findByText(/al menos 10 caracteres/i)).toBeInTheDocument()
    expect(screen.getByText('Las contraseñas no coinciden.')).toBeInTheDocument()
  })

  it('shows server field errors next to the field', async () => {
    const user = userEvent.setup()
    server.use(
      http.post(api('/auth/register'), () =>
        problem(409, 'email_already_registered', 'Ya existe una cuenta con ese correo.'),
      ),
    )
    renderApp('/registro')

    await user.type(await screen.findByLabelText(/^nombre/i), 'Laura')
    await user.type(screen.getByLabelText(/correo electrónico/i), 'laura@example.test')
    await user.type(screen.getByLabelText(/^contraseña/i), 'contraseña-larga')
    await user.type(screen.getByLabelText(/confirmar contraseña/i), 'contraseña-larga')
    await user.click(screen.getByRole('button', { name: /crear cuenta/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe una cuenta con ese correo.')
  })

  it('creates the account and goes to the profile', async () => {
    const user = userEvent.setup()
    server.use(
      http.post(api('/auth/register'), () => HttpResponse.json(authResponse(), { status: 201 })),
      http.get(api('/profiles/me'), () => problem(404, 'profile_not_found', 'Sin perfil')),
    )
    const { router } = renderApp('/registro')

    await user.type(await screen.findByLabelText(/^nombre/i), 'Laura')
    await user.type(screen.getByLabelText(/correo electrónico/i), 'laura@example.test')
    await user.type(screen.getByLabelText(/^contraseña/i), 'contraseña-larga')
    await user.type(screen.getByLabelText(/confirmar contraseña/i), 'contraseña-larga')
    await user.click(screen.getByRole('button', { name: /crear cuenta/i }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/app/perfil'))
  })
})

describe('logout', () => {
  it('revokes the session and returns to the landing page', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let logoutCalls = 0
    server.use(
      http.post(api('/auth/logout'), () => {
        logoutCalls += 1
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const { router } = renderApp('/app/configuracion')

    await user.click(await screen.findByRole('button', { name: /cerrar sesión/i }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/'))
    expect(logoutCalls).toBe(1)
    expect(session.get()).toBeNull()
  })
})
