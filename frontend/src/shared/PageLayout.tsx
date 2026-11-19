import type { ReactNode } from 'react'
import { Link } from 'react-router'
import './PageLayout.css'

type PageLayoutProps = {
  children: ReactNode
}

// The frame of every page: the header with the logo on top,
// and the content of the page below it.
function PageLayout({ children }: PageLayoutProps) {
  return (
    <div className="page">
      <header className="page-header">
        <div className="page-header-content">
          <Link className="logo" to="/">
            Authdoc
          </Link>
        </div>
      </header>

      <main className="page-content">{children}</main>
    </div>
  )
}

export default PageLayout
