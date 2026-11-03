import { Link } from 'react-router'
import { primaryLinkClass, secondaryLinkClass } from '../../shared/ui/Button'
import { Reveal } from '../../shared/ui/Reveal'
import { RequirementMap } from '../analyses/RequirementMap'
import { demoResult } from './demoAnalysis'
import { ArrowDoodle, CvDoodle, PostingDoodle, VerdictDoodle } from './Doodles'
import { HeroScene } from './HeroScene'
import { PipelineDemo } from './PipelineDemo'

const steps = [
  { title: 'Sube tu CV', body: 'Lo convertimos en un perfil que revisas y corriges. Si no está en tu CV, no aparece.', Doodle: CvDoodle },
  { title: 'Pega la vacante', body: 'Copia el texto de la oferta tal como lo publicó la empresa.', Doodle: PostingDoodle },
  { title: 'Mira qué tienes y qué te falta', body: 'Requisito por requisito, con ideas para tu CV, un resumen y una carta listos para editar.', Doodle: VerdictDoodle },
] as const

export function LandingPage() {
  return (
    <div className="mx-auto max-w-6xl">
      <section aria-labelledby="hero-title" className="grid items-center gap-12 lg:grid-cols-[minmax(0,5fr)_minmax(0,5fr)] lg:gap-16">
        <div className="rise">
          <p className="font-[family-name:var(--font-hand)] text-2xl text-pen">antes de darle a «postular»…</p>
          <h1 id="hero-title" className="mt-1 text-4xl font-bold leading-[1.08] md:text-[3.4rem]">
            Compara tu perfil con cada vacante, requisito por requisito.
          </h1>
          <p className="mt-5 max-w-xl text-lg leading-relaxed text-graphite">
            PostulaAI marca la oferta como lo harías tú con un resaltador: en verde lo que ya demuestras, en rojo lo que te falta. Sin
            adivinar y sin inventar nada.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-3">
            <Link to="/registro" className={`${primaryLinkClass} px-6 py-3 text-base transition-transform hover:-translate-y-0.5`}>
              Crear cuenta gratis
            </Link>
            <Link to="/iniciar-sesion" className={`${secondaryLinkClass} px-6 py-3 text-base`}>
              Ya tengo cuenta
            </Link>
          </div>
        </div>
        <HeroScene />
      </section>

      <section aria-labelledby="steps-title" className="mt-28">
        <Reveal>
          <h2 id="steps-title" className="text-3xl font-bold">
            Tres pasos, cinco minutos
          </h2>
        </Reveal>
        <ol className="mt-8 grid gap-6 md:grid-cols-3">
          {steps.map(({ title, body, Doodle }, index) => (
            <li key={title}>
              <Reveal delay={index * 120} className="h-full">
                <div className="h-full rounded-[var(--radius-sheet)] border border-rule bg-sheet p-6 transition-transform duration-300 hover:-translate-y-1 hover:rotate-[-0.5deg]">
                  <div className="flex items-start justify-between">
                    <Doodle />
                    <span aria-hidden="true" className="font-[family-name:var(--font-hand)] text-5xl leading-none text-pen">
                      {index + 1}
                    </span>
                  </div>
                  <h3 className="mt-3 text-lg font-bold">{title}</h3>
                  <p className="mt-1 text-graphite">{body}</p>
                </div>
              </Reveal>
            </li>
          ))}
        </ol>
      </section>

      <section aria-labelledby="try-title" className="mt-28 grid items-start gap-10 lg:grid-cols-[minmax(0,4fr)_minmax(0,6fr)]">
        <Reveal>
          <h2 id="try-title" className="text-3xl font-bold">
            Pruébalo aquí mismo
          </h2>
          <p className="mt-3 max-w-md text-lg text-graphite">
            Así se ve un resultado real (con datos inventados). Toca cualquier requisito para ver la evidencia o qué hacer al respecto.
          </p>
          <div className="mt-4 hidden items-center gap-2 lg:flex">
            <span className="font-[family-name:var(--font-hand)] text-2xl text-pen">¡toca uno!</span>
            <ArrowDoodle className="h-12 w-20" />
          </div>
        </Reveal>
        <Reveal delay={120}>
          <RequirementMap result={demoResult} />
        </Reveal>
      </section>

      <section aria-labelledby="track-title" className="mt-28 grid items-center gap-10 rounded-[var(--radius-sheet)] bg-note-blue p-6 md:grid-cols-2 md:p-10">
        <Reveal>
          <h2 id="track-title" className="text-3xl font-bold">
            Y no pierdas el hilo
          </h2>
          <p className="mt-3 text-lg text-graphite">
            Marca a qué te postulaste, cuándo y qué carta enviaste. Cuando te llamen, sabrás exactamente qué les dijiste.
          </p>
        </Reveal>
        <Reveal delay={120}>
          <PipelineDemo />
        </Reveal>
      </section>

      <section aria-labelledby="honesty-title" className="mt-28 flex justify-center">
        <Reveal>
          <div className="relative max-w-xl rotate-[-1.2deg] bg-note p-7 shadow-[0_14px_30px_-16px_rgba(29,36,51,0.4)] md:p-9">
            <span aria-hidden="true" className="absolute -top-3 left-1/2 h-6 w-24 -translate-x-1/2 rotate-2 bg-sheet/70 shadow-sm" />
            <h2 id="honesty-title" className="font-[family-name:var(--font-hand)] text-4xl leading-none text-ink">
              Sin inventar nada
            </h2>
            <p className="mt-3">
              Cada requisito que aparece en verde cita la parte de tu perfil que lo respalda. Si no hay evidencia, aparece como brecha. Son
              recomendaciones: no garantizan una contratación.
            </p>
          </div>
        </Reveal>
      </section>

      <section aria-labelledby="cta-title" className="mt-28">
        <Reveal>
          <div className="rounded-[var(--radius-sheet)] bg-pen px-6 py-12 text-center text-sheet md:py-16">
            <h2 id="cta-title" className="text-3xl font-bold md:text-4xl">
              Tu próxima postulación, mejor preparada.
            </h2>
            <p className="mx-auto mt-3 max-w-md text-pen-soft">Crea tu cuenta, sube tu CV y analiza tu primera vacante hoy.</p>
            <Link
              to="/registro"
              className="mt-7 inline-flex items-center rounded-[var(--radius-field)] bg-sheet px-6 py-3 font-semibold text-pen-strong transition-transform hover:-translate-y-0.5"
            >
              Empezar gratis
            </Link>
          </div>
        </Reveal>
      </section>
    </div>
  )
}
