interface DeltaBadgeProps {
  delta: number | null
  label: string
}

export function DeltaBadge({ delta, label }: DeltaBadgeProps) {
  if (delta === null) return null

  const positive = delta >= 0
  const arrow = positive ? '▲' : '▼'
  const pct = Math.abs(delta).toFixed(1)
  const color = positive
    ? 'bg-green-100 text-green-700'
    : 'bg-red-100 text-red-700'

  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${color}`}>
      {arrow} {pct}% {label}
    </span>
  )
}
