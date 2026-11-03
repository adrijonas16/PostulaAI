/** Shown before the user waits for something that cannot work: the server has no Anthropic key. */
export function AiNotConfigured({ feature }: { feature: string }) {
  return (
    <div role="alert" className="rounded-[var(--radius-field)] border-2 border-redpen bg-redpen-soft p-4 text-sm">
      <p className="font-bold text-redpen">{feature} no está disponible todavía</p>
      <p className="mt-1">
        El servidor no tiene configurada la clave de Anthropic. En tu equipo, abre el archivo <code className="font-semibold">.env</code> del proyecto,
        completa la línea <code className="font-semibold">ANTHROPIC_API_KEY=</code> y vuelve a ejecutar el script de arranque.
      </p>
    </div>
  )
}
