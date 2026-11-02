import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { formatDate } from '../../shared/lib/formatDate'
import { Alert } from '../../shared/ui/Alert'
import { LoadingState } from '../../shared/ui/LoadingState'
import { useAuth } from '../auth/AuthContext'
import { analysisKeys, getUsage, listAnalyses } from '../analyses/analysesApi'
import { bandLabels } from '../analyses/analysisLabels'
import { jobPostingKeys, listJobPostings } from '../jobs/jobsApi'
import { getProfile, profileQueryKey } from '../profile/profileApi'

const RECENT_COUNT = 5

export function DashboardPage() {
  const { user } = useAuth()
  const profileQuery = useQuery({ queryKey: profileQueryKey, queryFn: getProfile })
  const usageQuery = useQuery({ queryKey: analysisKeys.usage, queryFn: getUsage })
  const analysesQuery = useQuery({ queryKey: analysisKeys.list(1), queryFn: () => listAnalyses(1) })
  const lastAnalysis = analysesQuery.data?.items.find((item) => item.status === 'completed')
  const recentQuery = useQuery({
    queryKey: [...jobPostingKeys.list(1, ''), RECENT_COUNT],
    queryFn: () => listJobPostings(1, '', RECENT_COUNT),
  })

  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-4xl space-y-6">
      <div>
        <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
          Inicio
        </h1>
        <p className="mt-2 text-ink-muted">Hola, {user?.displayName}. Este es el estado de tus postulaciones.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <article aria-labelledby="profile-card-title" className="rounded-lg border border-line bg-surface p-5">
          <h2 id="profile-card-title" className="font-semibold">
            Perfil profesional
          </h2>
          {profileQuery.isPending && <LoadingState label="Cargando perfil…" />}
          {profileQuery.isError && <Alert tone="error">No pudimos cargar tu perfil.</Alert>}
          {profileQuery.data === null && (
            <>
              <p className="mt-2 text-sm text-ink-muted">Aún no lo has creado. Es la única fuente que usará el análisis.</p>
              <Link to="/app/perfil" className="mt-4 inline-block rounded-md bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-strong">
                Completar mi perfil
              </Link>
            </>
          )}
          {profileQuery.data != null && (
            <>
              <dl className="mt-3 grid grid-cols-2 gap-2 text-sm">
                <dt className="text-ink-muted">Experiencias</dt>
                <dd>{profileQuery.data.experiences.length}</dd>
                <dt className="text-ink-muted">Habilidades técnicas</dt>
                <dd>{profileQuery.data.technicalSkills.length}</dd>
                <dt className="text-ink-muted">Actualizado</dt>
                <dd>{formatDate(profileQuery.data.updatedAt)}</dd>
              </dl>
              <Link to="/app/perfil" className="mt-4 inline-block text-sm font-medium text-accent underline">
                Editar perfil
              </Link>
            </>
          )}
        </article>

        <article aria-labelledby="analysis-card-title" className="rounded-lg border border-line bg-surface p-5">
          <h2 id="analysis-card-title" className="font-semibold">
            Análisis
          </h2>
          {usageQuery.data !== undefined && (
            <p className="mt-2 text-sm text-ink-muted">
              Te quedan {usageQuery.data.remaining} de {usageQuery.data.limit} análisis en las últimas {usageQuery.data.windowHours} horas.
            </p>
          )}
          {lastAnalysis !== undefined && lastAnalysis.scoreBand !== null && (
            <p className="mt-2 text-sm">
              Último:{' '}
              <Link to={`/app/analisis/${lastAnalysis.id}`} className="font-medium text-accent underline">
                {lastAnalysis.jobTitle}
              </Link>{' '}
              · {bandLabels[lastAnalysis.scoreBand]}
            </p>
          )}
          <Link to="/app/analisis/nuevo" className="mt-4 inline-block rounded-md bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-strong">
            Nuevo análisis
          </Link>
        </article>
      </div>

      <article aria-labelledby="recent-title" className="rounded-lg border border-line bg-surface p-5">
        <div className="flex items-center justify-between">
          <h2 id="recent-title" className="font-semibold">
            Vacantes recientes
          </h2>
          <Link to="/app/vacantes/nueva" className="text-sm font-medium text-accent underline">
            Nueva vacante
          </Link>
        </div>
        {recentQuery.isPending && <LoadingState label="Cargando vacantes…" />}
        {recentQuery.isError && <Alert tone="error">No pudimos cargar tus vacantes.</Alert>}
        {recentQuery.data?.items.length === 0 && <p className="mt-3 text-sm text-ink-muted">Aún no has guardado vacantes.</p>}
        {recentQuery.data !== undefined && recentQuery.data.items.length > 0 && (
          <>
            <ul className="mt-3 divide-y divide-line">
              {recentQuery.data.items.map((posting) => (
                <li key={posting.id} className="flex items-center justify-between py-2 text-sm">
                  <Link to={`/app/vacantes/${posting.id}/editar`} className="font-medium hover:underline">
                    {posting.title}
                  </Link>
                  <span className="text-ink-muted">{posting.company ?? 'Empresa no indicada'}</span>
                </li>
              ))}
            </ul>
            {recentQuery.data.totalCount > RECENT_COUNT && (
              <Link to="/app/vacantes" className="mt-3 inline-block text-sm font-medium text-accent underline">
                Ver las {recentQuery.data.totalCount} vacantes
              </Link>
            )}
          </>
        )}
      </article>
    </section>
  )
}
