import { useEffect, useRef, useState, type CSSProperties } from 'react'
import { useInView, usePrefersReducedMotion } from '../../shared/lib/motion'

interface DemoLine {
  text: string
  met: boolean
  required: boolean
}

// Fictional posting used only for the illustration.
const lines: DemoLine[] = [
  { text: '2 años diseñando apps móviles', met: true, required: true },
  { text: 'Figma y prototipos interactivos', met: true, required: true },
  { text: 'Investigación con usuarios', met: true, required: true },
  { text: 'Inglés avanzado', met: false, required: true },
  { text: 'Portafolio publicado', met: true, required: false },
]

const SCORE_STEP = lines.length + 1
const LAST_STEP = SCORE_STEP + 4 // hold the finished sheet for a few beats, then start over
const BEAT_MS = 850

function MarkerPen() {
  return (
    <svg viewBox="0 0 64 20" className="h-5 w-16 drop-shadow-sm" aria-hidden="true">
      <rect x="14" y="3" width="44" height="14" rx="4" fill="var(--color-ink)" />
      <rect x="44" y="3" width="6" height="14" fill="var(--color-marker)" />
      <path d="M14 5 L4 8 L1 10 L4 12 L14 15 Z" fill="var(--color-marker)" stroke="var(--color-ink)" strokeWidth="1.2" />
    </svg>
  )
}

function WavyUnderline({ on }: { on: boolean }) {
  return (
    <svg viewBox="0 0 160 8" preserveAspectRatio="none" className="absolute -bottom-1.5 left-0 h-2 w-full" aria-hidden="true">
      <path
        d="M1 4 Q 6 0 11 4 T 21 4 T 31 4 T 41 4 T 51 4 T 61 4 T 71 4 T 81 4 T 91 4 T 101 4 T 111 4 T 121 4 T 131 4 T 141 4 T 151 4 T 159 4"
        pathLength={1}
        fill="none"
        stroke="var(--color-redpen)"
        strokeWidth="1.6"
        className={`draw-stroke ${on ? 'is-on' : ''}`}
      />
    </svg>
  )
}

/**
 * The page's one orchestrated moment: a highlighter marks a job posting line by line,
 * then the verdict appears. Loops only while visible; shows the finished sheet under reduced motion.
 */
export function HeroScene() {
  const ref = useRef<HTMLDivElement>(null)
  const inView = useInView(ref)
  const reduced = usePrefersReducedMotion()
  const animate = inView && !reduced
  const [step, setStep] = useState(0)
  const rowRefs = useRef<(HTMLLIElement | null)[]>([])
  const [rowTops, setRowTops] = useState<number[]>([])

  useEffect(() => {
    const measure = () => setRowTops(rowRefs.current.map((row) => row?.offsetTop ?? 0))
    measure()
    window.addEventListener('resize', measure)
    return () => window.removeEventListener('resize', measure)
  }, [])

  useEffect(() => {
    if (!animate) return
    const timer = setInterval(() => setStep((current) => (current >= LAST_STEP ? 0 : current + 1)), BEAT_MS)
    return () => clearInterval(timer)
  }, [animate])

  const shown = animate ? step : SCORE_STEP
  const marked = Math.min(shown, lines.length)
  const penRow = shown >= 1 && shown <= lines.length ? shown - 1 : null
  const scoreVisible = shown >= SCORE_STEP
  const requiredMet = lines.filter((line) => line.required && line.met).length
  const requiredTotal = lines.filter((line) => line.required).length

  return (
    <div ref={ref} className="relative mx-auto w-full max-w-md" aria-hidden="true">
      <div className="absolute -right-3 -top-3 hidden h-full w-full rotate-2 rounded-[var(--radius-sheet)] bg-note md:block" />
      <div className="relative rotate-[-1deg] rounded-[var(--radius-sheet)] border border-rule bg-sheet p-6 shadow-[0_18px_40px_-18px_rgba(29,36,51,0.35)]">
        <p className="text-xs font-semibold text-graphite">Vacante</p>
        <p className="text-xl font-bold">Diseñadora UX</p>
        <p className="text-sm text-graphite">Estudio Ejemplo, remoto</p>

        <ul className="relative mt-5 space-y-3.5">
          {lines.map((line, index) => {
            const on = index < marked
            return (
              <li
                key={line.text}
                ref={(element) => {
                  rowRefs.current[index] = element
                }}
                className="flex items-baseline justify-between gap-3"
              >
                <span className="relative">
                  <span className={line.met ? `marker-sweep ${on ? 'is-on' : ''}` : ''}>{line.text}</span>
                  {!line.met && <WavyUnderline on={on} />}
                </span>
                <span
                  className={`shrink-0 font-[family-name:var(--font-hand)] text-lg leading-none transition-opacity duration-300 ${
                    on ? 'opacity-100' : 'opacity-0'
                  } ${line.met ? 'text-pen' : 'text-redpen'}`}
                >
                  {line.met ? '¡lo tienes!' : 'te falta'}
                </span>
              </li>
            )
          })}

          {penRow !== null && rowTops[penRow] !== undefined && (
            <span
              key={penRow}
              className="pointer-events-none absolute left-0"
              style={{ top: rowTops[penRow] - 4, animation: `pen-swipe ${BEAT_MS - 150}ms ease-in-out both`, '--swipe': '220px' } as CSSProperties}
            >
              <MarkerPen />
            </span>
          )}
        </ul>
      </div>

      <div
        className={`relative z-10 -mt-4 ml-auto w-[85%] rounded-[var(--radius-sheet)] border-2 border-ink bg-sheet p-4 transition-all duration-500 ${
          scoreVisible ? 'translate-y-0 opacity-100' : 'translate-y-4 opacity-0'
        }`}
      >
        <div className="flex items-center justify-between gap-2">
          <p className="font-bold">
            Cumples {requiredMet} de {requiredTotal} obligatorios
          </p>
          <span className={`rounded-full bg-marker-soft px-2.5 py-0.5 text-xs font-semibold ${scoreVisible ? 'pop' : 'opacity-0'}`}>Encaje medio</span>
        </div>
        <div className="mt-3 flex gap-1">
          {lines.map((line, index) => (
            <span
              key={line.text}
              className={`h-3 flex-1 rounded-sm transition-all duration-300 ${
                !scoreVisible ? 'scale-x-0' : 'scale-x-100'
              } ${line.met ? (line.required ? 'bg-marker' : 'bg-marker/60') : 'border-2 border-redpen bg-redpen-soft'}`}
              style={{ transitionDelay: scoreVisible ? `${index * 90}ms` : '0ms', transformOrigin: 'left' }}
            />
          ))}
        </div>
        <p className="mt-3 font-[family-name:var(--font-hand)] text-lg leading-tight text-pen">Sube tu nivel de inglés en el CV si es real, y postúlate.</p>
      </div>
    </div>
  )
}
