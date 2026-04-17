interface RiskSignalsProps {
  signals: { message: string }[]
}

export function RiskSignals({ signals }: RiskSignalsProps) {
  if (signals.length === 0) return null
  return (
    <ul className="space-y-1">
      {signals.slice(0, 2).map((s, i) => (
        <li key={i} className="flex items-start gap-2 text-xs text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-500/10 rounded-lg px-3 py-2">
          <span className="flex-shrink-0">⚠</span>
          <span>{s.message}</span>
        </li>
      ))}
    </ul>
  )
}
