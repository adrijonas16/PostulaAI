import { useQuery } from '@tanstack/react-query'
import { apiRequest } from './client'

export interface AiStatusResponse {
  configured: boolean
  dailyLimitReached: boolean
}

export const aiStatusQueryKey = ['ai-status'] as const

export function getAiStatus(): Promise<AiStatusResponse> {
  return apiRequest<AiStatusResponse>('/ai/status')
}

/** Server-side reasons AI features cannot run right now; `undefined` while loading or on error. */
export function useAiStatus(): AiStatusResponse | undefined {
  const query = useQuery({ queryKey: aiStatusQueryKey, queryFn: getAiStatus, staleTime: 60_000 })
  return query.data
}
