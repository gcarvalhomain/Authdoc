import { Link } from 'react-router'
import PageLayout from '../shared/PageLayout.tsx'

function NotFoundPage() {
  return (
    <PageLayout>
      <h1>Page not found</h1>
      <p>The page you are looking for does not exist.</p>
      <Link to="/">Go to the home page</Link>
    </PageLayout>
  )
}

export default NotFoundPage
