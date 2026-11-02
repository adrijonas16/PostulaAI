/** Error raised for any non-2xx API response, built from RFC 9457 ProblemDetails. */
export class ApiError extends Error {
  readonly status: number
  readonly errorCode: string
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>

  constructor(status: number, errorCode: string, title: string, fieldErrors: Record<string, string[]> = {}) {
    super(title)
    this.name = 'ApiError'
    this.status = status
    this.errorCode = errorCode
    this.fieldErrors = fieldErrors
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    const fallbackTitle = 'No pudimos completar la solicitud. Intenta de nuevo.'
    let body: unknown = null
    try {
      body = await response.json()
    } catch {
      // Non-JSON error bodies (proxies, network devices) fall back to a generic message.
    }

    if (!isRecord(body)) {
      return new ApiError(response.status, 'request_failed', fallbackTitle)
    }

    const title = typeof body.title === 'string' ? body.title : fallbackTitle
    const errorCode = typeof body.errorCode === 'string' ? body.errorCode : 'request_failed'
    return new ApiError(response.status, errorCode, title, parseFieldErrors(body.errors))
  }
}

export function networkError(): ApiError {
  return new ApiError(0, 'network_error', 'No hay conexión con el servidor. Revisa tu conexión e intenta de nuevo.')
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function parseFieldErrors(value: unknown): Record<string, string[]> {
  if (!isRecord(value)) {
    return {}
  }

  const result: Record<string, string[]> = {}
  for (const [key, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      result[key] = messages.filter((message): message is string => typeof message === 'string')
    }
  }
  return result
}
