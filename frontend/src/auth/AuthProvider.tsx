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
    getRefreshToken() ? { status: 'loading' } : { status: 'signedOut' },
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
        if (!ignore) {
          setAuth({ status: 'signedOut' })
        }
      })

    return () => {
      ignore = true
    }
  }, [])

  // When the API rejects the refresh token, the user is signed out.
  useEffect(() => onSessionExpired(() => setAuth({ status: 'signedOut' })), [])

  function signIn(login: LoginResponse) {
    startSession(login)
    setAuth({ status: 'signedIn', user: login.user })
  }

  // React 19: a Context works as its own provider component.
  return <AuthContext value={{ auth, signIn }}>{children}</AuthContext>
}

export default AuthProvider
