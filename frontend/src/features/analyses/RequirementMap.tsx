import { useId, useRef, useState, type CSSProperties } from 'react'
import { useInView } from '../../shared/lib/motion'
import type { AnalysisGap, AnalysisStrength, JobAnalysisResult } from '../../shared/api/types'
import { gapTypeLabels, importanceLabels } from './analysisLabels'

type Filter = 'all' | 'met' | 'missing'

type Row =
  | { kind: 'met'; key: string; item: AnalysisStrength }
  | { kind: 'missing'; key: string; item: AnalysisGap }

const filters: { value: Filter; label: string }[] = [
  { value: 'all', label: 'Todos' },
  { value: 'met', label: 'Lo tienes' },
  { value: 'missing', label: 'Te falta' },
]

function buildRows(result: JobAnalysisResult): Row[] {
  const rows: Row[] = [
    ...result.strengths.map((item, index): Row => ({ kind: 'met', key: `s${index}`, item })),
    ...result.gaps.map((item, index): Row => ({ kind: 'missing', key: `g${index}`, item })),
  ]
  // Required first; within each importance, what you have before what you lack.
  return rows.sort((a, b) => {
    const importance = Number(a.item.importance === 'preferred') - Number(b.item.importance === 'preferred')
    return importance !== 0 ? importance : Number(a.kind === 'missing') - Number(b.kind === 'missing')
  })
}

/**
 * The job's requirements, marked up the way you would mark a printed CV:
 * highlighted when your profile proves it, underlined in red when it does not.
 * Each row opens to show the evidence or what to do about it.
 */
export function RequirementMap({ result }: { result: JobAnalysisResult }) {
  const [filter, setFilter] = useState<Filter>('all')
  const [open, setOpen] = useState<ReadonlySet<string>>(new Set())
  const name = useId()
  const sectionRef = useRef<HTMLElement>(null)
  // The highlighter draws over each requirement the first time the list comes into view.
  const drawn = useInView(sectionRef, { once: true, threshold: 0.2 })
  const rows = buildRows(result)
  const visible = rows.filter((row) => filter === 'all' || row.kind === filter)
  const counts: Record<Filter, number> = {
    all: rows.length,
    met: result.strengths.length,
    missing: result.gaps.length,
  }

  const toggle = (key: string) =>
    setOpen((current) => {
      const next = new Set(current)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })

  return (
    <section ref={sectionRef} aria-labelledby="map-title" className="rounded-[var(--radius-sheet)] border border-rule bg-sheet p-5 md:p-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 id="map-title" className="text-xl font-bold">
            Requisitos de la vacante
          </h2>
          <p className="mt-1 text-sm text-graphite">Toca un requisito para ver la evidencia de tu perfil o qué hacer al respecto.</p>
        </div>
        <fieldset className="flex rounded-full border border-rule bg-paper p-1">
          <legend className="sr-only">Mostrar</legend>
          {filters.map((option) => (
            <label
              key={option.value}
              className="cursor-pointer rounded-full px-3 py-1.5 text-sm font-semibold text-graphite has-[:checked]:bg-ink has-[:checked]:text-sheet has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-pen"
            >
              <input
                type="radio"
                name={name}
                value={option.value}
                checked={filter === option.value}
                onChange={() => setFilter(option.value)}
                className="sr-only"
              />
              {option.label} <span className="font-normal">({counts[option.value]})</span>
            </label>
          ))}
        </fieldset>
      </div>

      {visible.length === 0 ? (
        <p className="mt-5 text-sm text-graphite">
          {filter === 'met'
            ? 'Con la información de tu perfil no encontramos requisitos claramente cumplidos.'
            : 'No se identificaron requisitos faltantes.'}
        </p>
      ) : (
        <ul aria-label="Requisitos" className="mt-5 divide-y divide-rule border-y border-rule">
          {visible.map((row, index) => {
            const expanded = open.has(row.key)
            const panelId = `${name}-${row.key}`
            return (
              <li key={row.key}>
                <button
                  type="button"
                  aria-expanded={expanded}
                  aria-controls={panelId}
                  onClick={() => toggle(row.key)}
                  className="flex w-full items-start gap-3 py-3 text-left hover:bg-paper focus-visible:outline-2 focus-visible:outline-pen"
                >
                  <span
                    aria-hidden="true"
                    className={`mt-0.5 grid size-6 shrink-0 place-items-center rounded-full text-sm font-bold ${
                      row.kind === 'met' ? 'bg-marker text-ink' : 'border-2 border-redpen text-redpen'
                    }`}
                  >
                    {row.kind === 'met' ? '✓' : '!'}
                  </span>
                  <span className="flex-1">
                    <span
                      className={`font-semibold transition-[text-decoration-color] duration-500 ${
                        row.kind === 'met' ? `marker-sweep ${drawn ? 'is-on' : ''}` : 'redline'
                      }`}
                      style={
                        {
                          transitionDelay: `${index * 110}ms`,
                          ...(row.kind === 'missing' && !drawn ? { textDecorationColor: 'transparent' } : {}),
                        } as CSSProperties
                      }
                    >
                      {row.item.requirement}
                    </span>
                    <span className="sr-only">{row.kind === 'met' ? ', lo tienes' : ', te falta'}</span>
                    <span className="mt-1 flex flex-wrap gap-2 text-xs">
                      <span className={row.item.importance === 'required' ? 'font-semibold text-ink' : 'text-graphite'}>
                        {importanceLabels[row.item.importance]}
                      </span>
                      {row.kind === 'missing' && <span className="text-redpen">{gapTypeLabels[row.item.gapType]}</span>}
                    </span>
                  </span>
                  <span aria-hidden="true" className={`mt-1 text-graphite transition-transform ${expanded ? 'rotate-180' : ''}`}>
                    ▾
                  </span>
                </button>
                <div id={panelId} hidden={!expanded} className="pb-4 pl-9 pr-2 text-sm">
                  {row.kind === 'met' ? (
                    <p>
                      <span className="font-semibold">Evidencia en tu perfil:</span> {row.item.evidence}
                    </p>
                  ) : (
                    <p>
                      <span className="font-semibold">Qué puedes hacer:</span> {row.item.recommendation}
                    </p>
                  )}
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
