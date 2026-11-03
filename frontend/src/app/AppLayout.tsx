import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { EmailVerificationBanner } from './EmailVerificationBanner'
import { useAuth } from '../features/auth/AuthContext'
import { Button } from '../shared/ui/Button'
import { Logo } from '../shared/ui/Logo'
import { useJourney, type JourneyStep } from './useJourney'

const steps: { step: JourneyStep; to: string; label: string; hint: string }[] = [
  { step: 'profile', to: '/app/perfil', label: 'Tu perfil', hint: 'Lo que sabes hacer' },
  { step: 'postings', to: '/app/vacantes', label: 'Tus vacantes', hint: 'Las ofertas que te interesan' },
  { step: 'analysis', to: '/app/analisis/nuevo', label: 'Analizar', hint: 'Compara perfil y vacante' },
]

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-3 rounded-[var(--radius-field)] px-3 py-2 text-sm no-underline ${
    isActive ? 'bg-pen-soft font-semibold text-pen-strong' : 'text-ink hover:bg-paper'
  }`

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const journey = useJourney()
  const [signingOut, setSigningOut] = useState(false)

  const handleLogout = async () => {
    setSigningOut(true)
    // Leave the protected area first; otherwise the route guard would redirect to the login page.
    await navigate('/', { replace: true })
    await logout()
  }

  return (
    <div className="min-h-screen md:grid md:grid-cols-[17rem_1fr]">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-10 focus:rounded focus:bg-sheet focus:p-2">
        Saltar al contenido
      </a>
      <aside className="border-b border-rule bg-sheet md:sticky md:top-0 md:flex md:h-screen md:flex-col md:border-b-0 md:border-r">
        <div className="px-5 py-5">
          <Logo to="/app" />
        </div>

        <nav aria-label="Principal" className="flex-1 overflow-x-auto px-3 pb-4">
          <ul className="mb-4 flex gap-1 md:flex-col">
            <li>
              <NavLink to="/app" end className={linkClass}>
                Inicio
              </NavLink>
            </li>
          </ul>

          <p id="journey-label" className="mb-2 hidden px-3 text-sm font-semibold text-graphite md:block">
            Tu camino
          </p>
          <ol aria-labelledby="journey-label" className="flex gap-1 md:flex-col">
            {steps.map(({ step, to, label, hint }, index) => {
              const done = journey.done[step]
              const isNext = journey.next === step
              return (
                <li key={step}>
                  <NavLink to={to} end={step !== 'postings'} className={linkClass}>
                    <span
                      aria-hidden="true"
                      className={`grid size-6 shrink-0 place-items-center rounded-full text-xs font-bold ${
                        done ? 'bg-marker text-ink' : isNext ? 'bg-pen text-white' : 'border border-rule text-graphite'
                      }`}
                    >
                      {done ? <span className="pop inline-block">✓</span> : index + 1}
                    </span>
                    <span className="flex flex-col leading-tight">
                      <span>{label}</span>
                      <span className="hidden text-xs font-normal text-graphite md:block">
                        {done ? 'Hecho' : isNext ? 'Siguiente paso' : hint}
                      </span>
                    </span>
                    <span className="sr-only">{done ? '(completado)' : isNext ? '(siguiente paso)' : ''}</span>
                  </NavLink>
                </li>
              )
            })}
          </ol>

          <ul className="mt-4 flex gap-1 md:flex-col">
            <li>
              <NavLink to="/app/analisis" end className={linkClass}>
                Historial de análisis
              </NavLink>
            </li>
            <li>
              <NavLink to="/app/configuracion" className={linkClass}>
                Configuración
              </NavLink>
            </li>
          </ul>
        </nav>

        <div className="flex items-center justify-between gap-3 border-t border-rule px-5 py-3 text-sm md:block md:py-4">
          <div className="min-w-0">
            <p className="truncate font-semibold" title={user?.email}>
              {user?.displayName}
            </p>
            <p className="truncate text-xs text-graphite">{user?.email}</p>
          </div>
          <Button variant="ghost" className="md:-ml-3 md:mt-2" onClick={() => void handleLogout()} disabled={signingOut}>
            {signingOut ? 'Cerrando sesión…' : 'Cerrar sesión'}
          </Button>
        </div>
      </aside>

      <main id="main" className="min-w-0 px-5 py-8 md:px-10 md:py-10">
        <EmailVerificationBanner />
        <Outlet />
      </main>
    </div>
  )
}
