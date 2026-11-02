// Mirrors the backend contracts (backend/src/PostulaAI.Api/Modules/*/Contracts). Keep in sync.

export interface UserResponse {
  id: string
  email: string
  displayName: string
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  user: UserResponse
}

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export const educationStatuses = ['completed', 'in_progress', 'incomplete'] as const
export type EducationStatus = (typeof educationStatuses)[number]

export const languageProficiencies = ['basic', 'intermediate', 'advanced', 'native'] as const
export type LanguageProficiency = (typeof languageProficiencies)[number]

/** Dates are ISO `YYYY-MM-DD` strings (DateOnly on the backend). */
export interface WorkExperienceDto {
  company: string
  role: string
  location: string | null
  startDate: string
  endDate: string | null
  isCurrent: boolean
  description: string | null
  achievements: string[]
  technologies: string[]
}

export interface EducationDto {
  institution: string
  degree: string
  fieldOfStudy: string | null
  startDate: string | null
  endDate: string | null
  status: EducationStatus
}

export interface ProjectDto {
  name: string
  description: string | null
  technologies: string[]
  url: string | null
}

export interface CertificationDto {
  name: string
  issuer: string
  issuedOn: string | null
  credentialUrl: string | null
}

export interface LanguageDto {
  language: string
  proficiency: LanguageProficiency
}

export interface ProfileLinksDto {
  linkedInUrl: string | null
  gitHubUrl: string | null
  portfolioUrl: string | null
}

export interface ProfileRequest {
  fullName: string
  headline: string | null
  summary: string | null
  links: ProfileLinksDto
  experiences: WorkExperienceDto[]
  education: EducationDto[]
  projects: ProjectDto[]
  certifications: CertificationDto[]
  technicalSkills: string[]
  softSkills: string[]
  languages: LanguageDto[]
}

export interface ProfileResponse extends ProfileRequest {
  id: string
  updatedAt: string
}

export const workModes = ['unspecified', 'onsite', 'hybrid', 'remote'] as const
export type WorkMode = (typeof workModes)[number]

export interface JobPostingRequest {
  title: string
  company: string | null
  location: string | null
  workMode: WorkMode
  description: string
  sourceUrl: string | null
}

export interface JobPostingResponse extends JobPostingRequest {
  id: string
  createdAt: string
  updatedAt: string
}

export interface JobPostingSummary {
  id: string
  title: string
  company: string | null
  location: string | null
  workMode: WorkMode
  createdAt: string
  updatedAt: string
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}
