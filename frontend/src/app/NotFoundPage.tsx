import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <main id="main" className="mx-auto max-w-xl px-6 py-24 text-center">
      <h1 className="text-2xl font-semibold">Página no encontrada</h1>
      <p className="mt-2 text-ink-muted">La dirección que buscas no existe o fue movida.</p>
      <Link to="/" className="mt-6 inline-block text-accent underline">
        Volver al inicio
      </Link>
    </main>
  )
}
