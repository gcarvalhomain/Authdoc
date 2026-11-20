import { apiRequest, ApiError, type RequestOptions } from './client.ts'
import type { LoginResponse } from './types.ts'

// Where the tokens of the signed-in user live.
//
// - The access token (JWT) stays only in memory, in the variable below. It is
//   lost when the page reloads, and that is fine: it lives for one hour only.
// - The refresh token goes to localStorage, so a reload does not sign the user
//   out: the app exchanges it for a new pair of tokens (POST /api/auth/refresh).
//
// localStorage can be read by any script on the page, so an XSS bug could steal
// the refresh token. The safer option is an httpOnly cookie, which needs changes
// in the API. See "Session" in the README.

const refreshTokenKey = 'authdoc.refreshToken'

let accessToken: string | null = null
let refreshing: Promise<LoginResponse> | null = null
const expiredListeners = new Set<() => void>()

// Saves the tokens of a sign-in or a refresh.
export function startSession(login: LoginResponse): void {
  accessToken = login.token
  writeRefreshToken(login.refreshToken)
}

// Forgets the tokens on this device.
export function clearSession(): void {
  accessToken = null
  writeRefreshToken(null)
}

export function getAccessToken(): string | null {
  return accessToken
}

export function getRefreshToken(): string | null {
  try {
    return localStorage.getItem(refreshTokenKey)
  } catch {
    // The browser can block localStorage (for example, some private windows).
    return null
  }
}

// Exchanges the refresh token for a new pair of tokens.
//
// Only one refresh runs at a time. The API accepts each refresh token only once,
// so two refresh calls with the same token would end the session: the second
// call gets 401. When a refresh is already on its way, the caller gets the same
// promise and waits for that answer.
export function refreshSession(): Promise<LoginResponse> {
  refreshing ??= runRefresh().finally(() => {
    refreshing = null
  })
  return refreshing
}

// Calls a route that needs a signed-in user: like apiRequest, plus the
// Authorization header with the access token.
export function authRequest<T>(path: string, options: Omit<RequestOptions, 'token'> = {}): Promise<T> {
  return apiRequest<T>(path, { ...options, token: accessToken ?? undefined })
}

// Calls the listener when the API rejects the refresh token, so the app knows
// that the session is over. Returns a function that removes the listener.
export function onSessionExpired(listener: () => void): () => void {
  expiredListeners.add(listener)
  return () => {
    expiredListeners.delete(listener)
  }
}

async function runRefresh(): Promise<LoginResponse> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) {
    throw new ApiError(401, 'UNAUTHORIZED', 'There is no session to refresh')
  }

  try {
    const response = await apiRequest<LoginResponse>('/auth/refresh', {
      method: 'POST',
      body: { refreshToken },
    })
    startSession(response)
    return response
  } catch (error) {
    // 401: the refresh token is expired, revoked or already used, so the session is over.
    // Other errors (no network, 429) do not end the session: the next try can work.
    if (error instanceof ApiError && error.status === 401) {
      clearSession()
      expiredListeners.forEach((listener) => listener())
    }
    throw error
  }
}

function writeRefreshToken(refreshToken: string | null): void {
  try {
    if (refreshToken === null) {
      localStorage.removeItem(refreshTokenKey)
    } else {
      localStorage.setItem(refreshTokenKey, refreshToken)
    }
  } catch {
    // Without localStorage the session still works, but a reload signs the user out.
  }
}
