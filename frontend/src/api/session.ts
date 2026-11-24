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
// in the API. See "Session" in docs/frontend.md.

const refreshTokenKey = 'authdoc.refreshToken'

let accessToken: string | null = null
let refreshing: Promise<LoginResponse> | null = null
// Changes on every clearSession. A refresh that started before a sign-out sees
// another number when its answer arrives, so it does not save the tokens again.
let sessionNumber = 0
const expiredListeners = new Set<() => void>()

// Saves the tokens of a sign-in or a refresh.
export function startSession(login: LoginResponse): void {
  accessToken = login.token
  writeRefreshToken(login.refreshToken)
}

// Forgets the tokens on this device.
export function clearSession(): void {
  sessionNumber++
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
//
// When the API answers 401 UNAUTHORIZED (the access token expired, or the role
// of the user changed), it refreshes the tokens and sends the call once more.
// Sending a POST again is safe here: the API checks the token before it runs
// the endpoint, so the first call did not change anything.
export async function authRequest<T>(
  path: string,
  options: Omit<RequestOptions, 'token'> = {},
): Promise<T> {
  // After a reload, the access token exists only after the first refresh.
  const sentToken = accessToken ?? (await refreshSession()).token

  try {
    return await apiRequest<T>(path, { ...options, token: sentToken })
  } catch (error) {
    if (!(error instanceof ApiError) || error.code !== 'UNAUTHORIZED') {
      throw error
    }

    // While this call was on its way, another call may have refreshed the tokens
    // already. Then the new access token is enough, and no new refresh is needed.
    const newToken =
      accessToken !== null && accessToken !== sentToken
        ? accessToken
        : (await refreshSession()).token

    // Only one more try: if the new token fails too, the error goes to the page.
    return apiRequest<T>(path, { ...options, token: newToken })
  }
}

// Calls the listener when the API rejects the refresh token, so the app knows
// that the session is over. Returns a function that removes the listener.
export function onSessionExpired(listener: () => void): () => void {
  expiredListeners.add(listener)
  return () => {
    expiredListeners.delete(listener)
  }
}

// The name of the lock that every tab of the app shares.
const refreshLockName = 'authdoc.refresh'

// Only one tab refreshes at a time. All the tabs share the refresh token in
// localStorage, and the API accepts each refresh token only once: if two tabs
// sent the same token, the API would see a used token come back.
//
// navigator.locks (the Web Locks API) gives the lock to one tab at a time, and
// the other tabs wait. The token is read inside the lock, so a tab that waited
// reads the new token that the first tab saved, not the used one.
async function runRefresh(): Promise<LoginResponse> {
  // Old browsers, and pages that are not https (except localhost), have no Web Locks.
  if (!('locks' in navigator)) {
    return refreshWithSavedToken()
  }

  return navigator.locks.request(refreshLockName, () => refreshWithSavedToken())
}

async function refreshWithSavedToken(): Promise<LoginResponse> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) {
    throw new ApiError(401, 'UNAUTHORIZED', 'There is no session to refresh')
  }

  const startedIn = sessionNumber
  let response: LoginResponse
  try {
    response = await apiRequest<LoginResponse>('/auth/refresh', {
      method: 'POST',
      body: { refreshToken },
    })
  } catch (error) {
    // 401: the refresh token is expired, revoked or already used, so the session is over.
    // Other errors (no network, 429) do not end the session: the next try can work.
    // After a sign-out there is no session to end, so nothing happens.
    if (error instanceof ApiError && error.status === 401 && startedIn === sessionNumber) {
      clearSession()
      expiredListeners.forEach((listener) => listener())
    }
    throw error
  }

  // The user signed out while this refresh was on its way: the new tokens are not saved.
  if (startedIn !== sessionNumber) {
    throw new ApiError(401, 'UNAUTHORIZED', 'The session ended during the refresh')
  }

  startSession(response)
  return response
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
