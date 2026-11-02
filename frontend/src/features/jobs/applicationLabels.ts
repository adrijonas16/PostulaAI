import type { ApplicationStatus } from '../../shared/api/types'

export const applicationStatusLabels: Record<ApplicationStatus, string> = {
  saved: 'Sin postular',
  applied: 'Postulé',
  interview: 'Entrevista',
  offer: 'Oferta',
  rejected: 'Descartada',
}

/** Pill colors: progress in pen blue, a win in green, a closed door in graphite. */
export const applicationStatusStyles: Record<ApplicationStatus, string> = {
  saved: 'border border-rule text-graphite',
  applied: 'bg-pen-soft text-pen-strong',
  interview: 'bg-pen text-sheet',
  offer: 'bg-marker text-ink',
  rejected: 'border border-dashed border-graphite text-graphite',
}

/** Today's date in the user's time zone as yyyy-MM-dd. */
export function todayIso(now = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

/** Formats a yyyy-MM-dd date without shifting it through UTC. */
export function formatDay(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  if (year === undefined || month === undefined || day === undefined) return iso
  return new Date(year, month - 1, day).toLocaleDateString('es', { day: 'numeric', month: 'short', year: 'numeric' })
}
