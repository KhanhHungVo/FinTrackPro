/** Returns % change from prev to curr. Returns null when prev === 0. */
export function calcDelta(curr: number, prev: number): number | null {
  if (prev === 0) return null
  return ((curr - prev) / Math.abs(prev)) * 100
}
