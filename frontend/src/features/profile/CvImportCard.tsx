import { useMutation } from '@tanstack/react-query'
import { useId, useRef, useState, type ChangeEvent } from 'react'
import { ApiError } from '../../shared/api/ApiError'
import type { CvImportResponse } from '../../shared/api/types'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { CV_MAX_BYTES, importCv } from './profileApi'

const ACCEPTED = '.pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document'

interface CvImportCardProps {
  hasExistingData: boolean
  onImported: (result: CvImportResponse) => void
}

/** Lets the user pre-fill the form from a CV. Nothing is saved until they press "Guardar perfil". */
export function CvImportCard({ hasExistingData, onImported }: CvImportCardProps) {
  const inputId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const mutation = useMutation({ mutationFn: importCv })

  const onFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    setError(null)
    const selected = event.target.files?.[0] ?? null
    if (selected !== null && selected.size > CV_MAX_BYTES) {
      setError('El archivo supera el máximo de 5 MB.')
      setFile(null)
      return
    }
    if (selected !== null && !/\.(pdf|docx)$/i.test(selected.name)) {
      setError('Sube tu CV en PDF o Word (.docx).')
      setFile(null)
      return
    }
    setFile(selected)
  }

  const onImport = async () => {
    if (file === null) return
    setError(null)
    try {
      const result = await mutation.mutateAsync(file)
      onImported(result)
      setFile(null)
      if (inputRef.current !== null) inputRef.current.value = ''
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No pudimos leer tu CV. Intenta de nuevo.')
    }
  }

  return (
    <section aria-labelledby="cv-import-title" className="rounded-lg border border-accent/30 bg-accent-soft p-5">
      <h2 id="cv-import-title" className="text-lg font-semibold text-accent-strong">
        Importar desde tu CV
      </h2>
      <p className="mt-1 text-sm text-ink">
        Sube tu CV en PDF o Word (.docx, máx. 5 MB) y precargamos el formulario. Solo copiamos lo que dice tu CV; revisa todo
        antes de guardar. El archivo no se guarda.
      </p>
      {hasExistingData && (
        <p className="mt-2 text-sm text-ink-muted">Reemplazará lo que hay ahora en el formulario. No se guarda nada hasta que pulses «Guardar perfil».</p>
      )}
      <div className="mt-4 flex flex-wrap items-center gap-3">
        <label htmlFor={inputId} className="sr-only">
          Archivo del CV
        </label>
        <input
          ref={inputRef}
          id={inputId}
          type="file"
          accept={ACCEPTED}
          onChange={onFileChange}
          disabled={mutation.isPending}
          className="block text-sm file:mr-3 file:rounded-md file:border-0 file:bg-surface file:px-3 file:py-2 file:text-sm file:font-medium"
        />
        <Button onClick={() => void onImport()} disabled={file === null || mutation.isPending}>
          {mutation.isPending ? 'Leyendo tu CV…' : 'Importar CV'}
        </Button>
      </div>
      {mutation.isPending && (
        <output aria-live="polite" className="mt-3 block text-sm text-accent-strong">
          Extrayendo los datos de tu CV. Puede tardar hasta un minuto.
        </output>
      )}
      {error !== null && (
        <div className="mt-3">
          <Alert tone="error">{error}</Alert>
        </div>
      )}
    </section>
  )
}
