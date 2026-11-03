import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { ApiError } from '../../shared/api/ApiError'
import { primaryLinkClass } from '../../shared/ui/Button'
import { LoadingState } from '../../shared/ui/LoadingState'
import { useAuth } from './AuthContext'
import { verifyEmail } from './authApi'

type State = { kind: 'checking' } | { kind: 'done' } | { kind: 'failed'; message: string }

/** Landing page of the link sent by e-mail. Works whether or not the user is signed in on this browser. */
export function VerifyEmailPage() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const { status, reloadUser } = useAuth()
  const [state, setState] = useState<State>(token === '' ? { kind: 'failed', message: 'Falta el código del enlace.' } : { kind: 'checking' })
  const started = useRef(false)

  useEffect(() => {
    // The token is single-use: never send it twice (React StrictMode runs effects twice in development).
    if (token === '' || started.current) return
    started.current = true
    verifyEmail(token)
      .then(() => setState({ kind: 'done' }))
      .catch((error: unknown) =>
        setState({
          kind: 'failed',
          message: error instanceof ApiError ? error.message : 'No pudimos confirmar tu correo. Intenta de nuevo más tarde.',
        }),
      )
  }, [token])

  useEffect(() => {
    if (state.kind === 'done' && status === 'authenticated') {
      void reloadUser()
    }
  }, [state.kind, status, reloadUser])

  return (
    <section aria-labelledby="page-title" className="mx-auto max-w-lg rounded-[var(--radius-sheet)] border border-rule bg-sheet p-6 md:p-8">
      <h1 id="page-title" className="text-2xl font-bold">
        Confirmar correo
      </h1>
      {state.kind === 'checking' && (
        <div className="mt-4">
          <LoadingState label="Confirmando tu correo…" />
        </div>
      )}
      {state.kind === 'done' && (
        <>
          <output className="mt-3 block">
            <span className="marker font-semibold">Listo, tu correo está confirmado.</span> Ya puedes importar tu CV y analizar vacantes.
          </output>
          <Link to={status === 'authenticated' ? '/app/perfil' : '/iniciar-sesion'} className={`${primaryLinkClass} mt-5`}>
            {status === 'authenticated' ? 'Ir a mi perfil' : 'Iniciar sesión'}
          </Link>
        </>
      )}
      {state.kind === 'failed' && (
        <>
          <p role="alert" className="mt-3 text-redpen">
            {state.message}
          </p>
          <Link to="/app" className="mt-5 inline-block font-semibold text-pen underline">
            Ir a la aplicación para pedir otro enlace
          </Link>
        </>
      )}
    </section>
  )
}
