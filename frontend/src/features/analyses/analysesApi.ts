import { apiRequest } from '../../shared/api/client'
import type { AnalysisResponse, AnalysisSummary, PagedResponse, UsageResponse } from '../../shared/api/types'

export const analysisKeys = {
  all: ['analyses'] as const,
  list: (page: number) => ['analyses', 'list', page] as const,
  detail: (id: string) => ['analyses', 'detail', id] as const,
  usage: ['usage', 'me'] as const,
}

export const ANALYSES_PAGE_SIZE = 10

export function createAnalysis(jobPostingId: string, language = 'es'): Promise<AnalysisResponse> {
  return apiRequest<AnalysisResponse>('/analyses', { method: 'POST', body: { jobPostingId, language } })
}

export function listAnalyses(page: number, pageSize = ANALYSES_PAGE_SIZE): Promise<PagedResponse<AnalysisSummary>> {
  return apiRequest<PagedResponse<AnalysisSummary>>(`/analyses?page=${page}&pageSize=${pageSize}`)
}

export function getAnalysis(id: string): Promise<AnalysisResponse> {
  return apiRequest<AnalysisResponse>(`/analyses/${encodeURIComponent(id)}`)
}

export function deleteAnalysis(id: string): Promise<void> {
  return apiRequest<void>(`/analyses/${encodeURIComponent(id)}`, { method: 'DELETE' })
}

export function getUsage(): Promise<UsageResponse> {
  return apiRequest<UsageResponse>('/usage/me')
}
