import { z } from 'zod'
import { workModes, type JobPostingRequest, type JobPostingResponse, type WorkMode } from '../../shared/api/types'

// Mirrors backend limits (Modules/JobPostings/Domain/JobPostingLimits).
export const DESCRIPTION_MIN = 30
export const DESCRIPTION_MAX = 20_000

export const workModeLabels: Record<WorkMode, string> = {
  unspecified: 'No especificada',
  onsite: 'Presencial',
  hybrid: 'Híbrida',
  remote: 'Remota',
}

export const jobPostingSchema = z.object({
  title: z.string().trim().min(1, 'Ingresa el título de la vacante.').max(200, 'No puede superar 200 caracteres.'),
  company: z.string().trim().max(200, 'No puede superar 200 caracteres.'),
  location: z.string().trim().max(200, 'No puede superar 200 caracteres.'),
  workMode: z.enum(workModes),
  description: z
    .string()
    .trim()
    .min(DESCRIPTION_MIN, `Pega la descripción completa de la vacante (al menos ${DESCRIPTION_MIN} caracteres).`)
    .max(DESCRIPTION_MAX, `La descripción no puede superar ${DESCRIPTION_MAX} caracteres.`),
  sourceUrl: z
    .string()
    .trim()
    .max(500, 'No puede superar 500 caracteres.')
    .refine((value) => value === '' || /^https?:\/\/\S+$/i.test(value), 'Ingresa una URL que empiece por http:// o https://.'),
})

export type JobPostingFormValues = z.infer<typeof jobPostingSchema>

export const emptyJobPosting: JobPostingFormValues = {
  title: '',
  company: '',
  location: '',
  workMode: 'unspecified',
  description: '',
  sourceUrl: '',
}

const nullIfEmpty = (value: string): string | null => (value.trim() === '' ? null : value.trim())

export function toJobPostingRequest(values: JobPostingFormValues): JobPostingRequest {
  return {
    title: values.title.trim(),
    company: nullIfEmpty(values.company),
    location: nullIfEmpty(values.location),
    workMode: values.workMode,
    description: values.description.trim(),
    sourceUrl: nullIfEmpty(values.sourceUrl),
  }
}

export function toJobPostingFormValues(posting: JobPostingResponse): JobPostingFormValues {
  return {
    title: posting.title,
    company: posting.company ?? '',
    location: posting.location ?? '',
    workMode: posting.workMode,
    description: posting.description,
    sourceUrl: posting.sourceUrl ?? '',
  }
}
