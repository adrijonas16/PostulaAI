import { useFieldArray, useWatch, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form'
import type { ReactNode } from 'react'
import { Button } from '../../shared/ui/Button'
import { CheckboxField, SelectField, TextAreaField, TextField } from '../../shared/ui/fields'
import {
  emptyCertification,
  emptyEducation,
  emptyExperience,
  emptyLanguage,
  emptyProject,
  type ProfileFormValues,
} from './profileForm'

interface SectionProps {
  control: Control<ProfileFormValues>
  register: UseFormRegister<ProfileFormValues>
  errors: FieldErrors<ProfileFormValues>
}

const educationStatusOptions = [
  { value: 'completed', label: 'Finalizado' },
  { value: 'in_progress', label: 'En curso' },
  { value: 'incomplete', label: 'No finalizado' },
] as const

const proficiencyOptions = [
  { value: '', label: 'Elige el nivel' },
  { value: 'basic', label: 'Básico' },
  { value: 'intermediate', label: 'Intermedio' },
  { value: 'advanced', label: 'Avanzado' },
  { value: 'native', label: 'Nativo' },
] as const

export function FormSection({ id, title, description, children }: { id: string; title: string; description?: string; children: ReactNode }) {
  return (
    <section aria-labelledby={id} className="rounded-lg border border-line bg-surface p-5">
      <h2 id={id} className="text-lg font-semibold">
        {title}
      </h2>
      {description !== undefined && <p className="mt-1 text-sm text-ink-muted">{description}</p>}
      <div className="mt-4 space-y-4">{children}</div>
    </section>
  )
}

function ItemCard({ title, onRemove, children }: { title: string; onRemove: () => void; children: ReactNode }) {
  return (
    <fieldset className="rounded-md border border-line p-4">
      <legend className="px-1 text-sm font-medium">{title}</legend>
      <div className="grid gap-4 md:grid-cols-2">{children}</div>
      <div className="mt-3 flex justify-end">
        <Button variant="danger" onClick={onRemove}>
          Eliminar {title.toLocaleLowerCase('es')}
        </Button>
      </div>
    </fieldset>
  )
}

function ExperienceItem({ index, control, register, errors, onRemove }: SectionProps & { index: number; onRemove: () => void }) {
  const isCurrent = useWatch({ control, name: `experiences.${index}.isCurrent` })
  const itemErrors = errors.experiences?.[index]
  return (
    <ItemCard title={`Experiencia ${index + 1}`} onRemove={onRemove}>
      <TextField label="Empresa" required error={itemErrors?.company?.message} {...register(`experiences.${index}.company`)} />
      <TextField label="Cargo" required error={itemErrors?.role?.message} {...register(`experiences.${index}.role`)} />
      <TextField label="Ubicación" error={itemErrors?.location?.message} {...register(`experiences.${index}.location`)} />
      <div className="self-end pb-2">
        <CheckboxField label="Es mi empleo actual" {...register(`experiences.${index}.isCurrent`)} />
      </div>
      <TextField label="Inicio" type="month" required error={itemErrors?.startMonth?.message} {...register(`experiences.${index}.startMonth`)} />
      <TextField
        label="Fin"
        type="month"
        disabled={isCurrent}
        error={itemErrors?.endMonth?.message}
        {...register(`experiences.${index}.endMonth`)}
      />
      <div className="md:col-span-2">
        <TextAreaField label="Descripción" error={itemErrors?.description?.message} {...register(`experiences.${index}.description`)} />
      </div>
      <div className="md:col-span-2">
        <TextAreaField
          label="Logros"
          hint="Uno por línea. Escribe solo logros reales; la IA no agregará otros."
          error={itemErrors?.achievementsText?.message}
          {...register(`experiences.${index}.achievementsText`)}
        />
      </div>
      <div className="md:col-span-2">
        <TextField
          label="Tecnologías usadas"
          hint="Separadas por comas."
          error={itemErrors?.technologiesText?.message}
          {...register(`experiences.${index}.technologiesText`)}
        />
      </div>
    </ItemCard>
  )
}

export function ExperienceSection(props: SectionProps) {
  const { fields, append, remove } = useFieldArray({ control: props.control, name: 'experiences' })
  return (
    <FormSection id="experience-title" title="Experiencia laboral">
      {fields.length === 0 && <p className="text-sm text-ink-muted">Aún no has agregado experiencia.</p>}
      {fields.map((field, index) => (
        <ExperienceItem key={field.id} index={index} onRemove={() => remove(index)} {...props} />
      ))}
      <Button variant="secondary" onClick={() => append({ ...emptyExperience })}>
        Agregar experiencia
      </Button>
    </FormSection>
  )
}

export function EducationSection({ control, register, errors }: SectionProps) {
  const { fields, append, remove } = useFieldArray({ control, name: 'education' })
  return (
    <FormSection id="education-title" title="Educación">
      {fields.length === 0 && <p className="text-sm text-ink-muted">Aún no has agregado estudios.</p>}
      {fields.map((field, index) => {
        const itemErrors = errors.education?.[index]
        return (
          <ItemCard key={field.id} title={`Estudio ${index + 1}`} onRemove={() => remove(index)}>
            <TextField label="Institución" required error={itemErrors?.institution?.message} {...register(`education.${index}.institution`)} />
            <TextField label="Título o programa" required error={itemErrors?.degree?.message} {...register(`education.${index}.degree`)} />
            <TextField label="Área de estudio" error={itemErrors?.fieldOfStudy?.message} {...register(`education.${index}.fieldOfStudy`)} />
            <SelectField label="Estado" options={educationStatusOptions} error={itemErrors?.status?.message} {...register(`education.${index}.status`)} />
            <TextField label="Inicio" type="month" error={itemErrors?.startMonth?.message} {...register(`education.${index}.startMonth`)} />
            <TextField label="Fin" type="month" error={itemErrors?.endMonth?.message} {...register(`education.${index}.endMonth`)} />
          </ItemCard>
        )
      })}
      <Button variant="secondary" onClick={() => append({ ...emptyEducation })}>
        Agregar estudio
      </Button>
    </FormSection>
  )
}

export function ProjectsSection({ control, register, errors }: SectionProps) {
  const { fields, append, remove } = useFieldArray({ control, name: 'projects' })
  return (
    <FormSection id="projects-title" title="Proyectos">
      {fields.length === 0 && <p className="text-sm text-ink-muted">Aún no has agregado proyectos.</p>}
      {fields.map((field, index) => {
        const itemErrors = errors.projects?.[index]
        return (
          <ItemCard key={field.id} title={`Proyecto ${index + 1}`} onRemove={() => remove(index)}>
            <TextField label="Nombre" required error={itemErrors?.name?.message} {...register(`projects.${index}.name`)} />
            <TextField label="Enlace" type="url" placeholder="https://" error={itemErrors?.url?.message} {...register(`projects.${index}.url`)} />
            <div className="md:col-span-2">
              <TextAreaField label="Descripción" error={itemErrors?.description?.message} {...register(`projects.${index}.description`)} />
            </div>
            <div className="md:col-span-2">
              <TextField label="Tecnologías" hint="Separadas por comas." error={itemErrors?.technologiesText?.message} {...register(`projects.${index}.technologiesText`)} />
            </div>
          </ItemCard>
        )
      })}
      <Button variant="secondary" onClick={() => append({ ...emptyProject })}>
        Agregar proyecto
      </Button>
    </FormSection>
  )
}

export function CertificationsSection({ control, register, errors }: SectionProps) {
  const { fields, append, remove } = useFieldArray({ control, name: 'certifications' })
  return (
    <FormSection id="certifications-title" title="Certificaciones">
      {fields.length === 0 && <p className="text-sm text-ink-muted">Aún no has agregado certificaciones.</p>}
      {fields.map((field, index) => {
        const itemErrors = errors.certifications?.[index]
        return (
          <ItemCard key={field.id} title={`Certificación ${index + 1}`} onRemove={() => remove(index)}>
            <TextField label="Nombre" required error={itemErrors?.name?.message} {...register(`certifications.${index}.name`)} />
            <TextField label="Entidad emisora" required error={itemErrors?.issuer?.message} {...register(`certifications.${index}.issuer`)} />
            <TextField label="Fecha de emisión" type="month" error={itemErrors?.issuedMonth?.message} {...register(`certifications.${index}.issuedMonth`)} />
            <TextField
              label="Enlace de verificación"
              type="url"
              placeholder="https://"
              error={itemErrors?.credentialUrl?.message}
              {...register(`certifications.${index}.credentialUrl`)}
            />
          </ItemCard>
        )
      })}
      <Button variant="secondary" onClick={() => append({ ...emptyCertification })}>
        Agregar certificación
      </Button>
    </FormSection>
  )
}

export function LanguagesSection({ control, register, errors }: SectionProps) {
  const { fields, append, remove } = useFieldArray({ control, name: 'languages' })
  return (
    <FormSection id="languages-title" title="Idiomas">
      {fields.length === 0 && <p className="text-sm text-ink-muted">Aún no has agregado idiomas.</p>}
      {fields.map((field, index) => {
        const itemErrors = errors.languages?.[index]
        return (
          <ItemCard key={field.id} title={`Idioma ${index + 1}`} onRemove={() => remove(index)}>
            <TextField label="Idioma" required error={itemErrors?.language?.message} {...register(`languages.${index}.language`)} />
            <SelectField label="Nivel" options={proficiencyOptions} error={itemErrors?.proficiency?.message} {...register(`languages.${index}.proficiency`)} />
          </ItemCard>
        )
      })}
      <Button variant="secondary" onClick={() => append({ ...emptyLanguage })}>
        Agregar idioma
      </Button>
    </FormSection>
  )
}
