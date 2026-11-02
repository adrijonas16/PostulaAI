import { useRef, type KeyboardEvent } from 'react'

export interface ProfileTab {
  id: string
  label: string
  /** How many items the section holds, when it is a list. */
  count?: number
  done: boolean
  hasErrors: boolean
}

/** WAI-ARIA tabs: arrow keys move between sections; each tab shows whether it is filled or has errors. */
export function ProfileTabs({ tabs, active, onSelect }: { tabs: ProfileTab[]; active: string; onSelect: (id: string) => void }) {
  const refs = useRef<(HTMLButtonElement | null)[]>([])

  const onKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    const delta = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0
    const target = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : delta === 0 ? null : (index + delta + tabs.length) % tabs.length
    if (target === null) return
    event.preventDefault()
    const tab = tabs[target]
    if (tab === undefined) return
    onSelect(tab.id)
    refs.current[target]?.focus()
  }

  return (
    <div role="tablist" aria-label="Secciones del perfil" className="-mx-1 flex gap-1 overflow-x-auto px-1 pb-1">
      {tabs.map((tab, index) => {
        const selected = tab.id === active
        return (
          <button
            key={tab.id}
            ref={(element) => {
              refs.current[index] = element
            }}
            type="button"
            role="tab"
            id={`tab-${tab.id}`}
            aria-selected={selected}
            aria-controls={`panel-${tab.id}`}
            tabIndex={selected ? 0 : -1}
            onClick={() => onSelect(tab.id)}
            onKeyDown={(event) => onKeyDown(event, index)}
            className={`flex shrink-0 items-center gap-2 rounded-t-[var(--radius-field)] border-b-[3px] px-3 py-2.5 text-sm font-semibold ${
              selected ? 'border-pen text-ink' : 'border-transparent text-graphite hover:border-rule hover:text-ink'
            }`}
          >
            <span
              aria-hidden="true"
              className={`grid size-5 place-items-center rounded-full text-[0.7rem] font-bold ${
                tab.hasErrors ? 'bg-redpen text-sheet' : tab.done ? 'bg-marker text-ink' : 'border border-rule text-graphite'
              }`}
            >
              {tab.hasErrors ? '!' : tab.done ? '✓' : ''}
            </span>
            {tab.label}
            {tab.count !== undefined && tab.count > 0 && <span className="font-normal text-graphite">{tab.count}</span>}
            {tab.hasErrors && <span className="sr-only">, tiene errores</span>}
          </button>
        )
      })}
    </div>
  )
}
