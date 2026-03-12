import { useFearGreed } from '@/entities/signal'

const labelColors: Record<string, string> = {
  'Extreme Fear': 'text-red-700',
  'Fear': 'text-red-400',
  'Neutral': 'text-yellow-500',
  'Greed': 'text-green-500',
  'Extreme Greed': 'text-green-700',
}

export function FearGreedWidget() {
  const { data, isLoading } = useFearGreed()

  if (isLoading) return <div className="animate-pulse h-24 rounded-lg bg-gray-100" />
  if (!data) return null

  const color = labelColors[data.label] ?? 'text-gray-500'

  return (
    <div className="rounded-lg border p-4 text-center">
      <p className="text-sm text-gray-500">Fear & Greed Index</p>
      <p className={`text-5xl font-bold mt-2 ${color}`}>{data.value}</p>
      <p className={`text-sm font-medium mt-1 ${color}`}>{data.label}</p>
    </div>
  )
}
