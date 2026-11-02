/** <output> has an implicit ARIA role of "status", so screen readers announce the loading message politely. */
export function LoadingState({ label }: { label: string }) {
  return (
    <output aria-live="polite" className="flex items-center gap-3 p-8 text-sm text-ink-muted">
      <span aria-hidden="true" className="size-4 animate-spin rounded-full border-2 border-line border-t-accent" />
      {label}
    </output>
  )
}
