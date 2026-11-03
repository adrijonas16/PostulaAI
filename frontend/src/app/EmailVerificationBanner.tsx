import { useEffect, useState } from 'react'
import { ApiError } from '../shared/api/ApiError'
import { Button } from '../shared/ui/Button'
import { useAuth } from '../features/auth/AuthContext'
import { resendVerification } from '../features/auth/authApi'

/** Reminds the user to confirm their e-mail; AI features stay locked until they do. */
export function EmailVerificationBanner() {
  const { user, reloadUser } = useAuth()
  const [feedback, setFeedback] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const pending = user?.emailVerificationPending === true

  // The link usually opens in another tab: pick up the change when the user comes back.
  useEffect(() => {
    if (!pending) return
    const onFocus = () => void reloadUser()
    window.addEventListener('focus', onFocus)
    return () => window.removeEventListener('focus', onFocus)
  }, [pending, reloadUser])

  if (!pending || user === null) return null

  const resend = async () => {
    setBusy(true)
    setFeedback(null)
    try {
      await resendVerification()
      setFeedback('Te enviamos un correo nuevo.')
    } catch (error) {
      setFeedback(error instanceof ApiError ? error.message : 'No pudimos enviar el correo. Intenta de nuevo.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section aria-labelledby="verify-banner-title" className="mb-6 rounded-[var(--radius-sheet)] border-2 border-pen bg-pen-soft p-4 md:p-5">
      <h2 id="verify-banner-title" className="font-bold text-pen-strong">
        Confirma tu correo
      </h2>
      <p className="mt-1 text-sm">
        Te enviamos un enlace a <strong>{user.email}</strong>. Mientras no lo confirmes puedes completar tu perfil y guardar vacantes, pero no
        importar tu CV ni analizar.
      </p>
      {import.meta.env.DEV && (
        <p className="mt-1 text-sm text-graphite">
          En local, los correos llegan a{' '}
          <a href="http://localhost:8025" target="_blank" rel="noopener noreferrer" className="font-semibold text-pen underline">
            localhost:8025<span className="sr-only"> (se abre en otra pestaña)</span>
          </a>
          .
        </p>
      )}
      <div className="mt-3 flex flex-wrap items-center gap-3">
        <Button variant="secondary" onClick={() => void resend()} disabled={busy}>
          {busy ? 'Enviando…' : 'Reenviar correo'}
        </Button>
        <Button variant="ghost" onClick={() => void reloadUser()}>
          Ya lo confirmé
        </Button>
        <span aria-live="polite" className="text-sm">
          {feedback}
        </span>
      </div>
    </section>
  )
}
