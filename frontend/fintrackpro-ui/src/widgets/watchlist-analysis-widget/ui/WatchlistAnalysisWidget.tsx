import { Link } from 'react-router'
import { useWatchlistAnalysis } from '@/entities/watched-symbol'
import { DataFreshnessBadge } from '@/shared/ui'

function formatPrice(value: number | null): string {
  if (value == null) return '—'
  return value >= 1000
    ? `$${value.toLocaleString('en-US', { maximumFractionDigits: 0 })}`
    : `$${value.toFixed(2)}`
}

function PctCell({ value }: { value: number | null }) {
  if (value == null) return <span className="font-mono text-[11px] text-gray-400 dark:text-slate-500">—</span>
  const positive = value >= 0
  return (
    <span className={`font-mono text-[11px] ${positive ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
      {`${positive ? '+' : ''}${value.toFixed(2)}%`}
    </span>
  )
}

function RsiCell({ value }: { value: number | null }) {
  if (value == null) return <span className="font-mono text-[11px] text-gray-400 dark:text-slate-500">—</span>
  const badge =
    value < 30 ? (
      <span className="ml-1 rounded px-1 text-[10px] font-bold bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400">OS</span>
    ) : value > 70 ? (
      <span className="ml-1 rounded px-1 text-[10px] font-bold bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400">OB</span>
    ) : null
  return (
    <span className="font-mono text-[11px] text-gray-600 dark:text-slate-400">
      {value.toFixed(1)}{badge}
    </span>
  )
}

const BINANCE_QUOTE_ASSETS = ['USDT', 'BUSD', 'USDC', 'USDS', 'DAI', 'BTC', 'ETH', 'BNB']

function getBinanceTradeUrl(symbol: string): string | null {
  for (const quote of BINANCE_QUOTE_ASSETS) {
    if (symbol.endsWith(quote)) {
      const base = symbol.slice(0, -quote.length)
      if (base && /^[A-Z0-9]+$/.test(base)) {
        return `https://www.binance.com/en/trade/${base}_${quote}`
      }
    }
  }
  return null
}

function BinanceLogo() {
  return (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" className="shrink-0">
      <path d="M12 1.5L6.22 5.25l2.25 1.3L12 4.22l3.53 2.33 2.25-1.3L12 1.5zM17.78 7.12L15.53 8.42l2.22 1.28v2.57l-2.22 1.28 2.25 1.3 2.22-1.28V8.4l-2.2-1.28zM6.22 7.12L4 8.4v4.17l2.22 1.28 2.25-1.3-2.22-1.28V8.42l2.22-1.3-2.25-1zM12 8.35L9.75 9.65v2.57L12 13.52l2.25-1.3V9.65L12 8.35zM6.22 14.17L4 15.45v2.15l2.22 1.28 2.25-1.3-2.22-1.28v-2.13h-.03zM17.78 14.17v2.13l-2.22 1.28 2.25 1.3 2.22-1.28v-2.15l-2.25-1.28zM12 15.53l-3.53 2.33v2.6L12 22.5l3.53-2.04v-2.6L12 15.53z" />
    </svg>
  )
}

function TradeButton({ symbol }: { symbol: string }) {
  const tradeUrl = getBinanceTradeUrl(symbol)
  if (!tradeUrl) return null
  const base = symbol.replace(new RegExp(`(${BINANCE_QUOTE_ASSETS.join('|')})$`), '')
  return (
    <a
      href={tradeUrl}
      target="_blank"
      rel="noopener noreferrer"
      className="inline-flex items-center gap-1.5 justify-center rounded-full px-2 py-0.5 text-[10px] font-medium tracking-[0.03em] border border-amber-300/60 text-amber-600 dark:border-amber-500/30 dark:text-amber-400 opacity-60 hover:opacity-100 transition-opacity duration-[120ms] no-underline whitespace-nowrap"
    >
      <BinanceLogo />
      <span className="hidden lg:inline">Trade {base}</span>
      <span className="sr-only lg:hidden">Trade {base} on Binance</span>
    </a>
  )
}

function SkeletonRow() {
  return (
    <li className="grid [grid-template-columns:minmax(100px,1fr)_90px_72px_88px_88px_88px] items-center px-4 py-[9px] border-b border-gray-50 dark:border-white/5 last:border-b-0 gap-2">
      {Array.from({ length: 6 }, (_, i) => (
        <div key={i} className="h-2.5 rounded-[3px] bg-gradient-to-r from-gray-100 via-gray-200 to-gray-100 dark:from-white/5 dark:via-white/8 dark:to-white/5 animate-pulse" />
      ))}
    </li>
  )
}

export function WatchlistAnalysisWidget() {
  const { data: items, isLoading, isFetching, dataUpdatedAt, refetch } = useWatchlistAnalysis()

  const isEmpty = !isLoading && (items?.length === 0)

  return (
    <div className="glass-card overflow-hidden">
      <div className="flex items-baseline justify-between px-4 pt-3.5 pb-2.5 border-b border-gray-100 dark:border-white/5">
        <h2 className="text-sm font-medium text-gray-700 dark:text-slate-200 m-0">My Watchlist — Analysis</h2>
        <DataFreshnessBadge
          dataUpdatedAt={dataUpdatedAt}
          isFetching={isFetching}
          onRefetch={refetch}
        />
      </div>

      {isEmpty ? (
        <div className="flex flex-col items-center gap-3 px-4 py-8 text-center">
          <p className="text-sm text-gray-500 dark:text-slate-400">
            No symbols yet — add some in Settings → Watchlist.
          </p>
          <Link
            to="/settings?tab=watchlist"
            className="text-sm text-blue-500 hover:underline"
          >
            Go to Settings →
          </Link>
        </div>
      ) : (
        <>
          <div className="overflow-x-auto">
            <div className="min-w-[640px]">
              <div className="grid [grid-template-columns:minmax(100px,1fr)_90px_72px_88px_88px_88px] px-4 py-1.5 border-b border-gray-100 dark:border-white/5 gap-2">
                <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500">Symbol</span>
                <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">Price</span>
                <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">24h%</span>
                <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">RSI Daily</span>
                <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">RSI Weekly</span>
                <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">Trade</span>
              </div>

              <ul className="list-none m-0 p-0 max-h-[360px] overflow-y-auto overscroll-contain">
                {isLoading
                  ? Array.from({ length: 3 }, (_, i) => <SkeletonRow key={i} />)
                  : items?.map((item) => (
                      <li
                        key={item.symbol}
                        className="grid [grid-template-columns:minmax(100px,1fr)_90px_72px_88px_88px_88px] items-center px-4 py-[9px] border-b border-gray-50 dark:border-white/5 last:border-b-0 gap-2"
                      >
                        <span className="font-mono text-[12px] font-semibold text-gray-800 dark:text-slate-200 tracking-[0.04em]">
                          {item.symbol}
                        </span>
                        <span className="font-mono text-[11px] text-gray-700 dark:text-slate-300 text-right">
                          {formatPrice(item.price)}
                        </span>
                        <span className="text-right">
                          <PctCell value={item.change24h} />
                        </span>
                        <span className="text-right">
                          <RsiCell value={item.rsiDaily} />
                        </span>
                        <span className="text-right">
                          <RsiCell value={item.rsiWeekly} />
                        </span>
                        <span className="flex justify-end">
                          <TradeButton symbol={item.symbol} />
                        </span>
                      </li>
                    ))}
              </ul>
            </div>
          </div>

          <div className="flex justify-end px-4 py-2 border-t border-gray-100 dark:border-white/5">
            <a
              className="text-[10px] text-gray-400 dark:text-slate-500 no-underline tracking-[0.03em] flex items-center gap-1 transition-colors duration-[120ms] hover:text-gray-500 dark:hover:text-slate-400"
              href="https://www.binance.com"
              target="_blank"
              rel="noopener noreferrer"
            >
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6M15 3h6v6M10 14L21 3"
                  stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
              via Binance
            </a>
          </div>
        </>
      )}
    </div>
  )
}
