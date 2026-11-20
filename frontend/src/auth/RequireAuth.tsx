import { Navigate, Outlet, useLocation } from 'react-router'
import PageLayout from '../shared/PageLayout.tsx'
import { useAuth } from './AuthContext.ts'

// A route that only a signed-in user can open. It works like RequireAuthorization()
// in the API: the routes inside it are drawn in <Outlet /> only when there is a session.
//
//   <Route element={<RequireAuth />}>
//     <Route path="/account" element={<AccountPage />} />
//   </Route>
function RequireAuth() {
  const { auth } = useAuth()
  const location = useLocation()

  // After a reload the app is still checking the saved session.
  // Sending the user to the sign-in page now would be wrong.
  if (auth.status === 'loading') {
    return (
      <PageLayout>
        <p>Loading...</p>
      </PageLayout>
    )
  }

  // "from" lets the sign-in page bring the user back to this page.
  if (auth.status === 'signedOut') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}

export default RequireAuth
