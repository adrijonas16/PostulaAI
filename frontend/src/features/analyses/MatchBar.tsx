import type { ScoreContext } from '../../shared/api/types'

/**
 * One segment per requirement: highlighted when the profile proves it, red-outlined when it is missing.
 * Required requirements come first and are taller, so the bar reads "how much of what matters do I have".
 */
export function MatchBar({ score, compact = false }: { score: ScoreContext; compact?: boolean }) {
  const required = score.requiredMet + score.requiredMissing
  const preferred = score.preferredMet + score.preferredMissing
  const label = `${score.requiredMet} de ${required} requisitos obligatorios y ${score.preferredMet} de ${preferred} deseables cumplidos`
  const height = compact ? 'h-2' : 'h-4'

  const segments = [
    ...Array.from({ length: score.requiredMet }, () => `${height} bg-marker`),
    ...Array.from({ length: score.requiredMissing }, () => `${height} border-2 border-redpen bg-redpen-soft`),
    ...Array.from({ length: score.preferredMet }, () => `${compact ? 'h-1.5' : 'h-2.5'} self-end bg-marker/60`),
    ...Array.from({ length: score.preferredMissing }, () => `${compact ? 'h-1.5' : 'h-2.5'} self-end border border-dashed border-graphite/60`),
  ]

  return (
    <div>
      <p className="sr-only">{label}</p>
      <div aria-hidden="true" className={`flex items-end gap-1 ${compact ? 'h-2' : 'h-4'}`}>
      {segments.map((className, index) => (
        // Segments are positional and never reorder, so the index is a stable key.
        // oxlint-disable-next-line react/no-array-index-key
        <span key={index} className={`min-w-2 flex-1 rounded-sm ${className}`} />
      ))}
      </div>
    </div>
  )
}

export function MatchLegend() {
  return (
    <ul aria-hidden="true" className="mt-3 flex flex-wrap gap-x-5 gap-y-1 text-xs text-graphite">
      <li className="flex items-center gap-1.5">
        <span aria-hidden="true" className="inline-block h-3 w-4 rounded-sm bg-marker" /> Obligatorio que tienes
      </li>
      <li className="flex items-center gap-1.5">
        <span aria-hidden="true" className="inline-block h-3 w-4 rounded-sm border-2 border-redpen bg-redpen-soft" /> Obligatorio que falta
      </li>
      <li className="flex items-center gap-1.5">
        <span aria-hidden="true" className="inline-block h-2 w-4 rounded-sm bg-marker/60" /> Deseable que tienes
      </li>
      <li className="flex items-center gap-1.5">
        <span aria-hidden="true" className="inline-block h-2 w-4 rounded-sm border border-dashed border-graphite/60" /> Deseable que falta
      </li>
    </ul>
  )
}
