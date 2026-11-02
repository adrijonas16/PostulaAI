import type { AuthResponse, ProfileResponse } from '../shared/api/types'

export const testUser = { id: '0190f0d2-0000-7000-8000-000000000001', email: 'laura@example.test', displayName: 'Laura Gómez' }

export function authResponse(accessToken = 'access-token-1'): AuthResponse {
  return { accessToken, accessTokenExpiresAt: '2030-01-01T00:00:00Z', user: testUser }
}

export const sampleProfile: ProfileResponse = {
  id: '0190f0d2-0000-7000-8000-0000000000aa',
  fullName: 'Laura Gómez',
  headline: 'Desarrolladora backend .NET',
  summary: 'Construyo APIs REST.',
  links: { linkedInUrl: 'https://www.linkedin.com/in/ejemplo', gitHubUrl: null, portfolioUrl: null },
  experiences: [
    {
      company: 'Empresa Ficticia S.A.',
      role: 'Desarrolladora backend',
      location: 'Bogotá',
      startDate: '2022-03-01',
      endDate: null,
      isCurrent: true,
      description: 'APIs para pagos.',
      achievements: ['Reduje la latencia p95 un 30 %'],
      technologies: ['C#', 'PostgreSQL'],
    },
  ],
  education: [
    { institution: 'Universidad Ejemplo', degree: 'Ingeniería de Sistemas', fieldOfStudy: null, startDate: '2015-02-01', endDate: '2020-12-01', status: 'completed' },
  ],
  projects: [],
  certifications: [],
  technicalSkills: ['C#', 'SQL'],
  softSkills: ['Comunicación'],
  languages: [{ language: 'Español', proficiency: 'native' }],
  updatedAt: '2026-11-01T10:00:00Z',
}
