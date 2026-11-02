import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { ApiError } from '../../shared/api/ApiError'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { SelectField } from '../../shared/ui/fields'
import { LoadingState } from '../../shared/ui/LoadingState'
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
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-3xl">
      <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
        Nuevo análisis
      </h1>
      <p className="mt-2 text-ink-muted">
        Comparamos tu perfil profesional con la vacante que elijas. La IA solo usa lo que escribiste en tu perfil.
      </p>

      <div className="mt-8 space-y-4">
        {!hasProfile && (
          <Alert tone="info">
            Antes de analizar necesitas tu perfil profesional.{' '}
            <Link to="/app/perfil" className="font-medium text-accent underline">
              Completar mi perfil
            </Link>
          </Alert>
        )}
        {hasProfile && postings.length === 0 && (
          <Alert tone="info">
            Aún no tienes vacantes guardadas.{' '}
            <Link to="/app/vacantes/nueva" className="font-medium text-accent underline">
              Guarda una vacante
            </Link>
          </Alert>
        )}

        {hasProfile && postings.length > 0 && (
          <form onSubmit={(event) => void onSubmit(event)} className="space-y-5 rounded-lg border border-line bg-surface p-5">
            <SelectField
              label="Vacante"
              value={jobPostingId}
              onChange={(event) => setSelected(event.target.value)}
              options={postings.map((posting) => ({
                value: posting.id,
                label: posting.company === null ? posting.title : `${posting.title} · ${posting.company}`,
              }))}
              disabled={mutation.isPending}
            />
            <p className="text-sm text-ink-muted">Idioma del análisis: español.</p>
            {usage !== undefined && (
              <p className="text-sm text-ink-muted">
                Te quedan <strong className="text-ink">{usage.remaining}</strong> de {usage.limit} análisis en las últimas {usage.windowHours} horas.
              </p>
            )}
            {error !== null && (
              <Alert tone="error">
                {error.message}
                {error.code === 'profile_required' && (
                  <>
                    {' '}
                    <Link to="/app/perfil" className="font-medium underline">
                      Ir a mi perfil
                    </Link>
                  </>
                )}
              </Alert>
            )}
            {mutation.isPending && (
              <output aria-live="polite" className="block rounded-md bg-accent-soft p-3 text-sm text-accent-strong">
                Analizando tu perfil frente a la vacante. Suele tardar entre 15 segundos y un minuto; no cierres esta página.
              </output>
            )}
            <Button type="submit" disabled={mutation.isPending || limitReached}>
              {mutation.isPending ? 'Analizando…' : 'Analizar compatibilidad'}
            </Button>
            {limitReached && <p className="text-sm text-ink-muted">Alcanzaste el límite de análisis por ahora.</p>}
          </form>
        )}
      </div>
    </section>
  )
}
