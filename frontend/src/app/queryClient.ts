import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '../shared/api/ApiError'

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Client errors (4xx) are deterministic; retrying only wastes requests.
        retry: (failureCount, error) => !(error instanceof ApiError && error.status >= 400 && error.status < 500) && failureCount < 2,
        refetchOnWindowFocus: false,
      },
    },
  })
}
