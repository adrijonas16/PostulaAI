/** Where to go once authenticated: the page that was requested, or a sensible default per entry page. */
export function postAuthTarget(pathname: string, state: unknown): string {
  if (typeof state === 'object' && state !== null && 'from' in state && typeof state.from === 'string' && state.from.startsWith('/app')) {
    return state.from
  }
  return pathname === '/registro' ? '/app/perfil' : '/app'
}
