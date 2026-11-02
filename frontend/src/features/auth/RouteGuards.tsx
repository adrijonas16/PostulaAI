import { Navigate, Outlet, useLocation } from 'react-router'
import { LoadingState } from '../../shared/ui/LoadingState'
import { useAuth } from './AuthContext'

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

/** Login and registration are pointless for an authenticated user. */
export function RedirectIfAuthenticated() {
  const { status } = useAuth()
  if (status === 'authenticated') {
    return <Navigate to="/app" replace />
  }
  return <Outlet />
}
