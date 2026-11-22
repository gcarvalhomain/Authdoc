import { startTransition, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { useAuth } from '../auth/AuthContext.ts'
import Button from './Button.tsx'
import './PageLayout.css'

type PageLayoutProps = {
  children: ReactNode
}

// The frame of every page: the header with the logo on top,
// and the content of the page below it.
function PageLayout({ children }: PageLayoutProps) {
  const { auth, signOut } = useAuth()
  const navigate = useNavigate()

  function handleSignOut() {
    // React Router changes the address inside startTransition (an update with a
    // lower priority). If signOut ran outside it, React would first draw the old
    // page without a session, and RequireAuth would open the sign-in page.
    // In the same transition, React applies both changes in one render.
    startTransition(() => {
      signOut()
      navigate('/', { replace: true })
    })
  }

  return (
    <div className="page">
      <header className="page-header">
        <div className="page-header-content">
          <Link className="logo" to="/">
            Authdoc
          </Link>

          {auth.status === 'signedIn' && (
            <div className="page-header-user">
              <Link className="page-header-link" to="/documents">
                Documents
              </Link>
              <Link className="page-header-name" to="/account">
                {auth.user.name}
              </Link>
              <Button onClick={handleSignOut}>Sign out</Button>
            </div>
          )}
        </div>
      </header>

      <main className="page-content">{children}</main>
    </div>
  )
}

export default PageLayout
