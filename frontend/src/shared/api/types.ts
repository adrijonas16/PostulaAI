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

export const applicationStatuses = ['saved', 'applied', 'interview', 'offer', 'rejected'] as const
export type ApplicationStatus = (typeof applicationStatuses)[number]

export interface ApplicationRequest {
  status: ApplicationStatus
  /** yyyy-MM-dd; required for every status except "saved". */
  appliedOn: string | null
  notes: string | null
  submittedSummary: string | null
  submittedCoverLetter: string | null
}

export interface ApplicationResponse extends ApplicationRequest {
  updatedAt: string | null
}

export interface JobPostingResponse extends JobPostingRequest {
  id: string
  application: ApplicationResponse
  createdAt: string
  updatedAt: string
}

export interface JobPostingSummary {
  id: string
  title: string
  company: string | null
  location: string | null
  workMode: WorkMode
  sourceUrl: string | null
  applicationStatus: ApplicationStatus
  appliedOn: string | null
  createdAt: string
  updatedAt: string
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type AnalysisStatus = 'pending' | 'completed' | 'failed'
export type RequirementImportance = 'required' | 'preferred'
export type GapType = 'missing_skill' | 'missing_experience' | 'unclear_evidence' | 'other'
export type ScoreBand = 'high' | 'medium' | 'low'

export interface AnalysisStrength {
  requirement: string
  evidence: string
  importance: RequirementImportance
}

export interface AnalysisGap {
  requirement: string
  gapType: GapType
  importance: RequirementImportance
  recommendation: string
}

export interface JobAnalysisResult {
  compatibilityScore: number
  summary: string
  strengths: AnalysisStrength[]
  gaps: AnalysisGap[]
  keywordsToInclude: string[]
  cvRecommendations: string[]
  adaptedProfessionalSummary: string
  coverLetterDraft: string
  disclaimer: string
}

export interface ScoreContext {
  band: ScoreBand
  requiredMet: number
  requiredMissing: number
  preferredMet: number
  preferredMissing: number
}

export interface AnalysisResponse {
  id: string
  status: AnalysisStatus
  jobPostingId: string | null
  jobTitle: string
  jobCompany: string | null
  language: string
  result: JobAnalysisResult | null
  score: ScoreContext | null
  errorCode: string | null
  promptId: string
  promptVersion: string
  model: string
  repairAttempted: boolean
  createdAt: string
  completedAt: string | null
}

export interface AnalysisSummary {
  id: string
  status: AnalysisStatus
  jobPostingId: string | null
  jobTitle: string
  jobCompany: string | null
  compatibilityScore: number | null
  scoreBand: ScoreBand | null
  errorCode: string | null
  createdAt: string
}

export interface UsageResponse {
  limit: number
  used: number
  remaining: number
  windowHours: number
  nextSlotAt: string | null
}

/** Profile pre-filled from a CV. Dates and language levels may be missing; the user completes them before saving. */
export interface ProfileDraft {
  fullName: string
  headline: string | null
  summary: string | null
  links: ProfileLinksDto
  experiences: (Omit<WorkExperienceDto, 'startDate'> & { startDate: string | null })[]
  education: EducationDto[]
  projects: ProjectDto[]
  certifications: CertificationDto[]
  technicalSkills: string[]
  softSkills: string[]
  languages: { language: string; proficiency: LanguageProficiency | null }[]
}

export interface CvImportResponse {
  draft: ProfileDraft
  warnings: string[]
}
