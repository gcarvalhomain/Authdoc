import { Link } from 'react-router'
import { useAuth } from '../auth/AuthContext.ts'
import PageLayout from '../shared/PageLayout.tsx'
import '../shared/Button.css'
import './HomePage.css'

function HomePage() {
  const { auth } = useAuth()

  if (auth.status === 'signedIn') {
    return (
      <PageLayout>
        <h1>Welcome back, {auth.user.name}</h1>
        <p>A personal document vault.</p>
        <div className="home-actions">
          <Link className="button button-primary" to="/documents">
            My documents
          </Link>
          <Link className="button button-secondary" to="/account">
            My account
          </Link>
        </div>
      </PageLayout>
    )
  }

  return (
    <PageLayout>
      <h1>Welcome</h1>
      <p>A personal document vault.</p>
      <div className="home-actions">
        <Link className="button button-primary" to="/login">
          Sign in
        </Link>
        <Link className="button button-secondary" to="/register">
          Create account
        </Link>
      </div>
    </PageLayout>
  )
}

export default HomePage
