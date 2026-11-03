import { useQuery } from '@tanstack/react-query'
import { apiRequest } from './client'

export interface AiStatusResponse {
  configured: boolean
}

export const aiStatusQueryKey = ['ai-status'] as const

export function getAiStatus(): Promise<AiStatusResponse> {
  return apiRequest<AiStatusResponse>('/ai/status')
}

/** `false` only when the server says the AI provider has no key; `undefined` while loading or on error. */
export function useAiConfigured(): boolean | undefined {
  const query = useQuery({ queryKey: aiStatusQueryKey, queryFn: getAiStatus, staleTime: 60_000 })
  return query.data?.configured
}
