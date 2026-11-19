import { Link } from 'react-router'
import PageLayout from '../shared/PageLayout.tsx'
import '../shared/Button.css'
import './HomePage.css'

function HomePage() {
  return (
    <PageLayout>
      <h1>Welcome</h1>
      <p>A personal document vault.</p>
      <div className="home-actions">
        <Link className="button button-primary" to="/login">
          Sign in
        </Link>
      </div>
    </PageLayout>
  )
}

export default HomePage
