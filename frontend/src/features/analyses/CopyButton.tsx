import { useEffect, useState } from 'react'
import { Button } from '../../shared/ui/Button'

export function CopyButton({ text, label }: { text: string; label: string }) {
  const [state, setState] = useState<'idle' | 'copied' | 'failed'>('idle')

  useEffect(() => {
    if (state !== 'copied') return
    const timer = setTimeout(() => setState('idle'), 2500)
    return () => clearTimeout(timer)
  }, [state])

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
      <span aria-live="polite" className="text-xs text-graphite">
        {state === 'failed' ? 'No se pudo copiar; selecciona el texto manualmente.' : ''}
      </span>
      <Button variant={state === 'copied' ? 'primary' : 'secondary'} onClick={() => void copy()} aria-label={label}>
        {state === 'copied' ? 'Copiado ✓' : 'Copiar'}
      </Button>
    </span>
  )
}
