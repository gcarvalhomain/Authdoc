import type { ErrorResponse } from './types.ts'

// The error that apiRequest throws when a call fails.
// A page catches it and reads `code` to decide what to show.
export class ApiError extends Error {
  status: number
  code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  token?: string
}

// Calls the API and returns the JSON of the answer.
// T is the type of that answer, for example:
//   const login = await apiRequest<LoginResponse>('/auth/login', { method: 'POST', body })
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, token } = options

  const headers: Record<string, string> = {}
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    // fetch fails like this only when no answer arrives at all (no network).
    throw new ApiError(0, 'NETWORK_ERROR', 'Could not reach the server')
  }

  if (!response.ok) {
    throw await readError(response)
  }

  // 204 No Content: the call worked and there is no body to read.
  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

// Turns an error answer into an ApiError.
async function readError(response: Response): Promise<ApiError> {
  try {
    const error = (await response.json()) as Partial<ErrorResponse>
    if (typeof error.code === 'string' && typeof error.message === 'string') {
      return new ApiError(response.status, error.code, error.message)
    }
  } catch {
    // The body is not JSON. The rules below decide the code.
  }

  // The API always answers an error as { code, message }, so this answer did not
  // come from the API. 502, 503 and 504 mean that something in the middle (in
  // development, the Vite proxy) could not reach the API, for example because
  // the API is not running.
  if (response.status >= 502 && response.status <= 504) {
    return new ApiError(response.status, 'NETWORK_ERROR', 'Could not reach the server')
  }

  return new ApiError(response.status, 'UNKNOWN_ERROR', 'An unexpected error occurred')
}
