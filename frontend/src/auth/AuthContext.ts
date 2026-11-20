import { createContext, useContext } from 'react'
import type { LoginResponse, UserResponse } from '../api/types.ts'

// The three states of the session. Only 'signedIn' has a user, so TypeScript
// does not let a page read the user before it checks the status:
//   if (auth.status === 'signedIn') { auth.user.name }
export type AuthState =
  | { status: 'loading' } // the app is checking the saved session after a reload
  // sessionExpired: the API rejected the refresh token, so the sign-in page
  // can tell the user why they are signed out.
  | { status: 'signedOut'; sessionExpired: boolean }
  | { status: 'signedIn'; user: UserResponse }

export type AuthContextValue = {
  auth: AuthState
  signIn: (login: LoginResponse) => void
  signOut: () => void
}

// A Context shares one value with every component below its provider, without
// passing it as a prop through each level. It is a bit like a Scoped service
// in ASP.NET Core: any component can ask for it.
export const AuthContext = createContext<AuthContextValue | null>(null)

// Reads the session. Every component that calls it draws again when the session changes.
export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)
  if (value === null) {
    throw new Error('useAuth must be used inside <AuthProvider>')
  }
  return value
}
