import { Link } from 'react-router'

const outcomes = [
  { title: 'Compatibilidad explicada', body: 'Una estimación con el detalle de qué requisitos cumples y cuáles no. Nunca un número aislado.' },
  { title: 'Fortalezas con evidencia', body: 'Cada fortaleza señala qué parte de tu perfil la respalda.' },
  { title: 'Brechas reales', body: 'Requisitos obligatorios y deseables que tu perfil no demuestra, con recomendaciones concretas.' },
  { title: 'Textos adaptados', body: 'Resumen profesional y borrador de carta de presentación basados solo en lo que tú escribiste.' },
] as const

export function LandingPage() {
  return (
    <div className="mx-auto max-w-5xl">
      <section aria-labelledby="hero-title" className="max-w-2xl">
        <p className="text-sm font-medium uppercase tracking-wide text-accent">Postula con criterio</p>
        <h1 id="hero-title" className="mt-3 text-4xl font-semibold leading-tight tracking-tight md:text-5xl">
          Compara tu perfil con cada vacante antes de postular.
        </h1>
        <p className="mt-5 text-lg text-ink-muted">
          PostulaAI analiza tu perfil profesional frente a una oferta laboral y te muestra coincidencias, brechas y
          recomendaciones honestas para adaptar tu CV.
        </p>
        <div className="mt-8 flex flex-wrap gap-3">
          <Link to="/registro" className="rounded-md bg-accent px-5 py-3 font-medium text-white hover:bg-accent-strong">
            Crear cuenta
          </Link>
          <Link to="/iniciar-sesion" className="rounded-md border border-line bg-surface px-5 py-3 font-medium hover:border-ink-muted">
            Ya tengo cuenta
          </Link>
        </div>
      </section>

      <section aria-labelledby="outcomes-title" className="mt-20">
        <h2 id="outcomes-title" className="text-xl font-semibold">Qué obtienes en cada análisis</h2>
        <ul className="mt-6 grid gap-4 md:grid-cols-2">
          {outcomes.map((outcome) => (
            <li key={outcome.title} className="rounded-lg border border-line bg-surface p-5">
              <h3 className="font-medium">{outcome.title}</h3>
              <p className="mt-1 text-sm text-ink-muted">{outcome.body}</p>
            </li>
          ))}
        </ul>
      </section>

      <section aria-labelledby="honesty-title" className="mt-16 rounded-lg bg-accent-soft p-6">
        <h2 id="honesty-title" className="font-semibold text-accent-strong">Sin inventar nada</h2>
        <p className="mt-2 text-sm text-ink">
          La IA solo usa la información que tú proporcionas. Sugiere cómo presentar mejor tu experiencia, pero nunca
          agrega habilidades, estudios o logros que no tienes. Los resultados son recomendaciones y no garantizan una
          contratación.
        </p>
      </section>
    </div>
  )
}
