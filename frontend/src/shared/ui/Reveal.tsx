import { useRef, type CSSProperties, type ReactNode } from 'react'
import { useInView } from '../lib/motion'

/** Fades and lifts its content in the first time it scrolls into view. Static under reduced motion (CSS). */
export function Reveal({ children, delay = 0, className = '' }: { children: ReactNode; delay?: number; className?: string }) {
  const ref = useRef<HTMLDivElement>(null)
  const shown = useInView(ref, { once: true, threshold: 0.15 })
  return (
    <div ref={ref} data-reveal={shown ? 'shown' : 'hidden'} style={{ '--reveal-delay': `${delay}ms` } as CSSProperties} className={className}>
      {children}
    </div>
  )
}
