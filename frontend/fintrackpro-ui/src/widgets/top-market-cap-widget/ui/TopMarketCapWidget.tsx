import { useMarketCapCoins } from '@/entities/signal'
import { DataFreshnessBadge } from '@/shared/ui'

const COINGECKO_BASE = 'https://www.coingecko.com/en/coins'

function formatPrice(value: number | null | undefined): string {
  if (value == null) return '—'
  return value >= 1000
    ? `$${value.toLocaleString('en-US', { maximumFractionDigits: 0 })}`
    : `$${value.toFixed(2)}`
}

function formatMarketCap(value: number | null | undefined): string {
  if (value == null) return '—'
  if (value >= 1e12) return `$${(value / 1e12).toFixed(1)}T`
  if (value >= 1e9) return `$${(value / 1e9).toFixed(1)}B`
  if (value >= 1e6) return `$${(value / 1e6).toFixed(1)}M`
  return `$${value.toLocaleString()}`
}

function PctCell({ value }: { value: number | null | undefined }) {
  if (value == null) return <span className="text-gray-400 dark:text-slate-500">—</span>
  const positive = value >= 0
  return (
    <span className={positive ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}>
      {`${positive ? '+' : ''}${value.toFixed(2)}%`}
    </span>
  )
}

function SkeletonRow() {
  return (
    <li className="grid [grid-template-columns:40px_1fr_110px_110px_64px_64px_64px] items-center px-4 py-[9px] border-b border-gray-50 dark:border-white/5 last:border-b-0 gap-2">
      {Array.from({ length: 7 }, (_, i) => (
        <div key={i} className="h-2.5 rounded-[3px] bg-gradient-to-r from-gray-100 via-gray-200 to-gray-100 dark:from-white/5 dark:via-white/8 dark:to-white/5 animate-pulse" />
      ))}
    </li>
  )
}

export function TopMarketCapWidget() {
  const { data: coins, isLoading, isError, isFetching, dataUpdatedAt, refetch } = useMarketCapCoins()

  return (
    <div className="glass-card overflow-hidden">
      <div className="flex items-baseline justify-between px-4 pt-3.5 pb-2.5 border-b border-gray-100 dark:border-white/5">
        <h2 className="text-sm font-medium text-gray-700 dark:text-slate-200 m-0">Top Market Cap</h2>
        <DataFreshnessBadge
          dataUpdatedAt={dataUpdatedAt}
          isFetching={isFetching}
          onRefetch={refetch}
        />
      </div>

      {isError && !isLoading && (
        <div className="px-4 py-6 text-center text-sm text-gray-500 dark:text-slate-400">
          Data temporarily unavailable
        </div>
      )}

      {!isError && (
        <div className="overflow-x-auto">
          <div className="min-w-[640px]">
            <div className="grid [grid-template-columns:40px_1fr_110px_110px_64px_64px_64px] px-4 py-1.5 border-b border-gray-100 dark:border-white/5 gap-2">
              <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500">Rank</span>
              <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500">Name</span>
              <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">Price</span>
              <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">Mkt Cap</span>
              <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">1h%</span>
              <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">24h%</span>
              <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">7d%</span>
            </div>

            <ul className="list-none m-0 p-0">
              {isLoading
                ? Array.from({ length: 10 }, (_, i) => <SkeletonRow key={i} />)
                : coins?.map((coin) => (
                    <li key={coin.id}>
                      <a
                        className="group grid [grid-template-columns:40px_1fr_110px_110px_64px_64px_64px] items-center px-4 py-[9px] border-b border-gray-50 dark:border-white/5 last:border-b-0 no-underline text-inherit border-l-2 border-l-transparent transition-[background,border-left-color] duration-[120ms] ease-[ease] cursor-pointer hover:bg-gray-50 dark:hover:bg-white/5 hover:border-l-gray-500 gap-2"
                        href={`${COINGECKO_BASE}/${coin.id}`}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        <span className="font-mono text-[11px] text-gray-400 dark:text-slate-500">
                          #{coin.rank}
                        </span>
                        <span className="text-[13px] font-medium text-gray-900 dark:text-slate-200 whitespace-nowrap overflow-hidden text-ellipsis pr-2">
                          {coin.name}
                          <span className="ml-1.5 font-mono text-[11px] text-gray-500 dark:text-slate-400 tracking-[0.06em]">
                            {coin.symbol.toUpperCase()}
                          </span>
                        </span>
                        <span className="font-mono text-[11px] text-gray-700 dark:text-slate-300 text-right">
                          {formatPrice(coin.price)}
                        </span>
                        <span className="font-mono text-[11px] text-gray-700 dark:text-slate-300 text-right">
                          {formatMarketCap(coin.marketCap)}
                        </span>
                        <span className="font-mono text-[11px] text-right">
                          <PctCell value={coin.change1h} />
                        </span>
                        <span className="font-mono text-[11px] text-right">
                          <PctCell value={coin.change24h} />
                        </span>
                        <span className="font-mono text-[11px] text-right">
                          <PctCell value={coin.change7d} />
                        </span>
                      </a>
                    </li>
                  ))}
            </ul>
          </div>
        </div>
      )}

      <div className="flex justify-end px-4 py-2 border-t border-gray-100 dark:border-white/5">
        <a
          className="text-[10px] text-gray-400 dark:text-slate-500 no-underline tracking-[0.03em] flex items-center gap-1 transition-colors duration-[120ms] hover:text-gray-500"
          href="https://www.coingecko.com"
          target="_blank"
          rel="noopener noreferrer"
        >
          Powered by CoinGecko
        </a>
      </div>
    </div>
  )
}
