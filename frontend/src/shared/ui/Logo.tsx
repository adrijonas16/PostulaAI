import { Link } from 'react-router'

export function Logo() {
  return (
    <Link to="/" className="inline-flex items-center gap-2 font-semibold tracking-tight text-ink no-underline">
      <span aria-hidden="true" className="grid size-7 place-items-center rounded-md bg-accent text-sm font-bold text-white">
        P
      </span>
      <span>PostulaAI</span>
    </Link>
  )
}
