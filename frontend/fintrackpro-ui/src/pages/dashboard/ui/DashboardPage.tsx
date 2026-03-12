import { FearGreedWidget } from '@/widgets/fear-greed-widget'
import { SignalsList } from '@/widgets/signals-list'
import { useTransactions } from '@/entities/transaction'
import { useTrades } from '@/entities/trade'
import { useTrendingCoins } from '@/entities/signal'

export function DashboardPage() {
  const currentMonth = new Date().toISOString().slice(0, 7)
  const { data: transactions } = useTransactions(currentMonth)
  const { data: trades } = useTrades()
  const { data: trending } = useTrendingCoins()

  const income = transactions?.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0) ?? 0
  const expense = transactions?.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0) ?? 0
  const totalPnl = trades?.reduce((s, t) => s + t.result, 0) ?? 0

  return (
    <div className="mx-auto max-w-5xl p-6 space-y-6">
      <h1 className="text-2xl font-bold">Dashboard</h1>

      {/* Finance summary */}
      <div className="grid grid-cols-3 gap-4">
        <div className="rounded-lg border p-4">
          <p className="text-sm text-gray-500">Income (this month)</p>
          <p className="text-2xl font-semibold text-green-600">${income.toFixed(2)}</p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-sm text-gray-500">Expenses (this month)</p>
          <p className="text-2xl font-semibold text-red-600">${expense.toFixed(2)}</p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-sm text-gray-500">Trading P&amp;L</p>
          <p className={`text-2xl font-semibold ${totalPnl >= 0 ? 'text-green-600' : 'text-red-600'}`}>
            {totalPnl >= 0 ? '+' : ''}{totalPnl.toFixed(2)}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Fear & Greed */}
        <FearGreedWidget />

        {/* Trending coins */}
        <div className="rounded-lg border p-4">
          <p className="text-sm font-medium text-gray-700 mb-3">Trending Coins</p>
          {trending?.map((coin) => (
            <div key={coin.id} className="flex items-center justify-between py-1 border-b last:border-0">
              <span className="text-sm font-medium">{coin.name}</span>
              <span className="text-xs text-gray-500">{coin.symbol.toUpperCase()}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Signals */}
      <div>
        <h2 className="text-lg font-semibold mb-3">Recent Signals</h2>
        <SignalsList />
      </div>
    </div>
  )
}
