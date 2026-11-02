import { apiRequest } from '../../shared/api/client'
import type { JobPostingRequest, JobPostingResponse, JobPostingSummary, PagedResponse } from '../../shared/api/types'

export const jobPostingKeys = {
  all: ['job-postings'] as const,
  list: (page: number, search: string) => ['job-postings', 'list', page, search] as const,
  detail: (id: string) => ['job-postings', 'detail', id] as const,
}

export const JOB_POSTINGS_PAGE_SIZE = 10

export function listJobPostings(page: number, search: string, pageSize = JOB_POSTINGS_PAGE_SIZE): Promise<PagedResponse<JobPostingSummary>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (search.trim() !== '') {
    params.set('search', search.trim())
  }
  return apiRequest<PagedResponse<JobPostingSummary>>(`/job-postings?${params.toString()}`)
}

export function getJobPosting(id: string): Promise<JobPostingResponse> {
  return apiRequest<JobPostingResponse>(`/job-postings/${encodeURIComponent(id)}`)
}

export function createJobPosting(request: JobPostingRequest): Promise<JobPostingResponse> {
  return apiRequest<JobPostingResponse>('/job-postings', { method: 'POST', body: request })
}

export function updateJobPosting(id: string, request: JobPostingRequest): Promise<JobPostingResponse> {
  return apiRequest<JobPostingResponse>(`/job-postings/${encodeURIComponent(id)}`, { method: 'PUT', body: request })
}

export function deleteJobPosting(id: string): Promise<void> {
  return apiRequest<void>(`/job-postings/${encodeURIComponent(id)}`, { method: 'DELETE' })
}
