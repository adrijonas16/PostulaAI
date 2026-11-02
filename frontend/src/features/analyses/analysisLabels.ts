import type { GapType, RequirementImportance, ScoreBand } from '../../shared/api/types'

export const bandLabels: Record<ScoreBand, string> = {
  high: 'Compatibilidad alta',
  medium: 'Compatibilidad media',
  low: 'Compatibilidad baja',
}

export const bandStyles: Record<ScoreBand, string> = {
  high: 'bg-emerald-50 text-emerald-900 border-emerald-200',
  medium: 'bg-amber-50 text-amber-900 border-amber-200',
  low: 'bg-rose-50 text-rose-900 border-rose-200',
}

export const importanceLabels: Record<RequirementImportance, string> = {
  required: 'Obligatorio',
  preferred: 'Deseable',
}

export const gapTypeLabels: Record<GapType, string> = {
  missing_skill: 'Habilidad faltante',
  missing_experience: 'Experiencia insuficiente',
  unclear_evidence: 'Evidencia poco clara',
  other: 'Otro',
}

/** User-facing explanation for a failed analysis, by backend error code. */
export function analysisErrorMessage(errorCode: string | null): string {
  switch (errorCode) {
    case 'ai_not_configured':
      return 'El análisis con IA no está configurado en este servidor (falta la API key de Anthropic).'
    case 'ai_rate_limited':
      return 'El servicio de IA estaba saturado. Intenta de nuevo en unos minutos.'
    case 'ai_invalid_output':
      return 'La IA no produjo un resultado válido, así que no guardamos nada incompleto. Intenta de nuevo.'
    case 'ai_refused':
    case 'ai_request_rejected':
      return 'La IA no pudo procesar esta vacante. Revisa su texto.'
    default:
      return 'No pudimos completar el análisis. Intenta de nuevo más tarde.'
  }
}
