import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { applyServerErrors } from '../../shared/lib/applyServerErrors'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { TextField } from '../../shared/ui/fields'
import { useAuth } from './AuthContext'
import { loginSchema, type LoginFormValues } from './schemas'

export function LoginPage() {
  const { login } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema), defaultValues: { email: '', password: '' } })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      // On success the RedirectIfAuthenticated guard navigates to the requested page.
      await login(values)
    } catch (error) {
      setFormError(applyServerErrors(error, setError))
    }
  })

  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-sm">
      <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
        Iniciar sesión
      </h1>
      <p className="mt-2 text-sm text-ink-muted">Accede a tu perfil, tus vacantes y tu historial de análisis.</p>

      <form noValidate onSubmit={onSubmit} className="mt-8 space-y-4">
        {formError !== null && <Alert tone="error">{formError}</Alert>}
        <TextField label="Correo electrónico" type="email" autoComplete="email" required error={errors.email?.message} {...register('email')} />
        <TextField label="Contraseña" type="password" autoComplete="current-password" required error={errors.password?.message} {...register('password')} />
        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Ingresando…' : 'Iniciar sesión'}
        </Button>
      </form>

      <p className="mt-6 text-sm text-ink-muted">
        ¿No tienes cuenta?{' '}
        <Link to="/registro" className="font-medium text-accent underline">
          Crear cuenta
        </Link>
      </p>
    </section>
  )
}
