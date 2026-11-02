import { Link, Outlet } from 'react-router'
import { Logo } from '../shared/ui/Logo'

export function PublicLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:rounded focus:bg-surface focus:p-2">
        Saltar al contenido
      </a>
      <header className="border-b border-line bg-surface">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <Logo />
          <nav aria-label="Cuenta" className="flex items-center gap-4 text-sm">
            <Link to="/iniciar-sesion" className="text-ink-muted hover:text-ink">
              Iniciar sesión
            </Link>
            <Link to="/registro" className="rounded-md bg-accent px-3 py-2 font-medium text-white hover:bg-accent-strong">
              Crear cuenta
            </Link>
          </nav>
        </div>
      </header>
      <main id="main" className="flex-1 px-6 py-12">
        <Outlet />
      </main>
      <footer className="border-t border-line px-6 py-6 text-center text-xs text-ink-muted">
        PostulaAI ofrece recomendaciones; no garantiza resultados en procesos de selección.
      </footer>
    </div>
  )
}
