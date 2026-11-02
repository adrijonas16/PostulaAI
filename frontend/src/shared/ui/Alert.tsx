import type { ReactNode } from 'react'

const tones = {
  error: 'border-red-200 bg-red-50 text-red-900',
  success: 'border-emerald-200 bg-emerald-50 text-emerald-900',
  info: 'border-line bg-surface text-ink',
} as const

interface AlertProps {
  tone: keyof typeof tones
  children: ReactNode
}

/** Errors are announced immediately (role=alert); other messages politely (role=status). */
export function Alert({ tone, children }: AlertProps) {
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={`rounded-md border p-3 text-sm ${tones[tone]}`}>
      {children}
    </div>
  )
}
