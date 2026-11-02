import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router'
import type { JobPostingSummary } from '../../shared/api/types'
import { formatDate } from '../../shared/lib/formatDate'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { LoadingState } from '../../shared/ui/LoadingState'
import { workModeLabels } from './jobPostingForm'
import { deleteJobPosting, JOB_POSTINGS_PAGE_SIZE, jobPostingKeys, listJobPostings } from './jobsApi'

function noticeFrom(state: unknown): string | null {
  return typeof state === 'object' && state !== null && 'notice' in state && typeof state.notice === 'string' ? state.notice : null
}

export function JobPostingsPage() {
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number.parseInt(searchParams.get('page') ?? '1', 10) || 1)
  const search = searchParams.get('q') ?? ''
  const [searchDraft, setSearchDraft] = useState(search)
  const [pendingDelete, setPendingDelete] = useState<string | null>(null)
  const [status, setStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(() => {
    const notice = noticeFrom(location.state)
    return notice === null ? null : { tone: 'success', message: notice }
  })
  const queryClient = useQueryClient()

  const listQuery = useQuery({
    queryKey: jobPostingKeys.list(page, search),
    queryFn: () => listJobPostings(page, search),
    placeholderData: keepPreviousData,
  })

  const deletion = useMutation({
    mutationFn: deleteJobPosting,
    onSuccess: async () => {
      setStatus({ tone: 'success', message: 'Vacante eliminada.' })
      await queryClient.invalidateQueries({ queryKey: jobPostingKeys.all })
    },
    onError: () => setStatus({ tone: 'error', message: 'No pudimos eliminar la vacante. Intenta de nuevo.' }),
    onSettled: () => setPendingDelete(null),
  })

  const onSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const next = new URLSearchParams()
    if (searchDraft.trim() !== '') next.set('q', searchDraft.trim())
    setSearchParams(next)
  }

  const goToPage = (target: number) => {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(target))
    setSearchParams(next)
  }

  const data = listQuery.data
  const totalPages = data === undefined ? 1 : Math.max(1, Math.ceil(data.totalCount / JOB_POSTINGS_PAGE_SIZE))

  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-4xl">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
            Mis vacantes
          </h1>
          <p className="mt-2 text-ink-muted">Las ofertas que guardaste para analizar.</p>
        </div>
        <Link to="/app/vacantes/nueva" className="rounded-md bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-strong">
          Nueva vacante
        </Link>
      </div>

      <search className="mt-6 block">
      <form onSubmit={onSearch} className="flex gap-2">
        <label htmlFor="job-search" className="sr-only">
          Buscar por cargo o empresa
        </label>
        <input
          id="job-search"
          type="search"
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
          placeholder="Buscar por cargo o empresa"
          maxLength={100}
          className="block w-full rounded-md border border-line bg-surface px-3 py-2 text-sm"
        />
        <Button type="submit" variant="secondary">
          Buscar
        </Button>
      </form>
      </search>

      <div className="mt-4">{status !== null && <Alert tone={status.tone}>{status.message}</Alert>}</div>

      {listQuery.isPending && <LoadingState label="Cargando vacantes…" />}
      {listQuery.isError && <Alert tone="error">No pudimos cargar tus vacantes. Recarga la página para intentarlo de nuevo.</Alert>}

      {data !== undefined && data.items.length === 0 && (
        <div className="mt-6 rounded-lg border border-dashed border-line bg-surface p-8 text-center text-sm text-ink-muted">
          {search === '' ? (
            <>
              Aún no has guardado vacantes.{' '}
              <Link to="/app/vacantes/nueva" className="font-medium text-accent underline">
                Agrega la primera
              </Link>
              .
            </>
          ) : (
            <>No hay vacantes que coincidan con “{search}”.</>
          )}
        </div>
      )}

      {data !== undefined && data.items.length > 0 && (
        <>
          <ul aria-label="Vacantes guardadas" className="mt-6 divide-y divide-line rounded-lg border border-line bg-surface">
            {data.items.map((posting) => (
              <JobPostingRow
                key={posting.id}
                posting={posting}
                confirming={pendingDelete === posting.id}
                deleting={deletion.isPending && deletion.variables === posting.id}
                onAskDelete={() => {
                  setStatus(null)
                  setPendingDelete(posting.id)
                }}
                onCancelDelete={() => setPendingDelete(null)}
                onConfirmDelete={() => deletion.mutate(posting.id)}
              />
            ))}
          </ul>
          <nav aria-label="Paginación" className="mt-4 flex items-center justify-between text-sm">
            <Button variant="secondary" disabled={page <= 1} onClick={() => goToPage(page - 1)}>
              Anterior
            </Button>
            <span className="text-ink-muted">
              Página {page} de {totalPages} · {data.totalCount} {data.totalCount === 1 ? 'vacante' : 'vacantes'}
            </span>
            <Button variant="secondary" disabled={page >= totalPages} onClick={() => goToPage(page + 1)}>
              Siguiente
            </Button>
          </nav>
        </>
      )}
    </section>
  )
}

interface JobPostingRowProps {
  posting: JobPostingSummary
  confirming: boolean
  deleting: boolean
  onAskDelete: () => void
  onCancelDelete: () => void
  onConfirmDelete: () => void
}

function JobPostingRow({ posting, confirming, deleting, onAskDelete, onCancelDelete, onConfirmDelete }: JobPostingRowProps) {
  const details = [posting.company, posting.location, workModeLabels[posting.workMode]].filter((value) => value !== null && value !== '')
  return (
    <li className="flex flex-wrap items-center justify-between gap-3 p-4">
      <div className="min-w-0">
        <h2 className="truncate font-medium">{posting.title}</h2>
        <p className="mt-1 text-sm text-ink-muted">
          {details.join(' · ')} · Guardada el {formatDate(posting.createdAt)}
        </p>
      </div>
      {confirming ? (
        <fieldset className="flex items-center gap-2 text-sm">
          <legend className="sr-only">Confirmar eliminación de {posting.title}</legend>
          <span aria-hidden="true">¿Eliminar esta vacante?</span>
          <Button variant="danger" onClick={onConfirmDelete} disabled={deleting}>
            {deleting ? 'Eliminando…' : 'Sí, eliminar'}
          </Button>
          <Button variant="ghost" onClick={onCancelDelete} disabled={deleting}>
            Cancelar
          </Button>
        </fieldset>
      ) : (
        <div className="flex items-center gap-2">
          <Link
            to={`/app/vacantes/${posting.id}/editar`}
            aria-label={`Editar ${posting.title}`}
            className="rounded-md px-3 py-2 text-sm font-medium text-accent hover:bg-accent-soft"
          >
            Editar
          </Link>
          <Button variant="danger" aria-label={`Eliminar ${posting.title}`} onClick={onAskDelete}>
            Eliminar
          </Button>
        </div>
      )}
    </li>
  )
}
