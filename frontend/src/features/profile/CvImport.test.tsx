import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import type { CvImportResponse } from '../../shared/api/types'
import { renderApp } from '../../test/renderApp'
import { api, problem, server, withAuthenticatedSession } from '../../test/server'

const importResult: CvImportResponse = {
  draft: {
    fullName: 'Laura Gómez',
    headline: 'Desarrolladora backend',
    summary: null,
    links: { linkedInUrl: 'https://www.linkedin.com/in/laura', gitHubUrl: null, portfolioUrl: null },
    experiences: [
      {
        company: 'Empresa Ficticia S.A.',
        role: 'Desarrolladora backend',
        location: 'Bogotá',
        startDate: null,
        endDate: null,
        isCurrent: true,
        description: null,
        achievements: ['Reduje la latencia un 30 %'],
        technologies: ['C#', 'PostgreSQL'],
      },
    ],
    education: [],
    projects: [],
    certifications: [],
    technicalSkills: ['C#', 'PostgreSQL'],
    softSkills: [],
    languages: [{ language: 'Inglés', proficiency: null }],
  },
  warnings: ['Se omitió la habilidad técnica «Kubernetes» porque no aparece en el CV.'],
}

const cvFile = () => new File(['%PDF-1.7 contenido ficticio'], 'mi-cv.pdf', { type: 'application/pdf' })

describe('CV import', () => {
  it('uploads the file, pre-fills the form without saving, and shows what to review', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    let uploadedBytes = -1
    let saves = 0
    server.use(
      http.post(api('/profiles/me/import'), async ({ request }) => {
        const file = (await request.formData()).get('file')
        // jsdom's File loses its name when serialized by Node's fetch; the backend detects the format by content anyway.
        uploadedBytes = typeof file === 'object' && file !== null && 'size' in file ? Number(file.size) : -1
        return HttpResponse.json(importResult)
      }),
      http.put(api('/profiles/me'), () => {
        saves += 1
        return HttpResponse.json({})
      }),
    )
    renderApp('/app/perfil')

    await user.upload(await screen.findByLabelText('Archivo del CV'), cvFile())
    expect(screen.getByText('mi-cv.pdf')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Importar CV' }))

    expect(await screen.findByDisplayValue('Laura Gómez')).toBeInTheDocument()
    expect(uploadedBytes).toBe(cvFile().size)
    expect(saves).toBe(0)
    expect(screen.getByText(/aún no se ha guardado/i)).toBeInTheDocument()
    expect(screen.getByText(/«Kubernetes»/)).toBeInTheDocument()
    // The first section with missing data opens by itself.
    await waitFor(() => expect(screen.getByRole('tab', { name: /experiencia/i })).toHaveAttribute('aria-selected', 'true'))
    const experience = screen.getByRole('group', { name: 'Experiencia 1' })
    expect(within(experience).getByLabelText(/tecnologías usadas/i)).toHaveValue('C#, PostgreSQL')
    // Missing data is flagged instead of guessed.
    await waitFor(() => expect(within(experience).getByLabelText(/inicio/i)).toHaveAttribute('aria-invalid', 'true'))
    await user.click(screen.getByRole('tab', { name: /habilidades/i }))
    const skills = screen.getByRole('list', { name: 'Habilidades técnicas agregadas' })
    expect(within(skills).getAllByRole('listitem')).toHaveLength(2)
    const language = screen.getByRole('group', { name: 'Idioma 1' })
    expect(within(language).getByLabelText('Nivel')).toHaveValue('')
    expect(within(language).getByText('Elige el nivel.', { selector: 'p' })).toBeInTheDocument()
  })

  it('rejects unsupported files before uploading', async () => {
    const user = userEvent.setup({ applyAccept: false })
    withAuthenticatedSession()
    renderApp('/app/perfil')

    await user.upload(await screen.findByLabelText('Archivo del CV'), new File(['texto'], 'cv.txt', { type: 'text/plain' }))

    expect(screen.getByRole('alert')).toHaveTextContent(/pdf o word/i)
    expect(screen.queryByRole('button', { name: 'Importar CV' })).not.toBeInTheDocument()
  })

  it('explains server errors such as scanned PDFs', async () => {
    const user = userEvent.setup()
    withAuthenticatedSession()
    server.use(
      http.post(api('/profiles/me/import'), () =>
        problem(422, 'cv_no_text', 'No encontramos texto en el archivo. Si es un PDF escaneado (una imagen), exporta tu CV como PDF con texto.'),
      ),
    )
    renderApp('/app/perfil')

    await user.upload(await screen.findByLabelText('Archivo del CV'), cvFile())
    await user.click(screen.getByRole('button', { name: 'Importar CV' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/pdf escaneado/i)
  })
})
