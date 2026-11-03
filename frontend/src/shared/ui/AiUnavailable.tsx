export type AiUnavailableReason = 'email_not_verified' | 'not_configured' | 'daily_limit'

const copy: Record<AiUnavailableReason, (feature: string) => { title: string; body: React.ReactNode }> = {
  email_not_verified: (feature) => ({
    title: `Confirma tu correo para usar ${feature}`,
    body: 'Te enviamos un enlace al registrarte. Ábrelo y vuelve aquí; si no lo encuentras, pide otro desde el aviso de arriba.',
  }),
  not_configured: (feature) => ({
    title: `${capitalize(feature)} no está disponible todavía`,
    body: (
      <>
        El servidor no tiene configurada la clave de Anthropic. En tu equipo, abre el archivo <code className="font-semibold">.env</code> del
        proyecto, completa la línea <code className="font-semibold">ANTHROPIC_API_KEY=</code> y vuelve a ejecutar el script de arranque.
      </>
    ),
  }),
  daily_limit: (feature) => ({
    title: `${capitalize(feature)} está en pausa hasta mañana`,
    body: 'PostulaAI alcanzó su límite diario de uso de IA para todos los usuarios. Vuelve a intentarlo mañana.',
  }),
}

function capitalize(text: string): string {
  return text.charAt(0).toLocaleUpperCase('es') + text.slice(1)
}

/** Shown before the user waits for something that cannot work right now. */
export function AiUnavailable({ reason, feature }: { reason: AiUnavailableReason; feature: string }) {
  const { title, body } = copy[reason](feature)
  const tone = reason === 'email_not_verified' ? 'border-pen bg-pen-soft' : 'border-redpen bg-redpen-soft'
  return (
    <div role="alert" className={`rounded-[var(--radius-field)] border-2 p-4 text-left text-sm ${tone}`}>
      <p className={`font-bold ${reason === 'email_not_verified' ? 'text-pen-strong' : 'text-redpen'}`}>{title}</p>
      <p className="mt-1">{body}</p>
    </div>
  )
}
