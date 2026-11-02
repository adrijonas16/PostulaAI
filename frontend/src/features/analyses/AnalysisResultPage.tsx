import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../shared/api/ApiError'
import type { AnalysisResponse, JobAnalysisResult, ScoreContext } from '../../shared/api/types'
import { formatDate } from '../../shared/lib/formatDate'
import { Alert } from '../../shared/ui/Alert'
import { primaryLinkClass } from '../../shared/ui/Button'
import { LoadingState } from '../../shared/ui/LoadingState'
import { analysisKeys, getAnalysis } from './analysesApi'
import { analysisErrorMessage, bandLabels } from './analysisLabels'
import { CopyButton } from './CopyButton'
import { EditableDraft } from './EditableDraft'
import { MatchBar, MatchLegend } from './MatchBar'
import { ApplicationPanel } from '../jobs/ApplicationPanel'
import { getJobPosting, jobPostingKeys } from '../jobs/jobsApi'
import { RequirementMap } from './RequirementMap'

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
        <h1 className="text-3xl font-bold">Resultado del análisis</h1>
        <div className="mt-6 space-y-3">
          <Alert tone="error">{notFound ? 'No encontramos ese análisis.' : 'No pudimos cargar el análisis. Intenta de nuevo.'}</Alert>
          <Link to="/app/analisis" className="text-sm font-semibold text-pen underline">
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
        <p className="text-sm text-graphite">Análisis del {formatDate(analysis.createdAt)}</p>
        <h1 id="page-title" className="mt-1 text-3xl font-bold leading-tight">
          {analysis.jobTitle}
        </h1>
        {analysis.jobCompany !== null && <p className="mt-1 text-lg text-graphite">{analysis.jobCompany}</p>}
      </header>

      {analysis.result === null || analysis.score === null ? (
        <FailedAnalysis analysis={analysis} />
      ) : (
        <AnalysisBody result={analysis.result} score={analysis.score} jobPostingId={analysis.jobPostingId} />
      )}

      <details className="rounded-[var(--radius-sheet)] border border-rule bg-sheet p-4 text-sm text-graphite">
        <summary className="cursor-pointer font-semibold text-ink">Detalles técnicos</summary>
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
    <div className="space-y-4">
      <Alert tone="error">{analysisErrorMessage(analysis.errorCode)}</Alert>
      {analysis.jobPostingId !== null && (
        <Link to={`/app/analisis/nuevo?vacante=${analysis.jobPostingId}`} className={primaryLinkClass}>
          Intentar de nuevo
        </Link>
      )}
    </div>
  )
}

function Verdict({ result, score }: { result: JobAnalysisResult; score: ScoreContext }) {
  const required = score.requiredMet + score.requiredMissing
  const preferred = score.preferredMet + score.preferredMissing
  return (
    <section aria-labelledby="score-title" className="rounded-[var(--radius-sheet)] border-2 border-ink bg-sheet p-5 md:p-7">
      <h2 id="score-title" className="text-2xl font-bold">
        {bandLabels[score.band]}
      </h2>
      <p className="mt-2 text-lg">
        Cumples <strong className="marker">{score.requiredMet} de {required}</strong> requisitos obligatorios
        {preferred > 0 && (
          <>
            {' '}
            y <strong>{score.preferredMet} de {preferred}</strong> deseables
          </>
        )}
        .
      </p>
      <div className="mt-5">
        <MatchBar score={score} />
        <MatchLegend />
      </div>
      <p className="mt-5 text-sm text-graphite">
        Puntaje estimado: <strong className="text-ink">{result.compatibilityScore}/100</strong>. Es una estimación de la IA, no una medida de tu valor
        profesional.
      </p>
      <p className="mt-4 max-w-2xl leading-relaxed">{result.summary}</p>
    </section>
  )
}

function CvChecklist({ items }: { items: string[] }) {
  const [done, setDone] = useState<ReadonlySet<number>>(new Set())
  const toggle = (index: number) =>
    setDone((current) => {
      const next = new Set(current)
      if (next.has(index)) next.delete(index)
      else next.add(index)
      return next
    })

  return (
    <section aria-labelledby="cv-title" className="rounded-[var(--radius-sheet)] border border-rule bg-sheet p-5 md:p-6">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 id="cv-title" className="text-lg font-bold">
          Ajustes para tu CV
        </h2>
        <p className="text-sm text-graphite" aria-live="polite">
          {done.size} de {items.length} hechos
        </p>
      </div>
      <ul className="mt-3 space-y-1">
        {items.map((item, index) => (
          <li key={item}>
            <label className="flex cursor-pointer items-start gap-3 rounded-[var(--radius-field)] p-2 hover:bg-paper">
              <input type="checkbox" checked={done.has(index)} onChange={() => toggle(index)} className="mt-1 size-4 shrink-0 accent-pen" />
              <span className={done.has(index) ? 'text-graphite line-through' : ''}>{item}</span>
            </label>
          </li>
        ))}
      </ul>
    </section>
  )
}

function AnalysisBody({ result, score, jobPostingId }: { result: JobAnalysisResult; score: ScoreContext; jobPostingId: string | null }) {
  const [summary, setSummary] = useState(result.adaptedProfessionalSummary)
  const [coverLetter, setCoverLetter] = useState(result.coverLetterDraft)
  const postingQuery = useQuery({
    queryKey: jobPostingKeys.detail(jobPostingId ?? 'none'),
    queryFn: () => getJobPosting(jobPostingId ?? ''),
    enabled: jobPostingId !== null,
    // The posting may have been deleted after the analysis; the analysis keeps its own snapshot.
    retry: false,
  })

  return (
    <>
      <Verdict result={result} score={score} />
      <RequirementMap result={result} />

      <div className="grid gap-6 md:grid-cols-2">
        <CvChecklist items={result.cvRecommendations} />

        <section aria-labelledby="keywords-title" className="rounded-[var(--radius-sheet)] border border-rule bg-sheet p-5 md:p-6">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 id="keywords-title" className="text-lg font-bold">
              Palabras clave para tu CV
            </h2>
            {result.keywordsToInclude.length > 0 && <CopyButton text={result.keywordsToInclude.join(', ')} label="Copiar palabras clave" />}
          </div>
          <p className="mt-1 text-sm text-graphite">Solo términos que tu perfil respalda. Úsalos tal cual aparecen en la vacante.</p>
          <ul aria-label="Palabras clave" className="mt-3 flex flex-wrap gap-2">
            {result.keywordsToInclude.map((keyword) => (
              <li key={keyword} className="rounded-[var(--radius-field)] bg-marker-soft px-2.5 py-1 text-sm font-semibold">
                {keyword}
              </li>
            ))}
          </ul>
        </section>
      </div>

      <EditableDraft
        id="summary-title"
        title="Resumen profesional adaptado"
        original={result.adaptedProfessionalSummary}
        text={summary}
        onChange={setSummary}
        copyLabel="Copiar resumen profesional"
      />
      <EditableDraft
        id="letter-title"
        title="Borrador de carta de presentación"
        original={result.coverLetterDraft}
        text={coverLetter}
        onChange={setCoverLetter}
        copyLabel="Copiar carta de presentación"
        hint="Personalízala antes de enviarla."
      />

      {postingQuery.data !== undefined && <ApplicationPanel posting={postingQuery.data} adaptedTexts={{ summary, coverLetter }} />}

      <aside aria-labelledby="disclaimer-title" className="rounded-[var(--radius-sheet)] bg-pen-soft p-5 text-sm">
        <h2 id="disclaimer-title" className="font-bold text-pen-strong">
          Aviso de transparencia
        </h2>
        <p className="mt-2">{result.disclaimer}</p>
        <p className="mt-2 text-graphite">
          PostulaAI solo usa la información de tu perfil y no agrega experiencia que no tengas. Revisa siempre el contenido antes de usarlo.
        </p>
      </aside>
    </>
  )
}
