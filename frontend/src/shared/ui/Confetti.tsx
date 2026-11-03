import type { CSSProperties } from 'react'
import { usePrefersReducedMotion } from '../lib/motion'

const colors = ['var(--color-marker)', 'var(--color-pen)', 'var(--color-redpen)', '#f2b84b', 'var(--color-pen-soft)']

/** A one-off burst of paper bits. Renders nothing under reduced motion. Remount (change `key`) to replay. */
export function Confetti({ pieces = 26 }: { pieces?: number }) {
  const reduced = usePrefersReducedMotion()
  if (reduced) return null

  return (
    <span aria-hidden="true" className="pointer-events-none absolute inset-x-0 top-0 z-10 block h-0 overflow-visible">
      {Array.from({ length: pieces }, (_, index) => {
        // Deterministic spread so renders are stable.
        const angle = (index / pieces) * Math.PI
        const style = {
          left: `${50 + Math.cos(angle) * 8}%`,
          '--dx': `${Math.round(Math.cos(angle) * 160 + ((index * 37) % 40) - 20)}px`,
          '--spin': `${(index % 2 === 0 ? 1 : -1) * (180 + ((index * 53) % 360))}deg`,
          background: colors[index % colors.length],
          animation: `confetti-fall ${900 + ((index * 71) % 600)}ms cubic-bezier(0.2, 0.7, 0.4, 1) ${(index * 17) % 120}ms both`,
        } as CSSProperties
        return <span key={index} className="absolute h-2.5 w-1.5 rounded-[1px]" style={style} />
      })}
    </span>
  )
}
