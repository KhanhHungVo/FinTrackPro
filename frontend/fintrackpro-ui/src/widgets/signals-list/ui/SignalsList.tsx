import { useSignals } from '@/entities/signal'

const signalLabels: Record<string, { label: string; color: string }> = {
  RsiOversold: { label: 'RSI Oversold', color: 'bg-red-100 text-red-700' },
  RsiOverbought: { label: 'RSI Overbought', color: 'bg-orange-100 text-orange-700' },
  VolumeSpike: { label: 'Volume Spike', color: 'bg-blue-100 text-blue-700' },
  FundingRate: { label: 'Funding Rate', color: 'bg-purple-100 text-purple-700' },
  EmaCross: { label: 'EMA Cross', color: 'bg-green-100 text-green-700' },
  BbSqueeze: { label: 'BB Squeeze', color: 'bg-yellow-100 text-yellow-700' },
}

export function SignalsList() {
  const { data, isLoading } = useSignals()

  if (isLoading) return <div className="animate-pulse h-32 rounded-lg bg-gray-100" />

  if (!data?.length) {
    return (
      <div className="rounded-lg border p-4 text-center text-gray-400 text-sm">
        No signals yet — add symbols to your watchlist.
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {data.map((signal) => {
        const meta = signalLabels[signal.signalType] ?? { label: signal.signalType, color: 'bg-gray-100' }
        return (
          <div key={signal.id} className="flex items-start gap-3 rounded-lg border p-3">
            <span className={`rounded px-2 py-0.5 text-xs font-medium ${meta.color}`}>
              {meta.label}
            </span>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium">{signal.symbol}</p>
              <p className="text-xs text-gray-500 truncate">{signal.message}</p>
            </div>
            <p className="text-xs text-gray-400 whitespace-nowrap">
              {new Date(signal.createdAt).toLocaleDateString()}
            </p>
          </div>
        )
      })}
    </div>
  )
}
