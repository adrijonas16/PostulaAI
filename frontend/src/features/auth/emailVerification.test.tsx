import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { authResponse, testUser } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { api, problem, server } from '../../test/server'

const pendingUser = { ...testUser, emailVerified: false, emailVerificationPending: true }

function withPendingSession() {
  let verified = false
  server.use(
    http.post(api('/auth/refresh'), () =>
      HttpResponse.json({ ...authResponse(), user: verified ? testUser : pendingUser }),
    ),
  )
  return {
    markVerified: () => {
      verified = true
    },
  }
}

describe('e-mail verification', () => {
  it('reminds the user, locks AI features, resends the e-mail and unlocks after confirming', async () => {
    const user = userEvent.setup()
    const session = withPendingSession()
    let resent = 0
    server.use(
      http.post(api('/auth/resend-verification'), () => {
        resent += 1
        return new HttpResponse(null, { status: 204 })
      }),
    )
    renderApp('/app/perfil')

    const banner = await screen.findByRole('region', { name: 'Confirma tu correo' })
    expect(banner).toHaveTextContent(testUser.email)
    expect(await screen.findByText(/confirma tu correo para usar la importación del cv/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Elegir archivo' })).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Reenviar correo' }))
    expect(await screen.findByText('Te enviamos un correo nuevo.')).toBeInTheDocument()
    expect(resent).toBe(1)

    session.markVerified()
    await user.click(screen.getByRole('button', { name: 'Ya lo confirmé' }))
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Confirma tu correo' })).not.toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Elegir archivo' })).toBeEnabled()
  })

  it('confirms the e-mail from the link exactly once', async () => {
    let calls = 0
    let sentToken: unknown = null
    server.use(
      http.post(api('/auth/verify-email'), async ({ request }) => {
        calls += 1
        sentToken = ((await request.json()) as { token: string }).token
        return new HttpResponse(null, { status: 204 })
      }),
    )
    renderApp('/verificar-correo?token=abc123')

    expect(await screen.findByText(/tu correo está confirmado/i)).toBeInTheDocument()
    expect(sentToken).toBe('abc123')
    expect(calls).toBe(1)
    const page = screen.getByRole('region', { name: 'Confirmar correo' })
    expect(within(page).getByRole('link', { name: 'Iniciar sesión' })).toHaveAttribute('href', '/iniciar-sesion')
  })

  it('explains an expired or used link', async () => {
    server.use(
      http.post(api('/auth/verify-email'), () =>
        problem(400, 'invalid_verification_token', 'El enlace de confirmación no es válido o ya venció. Pide uno nuevo desde la aplicación.'),
      ),
    )
    renderApp('/verificar-correo?token=viejo')

    expect(await screen.findByRole('alert')).toHaveTextContent(/ya venció/i)
  })
})
