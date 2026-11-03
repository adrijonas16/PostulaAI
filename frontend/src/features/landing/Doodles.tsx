/** Hand-drawn style line illustrations for the landing steps. Decorative only. */
const stroke = { fill: 'none', stroke: 'var(--color-ink)', strokeWidth: 2.2, strokeLinecap: 'round', strokeLinejoin: 'round' } as const

export function CvDoodle() {
  return (
    <svg viewBox="0 0 120 100" className="h-24 w-28" aria-hidden="true">
      <rect {...stroke} x="30" y="10" width="58" height="76" rx="6" fill="var(--color-sheet)" />
      <circle {...stroke} cx="46" cy="28" r="7" fill="var(--color-pen-soft)" />
      <path d="M58 25 H78 M58 32 H72" {...stroke} />
      <rect x="40" y="46" width="38" height="6" rx="2" fill="var(--color-marker)" stroke="none" />
      <path d="M40 49 H78 M40 60 H76 M40 70 H64" {...stroke} />
      <path d="M86 70 l14 -14 M96 52 l6 6" stroke="var(--color-pen)" strokeWidth="2.2" strokeLinecap="round" fill="none" />
      <path d="M8 46 q8 -10 18 -4" stroke="var(--color-pen)" strokeWidth="2" fill="none" strokeLinecap="round" />
      <path d="M22 38 l4 4 -6 2" stroke="var(--color-pen)" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

export function PostingDoodle() {
  return (
    <svg viewBox="0 0 120 100" className="h-24 w-28" aria-hidden="true">
      <rect {...stroke} x="26" y="16" width="62" height="72" rx="6" fill="var(--color-sheet)" />
      <rect {...stroke} x="44" y="9" width="26" height="12" rx="3" fill="var(--color-pen-soft)" />
      <path d="M38 36 H76 M38 46 H70 M38 56 H74 M38 66 H62" {...stroke} />
      <path d="M92 30 v20 M82 40 h20" stroke="var(--color-pen)" strokeWidth="2.6" strokeLinecap="round" />
      <path d="M8 74 q6 6 14 2" stroke="var(--color-marker)" strokeWidth="5" fill="none" strokeLinecap="round" />
    </svg>
  )
}

export function VerdictDoodle() {
  return (
    <svg viewBox="0 0 120 100" className="h-24 w-28" aria-hidden="true">
      <rect {...stroke} x="18" y="20" width="70" height="60" rx="6" fill="var(--color-sheet)" />
      <rect x="30" y="34" width="34" height="6" rx="2" fill="var(--color-marker)" stroke="none" />
      <path d="M30 37 H64" {...stroke} />
      <rect x="30" y="48" width="28" height="6" rx="2" fill="var(--color-marker)" stroke="none" />
      <path d="M30 51 H58" {...stroke} />
      <path d="M30 64 q3 -3 6 0 t6 0 t6 0 t6 0" stroke="var(--color-redpen)" strokeWidth="2" fill="none" strokeLinecap="round" />
      <circle {...stroke} cx="90" cy="64" r="16" fill="var(--color-marker-soft)" />
      <path d="M83 64 l5 5 9 -10" stroke="var(--color-pen)" strokeWidth="3" fill="none" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

export function ArrowDoodle({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 80 50" className={className} aria-hidden="true">
      <path d="M4 8 C 30 4, 52 14, 64 38" stroke="var(--color-pen)" strokeWidth="2.4" fill="none" strokeLinecap="round" />
      <path d="M54 34 L65 40 L68 27" stroke="var(--color-pen)" strokeWidth="2.4" fill="none" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}
