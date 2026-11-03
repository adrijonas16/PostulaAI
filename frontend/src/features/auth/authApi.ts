import { apiRequest } from '../../shared/api/client'
import { session } from '../../shared/api/session'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../../shared/api/types'

export async function login(request: LoginRequest): Promise<AuthResponse> {
  const auth = await apiRequest<AuthResponse>('/auth/login', { method: 'POST', body: request, auth: false })
  session.set(auth)
  return auth
}

export async function register(request: RegisterRequest): Promise<AuthResponse> {
  const auth = await apiRequest<AuthResponse>('/auth/register', { method: 'POST', body: request, auth: false })
  session.set(auth)
  return auth
}

export function verifyEmail(token: string): Promise<void> {
  return apiRequest<void>('/auth/verify-email', { method: 'POST', body: { token }, auth: false })
}

export function resendVerification(): Promise<void> {
  return apiRequest<void>('/auth/resend-verification', { method: 'POST' })
}

export async function logout(): Promise<void> {
  try {
    await apiRequest<void>('/auth/logout', { method: 'POST', auth: false })
  } finally {
    session.set(null)
  }
}
