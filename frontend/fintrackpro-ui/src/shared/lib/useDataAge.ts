import { useEffect, useState } from 'react'

function computeAge(dataUpdatedAt: number): number {
  return Math.floor((Date.now() - dataUpdatedAt) / 1000)
}

export function useDataAge(dataUpdatedAt: number): number {
  const [seconds, setSeconds] = useState(() => computeAge(dataUpdatedAt))

  useEffect(() => {
    const id = setInterval(() => setSeconds(computeAge(dataUpdatedAt)), 1000)
    return () => clearInterval(id)
  }, [dataUpdatedAt])

  return seconds
}
