import { NavLink, Outlet } from 'react-router'
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
      </aside>
      <main id="main" className="flex-1 px-6 py-10">
        <Outlet />
      </main>
    </div>
  )
}
