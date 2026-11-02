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
      <h1 id="page-title" className="text-3xl font-bold">
        {title}
      </h1>
      <p className="mt-2 text-graphite">{description}</p>
      <p className="mt-6 rounded-[var(--radius-sheet)] border-2 border-dashed border-rule bg-sheet p-4 text-sm text-graphite">
        Pantalla en construcción. Disponible a partir de la Fase {phase}.
      </p>
      {children}
    </section>
  )
}
