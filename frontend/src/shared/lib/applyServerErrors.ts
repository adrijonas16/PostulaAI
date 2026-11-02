import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { ApiError } from '../api/ApiError'

/**
 * Maps backend field errors (camelCase paths such as "experiences[0].endDate") onto form fields.
 * `mapPath` translates API paths to form paths when they differ. Returns the message for a general alert.
 */
export function applyServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  mapPath: (apiPath: string) => string | null = (apiPath) => apiPath.replace(/\[(\d+)\]/g, '.$1'),
): string {
  if (!(error instanceof ApiError)) {
    return 'Ocurrió un error inesperado. Intenta de nuevo.'
  }

  for (const [apiPath, messages] of Object.entries(error.fieldErrors)) {
    const formPath = mapPath(apiPath)
    const message = messages[0]
    if (formPath !== null && message !== undefined) {
      setError(formPath as Path<T>, { type: 'server', message })
    }
  }
  return error.message
}
