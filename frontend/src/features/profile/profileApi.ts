import { ApiError } from '../../shared/api/ApiError'
import { apiRequest } from '../../shared/api/client'
import type { CvImportResponse, ProfileRequest, ProfileResponse } from '../../shared/api/types'

export const profileQueryKey = ['profile', 'me'] as const

/** Returns null when the user has not created a profile yet. */
export async function getProfile(): Promise<ProfileResponse | null> {
  try {
    return await apiRequest<ProfileResponse>('/profiles/me')
  } catch (error) {
    if (error instanceof ApiError && error.errorCode === 'profile_not_found') {
      return null
    }
    throw error
  }
}

export function saveProfile(request: ProfileRequest): Promise<ProfileResponse> {
  return apiRequest<ProfileResponse>('/profiles/me', { method: 'PUT', body: request })
}

export const CV_MAX_BYTES = 5 * 1024 * 1024

/** Uploads a CV (PDF or DOCX) and returns an unsaved draft. Nothing is stored until the user saves the profile. */
export function importCv(file: File): Promise<CvImportResponse> {
  const form = new FormData()
  form.append('file', file)
  return apiRequest<CvImportResponse>('/profiles/me/import', { method: 'POST', body: form })
}
