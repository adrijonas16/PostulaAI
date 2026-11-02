import { Navigate, Outlet, useLocation } from 'react-router'
import { LoadingState } from '../../shared/ui/LoadingState'
import { useAuth } from './AuthContext'
import { postAuthTarget } from './postAuthTarget'

/** Protects /app routes: anonymous users are sent to login and brought back afterwards. */
export function RequireAuth() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return <LoadingState label="Cargando tu sesión…" />
  }
  if (status === 'anonymous') {
    return <Navigate to="/iniciar-sesion" replace state={{ from: location.pathname }} />
  }
  return <Outlet />
}

/**
 * Login and registration redirect once the user is authenticated. This guard is the single place that
 * navigates after login/registration, so there is no race with a second navigation from the form.
 */
export function RedirectIfAuthenticated() {
  const { status } = useAuth()
  const location = useLocation()
  if (status === 'authenticated') {
    return <Navigate to={postAuthTarget(location.pathname, location.state)} replace />
  }
  return <Outlet />
}
