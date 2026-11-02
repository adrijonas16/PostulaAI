import { useId, useState } from 'react'
import { CopyButton } from './CopyButton'

/** A generated text the user can adjust before copying. Edits stay on this screen; nothing is saved. */
export function EditableDraft({ id, title, original, copyLabel, hint }: { id: string; title: string; original: string; copyLabel: string; hint?: string }) {
  const [text, setText] = useState(original)
  const fieldId = useId()
  const edited = text !== original

  return (
    <section aria-labelledby={id} className="rounded-[var(--radius-sheet)] border border-rule bg-sheet p-5 md:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 id={id} className="text-lg font-bold">
          {title}
        </h2>
        <CopyButton text={text} label={copyLabel} />
      </div>
      <label htmlFor={fieldId} className="mt-3 block text-sm text-graphite">
        Puedes editarlo aquí antes de copiarlo. {hint}
      </label>
      <textarea
        id={fieldId}
        value={text}
        onChange={(event) => setText(event.target.value)}
        rows={Math.min(16, Math.max(4, Math.ceil(original.length / 90) + original.split('\n').length))}
        className="mt-2 w-full resize-y rounded-[var(--radius-field)] border border-rule bg-paper p-3 text-[0.95rem] leading-relaxed focus:border-pen focus:bg-sheet focus:outline-none focus:ring-2 focus:ring-pen-soft"
      />
      {edited && (
        <button type="button" onClick={() => setText(original)} className="mt-2 text-sm font-semibold text-pen underline">
          Volver al texto original
        </button>
      )}
    </section>
  )
}
