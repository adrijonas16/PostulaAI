import type { ReactNode } from 'react'

interface SheetProps {
  id?: string
  title?: string
  description?: ReactNode
  action?: ReactNode
  children: ReactNode
  className?: string
}

/** A white sheet of paper on the desk: the one container shape used for grouped content. */
export function Sheet({ id, title, description, action, children, className = '' }: SheetProps) {
  return (
    <section aria-labelledby={title === undefined ? undefined : id} className={`rounded-[var(--radius-sheet)] border border-rule bg-sheet p-5 md:p-6 ${className}`}>
      {(title !== undefined || action !== undefined) && (
        <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
          <div>
            {title !== undefined && (
              <h2 id={id} className="text-lg font-bold">
                {title}
              </h2>
            )}
            {description !== undefined && <p className="mt-1 text-sm text-graphite">{description}</p>}
          </div>
          {action}
        </div>
      )}
      {children}
    </section>
  )
}

export function PageHeader({ title, description, action }: { title: string; description?: ReactNode; action?: ReactNode }) {
  return (
    <header className="mb-8 flex flex-wrap items-end justify-between gap-4">
      <div className="max-w-2xl">
        <h1 id="page-title" className="text-3xl font-bold">
          {title}
        </h1>
        {description !== undefined && <p className="mt-2 text-graphite">{description}</p>}
      </div>
      {action}
    </header>
  )
}
