import { Link } from 'react-router'
import { primaryLinkClass, secondaryLinkClass } from '../../shared/ui/Button'
import { BandPill } from '../analyses/BandPill'
import { MatchBar, MatchLegend } from '../analyses/MatchBar'
import { RequirementMap } from '../analyses/RequirementMap'
import { demoResult, demoScore } from './demoAnalysis'

const steps = [
  { title: 'Sube tu CV', body: 'Lo convertimos en un perfil que puedes revisar y corregir. Nada se inventa: si no está en tu CV, no aparece.' },
  { title: 'Pega la vacante', body: 'Copia el texto de la oferta tal como lo publicó la empresa.' },
  { title: 'Lee tu mapa de requisitos', body: 'Qué demuestras, qué te falta y qué cambiar en tu CV, más un resumen y una carta que puedes editar.' },
] as const

export function LandingPage() {
  return (
    <div className="mx-auto max-w-6xl">
      <div className="grid items-start gap-10 lg:grid-cols-[minmax(0,5fr)_minmax(0,6fr)]">
        <section aria-labelledby="hero-title" className="lg:sticky lg:top-8">
          <h1 id="hero-title" className="text-4xl font-bold leading-[1.1] md:text-[3.25rem]">
            Compara tu perfil con cada vacante antes de postular.
          </h1>
          <p className="mt-5 max-w-xl text-lg leading-relaxed text-graphite">
            PostulaAI revisa la oferta requisito por requisito y lo marca como lo harías tú con un resaltador: lo que ya demuestras en
            verde, lo que te falta en rojo.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link to="/registro" className={`${primaryLinkClass} px-6 py-3 text-base`}>
              Crear cuenta gratis
            </Link>
            <Link to="/iniciar-sesion" className={`${secondaryLinkClass} px-6 py-3 text-base`}>
              Ya tengo cuenta
            </Link>
          </div>
          <p className="mt-6 max-w-md text-sm text-graphite">
            La IA solo usa lo que está en tu perfil. Nunca agrega experiencia, estudios ni habilidades que no tienes.
          </p>
        </section>

        <section aria-labelledby="demo-title" className="space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 id="demo-title" className="text-sm font-semibold text-graphite">
              Así se ve un resultado. Pruébalo: toca un requisito.
            </h2>
            <span className="rounded-full border border-graphite/40 px-2.5 py-0.5 text-xs text-graphite">Ejemplo ficticio</span>
          </div>
          <div className="rounded-[var(--radius-sheet)] border-2 border-ink bg-sheet p-5">
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <p className="font-bold">Ejecutiva de ventas B2B</p>
              <BandPill band={demoScore.band} />
            </div>
            <p className="mt-1 text-sm">
              Cumples <strong className="marker">3 de 4</strong> requisitos obligatorios y <strong>1 de 2</strong> deseables.
            </p>
            <div className="mt-4">
              <MatchBar score={demoScore} />
              <MatchLegend />
            </div>
          </div>
          <RequirementMap result={demoResult} />
        </section>
      </div>

      <section aria-labelledby="steps-title" className="mt-24">
        <h2 id="steps-title" className="text-2xl font-bold">
          Tres pasos por postulación
        </h2>
        <ol className="mt-6 grid gap-6 md:grid-cols-3">
          {steps.map((step, index) => (
            <li key={step.title} className="border-t-2 border-ink pt-4">
              <span aria-hidden="true" className="text-3xl font-bold text-pen">
                {index + 1}
              </span>
              <h3 className="mt-2 text-lg font-bold">{step.title}</h3>
              <p className="mt-1 text-graphite">{step.body}</p>
            </li>
          ))}
        </ol>
      </section>

      <section aria-labelledby="honesty-title" className="mt-20 rounded-[var(--radius-sheet)] bg-pen-soft p-6 md:p-8">
        <h2 id="honesty-title" className="text-xl font-bold text-pen-strong">
          Sin inventar nada
        </h2>
        <p className="mt-2 max-w-2xl">
          Cada requisito marcado como cumplido cita la parte de tu perfil que lo respalda. Si no hay evidencia, aparece como brecha. Los
          resultados son recomendaciones y no garantizan una contratación.
        </p>
      </section>
    </div>
  )
}
