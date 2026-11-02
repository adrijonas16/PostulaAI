import { Link, Outlet } from 'react-router'
import { primaryLinkClass } from '../shared/ui/Button'
import { Logo } from '../shared/ui/Logo'

export function PublicLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:rounded focus:bg-sheet focus:p-2">
        Saltar al contenido
      </a>
      <header>
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-5">
          <Logo />
          <nav aria-label="Cuenta" className="flex items-center gap-4 text-sm">
            <Link to="/iniciar-sesion" className="font-semibold text-graphite hover:text-ink">
              Iniciar sesión
            </Link>
            <Link to="/registro" className={primaryLinkClass}>
              Crear cuenta
            </Link>
          </nav>
        </div>
      </header>
      <main id="main" className="flex-1 px-6 py-8 md:py-14">
        <Outlet />
      </main>
      <footer className="mt-16 border-t border-rule px-6 py-6 text-center text-xs text-graphite">
        PostulaAI ofrece recomendaciones; no garantiza resultados en procesos de selección.
      </footer>
    </div>
  )
}
