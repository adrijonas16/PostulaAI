import type { ScoreBand } from '../../shared/api/types'

const styles: Record<ScoreBand, string> = {
  high: 'bg-marker text-ink',
  medium: 'bg-marker-soft text-ink',
  low: 'bg-redpen-soft text-redpen',
}

const bandWords: Record<ScoreBand, string> = {
  high: 'Encaje alto',
  medium: 'Encaje medio',
  low: 'Encaje bajo',
}

export function BandPill({ band, score }: { band: ScoreBand; score?: number | null }) {
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold ${styles[band]}`}>
      {bandWords[band]}
      {score != null && <span className="font-normal">{score}/100</span>}
    </span>
  )
}
