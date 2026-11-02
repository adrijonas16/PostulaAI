import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../shared/api/ApiError'
import type { AnalysisResponse, JobAnalysisResult, ScoreContext } from '../../shared/api/types'
import { formatDate } from '../../shared/lib/formatDate'
import { Alert } from '../../shared/ui/Alert'
import { LoadingState } from '../../shared/ui/LoadingState'
import { analysisKeys, getAnalysis } from './analysesApi'
import { analysisErrorMessage, bandLabels, bandStyles, gapTypeLabels, importanceLabels } from './analysisLabels'
import { CopyButton } from './CopyButton'

export function AnalysisResultPage() {
  const { analysisId = '' } = useParams()
  const query = useQuery({ queryKey: analysisKeys.detail(analysisId), queryFn: () => getAnalysis(analysisId) })

  if (query.isPending) {
    return <LoadingState label="Cargando el análisis…" />
  }

  if (query.isError) {
    const notFound = query.error instanceof ApiError && query.error.status === 404
    return (
      <section className="mx-auto max-w-3xl">
        <h1 className="text-2xl font-semibold tracking-tight">Resultado del análisis</h1>
        <div className="mt-6 space-y-3">
          <Alert tone="error">{notFound ? 'No encontramos ese análisis.' : 'No pudimos cargar el análisis. Intenta de nuevo.'}</Alert>
          <Link to="/app/analisis" className="text-sm font-medium text-accent underline">
            Ver mi historial
          </Link>
        </div>
      </section>
    )
  }

  const analysis = query.data
  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-4xl space-y-6">
      <header>
        <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
          Resultado del análisis
        </h1>
        <p className="mt-2 text-ink-muted">
          {analysis.jobTitle}
          {analysis.jobCompany !== null && ` · ${analysis.jobCompany}`} · {formatDate(analysis.createdAt)}
        </p>
      </header>

      {analysis.result === null || analysis.score === null ? (
        <FailedAnalysis analysis={analysis} />
      ) : (
        <AnalysisBody result={analysis.result} score={analysis.score} />
      )}

      <details className="rounded-lg border border-line bg-surface p-4 text-sm text-ink-muted">
        <summary className="cursor-pointer font-medium text-ink">Detalles técnicos</summary>
        <dl className="mt-3 grid gap-2 sm:grid-cols-[12rem_1fr]">
          <dt>Modelo</dt>
          <dd>{analysis.model}</dd>
          <dt>Prompt</dt>
          <dd>
            {analysis.promptId} {analysis.promptVersion}
          </dd>
          <dt>Corrección automática</dt>
          <dd>{analysis.repairAttempted ? 'Sí: la primera respuesta no pasó la validación y se pidió una corrección.' : 'No fue necesaria.'}</dd>
        </dl>
      </details>
    </section>
  )
}

function FailedAnalysis({ analysis }: { analysis: AnalysisResponse }) {
  return (
    <div className="space-y-3">
      <Alert tone="error">{analysisErrorMessage(analysis.errorCode)}</Alert>
      {analysis.jobPostingId !== null && (
        <Link to={`/app/analisis/nuevo?vacante=${analysis.jobPostingId}`} className="inline-block text-sm font-medium text-accent underline">
          Intentar de nuevo
        </Link>
      )}
    </div>
  )
}

function Section({ id, title, children, action }: { id: string; title: string; children: ReactNode; action?: ReactNode }) {
  return (
    <section aria-labelledby={id} className="rounded-lg border border-line bg-surface p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 id={id} className="text-lg font-semibold">
          {title}
        </h2>
        {action}
      </div>
      <div className="mt-3">{children}</div>
    </section>
  )
}

function Badge({ children, tone = 'neutral' }: { children: ReactNode; tone?: 'neutral' | 'strong' }) {
  return (
    <span
      className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${tone === 'strong' ? 'bg-ink text-white' : 'bg-canvas text-ink-muted'}`}
    >
      {children}
    </span>
  )
}

function AnalysisBody({ result, score }: { result: JobAnalysisResult; score: ScoreContext }) {
  return (
    <>
      <section aria-labelledby="score-title" className={`rounded-lg border p-5 ${bandStyles[score.band]}`}>
        <h2 id="score-title" className="text-xl font-semibold">
          {bandLabels[score.band]}
        </h2>
        <p className="mt-1 text-sm">
          Puntaje estimado: <strong>{result.compatibilityScore}/100</strong>. Es una estimación de la IA, no una medida de tu valor profesional.
        </p>
        <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2">
          <div className="rounded-md bg-white/70 p-3">
            <dt className="font-medium">Requisitos obligatorios</dt>
            <dd>
              {score.requiredMet} cumplidos · {score.requiredMissing} por trabajar
            </dd>
          </div>
          <div className="rounded-md bg-white/70 p-3">
            <dt className="font-medium">Requisitos deseables</dt>
            <dd>
              {score.preferredMet} cumplidos · {score.preferredMissing} por trabajar
            </dd>
          </div>
        </dl>
        <p className="mt-4 text-sm">{result.summary}</p>
      </section>

      <Section id="strengths-title" title="Fortalezas respaldadas por tu perfil">
        {result.strengths.length === 0 ? (
          <p className="text-sm text-ink-muted">No encontramos requisitos claramente cumplidos con la información de tu perfil.</p>
        ) : (
          <ul className="space-y-3">
            {result.strengths.map((strength) => (
              <li key={`${strength.requirement}-${strength.evidence}`} className="border-l-2 border-emerald-400 pl-3">
                <p className="font-medium">
                  {strength.requirement} <Badge tone={strength.importance === 'required' ? 'strong' : 'neutral'}>{importanceLabels[strength.importance]}</Badge>
                </p>
                <p className="mt-1 text-sm text-ink-muted">Evidencia en tu perfil: {strength.evidence}</p>
              </li>
            ))}
          </ul>
        )}
      </Section>

      <Section id="gaps-title" title="Brechas y cómo abordarlas">
        {result.gaps.length === 0 ? (
          <p className="text-sm text-ink-muted">No se identificaron brechas.</p>
        ) : (
          <ul className="space-y-3">
            {result.gaps.map((gap) => (
              <li key={`${gap.requirement}-${gap.gapType}`} className="border-l-2 border-rose-300 pl-3">
                <p className="font-medium">
                  {gap.requirement} <Badge tone={gap.importance === 'required' ? 'strong' : 'neutral'}>{importanceLabels[gap.importance]}</Badge>{' '}
                  <Badge>{gapTypeLabels[gap.gapType]}</Badge>
                </p>
                <p className="mt-1 text-sm text-ink-muted">{gap.recommendation}</p>
              </li>
            ))}
          </ul>
        )}
      </Section>

      <div className="grid gap-6 md:grid-cols-2">
        <Section id="keywords-title" title="Palabras clave para tu CV">
          <p className="mb-3 text-xs text-ink-muted">Solo términos que tu perfil respalda.</p>
          <ul aria-label="Palabras clave" className="flex flex-wrap gap-2">
            {result.keywordsToInclude.map((keyword) => (
              <li key={keyword} className="rounded-full border border-line bg-canvas px-3 py-1 text-sm">
                {keyword}
              </li>
            ))}
          </ul>
        </Section>

        <Section id="cv-title" title="Recomendaciones para tu CV">
          <ol className="list-decimal space-y-2 pl-5 text-sm">
            {result.cvRecommendations.map((recommendation) => (
              <li key={recommendation}>{recommendation}</li>
            ))}
          </ol>
        </Section>
      </div>

      <Section
        id="summary-title"
        title="Resumen profesional adaptado"
        action={<CopyButton text={result.adaptedProfessionalSummary} label="Copiar resumen profesional" />}
      >
        <p className="whitespace-pre-line text-sm leading-relaxed">{result.adaptedProfessionalSummary}</p>
      </Section>

      <Section
        id="letter-title"
        title="Borrador de carta de presentación"
        action={<CopyButton text={result.coverLetterDraft} label="Copiar carta de presentación" />}
      >
        <p className="whitespace-pre-line text-sm leading-relaxed">{result.coverLetterDraft}</p>
        <p className="mt-3 text-xs text-ink-muted">Revísala y personalízala antes de enviarla.</p>
      </Section>

      <aside aria-labelledby="disclaimer-title" className="rounded-lg bg-accent-soft p-5 text-sm">
        <h2 id="disclaimer-title" className="font-semibold text-accent-strong">
          Aviso de transparencia
        </h2>
        <p className="mt-2">{result.disclaimer}</p>
        <p className="mt-2 text-ink-muted">
          PostulaAI solo usa la información de tu perfil y no agrega experiencia que no tengas. Revisa siempre el contenido antes de usarlo.
        </p>
      </aside>
    </>
  )
}
