const formatter = new Intl.DateTimeFormat('es', { day: 'numeric', month: 'short', year: 'numeric' })

/** Formats an ISO timestamp as a short Spanish date, e.g. "2 nov 2026". */
export function formatDate(iso: string): string {
  return formatter.format(new Date(iso))
}
