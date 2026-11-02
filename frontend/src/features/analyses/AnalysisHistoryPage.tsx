import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { formatDate } from '../../shared/lib/formatDate'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { LoadingState } from '../../shared/ui/LoadingState'
import { ANALYSES_PAGE_SIZE, analysisKeys, deleteAnalysis, listAnalyses } from './analysesApi'
import { bandLabels } from './analysisLabels'

export function AnalysisHistoryPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number.parseInt(searchParams.get('page') ?? '1', 10) || 1)
  const [status, setStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null)
  const queryClient = useQueryClient()

  const query = useQuery({ queryKey: analysisKeys.list(page), queryFn: () => listAnalyses(page), placeholderData: keepPreviousData })
  const deletion = useMutation({
    mutationFn: deleteAnalysis,
    onSuccess: async () => {
      setStatus({ tone: 'success', message: 'Análisis eliminado.' })
      await queryClient.invalidateQueries({ queryKey: analysisKeys.all })
    },
    onError: () => setStatus({ tone: 'error', message: 'No pudimos eliminar el análisis.' }),
  })

  const data = query.data
  const totalPages = data === undefined ? 1 : Math.max(1, Math.ceil(data.totalCount / ANALYSES_PAGE_SIZE))

  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-4xl">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
            Historial de análisis
          </h1>
          <p className="mt-2 text-ink-muted">Cada análisis conserva la versión del perfil y de la vacante que se compararon.</p>
        </div>
        <Link to="/app/analisis/nuevo" className="rounded-md bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-strong">
          Nuevo análisis
        </Link>
      </div>

      <div className="mt-4">{status !== null && <Alert tone={status.tone}>{status.message}</Alert>}</div>
      {query.isPending && <LoadingState label="Cargando historial…" />}
      {query.isError && <Alert tone="error">No pudimos cargar tu historial.</Alert>}
      {data?.items.length === 0 && (
        <p className="mt-6 rounded-lg border border-dashed border-line bg-surface p-8 text-center text-sm text-ink-muted">
          Aún no has hecho análisis.
        </p>
      )}

      {data !== undefined && data.items.length > 0 && (
        <>
          <ul aria-label="Análisis realizados" className="mt-6 divide-y divide-line rounded-lg border border-line bg-surface">
            {data.items.map((item) => (
              <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
                <div className="min-w-0">
                  <h2 className="truncate font-medium">
                    <Link to={`/app/analisis/${item.id}`} className="hover:underline">
                      {item.jobTitle}
                    </Link>
                  </h2>
                  <p className="mt-1 text-sm text-ink-muted">
                    {item.jobCompany ?? 'Empresa no indicada'} · {formatDate(item.createdAt)} ·{' '}
                    {item.status === 'completed' && item.scoreBand !== null
                      ? `${bandLabels[item.scoreBand]} (${item.compatibilityScore}/100)`
                      : item.status === 'failed'
                        ? 'No se completó'
                        : 'En proceso'}
                  </p>
                </div>
                <Button
                  variant="danger"
                  aria-label={`Eliminar análisis de ${item.jobTitle}`}
                  disabled={deletion.isPending}
                  onClick={() => {
                    setStatus(null)
                    deletion.mutate(item.id)
                  }}
                >
                  Eliminar
                </Button>
              </li>
            ))}
          </ul>
          <nav aria-label="Paginación" className="mt-4 flex items-center justify-between text-sm">
            <Button variant="secondary" disabled={page <= 1} onClick={() => setSearchParams({ page: String(page - 1) })}>
              Anterior
            </Button>
            <span className="text-ink-muted">
              Página {page} de {totalPages}
            </span>
            <Button variant="secondary" disabled={page >= totalPages} onClick={() => setSearchParams({ page: String(page + 1) })}>
              Siguiente
            </Button>
          </nav>
        </>
      )}
    </section>
  )
}
