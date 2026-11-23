import { Route, Routes } from 'react-router'
import RequireAuth from './auth/RequireAuth.tsx'
import AccountPage from './pages/AccountPage.tsx'
import ChangePasswordPage from './pages/ChangePasswordPage.tsx'
import DocumentsPage from './pages/DocumentsPage.tsx'
import EditDocumentPage from './pages/EditDocumentPage.tsx'
import HomePage from './pages/HomePage.tsx'
import LoginPage from './pages/LoginPage.tsx'
import NewDocumentPage from './pages/NewDocumentPage.tsx'
import NotFoundPage from './pages/NotFoundPage.tsx'
import RegisterPage from './pages/RegisterPage.tsx'

// The map of the app: each address shows one page.
// It works like the MapGet calls in the API, but in the browser.
function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      {/* The routes inside RequireAuth need a signed-in user. */}
      <Route element={<RequireAuth />}>
        <Route path="/account" element={<AccountPage />} />
        <Route path="/account/password" element={<ChangePasswordPage />} />
        <Route path="/documents" element={<DocumentsPage />} />
        <Route path="/documents/new" element={<NewDocumentPage />} />
        {/* ":id" is a part of the address that the page reads with useParams. */}
        <Route path="/documents/:id/edit" element={<EditDocumentPage />} />
      </Route>
      {/* "*" matches any address that no route above matched. */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
