import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react'

const controlClass =
  'mt-1 block w-full rounded-md border bg-surface px-3 py-2 text-sm text-ink shadow-sm placeholder:text-ink-muted/70 focus:border-accent'

interface FieldShellProps {
  id: string
  label: string
  hint?: string | undefined
  error?: string | undefined
  required?: boolean | undefined
  children: ReactNode
}

function FieldShell({ id, label, hint, error, required, children }: FieldShellProps) {
  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium text-ink">
        {label}
        {required === true && (
          <span className="text-red-700" aria-hidden="true">
            {' '}
            *
          </span>
        )}
      </label>
      {children}
      {hint !== undefined && error === undefined && (
        <p id={`${id}-hint`} className="mt-1 text-xs text-ink-muted">
          {hint}
        </p>
      )}
      {error !== undefined && (
        <p id={`${id}-error`} className="mt-1 text-xs text-red-700">
          {error}
        </p>
      )}
    </div>
  )
}

function describedBy(id: string, hint: string | undefined, error: string | undefined): string | undefined {
  if (error !== undefined) return `${id}-error`
  if (hint !== undefined) return `${id}-hint`
  return undefined
}

interface CommonProps {
  label: string
  hint?: string | undefined
  error?: string | undefined
}

export function TextField({ label, hint, error, required, ...props }: CommonProps & InputHTMLAttributes<HTMLInputElement>) {
  const id = useId()
  return (
    <FieldShell id={id} label={label} hint={hint} error={error} required={required}>
      <input
        id={id}
        aria-invalid={error !== undefined}
        aria-describedby={describedBy(id, hint, error)}
        aria-required={required}
        className={`${controlClass} ${error !== undefined ? 'border-red-400' : 'border-line'}`}
        {...props}
      />
    </FieldShell>
  )
}

export function TextAreaField({ label, hint, error, required, ...props }: CommonProps & TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const id = useId()
  return (
    <FieldShell id={id} label={label} hint={hint} error={error} required={required}>
      <textarea
        id={id}
        rows={4}
        aria-invalid={error !== undefined}
        aria-describedby={describedBy(id, hint, error)}
        aria-required={required}
        className={`${controlClass} ${error !== undefined ? 'border-red-400' : 'border-line'}`}
        {...props}
      />
    </FieldShell>
  )
}

interface SelectFieldProps extends CommonProps, SelectHTMLAttributes<HTMLSelectElement> {
  options: readonly { value: string; label: string }[]
}

export function SelectField({ label, hint, error, required, options, ...props }: SelectFieldProps) {
  const id = useId()
  return (
    <FieldShell id={id} label={label} hint={hint} error={error} required={required}>
      <select
        id={id}
        aria-invalid={error !== undefined}
        aria-describedby={describedBy(id, hint, error)}
        className={`${controlClass} ${error !== undefined ? 'border-red-400' : 'border-line'}`}
        {...props}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </FieldShell>
  )
}

export function CheckboxField({ label, ...props }: { label: string } & InputHTMLAttributes<HTMLInputElement>) {
  const id = useId()
  return (
    <div className="flex items-center gap-2">
      <input id={id} type="checkbox" className="size-4 rounded border-line accent-accent" {...props} />
      <label htmlFor={id} className="text-sm text-ink">
        {label}
      </label>
    </div>
  )
}
