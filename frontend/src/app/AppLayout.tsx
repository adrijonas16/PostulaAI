import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { useAuth } from '../features/auth/AuthContext'
import { Button } from '../shared/ui/Button'
import { Logo } from '../shared/ui/Logo'

const navigation = [
  { to: '/app', label: 'Inicio', end: true },
  { to: '/app/perfil', label: 'Perfil profesional', end: false },
  { to: '/app/vacantes/nueva', label: 'Nueva vacante', end: false },
  { to: '/app/analisis/nuevo', label: 'Nuevo análisis', end: true },
  { to: '/app/analisis', label: 'Historial', end: true },
  { to: '/app/configuracion', label: 'Configuración', end: false },
] as const

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [signingOut, setSigningOut] = useState(false)

  const handleLogout = async () => {
    setSigningOut(true)
    // Leave the protected area first; otherwise the route guard would redirect to the login page.
    await navigate('/', { replace: true })
    await logout()
  }

  return (
    <div className="flex min-h-screen flex-col md:flex-row">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:rounded focus:bg-surface focus:p-2">
        Saltar al contenido
      </a>
      <aside className="border-b border-line bg-surface md:w-60 md:border-b-0 md:border-r">
        <div className="px-5 py-4">
          <Logo />
        </div>
        <nav aria-label="Principal" className="px-3 pb-4">
          <ul className="flex flex-wrap gap-1 md:flex-col">
            {navigation.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    `block rounded-md px-3 py-2 text-sm ${isActive ? 'bg-accent-soft font-medium text-accent-strong' : 'text-ink-muted hover:bg-canvas hover:text-ink'}`
                  }
                >
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
        <div className="border-t border-line px-5 py-4 text-sm">
          <p className="truncate font-medium" title={user?.email}>
            {user?.displayName}
          </p>
          <p className="truncate text-xs text-ink-muted">{user?.email}</p>
          <Button variant="ghost" className="mt-2 -ml-3" onClick={() => void handleLogout()} disabled={signingOut}>
            {signingOut ? 'Cerrando sesión…' : 'Cerrar sesión'}
          </Button>
        </div>
      </aside>
      <main id="main" className="flex-1 px-6 py-10">
        <Outlet />
      </main>
    </div>
  )
}
