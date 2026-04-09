import { useTranslation } from 'react-i18next'
import { useSignals } from '@/entities/signal'

const signalColors: Record<string, string> = {
  RsiOversold: 'bg-red-100 text-red-700',
  RsiOverbought: 'bg-orange-100 text-orange-700',
  VolumeSpike: 'bg-blue-100 text-blue-700',
  FundingRate: 'bg-purple-100 text-purple-700',
  EmaCross: 'bg-green-100 text-green-700',
  BbSqueeze: 'bg-yellow-100 text-yellow-700',
}

export function SignalsList() {
  const { t } = useTranslation()
  const { data, isLoading } = useSignals()

  const signalLabels: Record<string, string> = {
    RsiOversold: t('signals.rsiOversold'),
    RsiOverbought: t('signals.rsiOverbought'),
    VolumeSpike: t('signals.volumeSpike'),
    FundingRate: t('signals.fundingRate'),
    EmaCross: t('signals.emaCross'),
    BbSqueeze: t('signals.bbSqueeze'),
  }

  if (isLoading) return <div className="animate-pulse h-32 rounded-lg bg-gray-100" />

  if (!data?.length) {
    return (
      <div className="rounded-lg border p-4 text-center text-gray-400 text-sm">
        {t('signals.noSignals')}
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {data.map((signal) => {
        const label = signalLabels[signal.signalType] ?? signal.signalType
        const color = signalColors[signal.signalType] ?? 'bg-gray-100'
        return (
          <div key={signal.id} className="flex items-start gap-3 rounded-lg border p-3">
            <span className={`rounded px-2 py-0.5 text-xs font-medium ${color}`}>
              {label}
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
