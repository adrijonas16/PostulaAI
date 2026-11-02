import { Link } from 'react-router'

/** Wordmark with a highlighter stroke: the product's core gesture. */
export function Logo({ to = '/' }: { to?: string }) {
  return (
    <Link to={to} className="inline-flex items-center text-lg font-bold text-ink no-underline" aria-label="PostulaAI, inicio">
      <span className="marker">PostulaAI</span>
    </Link>
  )
}
