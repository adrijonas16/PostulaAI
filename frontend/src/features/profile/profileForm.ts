import { z } from 'zod'
import {
  educationStatuses,
  languageProficiencies,
  type ProfileDraft,
  type ProfileRequest,
  type ProfileResponse,
} from '../../shared/api/types'

// Mirrors backend limits (Modules/ProfessionalProfiles/Domain/ProfileLimits). The backend is the source of truth.
const MONTH = /^\d{4}-(0[1-9]|1[0-2])$/

const required = (message: string, max: number) =>
  z.string().trim().min(1, message).max(max, `No puede superar ${max} caracteres.`)
const optional = (max: number) => z.string().trim().max(max, `No puede superar ${max} caracteres.`)
const optionalUrl = z
  .string()
  .trim()
  .max(500, 'No puede superar 500 caracteres.')
  .refine((value) => value === '' || /^https?:\/\/\S+$/i.test(value), 'Ingresa una URL que empiece por http:// o https://.')
const month = z.string().regex(MONTH, 'Elige mes y año.')
const optionalMonth = z.string().refine((value) => value === '' || MONTH.test(value), 'Elige mes y año.')

const experienceSchema = z
  .object({
    company: required('Ingresa la empresa.', 150),
    role: required('Ingresa el cargo.', 150),
    location: optional(150),
    startMonth: month,
    endMonth: optionalMonth,
    isCurrent: z.boolean(),
    description: optional(4000),
    achievementsText: z.string(),
    technologiesText: z.string(),
  })
  .superRefine((value, context) => {
    if (!value.isCurrent && value.endMonth === '') {
      context.addIssue({ code: 'custom', path: ['endMonth'], message: 'Indica la fecha de fin o marca que es tu empleo actual.' })
    }
    if (!value.isCurrent && value.endMonth !== '' && value.endMonth < value.startMonth) {
      context.addIssue({ code: 'custom', path: ['endMonth'], message: 'La fecha de fin debe ser posterior a la de inicio.' })
    }
  })

const educationSchema = z
  .object({
    institution: required('Ingresa la institución.', 200),
    degree: required('Ingresa el título o programa.', 200),
    fieldOfStudy: optional(200),
    startMonth: optionalMonth,
    endMonth: optionalMonth,
    status: z.enum(educationStatuses),
  })
  .superRefine((value, context) => {
    if (value.startMonth !== '' && value.endMonth !== '' && value.endMonth < value.startMonth) {
      context.addIssue({ code: 'custom', path: ['endMonth'], message: 'La fecha de fin debe ser posterior a la de inicio.' })
    }
  })

export const profileSchema = z.object({
  fullName: required('Ingresa tu nombre profesional.', 150),
  headline: optional(200),
  summary: optional(3000),
  links: z.object({ linkedInUrl: optionalUrl, gitHubUrl: optionalUrl, portfolioUrl: optionalUrl }),
  experiences: z.array(experienceSchema).max(30, 'Puedes registrar hasta 30 experiencias.'),
  education: z.array(educationSchema).max(20, 'Puedes registrar hasta 20 estudios.'),
  projects: z
    .array(
      z.object({
        name: required('Ingresa el nombre del proyecto.', 150),
        description: optional(3000),
        technologiesText: z.string(),
        url: optionalUrl,
      }),
    )
    .max(30, 'Puedes registrar hasta 30 proyectos.'),
  certifications: z
    .array(
      z.object({
        name: required('Ingresa el nombre de la certificación.', 200),
        issuer: required('Ingresa la entidad emisora.', 200),
        issuedMonth: optionalMonth,
        credentialUrl: optionalUrl,
      }),
    )
    .max(30, 'Puedes registrar hasta 30 certificaciones.'),
  technicalSkillsText: z.string(),
  softSkillsText: z.string(),
  languages: z
    .array(
      z.object({
        language: required('Ingresa el idioma.', 100),
        // '' = not chosen yet (e.g. a CV that does not state the level); it must be chosen before saving.
        proficiency: z.union([z.enum(languageProficiencies), z.literal('')]).refine((value) => value.length > 0, 'Elige el nivel.'),
      }),
    )
    .max(20, 'Puedes registrar hasta 20 idiomas.'),
})

export type ProfileFormValues = z.infer<typeof profileSchema>
export type ExperienceFormValues = ProfileFormValues['experiences'][number]
export type EducationFormValues = ProfileFormValues['education'][number]
export type ProjectFormValues = ProfileFormValues['projects'][number]
export type CertificationFormValues = ProfileFormValues['certifications'][number]
export type LanguageFormValues = ProfileFormValues['languages'][number]

export const emptyExperience: ExperienceFormValues = {
  company: '',
  role: '',
  location: '',
  startMonth: '',
  endMonth: '',
  isCurrent: false,
  description: '',
  achievementsText: '',
  technologiesText: '',
}
export const emptyEducation: EducationFormValues = {
  institution: '',
  degree: '',
  fieldOfStudy: '',
  startMonth: '',
  endMonth: '',
  status: 'completed',
}
export const emptyProject: ProjectFormValues = { name: '', description: '', technologiesText: '', url: '' }
export const emptyCertification: CertificationFormValues = { name: '', issuer: '', issuedMonth: '', credentialUrl: '' }
export const emptyLanguage: LanguageFormValues = { language: '', proficiency: 'intermediate' }

export const emptyProfile: ProfileFormValues = {
  fullName: '',
  headline: '',
  summary: '',
  links: { linkedInUrl: '', gitHubUrl: '', portfolioUrl: '' },
  experiences: [],
  education: [],
  projects: [],
  certifications: [],
  technicalSkillsText: '',
  softSkillsText: '',
  languages: [],
}

/** Splits comma- or line-separated text into trimmed, de-duplicated (case-insensitive) items. */
export function splitList(text: string, separator: RegExp = /[,\n]/): string[] {
  const seen = new Set<string>()
  const items: string[] = []
  for (const raw of text.split(separator)) {
    const item = raw.trim()
    const key = item.toLocaleLowerCase('es')
    if (item !== '' && !seen.has(key)) {
      seen.add(key)
      items.push(item)
    }
  }
  return items
}

const nullIfEmpty = (value: string): string | null => (value.trim() === '' ? null : value.trim())
const monthToDate = (value: string): string | null => (value === '' ? null : `${value}-01`)
const dateToMonth = (value: string | null): string => (value === null ? '' : value.slice(0, 7))

export function toProfileRequest(values: ProfileFormValues): ProfileRequest {
  return {
    fullName: values.fullName.trim(),
    headline: nullIfEmpty(values.headline),
    summary: nullIfEmpty(values.summary),
    links: {
      linkedInUrl: nullIfEmpty(values.links.linkedInUrl),
      gitHubUrl: nullIfEmpty(values.links.gitHubUrl),
      portfolioUrl: nullIfEmpty(values.links.portfolioUrl),
    },
    experiences: values.experiences.map((item) => ({
      company: item.company.trim(),
      role: item.role.trim(),
      location: nullIfEmpty(item.location),
      startDate: `${item.startMonth}-01`,
      endDate: item.isCurrent ? null : monthToDate(item.endMonth),
      isCurrent: item.isCurrent,
      description: nullIfEmpty(item.description),
      achievements: splitList(item.achievementsText, /\n/),
      technologies: splitList(item.technologiesText),
    })),
    education: values.education.map((item) => ({
      institution: item.institution.trim(),
      degree: item.degree.trim(),
      fieldOfStudy: nullIfEmpty(item.fieldOfStudy),
      startDate: monthToDate(item.startMonth),
      endDate: monthToDate(item.endMonth),
      status: item.status,
    })),
    projects: values.projects.map((item) => ({
      name: item.name.trim(),
      description: nullIfEmpty(item.description),
      technologies: splitList(item.technologiesText),
      url: nullIfEmpty(item.url),
    })),
    certifications: values.certifications.map((item) => ({
      name: item.name.trim(),
      issuer: item.issuer.trim(),
      issuedOn: monthToDate(item.issuedMonth),
      credentialUrl: nullIfEmpty(item.credentialUrl),
    })),
    technicalSkills: splitList(values.technicalSkillsText),
    softSkills: splitList(values.softSkillsText),
    languages: values.languages.map((item) => {
      if (item.proficiency === '') {
        throw new Error('Language level must be validated before building the request.')
      }
      return { language: item.language.trim(), proficiency: item.proficiency }
    }),
  }
}

export function toFormValues(profile: ProfileResponse): ProfileFormValues {
  return {
    fullName: profile.fullName,
    headline: profile.headline ?? '',
    summary: profile.summary ?? '',
    links: {
      linkedInUrl: profile.links.linkedInUrl ?? '',
      gitHubUrl: profile.links.gitHubUrl ?? '',
      portfolioUrl: profile.links.portfolioUrl ?? '',
    },
    experiences: profile.experiences.map((item) => ({
      company: item.company,
      role: item.role,
      location: item.location ?? '',
      startMonth: dateToMonth(item.startDate),
      endMonth: dateToMonth(item.endDate),
      isCurrent: item.isCurrent,
      description: item.description ?? '',
      achievementsText: item.achievements.join('\n'),
      technologiesText: item.technologies.join(', '),
    })),
    education: profile.education.map((item) => ({
      institution: item.institution,
      degree: item.degree,
      fieldOfStudy: item.fieldOfStudy ?? '',
      startMonth: dateToMonth(item.startDate),
      endMonth: dateToMonth(item.endDate),
      status: item.status,
    })),
    projects: profile.projects.map((item) => ({
      name: item.name,
      description: item.description ?? '',
      technologiesText: item.technologies.join(', '),
      url: item.url ?? '',
    })),
    certifications: profile.certifications.map((item) => ({
      name: item.name,
      issuer: item.issuer,
      issuedMonth: dateToMonth(item.issuedOn),
      credentialUrl: item.credentialUrl ?? '',
    })),
    technicalSkillsText: profile.technicalSkills.join(', '),
    softSkillsText: profile.softSkills.join(', '),
    languages: profile.languages.map((item) => ({ language: item.language, proficiency: item.proficiency })),
  }
}

const fieldRenames: Record<string, string> = {
  startDate: 'startMonth',
  endDate: 'endMonth',
  issuedOn: 'issuedMonth',
  technologies: 'technologiesText',
  achievements: 'achievementsText',
}

/** Maps API error paths ("experiences[0].endDate", "technicalSkills[2]") to form field paths. */
export function profileErrorPath(apiPath: string): string | null {
  const topLevelList = /^(technicalSkills|softSkills)(\[\d+\])?$/.exec(apiPath)
  if (topLevelList?.[1] !== undefined) {
    return `${topLevelList[1]}Text`
  }

  const segments = apiPath.replace(/\[(\d+)\]/g, '.$1').split('.')
  const last = segments.at(-1)
  if (last !== undefined && /^\d+$/.test(last) && segments.length >= 2) {
    // An item inside a nested list (e.g. experiences.0.technologies.3): point at the text field.
    segments.pop()
  }
  const field = segments.at(-1)
  if (field !== undefined && field in fieldRenames) {
    segments[segments.length - 1] = fieldRenames[field] ?? field
  }
  return segments.join('.')
}

/** Converts an imported CV draft into form values; missing data stays empty so validation asks the user for it. */
export function draftToFormValues(draft: ProfileDraft): ProfileFormValues {
  const base = toFormValues({
    ...draft,
    id: '',
    updatedAt: '',
    experiences: draft.experiences.map((item) => ({ ...item, startDate: item.startDate ?? '' })),
    languages: [],
  })
  return {
    ...base,
    languages: draft.languages.map((item) => ({ language: item.language, proficiency: item.proficiency ?? '' })),
  }
}
