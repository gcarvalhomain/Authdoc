import { useEffect, useState, type ReactNode } from 'react'
import { getRefreshToken, onSessionExpired, refreshSession, startSession } from '../api/session.ts'
import type { LoginResponse } from '../api/types.ts'
import { AuthContext, type AuthState } from './AuthContext.ts'

type AuthProviderProps = {
  children: ReactNode
}

// Keeps the session of the app and gives it to every component through AuthContext.
function AuthProvider({ children }: AuthProviderProps) {
  // With a saved refresh token the app starts as 'loading' and tries to restore
  // the session. Without one, there is nothing to restore.
  const [auth, setAuth] = useState<AuthState>(() =>
    getRefreshToken() ? { status: 'loading' } : { status: 'signedOut', sessionExpired: false },
  )

  // Runs once, when the app opens: exchanges the saved refresh token for new tokens.
  useEffect(() => {
    if (!getRefreshToken()) {
      return
    }

    // In development, StrictMode runs this effect twice on purpose. The first run
    // is cancelled (ignore = true), and refreshSession makes both runs share one call.
    let ignore = false
    refreshSession()
      .then((response) => {
        if (!ignore) {
          setAuth({ status: 'signedIn', user: response.user })
        }
      })
      .catch(() => {
        // If the API rejected the token, the listener below has already set
        // { sessionExpired: true }. "current" keeps that state and does not overwrite it.
        if (!ignore) {
          setAuth((current) =>
            current.status === 'loading' ? { status: 'signedOut', sessionExpired: false } : current,
          )
        }
      })

    return () => {
      ignore = true
    }
  }, [])

  // When the API rejects the refresh token, the user is signed out. On a page
  // inside RequireAuth, this sends the user to the sign-in page at once.
  useEffect(
    () => onSessionExpired(() => setAuth({ status: 'signedOut', sessionExpired: true })),
    [],
  )

  function signIn(login: LoginResponse) {
    startSession(login)
    setAuth({ status: 'signedIn', user: login.user })
  }

  // React 19: a Context works as its own provider component.
  return <AuthContext value={{ auth, signIn }}>{children}</AuthContext>
}

export default AuthProvider
