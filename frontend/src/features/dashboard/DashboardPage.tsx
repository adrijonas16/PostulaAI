import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { useJourney, type JourneyStep } from '../../app/useJourney'
import { formatDate } from '../../shared/lib/formatDate'
import { primaryLinkClass, secondaryLinkClass } from '../../shared/ui/Button'
import { LoadingState } from '../../shared/ui/LoadingState'
import { Sheet } from '../../shared/ui/Sheet'
import { useAuth } from '../auth/AuthContext'
import { analysisKeys, getUsage, listAnalyses } from '../analyses/analysesApi'
import { BandPill } from '../analyses/BandPill'
import { jobPostingKeys, listJobPostings } from '../jobs/jobsApi'
import { getProfile, profileQueryKey } from '../profile/profileApi'

const RECENT_COUNT = 5

function greeting(now = new Date()): string {
  const hour = now.getHours()
  if (hour < 12) return 'Buenos días'
  if (hour < 19) return 'Buenas tardes'
  return 'Buenas noches'
}

function firstName(displayName: string | undefined): string {
  return displayName?.trim().split(/\s+/)[0] ?? ''
}

const nextStepCopy: Record<JourneyStep | 'done', { title: string; body: string; cta: string; to: string }> = {
  profile: {
    title: 'Empieza por tu perfil',
    body: 'Importa tu CV o escríbelo a mano. Es lo único que usará el análisis, así que nunca se inventa nada.',
    cta: 'Completar mi perfil',
    to: '/app/perfil',
  },
  postings: {
    title: 'Guarda una vacante',
    body: 'Pega el texto de una oferta que te interese, tal como la publicó la empresa.',
    cta: 'Agregar vacante',
    to: '/app/vacantes/nueva',
  },
  analysis: {
    title: 'Analiza tu primera vacante',
    body: 'Te mostramos qué requisitos ya demuestras, cuáles te faltan y cómo adaptar tu CV.',
    cta: 'Analizar compatibilidad',
    to: '/app/analisis/nuevo',
  },
  done: {
    title: 'Todo listo para tu próxima postulación',
    body: 'Guarda otra vacante o analiza una que ya tengas.',
    cta: 'Nuevo análisis',
    to: '/app/analisis/nuevo',
  },
}

export function DashboardPage() {
  const { user } = useAuth()
  const journey = useJourney()
  const profileQuery = useQuery({ queryKey: profileQueryKey, queryFn: getProfile })
  const usageQuery = useQuery({ queryKey: analysisKeys.usage, queryFn: getUsage })
  const analysesQuery = useQuery({ queryKey: analysisKeys.list(1), queryFn: () => listAnalyses(1) })
  const recentQuery = useQuery({
    queryKey: [...jobPostingKeys.list(1, ''), RECENT_COUNT],
    queryFn: () => listJobPostings(1, '', RECENT_COUNT),
  })

  const next = nextStepCopy[journey.next ?? 'done']
  const profile = profileQuery.data
  const recentAnalyses = (analysesQuery.data?.items ?? []).slice(0, 3)

  return (
    <div className="mx-auto w-full max-w-5xl">
      <header className="mb-8">
        <h1 id="page-title" className="text-3xl font-bold md:text-4xl">
          {greeting()}, {firstName(user?.displayName)}{' '}
          <span className="wave" aria-hidden="true">
            👋
          </span>
        </h1>
        <p className="mt-2 text-graphite">Prepara cada postulación en tres pasos: tu perfil, la vacante y el análisis.</p>
      </header>

      {journey.loading ? (
        <LoadingState label="Cargando tu progreso…" />
      ) : (
        <section aria-labelledby="next-step-title" className="mb-8 rounded-[var(--radius-sheet)] border-2 border-pen bg-sheet p-6 md:p-8">
          <p className="text-sm font-semibold text-pen">Tu siguiente paso</p>
          <h2 id="next-step-title" className="mt-1 text-2xl font-bold">
            {next.title}
          </h2>
          <p className="mt-2 max-w-xl text-graphite">{next.body}</p>
          <div className="mt-5 flex flex-wrap gap-3">
            <Link to={next.to} className={`${primaryLinkClass} px-5 py-3 text-base`}>
              {next.cta}
            </Link>
            {journey.next === 'profile' && (
              <Link to="/app/vacantes/nueva" className={secondaryLinkClass}>
                Prefiero guardar una vacante primero
              </Link>
            )}
          </div>
        </section>
      )}

      <div className="grid gap-5 lg:grid-cols-3">
        <Sheet id="profile-card-title" title="Perfil profesional">
          {profileQuery.isPending && <LoadingState label="Cargando perfil…" />}
          {profile === null && (
            <>
              <p className="text-sm text-graphite">Aún no lo has creado.</p>
              <Link to="/app/perfil" className={`${secondaryLinkClass} mt-4`}>
                Completar mi perfil
              </Link>
            </>
          )}
          {profile != null && (
            <>
              <dl className="grid grid-cols-[1fr_auto] gap-y-1 text-sm">
                <dt className="text-graphite">Experiencias</dt>
                <dd className="font-semibold">{profile.experiences.length}</dd>
                <dt className="text-graphite">Habilidades técnicas</dt>
                <dd className="font-semibold">{profile.technicalSkills.length}</dd>
                <dt className="text-graphite">Actualizado</dt>
                <dd className="font-semibold">{formatDate(profile.updatedAt)}</dd>
              </dl>
              <Link to="/app/perfil" className="mt-4 inline-block text-sm font-semibold text-pen underline">
                Editar perfil
              </Link>
            </>
          )}
        </Sheet>

        <Sheet id="recent-title" title="Vacantes recientes" action={<Link to="/app/vacantes/nueva" className="text-sm font-semibold text-pen underline">Nueva vacante</Link>}>
          {recentQuery.data?.items.length === 0 && <p className="text-sm text-graphite">Aún no has guardado vacantes.</p>}
          {recentQuery.data !== undefined && recentQuery.data.items.length > 0 && (
            <>
              <ul className="divide-y divide-rule">
                {recentQuery.data.items.map((posting) => (
                  <li key={posting.id} className="py-2 text-sm">
                    <Link to={`/app/vacantes/${posting.id}/editar`} className="font-semibold text-ink hover:text-pen">
                      {posting.title}
                    </Link>
                    <span className="block text-graphite">{posting.company ?? 'Empresa no indicada'}</span>
                  </li>
                ))}
              </ul>
              {recentQuery.data.totalCount > RECENT_COUNT && (
                <Link to="/app/vacantes" className="mt-3 inline-block text-sm font-semibold text-pen underline">
                  Ver las {recentQuery.data.totalCount} vacantes
                </Link>
              )}
            </>
          )}
        </Sheet>

        <Sheet
          id="analysis-card-title"
          title="Análisis"
          action={<Link to="/app/analisis/nuevo" className="text-sm font-semibold text-pen underline">Nuevo análisis</Link>}
        >
          {usageQuery.data !== undefined && (
            <p className="text-sm text-graphite">
              Te quedan <strong className="text-ink">{usageQuery.data.remaining}</strong> de {usageQuery.data.limit} análisis en las últimas{' '}
              {usageQuery.data.windowHours} horas.
            </p>
          )}
          {recentAnalyses.length === 0 && <p className="mt-2 text-sm text-graphite">Tus resultados aparecerán aquí.</p>}
          <ul className="mt-3 space-y-2">
            {recentAnalyses.map((item) => (
              <li key={item.id} className="flex flex-wrap items-center justify-between gap-2 text-sm">
                <Link to={`/app/analisis/${item.id}`} className="font-semibold text-ink hover:text-pen">
                  {item.jobTitle}
                </Link>
                {item.scoreBand !== null ? <BandPill band={item.scoreBand} /> : <span className="text-xs text-redpen">No se completó</span>}
              </li>
            ))}
          </ul>
        </Sheet>
      </div>
    </div>
  )
}
