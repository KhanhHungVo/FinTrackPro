import { useTranslation } from 'react-i18next'
import { useSignals } from '@/entities/signal'
import { DismissSignalButton } from '@/features/dismiss-signal'
import { TruncatedText } from '@/shared/ui'

const signalColors: Record<string, string> = {
  RsiOversold: 'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400',
  RsiOverbought: 'bg-orange-100 text-orange-700 dark:bg-orange-500/15 dark:text-orange-400',
  VolumeSpike: 'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400',
  FundingRate: 'bg-purple-100 text-purple-700 dark:bg-purple-500/15 dark:text-purple-400',
  EmaCross: 'bg-green-100 text-green-700 dark:bg-green-500/15 dark:text-green-400',
  BbSqueeze: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-500/15 dark:text-yellow-400',
}

interface SignalsListProps {
  count?: number
}

export function SignalsList({ count = 20 }: SignalsListProps) {
  const { t } = useTranslation()
  const { data, isLoading } = useSignals(count)

  const signalLabels: Record<string, string> = {
    RsiOversold: t('signals.rsiOversold'),
    RsiOverbought: t('signals.rsiOverbought'),
    VolumeSpike: t('signals.volumeSpike'),
    FundingRate: t('signals.fundingRate'),
    EmaCross: t('signals.emaCross'),
    BbSqueeze: t('signals.bbSqueeze'),
  }

  if (isLoading) return <div className="animate-pulse h-32 rounded-lg bg-gray-100 dark:bg-white/5" />

  if (!data?.length) {
    return (
      <div className="glass-card p-4 text-center text-gray-400 dark:text-slate-500 text-sm">
        {t('signals.noSignals')}
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {data.map((signal) => {
        const label = signalLabels[signal.signalType] ?? signal.signalType
        const color = signalColors[signal.signalType] ?? 'bg-gray-100 dark:bg-white/5'
        return (
          <div key={signal.id} className="flex items-start gap-3 glass-card p-3">
            <span className={`rounded px-2 py-0.5 text-xs font-medium ${color}`}>
              {label}
            </span>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium">{signal.symbol}</p>
              <TruncatedText text={signal.message} className="text-xs text-gray-500 dark:text-slate-400" as="p" />
            </div>
            <p className="text-xs text-gray-400 dark:text-slate-500 whitespace-nowrap">
              {new Date(signal.createdAt).toLocaleDateString()}
            </p>
            <DismissSignalButton signalId={signal.id} />
          </div>
        )
      })}
    </div>
  )
}
