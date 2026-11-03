import { useEffect, useRef, useState } from 'react'
import { useInView, usePrefersReducedMotion } from '../../shared/lib/motion'

const stages = ['Guardada', 'Postulé', 'Entrevista', 'Oferta'] as const

/** A posting card hopping through application stages: shows the tracking feature at a glance. */
export function PipelineDemo() {
  const ref = useRef<HTMLDivElement>(null)
  const inView = useInView(ref)
  const reduced = usePrefersReducedMotion()
  const [stage, setStage] = useState(0)

  useEffect(() => {
    if (!inView || reduced) return
    const timer = setInterval(() => setStage((current) => (current + 1) % (stages.length + 1)), 1100)
    return () => clearInterval(timer)
  }, [inView, reduced])

  const current = reduced ? stages.length - 1 : Math.min(stage, stages.length - 1)

  return (
    <div ref={ref} aria-hidden="true" className="grid grid-cols-4 gap-2">
      {stages.map((name, index) => (
        <div key={name} className="flex min-h-28 flex-col rounded-[var(--radius-field)] border border-dashed border-graphite/40 bg-sheet/60 p-2">
          <p className="text-center text-xs font-semibold text-graphite">{name}</p>
          {index === current && (
            <div key={`${name}-${stage}`} className="rise mt-2 rounded-[var(--radius-field)] border border-rule bg-sheet p-2 shadow-sm">
              <p className="text-xs font-bold leading-tight">Diseñadora UX</p>
              <p className="text-[0.7rem] text-graphite">Estudio Ejemplo</p>
              {name === 'Oferta' && <p className="pop mt-1 font-[family-name:var(--font-hand)] text-base leading-none text-pen">¡bien!</p>}
            </div>
          )}
        </div>
      ))}
    </div>
  )
}
