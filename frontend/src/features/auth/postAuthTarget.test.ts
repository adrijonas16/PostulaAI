import { describe, expect, it } from 'vitest'
import { postAuthTarget } from './postAuthTarget'

describe('postAuthTarget', () => {
  it('returns to the protected page that was requested', () => {
    expect(postAuthTarget('/iniciar-sesion', { from: '/app/perfil' })).toBe('/app/perfil')
  })

  it('ignores targets outside the app (open-redirect guard)', () => {
    expect(postAuthTarget('/iniciar-sesion', { from: 'https://malicioso.example' })).toBe('/app')
  })

  it('sends new accounts to complete their profile', () => {
    expect(postAuthTarget('/registro', null)).toBe('/app/perfil')
  })
})
