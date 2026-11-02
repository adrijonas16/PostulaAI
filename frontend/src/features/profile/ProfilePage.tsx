import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Controller, useForm, useWatch, type FieldErrors } from 'react-hook-form'
import { applyServerErrors } from '../../shared/lib/applyServerErrors'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { TextAreaField, TextField } from '../../shared/ui/fields'
import { LoadingState } from '../../shared/ui/LoadingState'
import { PageHeader } from '../../shared/ui/Sheet'
import { TagInput } from '../../shared/ui/TagInput'
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
import { ProfileTabs, type ProfileTab } from './ProfileTabs'

type TabId = 'datos' | 'experiencia' | 'formacion' | 'proyectos' | 'habilidades'

const tabOrder: TabId[] = ['datos', 'experiencia', 'formacion', 'proyectos', 'habilidades']

const tabFields: Record<TabId, (keyof ProfileFormValues)[]> = {
  datos: ['fullName', 'headline', 'summary', 'links'],
  experiencia: ['experiences'],
  formacion: ['education', 'certifications'],
  proyectos: ['projects'],
  habilidades: ['technicalSkillsText', 'softSkillsText', 'languages'],
}

function tabsWithErrors(errors: FieldErrors<ProfileFormValues>): Set<TabId> {
  return new Set(tabOrder.filter((id) => tabFields[id].some((name) => errors[name] !== undefined)))
}

export function ProfilePage() {
  const queryClient = useQueryClient()
  const profileQuery = useQuery({ queryKey: profileQueryKey, queryFn: getProfile })
  const [status, setStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null)
  const [importNotice, setImportNotice] = useState<string[] | null>(null)
  const [tab, setTab] = useState<TabId>('datos')

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
  const values = useWatch({ control })

  useEffect(() => {
    if (profileQuery.data !== undefined) {
      reset(profileQuery.data === null ? emptyProfile : toFormValues(profileQuery.data))
    }
  }, [profileQuery.data, reset])

  const errorTabs = tabsWithErrors(errors)

  /**
   * Opens the first section that needs attention. Outside of the submit handler the rendered
   * `errors` can be one render behind, so the current state is read through getFieldState.
   */
  const showFirstError = (current?: FieldErrors<ProfileFormValues>) => {
    const isInvalid = (name: keyof ProfileFormValues) => (current !== undefined ? current[name] !== undefined : form.getFieldState(name).invalid)
    const target = tabOrder.find((id) => tabFields[id].some(isInvalid))
    if (target !== undefined) setTab(target)
  }

  const mutation = useMutation({
    mutationFn: saveProfile,
    onSuccess: (saved) => {
      queryClient.setQueryData(profileQueryKey, saved)
    },
  })

  const onSubmit = handleSubmit(
    async (submitted) => {
      setStatus(null)
      try {
        await mutation.mutateAsync(toProfileRequest(submitted))
        setImportNotice(null)
        setStatus({ tone: 'success', message: 'Perfil guardado.' })
      } catch (error) {
        setStatus({ tone: 'error', message: applyServerErrors(error, setError, profileErrorPath) })
        showFirstError()
      }
    },
    (invalid) => {
      showFirstError(invalid)
      setStatus({ tone: 'error', message: 'Revisa los campos marcados antes de guardar.' })
    },
  )

  if (profileQuery.isPending) {
    return <LoadingState label="Cargando tu perfil…" />
  }

  if (profileQuery.isError) {
    return (
      <div className="mx-auto max-w-3xl">
        <PageHeader title="Tu perfil" />
        <Alert tone="error">No pudimos cargar tu perfil. Recarga la página para intentarlo de nuevo.</Alert>
      </div>
    )
  }

  const filled = (text: string | undefined) => (text ?? '').trim().length > 0
  const tabs: ProfileTab[] = [
    { id: 'datos', label: 'Datos', done: filled(values.fullName) && filled(values.summary), hasErrors: errorTabs.has('datos') },
    {
      id: 'experiencia',
      label: 'Experiencia',
      count: values.experiences?.length ?? 0,
      done: (values.experiences?.length ?? 0) > 0,
      hasErrors: errorTabs.has('experiencia'),
    },
    {
      id: 'formacion',
      label: 'Formación',
      count: (values.education?.length ?? 0) + (values.certifications?.length ?? 0),
      done: (values.education?.length ?? 0) > 0,
      hasErrors: errorTabs.has('formacion'),
    },
    {
      id: 'proyectos',
      label: 'Proyectos',
      count: values.projects?.length ?? 0,
      done: (values.projects?.length ?? 0) > 0,
      hasErrors: errorTabs.has('proyectos'),
    },
    {
      id: 'habilidades',
      label: 'Habilidades e idiomas',
      done: filled(values.technicalSkillsText) && (values.languages?.length ?? 0) > 0,
      hasErrors: errorTabs.has('habilidades'),
    },
  ]
  const isEmptyProfile = profileQuery.data === null && !isDirty
  const panel = (id: TabId) => ({ role: 'tabpanel', id: `panel-${id}`, 'aria-labelledby': `tab-${id}`, hidden: tab !== id, className: 'space-y-5' }) as const

  return (
    <div className="mx-auto w-full max-w-3xl">
      <PageHeader
        title="Tu perfil"
        description="Es lo único que usa el análisis. La IA no agregará experiencia, estudios ni habilidades que no estén aquí."
      />

      <div className="space-y-4">
        <CvImportCard
          prominent={isEmptyProfile}
          hasExistingData={isDirty || profileQuery.data !== null}
          onImported={({ draft, warnings }) => {
            reset(draftToFormValues(draft), { keepDefaultValues: true })
            setStatus(null)
            setImportNotice(warnings)
            // Show what is still missing (dates, language levels...) right away.
            void trigger().then(() => showFirstError())
          }}
        />
        {importNotice !== null && (
          <Alert tone="info">
            <p className="font-semibold">Formulario precargado desde tu CV. Aún no se ha guardado.</p>
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

      <form noValidate onSubmit={onSubmit} className="mt-8">
        <ProfileTabs tabs={tabs} active={tab} onSelect={(id) => setTab(id as TabId)} />

        <div className="mt-5">
          <div {...panel('datos')}>
            <FormSection id="basics-title" title="Datos principales">
              <TextField label="Nombre profesional" required autoComplete="name" error={errors.fullName?.message} {...register('fullName')} />
              <TextField
                label="Titular"
                placeholder="Por ejemplo: Desarrolladora backend .NET"
                error={errors.headline?.message}
                {...register('headline')}
              />
              <TextAreaField
                label="Resumen profesional"
                rows={5}
                hint="Dos o tres frases sobre lo que haces y en qué eres fuerte."
                error={errors.summary?.message}
                {...register('summary')}
              />
            </FormSection>
            <FormSection id="links-title" title="Enlaces profesionales">
              <TextField label="LinkedIn" type="url" placeholder="https://" error={errors.links?.linkedInUrl?.message} {...register('links.linkedInUrl')} />
              <TextField label="GitHub" type="url" placeholder="https://" error={errors.links?.gitHubUrl?.message} {...register('links.gitHubUrl')} />
              <TextField
                label="Portafolio"
                type="url"
                placeholder="https://"
                error={errors.links?.portfolioUrl?.message}
                {...register('links.portfolioUrl')}
              />
            </FormSection>
          </div>

          <div {...panel('experiencia')}>
            <ExperienceSection control={control} register={register} errors={errors} />
          </div>

          <div {...panel('formacion')}>
            <EducationSection control={control} register={register} errors={errors} />
            <CertificationsSection control={control} register={register} errors={errors} />
          </div>

          <div {...panel('proyectos')}>
            <ProjectsSection control={control} register={register} errors={errors} />
          </div>

          <div {...panel('habilidades')}>
            <FormSection id="skills-title" title="Habilidades" description="Incluye solo las que puedas demostrar con tu experiencia o proyectos.">
              <Controller
                control={control}
                name="technicalSkillsText"
                render={({ field }) => (
                  <TagInput
                    label="Habilidades técnicas"
                    hint="Pulsa Enter o escribe una coma para agregar cada una."
                    value={field.value}
                    onChange={field.onChange}
                    error={errors.technicalSkillsText?.message}
                  />
                )}
              />
              <Controller
                control={control}
                name="softSkillsText"
                render={({ field }) => (
                  <TagInput label="Habilidades blandas" value={field.value} onChange={field.onChange} error={errors.softSkillsText?.message} />
                )}
              />
            </FormSection>
            <LanguagesSection control={control} register={register} errors={errors} />
          </div>
        </div>

        <div className="sticky bottom-0 z-10 -mx-4 mt-8 border-t border-rule bg-paper/95 px-4 py-4 backdrop-blur md:-mx-6 md:px-6">
          {status !== null && (
            <div className="mb-3">
              <Alert tone={status.tone}>{status.message}</Alert>
            </div>
          )}
          <div className="flex flex-wrap items-center gap-4">
            <Button type="submit" size="lg" disabled={isSubmitting}>
              {isSubmitting ? 'Guardando…' : 'Guardar perfil'}
            </Button>
            <p className="text-sm text-graphite" aria-live="polite">
              {isDirty ? (
                <span className="inline-flex items-center gap-2">
                  <span aria-hidden="true" className="size-2 rounded-full bg-redpen" />
                  Tienes cambios sin guardar
                </span>
              ) : profileQuery.data !== null ? (
                'Todo guardado'
              ) : null}
            </p>
          </div>
        </div>
      </form>
    </div>
  )
}
