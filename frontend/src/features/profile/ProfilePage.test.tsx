import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import type { ProfileRequest } from '../../shared/api/types'
import { sampleProfile } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { api, problem, server, withAuthenticatedSession } from '../../test/server'

function withProfile(profile: typeof sampleProfile | null) {
  withAuthenticatedSession()
  server.use(
    http.get(api('/profiles/me'), () =>
      profile === null ? problem(404, 'profile_not_found', 'Todavía no has creado tu perfil profesional.') : HttpResponse.json(profile),
    ),
  )
}

describe('ProfilePage', () => {
  it('prefills the form with the saved profile', async () => {
    withProfile(sampleProfile)
    renderApp('/app/perfil')

    expect(await screen.findByDisplayValue('Laura Gómez')).toBeInTheDocument()
    const experience = screen.getByRole('group', { name: 'Experiencia 1' })
    expect(within(experience).getByLabelText(/empresa/i)).toHaveValue('Empresa Ficticia S.A.')
    expect(within(experience).getByLabelText(/es mi empleo actual/i)).toBeChecked()
    expect(within(experience).getByLabelText(/^fin/i)).toBeDisabled()
    expect(within(experience).getByLabelText(/tecnologías usadas/i)).toHaveValue('C#, PostgreSQL')
    expect(screen.getByLabelText(/habilidades técnicas/i)).toHaveValue('C#, SQL')
  })

  it('creates a profile from an empty form and sends the API contract', async () => {
    const user = userEvent.setup()
    withProfile(null)
    let sent: ProfileRequest | null = null
    server.use(
      http.put(api('/profiles/me'), async ({ request }) => {
        sent = (await request.json()) as ProfileRequest
        return HttpResponse.json({ ...sampleProfile, ...sent })
      }),
    )
    renderApp('/app/perfil')

    await user.type(await screen.findByLabelText(/nombre profesional/i), 'Laura Gómez')
    await user.click(screen.getByRole('button', { name: /agregar experiencia/i }))
    const experience = screen.getByRole('group', { name: 'Experiencia 1' })
    await user.type(within(experience).getByLabelText(/empresa/i), 'Empresa Ficticia')
    await user.type(within(experience).getByLabelText(/cargo/i), 'Analista')
    await user.type(within(experience).getByLabelText(/inicio/i), '2021-05')
    await user.click(within(experience).getByLabelText(/es mi empleo actual/i))
    await user.type(within(experience).getByLabelText(/tecnologías usadas/i), 'Python, SQL')
    await user.type(screen.getByLabelText(/habilidades técnicas/i), 'Python, python, Excel')
    await user.click(screen.getByRole('button', { name: /guardar perfil/i }))

    expect(await screen.findByText('Perfil guardado.')).toBeInTheDocument()
    expect(sent).toMatchObject({
      fullName: 'Laura Gómez',
      headline: null,
      experiences: [
        { company: 'Empresa Ficticia', role: 'Analista', startDate: '2021-05-01', endDate: null, isCurrent: true, technologies: ['Python', 'SQL'] },
      ],
      technicalSkills: ['Python', 'Excel'],
      education: [],
      languages: [],
    })
  })

  it('blocks submission and marks fields when required data is missing', async () => {
    const user = userEvent.setup()
    withProfile(null)
    let calls = 0
    server.use(
      http.put(api('/profiles/me'), () => {
        calls += 1
        return HttpResponse.json(sampleProfile)
      }),
    )
    renderApp('/app/perfil')

    await user.click(await screen.findByRole('button', { name: /agregar experiencia/i }))
    await user.click(screen.getByRole('button', { name: /guardar perfil/i }))

    expect(await screen.findByText('Ingresa tu nombre profesional.')).toBeInTheDocument()
    expect(screen.getByText('Ingresa la empresa.')).toBeInTheDocument()
    expect(screen.getByText('Indica la fecha de fin o marca que es tu empleo actual.')).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent(/revisa los campos/i)
    expect(calls).toBe(0)
  })

  it('shows backend validation errors next to the matching field', async () => {
    const user = userEvent.setup()
    withProfile(sampleProfile)
    server.use(
      http.put(api('/profiles/me'), () =>
        problem(400, 'validation_failed', 'Revisa los datos enviados.', {
          'experiences[0].startDate': ['La fecha de inicio no puede estar en el futuro.'],
        }),
      ),
    )
    renderApp('/app/perfil')

    await screen.findByDisplayValue('Laura Gómez')
    await user.click(screen.getByRole('button', { name: /guardar perfil/i }))

    const experience = screen.getByRole('group', { name: 'Experiencia 1' })
    await waitFor(() =>
      expect(within(experience).getByLabelText(/inicio/i)).toHaveAccessibleDescription('La fecha de inicio no puede estar en el futuro.'),
    )
    expect(screen.getByRole('alert')).toHaveTextContent('Revisa los datos enviados.')
  })

  it('removes an item from a list', async () => {
    const user = userEvent.setup()
    withProfile(sampleProfile)
    renderApp('/app/perfil')

    await screen.findByDisplayValue('Laura Gómez')
    await user.click(screen.getByRole('button', { name: /eliminar experiencia 1/i }))

    expect(screen.queryByRole('group', { name: 'Experiencia 1' })).not.toBeInTheDocument()
    expect(screen.getByText('Aún no has agregado experiencia.')).toBeInTheDocument()
  })
})
