import { useAiStatus } from '../shared/api/aiStatus'
import { useAuth } from '../features/auth/AuthContext'

export type AiBlockReason = 'email_not_verified' | 'not_configured' | 'daily_limit'

/** Why AI features (analysis, CV import) cannot run for this user right now, or null when they can. */
export function useAiBlockReason(): AiBlockReason | null {
  const { user } = useAuth()
  const status = useAiStatus()
  if (user?.emailVerificationPending === true) return 'email_not_verified'
  if (status?.configured === false) return 'not_configured'
  if (status?.dailyLimitReached === true) return 'daily_limit'
  return null
}
