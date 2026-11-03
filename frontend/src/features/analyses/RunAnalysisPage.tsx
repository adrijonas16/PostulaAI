import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { ApiError } from '../../shared/api/ApiError'
import { useAiBlockReason } from '../../app/useAiAvailability'
import { AiUnavailable } from '../../shared/ui/AiUnavailable'
import { Alert } from '../../shared/ui/Alert'
import { Button, primaryLinkClass } from '../../shared/ui/Button'
import { LoadingState } from '../../shared/ui/LoadingState'
import { PageHeader, Sheet } from '../../shared/ui/Sheet'
import { jobPostingKeys, listJobPostings } from '../jobs/jobsApi'
import { getProfile, profileQueryKey } from '../profile/profileApi'
import { analysisKeys, createAnalysis, getUsage } from './analysesApi'

const MAX_POSTINGS = 50

export function RunAnalysisPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState(searchParams.get('vacante') ?? '')
  const [error, setError] = useState<{ message: string; code: string } | null>(null)

  const profileQuery = useQuery({ queryKey: profileQueryKey, queryFn: getProfile })
  const postingsQuery = useQuery({ queryKey: [...jobPostingKeys.list(1, ''), MAX_POSTINGS], queryFn: () => listJobPostings(1, '', MAX_POSTINGS) })
  const usageQuery = useQuery({ queryKey: analysisKeys.usage, queryFn: getUsage })
  const blockReason = useAiBlockReason()

  const mutation = useMutation({
    mutationFn: (jobPostingId: string) => createAnalysis(jobPostingId),
    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: analysisKeys.all })
      await queryClient.invalidateQueries({ queryKey: analysisKeys.usage })
    },
  })

  const postings = postingsQuery.data?.items ?? []
  const jobPostingId = selected !== '' ? selected : (postings[0]?.id ?? '')

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    if (jobPostingId === '') return
    try {
      const analysis = await mutation.mutateAsync(jobPostingId)
      await navigate(`/app/analisis/${analysis.id}`)
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? { message: caught.message, code: caught.errorCode }
          : { message: 'Ocurrió un error inesperado. Intenta de nuevo.', code: 'unknown' },
      )
    }
  }

  if (profileQuery.isPending || postingsQuery.isPending) {
    return <LoadingState label="Preparando el análisis…" />
  }

  const hasProfile = profileQuery.data != null
  const usage = usageQuery.data
  const limitReached = usage !== undefined && usage.remaining === 0

  return (
    <div className="mx-auto w-full max-w-3xl">
      <PageHeader
        title="Nuevo análisis"
        description="Elige una vacante. Comparamos cada requisito con tu perfil y te mostramos qué tienes y qué te falta."
      />

      <div className="space-y-4">
        {!hasProfile && (
          <Sheet id="need-profile-title" title="Primero, tu perfil">
            <p className="text-graphite">El análisis solo usa lo que está en tu perfil, así que lo necesitamos antes de comparar.</p>
            <Link to="/app/perfil" className={`${primaryLinkClass} mt-4`}>
              Completar mi perfil
            </Link>
          </Sheet>
        )}
        {hasProfile && postings.length === 0 && (
          <Sheet id="need-posting-title" title="Guarda una vacante">
            <p className="text-graphite">Aún no tienes vacantes guardadas. Pega el texto de una oferta y vuelve aquí.</p>
            <Link to="/app/vacantes/nueva" className={`${primaryLinkClass} mt-4`}>
              Guardar una vacante
            </Link>
          </Sheet>
        )}

        {hasProfile && postings.length > 0 && (
          <form onSubmit={(event) => void onSubmit(event)} className="space-y-6">
            <fieldset disabled={mutation.isPending}>
              <legend className="text-lg font-bold">Vacante</legend>
              <div className="mt-3 grid gap-2 sm:grid-cols-2">
                {postings.map((posting) => (
                  // The label's text comes from the posting data, which the rule cannot see statically.
                  // oxlint-disable-next-line jsx-a11y/label-has-associated-control
                  <label
                    key={posting.id}
                    className="flex cursor-pointer items-start gap-3 rounded-[var(--radius-sheet)] border-2 border-rule bg-sheet p-4 hover:border-graphite has-[:checked]:border-pen has-[:checked]:bg-pen-soft has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-pen"
                  >
                    <input
                      type="radio"
                      name="vacante"
                      value={posting.id}
                      checked={jobPostingId === posting.id}
                      onChange={() => setSelected(posting.id)}
                      className="mt-1 size-4 accent-pen"
                    />
                    <span className="min-w-0">
                      <span className="block font-semibold">{posting.title}</span>
                      <span className="block text-sm text-graphite">{posting.company ?? 'Empresa no indicada'}</span>
                    </span>
                  </label>
                ))}
              </div>
              <Link to="/app/vacantes/nueva" className="mt-3 inline-block text-sm font-semibold text-pen underline">
                Agregar otra vacante
              </Link>
            </fieldset>

            <div className="rounded-[var(--radius-sheet)] border border-rule bg-sheet p-5">
              {usage !== undefined && (
                <p className="text-sm text-graphite">
                  Te quedan <strong className="text-ink">{usage.remaining}</strong> de {usage.limit} análisis en las últimas {usage.windowHours} horas.
                  El análisis se hace en español.
                </p>
              )}
              {blockReason !== null && (
                <div className="mt-4">
                  <AiUnavailable reason={blockReason} feature="el análisis" />
                </div>
              )}
              {error !== null && (
                <div className="mt-4">
                  <Alert tone="error">
                    {error.message}
                    {error.code === 'profile_required' && (
                      <>
                        {' '}
                        <Link to="/app/perfil" className="font-semibold underline">
                          Ir a mi perfil
                        </Link>
                      </>
                    )}
                  </Alert>
                </div>
              )}
              {mutation.isPending && (
                <output aria-live="polite" className="mt-4 block rounded-[var(--radius-field)] bg-pen-soft p-4 text-sm text-pen-strong">
                  <span className="mb-3 block h-1.5 overflow-hidden rounded-full bg-sheet">
                    <span className="block h-full w-1/3 animate-[progress_1.6s_ease-in-out_infinite] rounded-full bg-pen motion-reduce:animate-none" />
                  </span>
                  Leyendo la vacante, buscando evidencia en tu perfil y redactando recomendaciones. Suele tardar entre 15 segundos y un minuto; no
                  cierres esta página.
                </output>
              )}
              <div className="mt-4 flex flex-wrap items-center gap-3">
                <Button type="submit" size="lg" disabled={mutation.isPending || limitReached || blockReason !== null}>
                  {mutation.isPending ? 'Analizando…' : 'Analizar compatibilidad'}
                </Button>
                {limitReached && <p className="text-sm text-redpen">Alcanzaste el límite de análisis por ahora.</p>}
              </div>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}
