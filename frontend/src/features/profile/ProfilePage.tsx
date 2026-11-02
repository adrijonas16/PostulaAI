import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { applyServerErrors } from '../../shared/lib/applyServerErrors'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { TextAreaField, TextField } from '../../shared/ui/fields'
import { LoadingState } from '../../shared/ui/LoadingState'
import { getProfile, profileQueryKey, saveProfile } from './profileApi'
import { CvImportCard } from './CvImportCard'
import { draftToFormValues, emptyProfile, profileErrorPath, profileSchema, toFormValues, toProfileRequest, type ProfileFormValues } from './profileForm'
import {
  CertificationsSection,
  EducationSection,
  ExperienceSection,
  FormSection,
  LanguagesSection,
  ProjectsSection,
} from './ProfileSections'

export function ProfilePage() {
  const queryClient = useQueryClient()
  const profileQuery = useQuery({ queryKey: profileQueryKey, queryFn: getProfile })
  const [status, setStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null)
  const [importNotice, setImportNotice] = useState<string[] | null>(null)

  const form = useForm<ProfileFormValues>({ resolver: zodResolver(profileSchema), defaultValues: emptyProfile })
  const {
    control,
    register,
    handleSubmit,
    reset,
    setError,
    trigger,
    formState: { errors, isSubmitting, isDirty },
  } = form

  useEffect(() => {
    if (profileQuery.data !== undefined) {
      reset(profileQuery.data === null ? emptyProfile : toFormValues(profileQuery.data))
    }
  }, [profileQuery.data, reset])

  const mutation = useMutation({
    mutationFn: saveProfile,
    onSuccess: (saved) => {
      queryClient.setQueryData(profileQueryKey, saved)
    },
  })

  const onSubmit = handleSubmit(
    async (values) => {
      setStatus(null)
      try {
        await mutation.mutateAsync(toProfileRequest(values))
        setImportNotice(null)
        setStatus({ tone: 'success', message: 'Perfil guardado.' })
      } catch (error) {
        setStatus({ tone: 'error', message: applyServerErrors(error, setError, profileErrorPath) })
      }
    },
    () => setStatus({ tone: 'error', message: 'Revisa los campos marcados antes de guardar.' }),
  )

  if (profileQuery.isPending) {
    return <LoadingState label="Cargando tu perfil…" />
  }

  if (profileQuery.isError) {
    return (
      <section className="mx-auto max-w-3xl">
        <h1 className="text-2xl font-semibold tracking-tight">Perfil profesional</h1>
        <div className="mt-6">
          <Alert tone="error">No pudimos cargar tu perfil. Recarga la página para intentarlo de nuevo.</Alert>
        </div>
      </section>
    )
  }

  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-3xl">
      <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
        Perfil profesional
      </h1>
      <p className="mt-2 text-ink-muted">
        Esta es la única información que usará el análisis. La IA no agregará experiencia, estudios ni habilidades que no
        escribas aquí.
      </p>

      <div className="mt-8 space-y-4">
        <CvImportCard
          hasExistingData={isDirty || profileQuery.data !== null}
          onImported={({ draft, warnings }) => {
            reset(draftToFormValues(draft), { keepDefaultValues: true })
            setStatus(null)
            setImportNotice(warnings)
            // Show what is still missing (dates, language levels...) right away.
            void trigger()
          }}
        />
        {importNotice !== null && (
          <Alert tone="info">
            <p className="font-medium">Formulario precargado desde tu CV. Aún no se ha guardado.</p>
            <p className="mt-1">Revisa cada sección, completa lo marcado en rojo y pulsa «Guardar perfil».</p>
            {importNotice.length > 0 && (
              <ul className="mt-2 list-disc space-y-1 pl-5">
                {importNotice.map((warning) => (
                  <li key={warning}>{warning}</li>
                ))}
              </ul>
            )}
          </Alert>
        )}
      </div>

      <form noValidate onSubmit={onSubmit} className="mt-6 space-y-6">
        <FormSection id="basics-title" title="Datos principales">
          <TextField label="Nombre profesional" required autoComplete="name" error={errors.fullName?.message} {...register('fullName')} />
          <TextField
            label="Titular"
            placeholder="Por ejemplo: Desarrolladora backend .NET"
            error={errors.headline?.message}
            {...register('headline')}
          />
          <TextAreaField label="Resumen profesional" rows={5} error={errors.summary?.message} {...register('summary')} />
        </FormSection>

        <ExperienceSection control={control} register={register} errors={errors} />
        <EducationSection control={control} register={register} errors={errors} />
        <ProjectsSection control={control} register={register} errors={errors} />
        <CertificationsSection control={control} register={register} errors={errors} />

        <FormSection id="skills-title" title="Habilidades" description="Separadas por comas. Incluye solo las que puedas demostrar.">
          <TextAreaField label="Habilidades técnicas" rows={3} error={errors.technicalSkillsText?.message} {...register('technicalSkillsText')} />
          <TextAreaField label="Habilidades blandas" rows={2} error={errors.softSkillsText?.message} {...register('softSkillsText')} />
        </FormSection>

        <LanguagesSection control={control} register={register} errors={errors} />

        <FormSection id="links-title" title="Enlaces profesionales">
          <TextField label="LinkedIn" type="url" placeholder="https://" error={errors.links?.linkedInUrl?.message} {...register('links.linkedInUrl')} />
          <TextField label="GitHub" type="url" placeholder="https://" error={errors.links?.gitHubUrl?.message} {...register('links.gitHubUrl')} />
          <TextField label="Portafolio" type="url" placeholder="https://" error={errors.links?.portfolioUrl?.message} {...register('links.portfolioUrl')} />
        </FormSection>

        <div className="sticky bottom-0 -mx-1 flex flex-col gap-3 border-t border-line bg-canvas/95 px-1 py-4 backdrop-blur">
          {status !== null && <Alert tone={status.tone}>{status.message}</Alert>}
          <div>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Guardando…' : 'Guardar perfil'}
            </Button>
          </div>
        </div>
      </form>
    </section>
  )
}
