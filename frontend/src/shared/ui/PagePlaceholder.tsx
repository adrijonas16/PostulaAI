import type { ReactNode } from 'react'

interface PagePlaceholderProps {
  title: string
  description: string
  phase: number
  children?: ReactNode
}

/** Honest placeholder for screens that are planned but not implemented yet. */
export function PagePlaceholder({ title, description, phase, children }: PagePlaceholderProps) {
  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-3xl">
      <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
        {title}
      </h1>
      <p className="mt-2 text-ink-muted">{description}</p>
      <p className="mt-6 rounded-lg border border-dashed border-line bg-surface p-4 text-sm text-ink-muted">
        Pantalla en construcción. Disponible a partir de la Fase {phase}.
      </p>
      {children}
    </section>
  )
}
