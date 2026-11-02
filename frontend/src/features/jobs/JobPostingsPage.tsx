import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router'
import { applicationStatuses, type ApplicationStatus, type JobPostingSummary } from '../../shared/api/types'
import { formatDate } from '../../shared/lib/formatDate'
import { Alert } from '../../shared/ui/Alert'
import { Button, primaryLinkClass } from '../../shared/ui/Button'
import { LoadingState } from '../../shared/ui/LoadingState'
import { applicationStatusLabels, formatDay, todayIso } from './applicationLabels'
import { ApplicationStatusPill } from './ApplicationStatusPill'
import { workModeLabels } from './jobPostingForm'
import { deleteJobPosting, getJobPosting, JOB_POSTINGS_PAGE_SIZE, jobPostingKeys, listJobPostings, updateApplication } from './jobsApi'

const isStatus = (value: string | null): value is ApplicationStatus => applicationStatuses.some((status) => status === value)

/** Marks a posting as applied today, keeping whatever notes and texts were already recorded. */
async function markApplied(id: string) {
  const posting = await getJobPosting(id)
  return updateApplication(id, { ...posting.application, status: 'applied', appliedOn: posting.application.appliedOn ?? todayIso() })
}

function noticeFrom(state: unknown): string | null {
  return typeof state === 'object' && state !== null && 'notice' in state && typeof state.notice === 'string' ? state.notice : null
}

export function JobPostingsPage() {
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number.parseInt(searchParams.get('page') ?? '1', 10) || 1)
  const search = searchParams.get('q') ?? ''
  const rawStatus = searchParams.get('estado')
  const statusFilter = isStatus(rawStatus) ? rawStatus : null
  const [searchDraft, setSearchDraft] = useState(search)
  const [pendingDelete, setPendingDelete] = useState<string | null>(null)
  const [status, setStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(() => {
    const notice = noticeFrom(location.state)
    return notice === null ? null : { tone: 'success', message: notice }
  })
  const queryClient = useQueryClient()

  const listQuery = useQuery({
    queryKey: jobPostingKeys.list(page, search, statusFilter),
    queryFn: () => listJobPostings(page, search, JOB_POSTINGS_PAGE_SIZE, statusFilter),
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

  const applying = useMutation({
    mutationFn: markApplied,
    onSuccess: async (saved) => {
      setStatus({ tone: 'success', message: `Marcaste «${saved.title}» como postulada.` })
      await queryClient.invalidateQueries({ queryKey: jobPostingKeys.all })
    },
    onError: () => setStatus({ tone: 'error', message: 'No pudimos guardar la postulación. Intenta de nuevo.' }),
  })

  const setStatusFilter = (value: ApplicationStatus | null) => {
    const next = new URLSearchParams()
    if (search !== '') next.set('q', search)
    if (value !== null) next.set('estado', value)
    setSearchParams(next)
  }

  const onSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const next = new URLSearchParams()
    if (searchDraft.trim() !== '') next.set('q', searchDraft.trim())
    if (statusFilter !== null) next.set('estado', statusFilter)
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
          <h1 id="page-title" className="text-3xl font-bold">
            Tus vacantes
          </h1>
          <p className="mt-2 text-graphite">Las ofertas que guardaste y en qué va cada postulación.</p>
        </div>
        <Link to="/app/vacantes/nueva" className={primaryLinkClass}>
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
          className="block w-full rounded-[var(--radius-field)] border border-rule bg-sheet px-3 py-2 text-[0.95rem] focus:border-pen focus:outline-none focus:ring-2 focus:ring-pen-soft"
        />
        <Button type="submit" variant="secondary">
          Buscar
        </Button>
      </form>
      </search>

      <fieldset className="mt-4">
        <legend className="sr-only">Filtrar por estado</legend>
        <div className="flex flex-wrap gap-2">
          {[null, ...applicationStatuses].map((option) => (
            <label
              key={option ?? 'all'}
              className="cursor-pointer rounded-full border border-rule bg-sheet px-3 py-1 text-sm font-semibold text-graphite hover:border-graphite has-[:checked]:border-ink has-[:checked]:bg-ink has-[:checked]:text-sheet has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-pen"
            >
              <input
                type="radio"
                name="estado"
                checked={statusFilter === option}
                onChange={() => setStatusFilter(option)}
                className="sr-only"
              />
              {option === null ? 'Todas' : applicationStatusLabels[option]}
            </label>
          ))}
        </div>
      </fieldset>

      <div className="mt-4">{status !== null && <Alert tone={status.tone}>{status.message}</Alert>}</div>

      {listQuery.isPending && <LoadingState label="Cargando vacantes…" />}
      {listQuery.isError && <Alert tone="error">No pudimos cargar tus vacantes. Recarga la página para intentarlo de nuevo.</Alert>}

      {data !== undefined && data.items.length === 0 && (
        <div className="mt-6 rounded-[var(--radius-sheet)] border-2 border-dashed border-rule bg-sheet p-8 text-center text-sm text-graphite">
          {statusFilter !== null && search === '' ? (
            <>No tienes vacantes en «{applicationStatusLabels[statusFilter]}».</>
          ) : search === '' ? (
            <>
              Aún no has guardado vacantes.{' '}
              <Link to="/app/vacantes/nueva" className="font-semibold text-pen underline">
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
          <ul aria-label="Vacantes guardadas" className="mt-6 divide-y divide-rule rounded-[var(--radius-sheet)] border border-rule bg-sheet">
            {data.items.map((posting) => (
              <JobPostingRow
                key={posting.id}
                posting={posting}
                confirming={pendingDelete === posting.id}
                deleting={deletion.isPending && deletion.variables === posting.id}
                marking={applying.isPending && applying.variables === posting.id}
                onMarkApplied={() => {
                  setStatus(null)
                  applying.mutate(posting.id)
                }}
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
            <span className="text-graphite">
              Página {page} de {totalPages} ({data.totalCount} {data.totalCount === 1 ? 'vacante' : 'vacantes'})
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
  marking: boolean
  onMarkApplied: () => void
  onAskDelete: () => void
  onCancelDelete: () => void
  onConfirmDelete: () => void
}

function JobPostingRow({ posting, confirming, deleting, marking, onMarkApplied, onAskDelete, onCancelDelete, onConfirmDelete }: JobPostingRowProps) {
  const details = [posting.company, posting.location, workModeLabels[posting.workMode]].filter((value) => value !== null && value !== '')
  return (
    <li className="flex flex-wrap items-center justify-between gap-3 p-4">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="truncate font-semibold">
            <Link to={`/app/vacantes/${posting.id}/editar`} className="hover:text-pen hover:underline">
              {posting.title}
            </Link>
          </h2>
          <ApplicationStatusPill status={posting.applicationStatus} />
        </div>
        {details.length > 0 && <p className="mt-1 text-sm text-graphite">{details.join(', ')}</p>}
        <p className="mt-0.5 text-xs text-graphite">
          {posting.appliedOn !== null ? `Te postulaste el ${formatDay(posting.appliedOn)}` : `Guardada el ${formatDate(posting.createdAt)}`}
          {posting.sourceUrl !== null && (
            <>
              {', '}
              <a href={posting.sourceUrl} target="_blank" rel="noopener noreferrer" className="font-semibold text-pen underline">
                ver oferta original<span className="sr-only"> de {posting.title} (se abre en otra pestaña)</span>
              </a>
            </>
          )}
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
        <div className="flex flex-wrap items-center gap-2">
          {posting.applicationStatus === 'saved' && (
            <Button variant="secondary" onClick={onMarkApplied} disabled={marking} aria-label={`Marcar ${posting.title} como postulada`}>
              {marking ? 'Guardando…' : 'Me postulé'}
            </Button>
          )}
          <Link
            to={`/app/analisis/nuevo?vacante=${posting.id}`}
            aria-label={`Analizar ${posting.title}`}
            className={primaryLinkClass}
          >
            Analizar
          </Link>
          <Link
            to={`/app/vacantes/${posting.id}/editar`}
            aria-label={`Editar ${posting.title}`}
            className="rounded-[var(--radius-field)] px-3 py-2 text-sm font-semibold text-pen hover:bg-pen-soft"
          >
            Editar
          </Link>
          <Button variant="ghost" aria-label={`Eliminar ${posting.title}`} onClick={onAskDelete}>
            Eliminar
          </Button>
        </div>
      )}
    </li>
  )
}
