import type { ButtonHTMLAttributes } from 'react'

const variants = {
  primary: 'bg-pen text-white hover:bg-pen-strong disabled:bg-graphite/40',
  secondary: 'border border-rule bg-sheet text-ink hover:border-pen hover:text-pen disabled:opacity-60',
  ghost: 'text-graphite hover:bg-pen-soft hover:text-pen-strong',
  danger: 'text-redpen hover:bg-redpen-soft',
} as const

const sizes = {
  md: 'px-4 py-2 text-sm',
  lg: 'px-5 py-3 text-base',
} as const

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: keyof typeof variants
  size?: keyof typeof sizes
}

export function Button({ variant = 'primary', size = 'md', className = '', type = 'button', ...props }: ButtonProps) {
  return (
    <button
      type={type}
      className={`inline-flex items-center justify-center gap-2 rounded-[var(--radius-field)] font-semibold transition-colors disabled:cursor-not-allowed ${variants[variant]} ${sizes[size]} ${className}`}
      {...props}
    />
  )
}

/** Same look as Button for router links that act as primary actions. */
export const primaryLinkClass =
  'inline-flex items-center justify-center gap-2 rounded-[var(--radius-field)] bg-pen px-4 py-2 text-sm font-semibold text-white no-underline hover:bg-pen-strong'
export const secondaryLinkClass =
  'inline-flex items-center justify-center gap-2 rounded-[var(--radius-field)] border border-rule bg-sheet px-4 py-2 text-sm font-semibold text-ink no-underline hover:border-pen hover:text-pen'
