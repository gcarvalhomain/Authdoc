import type { ReactNode } from 'react'
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
          <span className="logo">Authdoc</span>
        </div>
      </header>

      <main className="page-content">{children}</main>
    </div>
  )
}

export default PageLayout
