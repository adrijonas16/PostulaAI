import { z } from 'zod'

// Mirrors backend rules (Modules/Auth/Contracts/AuthValidators.cs).
export const PASSWORD_MIN_LENGTH = 10
export const PASSWORD_MAX_LENGTH = 128

export const loginSchema = z.object({
  email: z.string().trim().min(1, 'Ingresa tu correo electrónico.'),
  password: z.string().min(1, 'Ingresa tu contraseña.').max(PASSWORD_MAX_LENGTH, 'La contraseña es demasiado larga.'),
})
export type LoginFormValues = z.infer<typeof loginSchema>

export const registerSchema = z
  .object({
    displayName: z.string().trim().min(1, 'Ingresa tu nombre.').max(100, 'El nombre no puede superar 100 caracteres.'),
    email: z.string().trim().min(1, 'Ingresa tu correo electrónico.').pipe(z.email('Ingresa un correo electrónico válido.')),
    password: z
      .string()
      .min(PASSWORD_MIN_LENGTH, `La contraseña debe tener al menos ${PASSWORD_MIN_LENGTH} caracteres.`)
      .max(PASSWORD_MAX_LENGTH, `La contraseña no puede superar ${PASSWORD_MAX_LENGTH} caracteres.`),
    confirmPassword: z.string(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Las contraseñas no coinciden.',
  })
export type RegisterFormValues = z.infer<typeof registerSchema>
