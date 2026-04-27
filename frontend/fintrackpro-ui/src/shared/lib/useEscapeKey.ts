import { useEffect } from 'react'

function useEscapeKey(handler: () => void, enabled = true): void {
  useEffect(() => {
    if (!enabled) return
    function listener(event: KeyboardEvent) {
      if (event.key === 'Escape') handler()
    }
    document.addEventListener('keydown', listener)
    return () => document.removeEventListener('keydown', listener)
  }, [handler, enabled])
}

export { useEscapeKey }
