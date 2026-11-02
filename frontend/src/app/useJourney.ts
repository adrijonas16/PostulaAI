import { useQuery } from '@tanstack/react-query'
import { analysisKeys, listAnalyses } from '../features/analyses/analysesApi'
import { jobPostingKeys, listJobPostings } from '../features/jobs/jobsApi'
import { getProfile, profileQueryKey } from '../features/profile/profileApi'

export type JourneyStep = 'profile' | 'postings' | 'analysis'

export interface Journey {
  loading: boolean
  done: Record<JourneyStep, boolean>
  /** First step that is not done yet; null when everything is done at least once. */
  next: JourneyStep | null
  postingsCount: number
  analysesCount: number
}

/** Where the user is in the profile → posting → analysis journey. Shares caches with the pages. */
export function useJourney(): Journey {
  const profile = useQuery({ queryKey: profileQueryKey, queryFn: getProfile })
  const postings = useQuery({ queryKey: [...jobPostingKeys.list(1, ''), 'count'], queryFn: () => listJobPostings(1, '', 1) })
  const analyses = useQuery({ queryKey: [...analysisKeys.list(1), 'count'], queryFn: () => listAnalyses(1, 1) })

  const done = {
    profile: profile.data != null,
    postings: (postings.data?.totalCount ?? 0) > 0,
    analysis: (analyses.data?.totalCount ?? 0) > 0,
  }
  const order: JourneyStep[] = ['profile', 'postings', 'analysis']

  return {
    loading: profile.isPending || postings.isPending || analyses.isPending,
    done,
    next: order.find((step) => !done[step]) ?? null,
    postingsCount: postings.data?.totalCount ?? 0,
    analysesCount: analyses.data?.totalCount ?? 0,
  }
}
