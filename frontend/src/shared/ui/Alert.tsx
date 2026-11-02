import type { ReactNode } from 'react'

const tones = {
  error: 'border-redpen/40 bg-redpen-soft text-ink',
  success: 'border-marker bg-marker-soft text-ink',
  info: 'border-pen/25 bg-pen-soft text-ink',
} as const

const marks = { error: '!', success: '✓', info: 'i' } as const
const markStyles = { error: 'bg-redpen text-white', success: 'bg-marker text-ink', info: 'bg-pen text-white' } as const

interface AlertProps {
  tone: keyof typeof tones
  children: ReactNode
}

/** Errors are announced immediately (role=alert); other messages politely (role=status). */
export function Alert({ tone, children }: AlertProps) {
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={`flex gap-3 rounded-[var(--radius-field)] border p-3 text-sm ${tones[tone]}`}>
      <span aria-hidden="true" className={`mt-0.5 grid size-5 shrink-0 place-items-center rounded-full text-xs font-bold ${markStyles[tone]}`}>
        {marks[tone]}
      </span>
      <div className="min-w-0">{children}</div>
    </div>
  )
}
