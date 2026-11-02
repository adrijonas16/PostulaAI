import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { applyServerErrors } from '../../shared/lib/applyServerErrors'
import { Alert } from '../../shared/ui/Alert'
import { Button } from '../../shared/ui/Button'
import { TextField } from '../../shared/ui/fields'
import { useAuth } from './AuthContext'
import { PASSWORD_MIN_LENGTH, registerSchema, type RegisterFormValues } from './schemas'

export function RegisterPage() {
  const { register: registerAccount } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { displayName: '', email: '', password: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(async ({ displayName, email, password }) => {
    setFormError(null)
    try {
      // On success the RedirectIfAuthenticated guard navigates to the profile.
      await registerAccount({ displayName, email, password })
    } catch (error) {
      setFormError(applyServerErrors(error, setError))
    }
  })

  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-sm">
      <h1 id="page-title" className="text-3xl font-bold">
        Crear cuenta
      </h1>
      <p className="mt-2 text-sm text-graphite">Regístrate con tu correo para empezar a analizar vacantes.</p>

      <form noValidate onSubmit={onSubmit} className="mt-8 space-y-4">
        {formError !== null && <Alert tone="error">{formError}</Alert>}
        <TextField label="Nombre" autoComplete="name" required error={errors.displayName?.message} {...register('displayName')} />
        <TextField label="Correo electrónico" type="email" autoComplete="email" required error={errors.email?.message} {...register('email')} />
        <TextField
          label="Contraseña"
          type="password"
          autoComplete="new-password"
          required
          hint={`Mínimo ${PASSWORD_MIN_LENGTH} caracteres. Una frase larga es más segura que una palabra corta.`}
          error={errors.password?.message}
          {...register('password')}
        />
        <TextField label="Confirmar contraseña" type="password" autoComplete="new-password" required error={errors.confirmPassword?.message} {...register('confirmPassword')} />
        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Creando cuenta…' : 'Crear cuenta'}
        </Button>
        <p className="text-xs text-graphite">
          Al crear tu cuenta aceptas que tu perfil se procese para generar los análisis que solicites. No usamos tus datos para entrenar modelos.
        </p>
      </form>

      <p className="mt-6 text-sm text-graphite">
        ¿Ya tienes cuenta?{' '}
        <Link to="/iniciar-sesion" className="font-semibold text-pen underline">
          Iniciar sesión
        </Link>
      </p>
    </section>
  )
}
