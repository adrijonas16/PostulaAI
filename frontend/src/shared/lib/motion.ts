import { useEffect, useState, type RefObject } from 'react'

const reducedQuery = '(prefers-reduced-motion: reduce)'

/** True when the user asked the OS for less motion. Animations then show their final state. */
export function usePrefersReducedMotion(): boolean {
  const [reduced, setReduced] = useState(() => typeof window !== 'undefined' && typeof window.matchMedia === 'function' && window.matchMedia(reducedQuery).matches)

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return
    const query = window.matchMedia(reducedQuery)
    const onChange = () => setReduced(query.matches)
    query.addEventListener('change', onChange)
    return () => query.removeEventListener('change', onChange)
  }, [])

  return reduced
}

/** True while the element is on screen (or always, where IntersectionObserver is unavailable). */
export function useInView(ref: RefObject<Element | null>, { once = false, threshold = 0.25 } = {}): boolean {
  const [inView, setInView] = useState(typeof IntersectionObserver === 'undefined')

  useEffect(() => {
    const element = ref.current
    if (element === null || typeof IntersectionObserver === 'undefined') return
    const observer = new IntersectionObserver(
      ([entry]) => {
        const visible = entry?.isIntersecting ?? false
        setInView(visible)
        if (visible && once) observer.disconnect()
      },
      { threshold },
    )
    observer.observe(element)
    return () => observer.disconnect()
  }, [ref, once, threshold])

  return inView
}
