import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { formatDate } from '../../shared/lib/formatDate'
import { Alert } from '../../shared/ui/Alert'
import { Button, primaryLinkClass } from '../../shared/ui/Button'
import { LoadingState } from '../../shared/ui/LoadingState'
import { PageHeader } from '../../shared/ui/Sheet'
import { BandPill } from './BandPill'
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
    <div className="mx-auto w-full max-w-4xl">
      <PageHeader
        title="Historial de análisis"
        description="Cada análisis guarda el perfil y la vacante tal como estaban cuando lo hiciste."
        action={
          <Link to="/app/analisis/nuevo" className={primaryLinkClass}>
            Nuevo análisis
          </Link>
        }
      />

      <div className="mt-4">{status !== null && <Alert tone={status.tone}>{status.message}</Alert>}</div>
      {query.isPending && <LoadingState label="Cargando historial…" />}
      {query.isError && <Alert tone="error">No pudimos cargar tu historial.</Alert>}
      {data?.items.length === 0 && (
        <div className="mt-6 rounded-[var(--radius-sheet)] border-2 border-dashed border-rule bg-sheet p-8 text-center">
          <p className="font-semibold">Aún no has hecho análisis.</p>
          <p className="mt-1 text-sm text-graphite">Elige una vacante guardada y compárala con tu perfil.</p>
          <Link to="/app/analisis/nuevo" className={`${primaryLinkClass} mt-4`}>
            Hacer mi primer análisis
          </Link>
        </div>
      )}

      {data !== undefined && data.items.length > 0 && (
        <>
          <ul aria-label="Análisis realizados" className="mt-6 divide-y divide-rule rounded-[var(--radius-sheet)] border border-rule bg-sheet">
            {data.items.map((item) => (
              <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
                <div className="min-w-0">
                  <h2 className="truncate font-semibold">
                    <Link to={`/app/analisis/${item.id}`} className="hover:text-pen hover:underline">
                      {item.jobTitle}
                    </Link>
                  </h2>
                  <p className="mt-1 text-sm text-graphite">
                    {item.jobCompany ?? 'Empresa no indicada'}, {formatDate(item.createdAt)}
                  </p>
                  <p className="mt-2 text-sm">
                    {item.status === 'completed' && item.scoreBand !== null ? (
                      <span className="sr-only">{`${bandLabels[item.scoreBand]} (${item.compatibilityScore}/100)`}</span>
                    ) : null}
                    {item.status === 'completed' && item.scoreBand !== null ? (
                      <span aria-hidden="true">
                        <BandPill band={item.scoreBand} score={item.compatibilityScore} />
                      </span>
                    ) : item.status === 'failed' ? (
                      <span className="text-redpen">No se completó</span>
                    ) : (
                      <span className="text-graphite">En proceso</span>
                    )}
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
            <span className="text-graphite">
              Página {page} de {totalPages}
            </span>
            <Button variant="secondary" disabled={page >= totalPages} onClick={() => setSearchParams({ page: String(page + 1) })}>
              Siguiente
            </Button>
          </nav>
        </>
      )}
    </div>
  )
}
