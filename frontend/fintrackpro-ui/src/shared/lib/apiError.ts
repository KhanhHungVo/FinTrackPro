import { isAxiosError } from 'axios'

export interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]> // FluentValidation field errors map
}

export type ApiErrorKind =
  | { type: 'validation'; details: ProblemDetails } // 400 w/ Problem Details
  | { type: 'forbidden' } // 403
  | { type: 'not_found' } // 404
  | { type: 'conflict'; message: string } // 409
  | { type: 'server_error' } // 5xx
  | { type: 'network' } // no response
  | { type: 'unknown'; message: string }

export function classifyApiError(error: unknown): ApiErrorKind {
  if (!isAxiosError(error)) return { type: 'unknown', message: String(error) }
  if (!error.response) return { type: 'network' }

  const { status, data } = error.response
  if (status === 400) return { type: 'validation', details: data as ProblemDetails }
  if (status === 403) return { type: 'forbidden' }
  if (status === 404) return { type: 'not_found' }
  if (status === 409)
    return { type: 'conflict', message: (data as ProblemDetails)?.detail ?? 'Conflict.' }
  if (status >= 500) return { type: 'server_error' }
  return { type: 'unknown', message: (error as Error).message }
}

/** One-line human-readable message suitable for toast notifications */
export function errorToastMessage(error: unknown): string {
  const kind = classifyApiError(error)
  switch (kind.type) {
    case 'validation':
      return kind.details.title ?? 'Please fix the highlighted fields.'
    case 'forbidden':
      return "You don't have permission to do that."
    case 'not_found':
      return 'The resource was not found.'
    case 'conflict':
      return kind.message
    case 'server_error':
      return 'A server error occurred. Please try again later.'
    case 'network':
      return 'Network error. Check your connection.'
    default:
      return 'Something went wrong.'
  }
}
