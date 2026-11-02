import type { JobAnalysisResult, ScoreContext } from '../../shared/api/types'

/** A fictional example shown on the landing page. It uses the same components as a real result. */
export const demoResult: JobAnalysisResult = {
  compatibilityScore: 72,
  summary: 'Tu experiencia con ventas B2B y CRM cubre lo esencial de la vacante. Lo que más pesa en contra es el inglés avanzado que piden.',
  strengths: [
    {
      requirement: '3 años de experiencia en ventas B2B',
      evidence: 'Ejecutiva comercial en Distribuidora Ejemplo (2021 a hoy), cartera de clientes empresariales.',
      importance: 'required',
    },
    { requirement: 'Manejo de CRM', evidence: 'Habilidades técnicas: HubSpot. Logro: migró la cartera del equipo a HubSpot.', importance: 'required' },
    { requirement: 'Negociación con clientes corporativos', evidence: 'Logro: renovó 12 contratos anuales con empresas medianas.', importance: 'required' },
    { requirement: 'Licencia de conducir', evidence: 'Certificaciones: licencia de conducir B1.', importance: 'preferred' },
  ],
  gaps: [
    {
      requirement: 'Inglés avanzado',
      gapType: 'missing_skill',
      importance: 'required',
      recommendation: 'Tu perfil indica inglés intermedio. Si tu nivel real es mayor, actualízalo; si no, menciónalo con honestidad en la entrevista.',
    },
    {
      requirement: 'Experiencia en licitaciones públicas',
      gapType: 'unclear_evidence',
      importance: 'preferred',
      recommendation: 'Si participaste en alguna licitación, agrégala a tu experiencia con el resultado concreto.',
    },
  ],
  keywordsToInclude: ['ventas B2B', 'HubSpot', 'negociación'],
  cvRecommendations: [],
  adaptedProfessionalSummary: '',
  coverLetterDraft: '',
  disclaimer: '',
}

export const demoScore: ScoreContext = { band: 'medium', requiredMet: 3, requiredMissing: 1, preferredMet: 1, preferredMissing: 1 }
