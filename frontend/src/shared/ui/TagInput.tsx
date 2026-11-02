import { useId, useState, type ClipboardEvent, type KeyboardEvent } from 'react'

interface TagInputProps {
  label: string
  hint?: string
  /** Comma-separated value, so it plugs into existing string form fields. */
  value: string
  onChange: (value: string) => void
  placeholder?: string
  error?: string | undefined
}

const split = (text: string) =>
  text
    .split(',')
    .map((item) => item.trim())
    .filter((item) => item.length > 0)

/** Chips you add with Enter or a comma and remove with ×. Duplicates (ignoring case) are skipped. */
export function TagInput({ label, hint, value, onChange, placeholder = 'Escribe y pulsa Enter', error }: TagInputProps) {
  const id = useId()
  const [draft, setDraft] = useState('')
  const tags = split(value)

  const add = (text: string) => {
    const known = new Set(tags.map((tag) => tag.toLocaleLowerCase('es')))
    const fresh: string[] = []
    for (const item of split(text)) {
      const key = item.toLocaleLowerCase('es')
      if (!known.has(key)) {
        known.add(key)
        fresh.push(item)
      }
    }
    if (fresh.length > 0) onChange([...tags, ...fresh].join(', '))
    setDraft('')
  }

  const remove = (index: number) => onChange(tags.filter((_, position) => position !== index).join(', '))

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault()
      add(draft)
    } else if (event.key === 'Backspace' && draft === '' && tags.length > 0) {
      remove(tags.length - 1)
    }
  }

  const onPaste = (event: ClipboardEvent<HTMLInputElement>) => {
    const text = event.clipboardData.getData('text')
    if (text.includes(',') || text.includes('\n')) {
      event.preventDefault()
      add(text.replaceAll('\n', ','))
    }
  }

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-semibold text-ink">
        {label}
      </label>
      <div
        className={`mt-1.5 flex flex-wrap items-center gap-1.5 rounded-[var(--radius-field)] border bg-sheet p-1.5 focus-within:border-pen focus-within:ring-2 focus-within:ring-pen-soft ${
          error !== undefined ? 'border-redpen' : 'border-rule'
        }`}
      >
        {tags.length > 0 && (
          <ul aria-label={`${label} agregadas`} className="contents">
            {tags.map((tag, index) => (
              <li key={tag} className="inline-flex items-center gap-1 rounded-[var(--radius-field)] bg-marker-soft py-0.5 pl-2.5 pr-1 text-sm font-semibold">
                {tag}
                <button
                  type="button"
                  onClick={() => remove(index)}
                  aria-label={`Quitar ${tag}`}
                  className="grid size-5 place-items-center rounded text-graphite hover:bg-marker hover:text-ink"
                >
                  ×
                </button>
              </li>
            ))}
          </ul>
        )}
        <input
          id={id}
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={onKeyDown}
          onPaste={onPaste}
          onBlur={() => add(draft)}
          placeholder={tags.length === 0 ? placeholder : 'Agregar otra'}
          aria-describedby={hint !== undefined ? `${id}-hint` : undefined}
          aria-invalid={error !== undefined}
          className="min-w-32 flex-1 bg-transparent px-1.5 py-1 text-[0.95rem] placeholder:text-graphite/70 focus:outline-none"
        />
      </div>
      {hint !== undefined && (
        <p id={`${id}-hint`} className="mt-1 text-xs text-graphite">
          {hint}
        </p>
      )}
      {error !== undefined && <p className="mt-1 text-xs text-redpen">{error}</p>}
    </div>
  )
}
