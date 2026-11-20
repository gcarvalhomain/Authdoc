import { useEffect, useState } from 'react'
import { getMe } from '../api/auth.ts'
import { getErrorMessage } from '../api/errorMessages.ts'
import type { UserResponse } from '../api/types.ts'
import Alert from '../shared/Alert.tsx'
import PageLayout from '../shared/PageLayout.tsx'
import './AccountPage.css'

function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString('en-US', { dateStyle: 'long' })
}

function AccountPage() {
  const [user, setUser] = useState<UserResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  // useEffect runs after the page is drawn. With [] it runs once, when the page opens.
  // The page asks the API for the user (and does not use the one in the session),
  // because an admin can change the name or the email after the sign-in.
  useEffect(() => {
    // If the user leaves the page before the answer arrives, ignore it.
    let ignore = false
    getMe()
      .then((me) => {
        if (!ignore) {
          setUser(me)
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setError(getErrorMessage(caught))
        }
      })

    return () => {
      ignore = true
    }
  }, [])

  return (
    <PageLayout>
      <h1>My account</h1>

      {error && <Alert variant="error">{error}</Alert>}
      {!error && !user && <p>Loading...</p>}

      {user && (
        <dl className="account-details">
          <dt>Name</dt>
          <dd>{user.name}</dd>
          <dt>Email</dt>
          <dd>{user.email}</dd>
          <dt>Role</dt>
          <dd>{user.role}</dd>
          <dt>Member since</dt>
          <dd>{formatDate(user.createdAt)}</dd>
        </dl>
      )}
    </PageLayout>
  )
}

export default AccountPage
