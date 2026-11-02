import { useAuth } from '../auth/AuthContext'
import { FormSection } from '../profile/ProfileSections'

export function SettingsPage() {
  const { user } = useAuth()

  return (
    <section aria-labelledby="page-title" className="mx-auto w-full max-w-3xl space-y-6">
      <h1 id="page-title" className="text-2xl font-semibold tracking-tight">
        Configuración
      </h1>

      <FormSection id="account-title" title="Cuenta">
        <dl className="grid gap-3 text-sm sm:grid-cols-[10rem_1fr]">
          <dt className="text-ink-muted">Nombre</dt>
          <dd>{user?.displayName}</dd>
          <dt className="text-ink-muted">Correo electrónico</dt>
          <dd>{user?.email}</dd>
        </dl>
      </FormSection>

      <FormSection id="analysis-language-title" title="Idioma de los análisis">
        <p className="text-sm text-ink-muted">Los análisis se generan en español. Otros idiomas se habilitarán después de evaluarlos.</p>
      </FormSection>

      <FormSection id="privacy-title" title="Privacidad">
        <ul className="list-disc space-y-1 pl-5 text-sm text-ink-muted">
          <li>Tu perfil y tus vacantes solo se envían al proveedor de IA cuando solicitas un análisis.</li>
          <li>No usamos tus datos para entrenar modelos ni los compartimos con terceros.</li>
          <li>La eliminación completa de la cuenta estará disponible próximamente; mientras tanto puedes solicitarla por el canal de contacto del proyecto.</li>
        </ul>
      </FormSection>
    </section>
  )
}
