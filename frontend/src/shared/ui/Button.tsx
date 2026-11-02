import type { ButtonHTMLAttributes } from 'react'

const variants = {
  primary: 'bg-accent text-white hover:bg-accent-strong disabled:opacity-60',
  secondary: 'border border-line bg-surface text-ink hover:border-ink-muted disabled:opacity-60',
  ghost: 'text-ink-muted hover:bg-canvas hover:text-ink',
  danger: 'text-red-700 hover:bg-red-50',
} as const

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: keyof typeof variants
}

export function Button({ variant = 'primary', className = '', type = 'button', ...props }: ButtonProps) {
  return (
    <button
      type={type}
      className={`inline-flex items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-colors disabled:cursor-not-allowed ${variants[variant]} ${className}`}
      {...props}
    />
  )
}
