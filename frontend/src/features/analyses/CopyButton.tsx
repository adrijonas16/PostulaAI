import { useState } from 'react'
import { Button } from '../../shared/ui/Button'

export function CopyButton({ text, label }: { text: string; label: string }) {
  const [state, setState] = useState<'idle' | 'copied' | 'failed'>('idle')

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(text)
      setState('copied')
    } catch {
      setState('failed')
    }
  }

  return (
    <span className="inline-flex items-center gap-2">
      <Button variant="secondary" onClick={() => void copy()} aria-label={label}>
        Copiar
      </Button>
      <span aria-live="polite" className="text-xs text-ink-muted">
        {state === 'copied' ? 'Copiado' : state === 'failed' ? 'No se pudo copiar; selecciona el texto manualmente.' : ''}
      </span>
    </span>
  )
}
