import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../../shared/api/ApiError'
import { applyServerErrors } from '../../shared/lib/applyServerErrors'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { SelectField, TextAreaField, TextField } from '../../shared/ui/fields'
import { LoadingState } from '../../shared/ui/LoadingState'
import {
  DESCRIPTION_MAX,
  emptyJobPosting,
  jobPostingSchema,
  toJobPostingFormValues,
  toJobPostingRequest,
  workModeLabels,
  type JobPostingFormValues,
} from './jobPostingForm'
import { createJobPosting, getJobPosting, jobPostingKeys, updateJobPosting } from './jobsApi'
import { workModes, type JobPostingRequest } from '../../shared/api/types'

const workModeOptions = workModes.map((mode) => ({ value: mode, label: workModeLabels[mode] }))

export function JobPostingEditorPage() {
  const { jobPostingId } = useParams()
  const isEditing = jobPostingId !== undefined
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const postingQuery = useQuery({
    queryKey: jobPostingKeys.detail(jobPostingId ?? 'new'),
    queryFn: () => getJobPosting(jobPostingId ?? ''),
    enabled: isEditing,
  })

  const {
    control,
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<JobPostingFormValues>({ resolver: zodResolver(jobPostingSchema), defaultValues: emptyJobPosting })

  useEffect(() => {
    if (postingQuery.data !== undefined) {
      reset(toJobPostingFormValues(postingQuery.data))
    }
  }, [postingQuery.data, reset])

  const description = useWatch({ control, name: 'description' })

  const mutation = useMutation({
    mutationFn: (request: JobPostingRequest) =>
      jobPostingId === undefined ? createJobPosting(request) : updateJobPosting(jobPostingId, request),
    onSuccess: async (saved) => {
      queryClient.setQueryData(jobPostingKeys.detail(saved.id), saved)
      await queryClient.invalidateQueries({ queryKey: [...jobPostingKeys.all, 'list'] })
    },
  })

  const onSubmit = handleSubmit(
    async (values) => {
      setFormError(null)
      try {
        await mutation.mutateAsync(toJobPostingRequest(values))
        await navigate('/app/vacantes', { state: { notice: isEditing ? 'Vacante actualizada.' : 'Vacante guardada.' } })
      } catch (error) {
        setFormError(applyServerErrors(error, setError))
      }
    },
    () => setFormError('Revisa los campos marcados antes de guardar.'),
  )

  const title = isEditing ? 'Editar vacante' : 'Nueva vacante'

  if (isEditing && postingQuery.isPending) {
    return <LoadingState label="Cargando la vacante…" />
  }

  if (isEditing && postingQuery.isError) {
    const notFound = postingQuery.error instanceof ApiError && postingQuery.error.status === 404
    return (
      <section className="mx-auto max-w-3xl">
        <h1 className="text-3xl font-bold">{title}</h1>
        <div className="mt-6 space-y-4">
          <Alert tone="error">{notFound ? 'No encontramos esa vacante.' : 'No pudimos cargar la vacante. Intenta de nuevo.'}</Alert>
          <Link to="/app/vacantes" className="text-sm font-semibold text-pen underline">
            Volver a mis vacantes
          </Link>
        </div>
      </section>
    )
  }

  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-3xl">
      <h1 id="page-title" className="text-3xl font-bold">
        {title}
      </h1>
      <p className="mt-2 text-graphite">Pega la oferta tal como la publicó la empresa. El análisis compara tu perfil con este texto.</p>

      <form noValidate onSubmit={onSubmit} className="mt-8 space-y-5 rounded-[var(--radius-sheet)] border border-rule bg-sheet p-5">
        {formError !== null && <Alert tone="error">{formError}</Alert>}
        <TextField label="Título del cargo" required error={errors.title?.message} {...register('title')} />
        <div className="grid gap-4 md:grid-cols-2">
          <TextField label="Empresa" hint="Opcional si la oferta es confidencial." error={errors.company?.message} {...register('company')} />
          <TextField label="Ubicación" error={errors.location?.message} {...register('location')} />
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          <SelectField label="Modalidad" options={workModeOptions} error={errors.workMode?.message} {...register('workMode')} />
          <TextField label="Enlace a la oferta" type="url" placeholder="https://" error={errors.sourceUrl?.message} {...register('sourceUrl')} />
        </div>
        <TextAreaField
          label="Descripción de la vacante"
          required
          rows={14}
          hint={`${description.length.toLocaleString('es')} / ${DESCRIPTION_MAX.toLocaleString('es')} caracteres. Incluye requisitos, responsabilidades y deseables.`}
          error={errors.description?.message}
          {...register('description')}
        />
        <div className="flex flex-wrap gap-3">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Guardando…' : 'Guardar vacante'}
          </Button>
          <Link to="/app/vacantes" className="inline-flex items-center rounded-[var(--radius-field)] px-4 py-2 text-sm text-graphite hover:text-ink">
            Cancelar
          </Link>
        </div>
      </form>
    </section>
  )
}
