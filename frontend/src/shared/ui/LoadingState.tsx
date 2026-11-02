/** <output> has an implicit ARIA role of "status", so screen readers announce the loading message politely. */
export function LoadingState({ label }: { label: string }) {
  return (
    <output aria-live="polite" className="flex items-center gap-3 p-8 text-sm text-graphite">
      <span aria-hidden="true" className="h-1.5 w-10 overflow-hidden rounded-full bg-rule">
        <span className="block h-full w-1/2 animate-pulse rounded-full bg-marker" />
      </span>
      {label}
    </output>
  )
}
