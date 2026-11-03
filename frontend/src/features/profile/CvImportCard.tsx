import { useMutation } from '@tanstack/react-query'
import { useEffect, useRef, useState, type ChangeEvent, type DragEvent } from 'react'
import { ApiError } from '../../shared/api/ApiError'
import type { CvImportResponse } from '../../shared/api/types'
import { useAiBlockReason } from '../../app/useAiAvailability'
import { AiUnavailable } from '../../shared/ui/AiUnavailable'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { CV_MAX_BYTES, importCv } from './profileApi'

const ACCEPTED = '.pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document'

interface CvImportCardProps {
  hasExistingData: boolean
  /** Big drop zone for an empty profile; a slim bar once the profile has data. */
  prominent: boolean
  onImported: (result: CvImportResponse) => void
  /** Lets the page lock the form while the CV is being read. */
  onBusyChange?: (busy: boolean) => void
}

function ImportProgress() {
  const [seconds, setSeconds] = useState(0)
  useEffect(() => {
    const timer = setInterval(() => setSeconds((value) => value + 1), 1000)
    return () => clearInterval(timer)
  }, [])

  return (
    <output aria-live="polite" className="mt-5 block rounded-[var(--radius-field)] bg-pen-soft p-4 text-left text-pen-strong">
      <span className="flex items-center justify-between gap-3 font-bold">
        Leyendo tu CV y llenando el formulario…
        <span className="font-normal tabular-nums" aria-hidden="true">
          {seconds} s
        </span>
      </span>
      <span className="mt-3 block h-2 overflow-hidden rounded-full bg-sheet">
        <span className="block h-full w-1/3 animate-[progress_1.6s_ease-in-out_infinite] rounded-full bg-pen motion-reduce:animate-none" />
      </span>
      <span className="mt-3 block text-sm">
        Suele tardar entre 20 segundos y un minuto. No cierres esta página; cuando termine, verás tus datos en cada pestaña.
      </span>
    </output>
  )
}

function validate(file: File): string | null {
  if (file.size > CV_MAX_BYTES) return 'El archivo supera el máximo de 5 MB.'
  if (!/\.(pdf|docx)$/i.test(file.name)) return 'Sube tu CV en PDF o Word (.docx).'
  return null
}

/** Lets the user pre-fill the form from a CV. Nothing is saved until they press "Guardar perfil". */
export function CvImportCard({ hasExistingData, prominent, onImported, onBusyChange }: CvImportCardProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const errorRef = useRef<HTMLDivElement>(null)
  const blockReason = useAiBlockReason()
  const unavailable = blockReason !== null
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [dragging, setDragging] = useState(false)
  const mutation = useMutation({ mutationFn: importCv })

  const choose = (selected: File | null) => {
    setError(null)
    if (selected === null) {
      setFile(null)
      return
    }
    const problem = validate(selected)
    setError(problem)
    setFile(problem === null ? selected : null)
  }

  const onFileChange = (event: ChangeEvent<HTMLInputElement>) => choose(event.target.files?.[0] ?? null)

  const onDrop = (event: DragEvent<HTMLElement>) => {
    event.preventDefault()
    setDragging(false)
    if (!mutation.isPending) choose(event.dataTransfer.files[0] ?? null)
  }

  // Bring a failure into view: the card may be far from where the user is looking.
  useEffect(() => {
    if (error !== null) {
      errorRef.current?.focus()
      errorRef.current?.scrollIntoView?.({ block: 'center', behavior: 'smooth' })
    }
  }, [error])

  const onImport = async () => {
    if (file === null) return
    setError(null)
    onBusyChange?.(true)
    try {
      const result = await mutation.mutateAsync(file)
      onImported(result)
      setFile(null)
      if (inputRef.current !== null) inputRef.current.value = ''
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No pudimos leer tu CV. Intenta de nuevo.')
    } finally {
      onBusyChange?.(false)
    }
  }

  return (
    // Dropping a file is a mouse shortcut; keyboard and screen-reader users use the "Elegir archivo" button.
    // oxlint-disable-next-line jsx-a11y/no-noninteractive-element-interactions
    <section
      aria-labelledby="cv-import-title"
      onDragOver={(event) => {
        event.preventDefault()
        setDragging(true)
      }}
      onDragLeave={() => setDragging(false)}
      onDrop={onDrop}
      className={`rounded-[var(--radius-sheet)] border-2 border-dashed transition-colors ${
        dragging ? 'border-pen bg-pen-soft' : 'border-graphite/40 bg-sheet'
      } ${prominent ? 'p-6 md:p-8' : 'p-4'}`}
    >
      <div className={prominent ? 'text-center' : 'flex flex-wrap items-center justify-between gap-3'}>
        <div>
          <h2 id="cv-import-title" className={prominent ? 'text-xl font-bold' : 'font-bold'}>
            {prominent ? 'Empieza con tu CV' : 'Importar desde tu CV'}
          </h2>
          <p className={`text-sm text-graphite ${prominent ? 'mx-auto mt-2 max-w-md' : 'mt-0.5'}`}>
            {prominent
              ? 'Arrastra aquí tu CV en PDF o Word (máx. 5 MB) y llenamos el formulario por ti. Solo copiamos lo que dice tu CV y el archivo no se guarda.'
              : 'Arrastra un PDF o Word aquí para volver a llenar el formulario.'}
          </p>
        </div>

        <div className={`flex flex-wrap items-center gap-3 ${prominent ? 'mt-5 justify-center' : ''}`}>
          <input
            ref={inputRef}
            type="file"
            accept={ACCEPTED}
            aria-label="Archivo del CV"
            onChange={onFileChange}
            disabled={mutation.isPending || unavailable}
            className="sr-only"
          />
          {file === null ? (
            <Button variant={prominent ? 'primary' : 'secondary'} size={prominent ? 'lg' : 'md'} onClick={() => inputRef.current?.click()} disabled={mutation.isPending || unavailable}>
              Elegir archivo
            </Button>
          ) : (
            <>
              <span className="inline-flex items-center gap-2 rounded-[var(--radius-field)] bg-marker-soft px-3 py-2 text-sm font-semibold">
                {file.name}
                <button
                  type="button"
                  onClick={() => choose(null)}
                  disabled={mutation.isPending}
                  aria-label="Quitar archivo"
                  className="text-graphite hover:text-ink"
                >
                  ×
                </button>
              </span>
              <Button size={prominent ? 'lg' : 'md'} onClick={() => void onImport()} disabled={mutation.isPending || unavailable}>
                {mutation.isPending ? 'Leyendo tu CV…' : 'Importar CV'}
              </Button>
            </>
          )}
        </div>
      </div>

      {file !== null && hasExistingData && !mutation.isPending && (
        <p className={`mt-3 text-sm text-graphite ${prominent ? 'text-center' : ''}`}>
          Reemplazará lo que hay ahora en el formulario. No se guarda nada hasta que pulses «Guardar perfil».
        </p>
      )}
      {blockReason !== null && (
        <div className="mt-5 text-left">
          <AiUnavailable reason={blockReason} feature="la importación del CV" />
          <p className="mt-2 text-sm text-graphite">Mientras tanto puedes llenar tu perfil a mano en las pestañas de abajo.</p>
        </div>
      )}
      {mutation.isPending && <ImportProgress />}
      {error !== null && (
        <div ref={errorRef} tabIndex={-1} className="mt-5 text-left focus:outline-none">
          <Alert tone="error">
            <span className="font-bold">No se pudo importar tu CV.</span> {error}
          </Alert>
        </div>
      )}
    </section>
  )
}
