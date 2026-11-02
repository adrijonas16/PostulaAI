import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useId, useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/ApiError'
import { applicationStatuses, type ApplicationStatus, type JobPostingResponse } from '../../shared/api/types'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { applicationStatusLabels, formatDay, todayIso } from './applicationLabels'
import { jobPostingKeys, updateApplication } from './jobsApi'

interface ApplicationPanelProps {
  posting: JobPostingResponse
  /** Texts the user just adapted (result page). When present, they can be saved as "what I sent". */
  adaptedTexts?: { summary: string; coverLetter: string }
}

/**
 * Where the user records that they applied, when, and what they sent.
 * PostulaAI cannot see other job sites, so this is always the user's own record.
 */
export function ApplicationPanel({ posting, adaptedTexts }: ApplicationPanelProps) {
  const queryClient = useQueryClient()
  const current = posting.application
  const [status, setStatus] = useState<ApplicationStatus>(current.status)
  const [appliedOn, setAppliedOn] = useState(current.appliedOn ?? todayIso())
  const [notes, setNotes] = useState(current.notes ?? '')
  const [keepTexts, setKeepTexts] = useState(adaptedTexts !== undefined)
  const [feedback, setFeedback] = useState<{ tone: 'success' | 'error'; message: string } | null>(null)
  const name = useId()
  const dateId = useId()
  const notesId = useId()

  const mutation = useMutation({
    mutationFn: () =>
      updateApplication(posting.id, {
        status,
        appliedOn: status === 'saved' ? null : appliedOn,
        notes: notes.trim() === '' ? null : notes,
        submittedSummary: keepTexts && adaptedTexts !== undefined ? adaptedTexts.summary : current.submittedSummary,
        submittedCoverLetter: keepTexts && adaptedTexts !== undefined ? adaptedTexts.coverLetter : current.submittedCoverLetter,
      }),
    onSuccess: async (saved) => {
      queryClient.setQueryData(jobPostingKeys.detail(saved.id), saved)
      await queryClient.invalidateQueries({ queryKey: jobPostingKeys.all })
      setFeedback({ tone: 'success', message: saved.application.status === 'saved' ? 'Postulación actualizada.' : 'Postulación guardada.' })
    },
    onError: (error) =>
      setFeedback({ tone: 'error', message: error instanceof ApiError ? error.message : 'No pudimos guardar la postulación. Intenta de nuevo.' }),
  })

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFeedback(null)
    mutation.mutate()
  }

  const hasSentTexts = current.submittedSummary !== null || current.submittedCoverLetter !== null

  return (
    <section aria-labelledby="application-title" className="rounded-[var(--radius-sheet)] border-2 border-pen bg-sheet p-5 md:p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="application-title" className="text-lg font-bold">
            Tu postulación
          </h2>
          <p className="mt-1 text-sm text-graphite">
            {current.status === 'saved' || current.appliedOn === null
              ? 'Cuando te postules en el sitio de la empresa, regístralo aquí para no perder el hilo.'
              : `Te postulaste el ${formatDay(current.appliedOn)}.`}
          </p>
        </div>
        {posting.sourceUrl !== null && (
          <a href={posting.sourceUrl} target="_blank" rel="noopener noreferrer" className="text-sm font-semibold text-pen underline">
            Abrir la oferta original<span className="sr-only"> (se abre en otra pestaña)</span>
          </a>
        )}
      </div>

      <form onSubmit={onSubmit} className="mt-5 space-y-5">
        <fieldset>
          <legend className="text-sm font-semibold">Estado</legend>
          <div className="mt-2 flex flex-wrap gap-2">
            {applicationStatuses.map((option) => (
              <label
                key={option}
                className="cursor-pointer rounded-full border-2 border-rule px-3.5 py-1.5 text-sm font-semibold text-graphite hover:border-graphite has-[:checked]:border-pen has-[:checked]:bg-pen has-[:checked]:text-sheet has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-pen"
              >
                <input type="radio" name={name} value={option} checked={status === option} onChange={() => setStatus(option)} className="sr-only" />
                {applicationStatusLabels[option]}
              </label>
            ))}
          </div>
        </fieldset>

        {status !== 'saved' && (
          <div className="grid gap-4 sm:grid-cols-[12rem_1fr]">
            <div>
              <label htmlFor={dateId} className="block text-sm font-semibold">
                Fecha de postulación
              </label>
              <input
                id={dateId}
                type="date"
                required
                max={todayIso()}
                value={appliedOn}
                onChange={(event) => setAppliedOn(event.target.value)}
                className="mt-1.5 block w-full rounded-[var(--radius-field)] border border-rule bg-sheet px-3 py-2 focus:border-pen focus:outline-none focus:ring-2 focus:ring-pen-soft"
              />
            </div>
            <div>
              <label htmlFor={notesId} className="block text-sm font-semibold">
                Notas <span className="font-normal text-graphite">(opcional)</span>
              </label>
              <textarea
                id={notesId}
                rows={2}
                maxLength={2000}
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                placeholder="Por ejemplo: me postulé por LinkedIn; entrevista el lunes con Ana."
                className="mt-1.5 block w-full rounded-[var(--radius-field)] border border-rule bg-sheet px-3 py-2 focus:border-pen focus:outline-none focus:ring-2 focus:ring-pen-soft"
              />
            </div>
          </div>
        )}

        {adaptedTexts !== undefined && status !== 'saved' && (
          <label className="flex cursor-pointer items-start gap-3 text-sm">
            <input type="checkbox" checked={keepTexts} onChange={(event) => setKeepTexts(event.target.checked)} className="mt-0.5 size-4 accent-pen" />
            <span>
              Guardar el resumen y la carta tal como quedaron en esta página, como lo que enviaste.
              {hasSentTexts && <span className="block text-graphite">Reemplazará los textos guardados antes.</span>}
            </span>
          </label>
        )}

        {feedback !== null && <Alert tone={feedback.tone}>{feedback.message}</Alert>}

        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'Guardando…' : 'Guardar postulación'}
        </Button>
      </form>

      {hasSentTexts && (
        <details className="mt-5 border-t border-rule pt-4 text-sm">
          <summary className="cursor-pointer font-semibold">Lo que enviaste</summary>
          {current.submittedSummary !== null && (
            <div className="mt-3">
              <h3 className="font-semibold">Resumen profesional</h3>
              <p className="mt-1 whitespace-pre-line text-graphite">{current.submittedSummary}</p>
            </div>
          )}
          {current.submittedCoverLetter !== null && (
            <div className="mt-3">
              <h3 className="font-semibold">Carta de presentación</h3>
              <p className="mt-1 whitespace-pre-line text-graphite">{current.submittedCoverLetter}</p>
            </div>
          )}
        </details>
      )}
    </section>
  )
}
