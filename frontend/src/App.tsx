import { Route, Routes } from 'react-router'
import HomePage from './pages/HomePage.tsx'
import LoginPage from './pages/LoginPage.tsx'
import NotFoundPage from './pages/NotFoundPage.tsx'

// The map of the app: each address shows one page.
// It works like the MapGet calls in the API, but in the browser.
function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />
      {/* "*" matches any address that no route above matched. */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
