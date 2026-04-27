import { useTranslation } from 'react-i18next'
import { useTrendingCoins } from '@/entities/signal'
import { DataFreshnessBadge, RowHoverCard } from '@/shared/ui'

const COINGECKO_BASE = 'https://www.coingecko.com/en/coins'

function formatPrice(value: number | null | undefined): string {
  if (value == null) return '—'
  return value >= 1000
    ? `$${value.toLocaleString('en-US', { maximumFractionDigits: 0 })}`
    : `$${value.toFixed(2)}`
}

function formatPct(value: number | null | undefined): { text: string; positive: boolean | null } {
  if (value == null) return { text: '—', positive: null }
  const text = `${value >= 0 ? '+' : ''}${value.toFixed(2)}%`
  return { text, positive: value >= 0 }
}

function PctCell({ value }: { value: number | null | undefined }) {
  const { text, positive } = formatPct(value)
  if (positive === null) return <span className="text-muted-foreground">—</span>
  return (
    <span className={positive
      ? 'text-green-600 dark:text-green-400'
      : 'text-red-600 dark:text-red-400'}>
      {text}
    </span>
  )
}

function SkeletonRow() {
  return (
    <li className="grid [grid-template-columns:40px_1fr_100px_64px_64px_64px] items-center px-4 py-[9px] border-b border-gray-50 dark:border-white/5 last:border-b-0 gap-2">
      {Array.from({ length: 6 }, (_, i) => (
        <div key={i} className="h-2.5 rounded-[3px] bg-gradient-to-r from-gray-100 via-gray-200 to-gray-100 dark:from-white/5 dark:via-white/8 dark:to-white/5 animate-pulse" />
      ))}
    </li>
  )
}

export function TrendingCoinsWidget() {
  const { t } = useTranslation()
  const { data: coins, isLoading, isFetching, dataUpdatedAt, refetch } = useTrendingCoins()

  return (
    <div className="glass-card overflow-hidden">
      <div className="flex items-baseline justify-between px-4 pt-3.5 pb-2.5 border-b border-gray-100 dark:border-white/5">
        <h2 className="text-sm font-medium text-gray-700 dark:text-slate-200 m-0">{t('market.trendingCoins')}</h2>
        <DataFreshnessBadge
          dataUpdatedAt={dataUpdatedAt}
          isFetching={isFetching}
          onRefetch={refetch}
          label={t('market.live')}
        />
      </div>

      <div className="overflow-x-auto">
        <div className="min-w-[520px]">
          <div className="grid [grid-template-columns:40px_1fr_100px_64px_64px_64px] px-4 py-1.5 border-b border-gray-100 dark:border-white/5 gap-2">
            <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500">{t('market.rank')}</span>
            <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500">{t('market.name')}</span>
            <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">Price</span>
            <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">1h%</span>
            <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">24h%</span>
            <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">7d%</span>
          </div>

          <ul className="list-none m-0 p-0">
            {isLoading
              ? Array.from({ length: 10 }, (_, i) => <SkeletonRow key={i} />)
              : coins?.map((coin) => (
                  <li key={coin.id}>
                    <RowHoverCard
                      href={`${COINGECKO_BASE}/${coin.id}`}
                      data={{
                        name: coin.name,
                        symbol: coin.symbol,
                        rank: coin.marketCapRank,
                        price: coin.price,
                        change1h: coin.change1h,
                        change24h: coin.change24h,
                        change7d: coin.change7d,
                      }}
                      className="group grid [grid-template-columns:40px_1fr_100px_64px_64px_64px] items-center px-4 py-[9px] border-b border-gray-50 dark:border-white/5 last:border-b-0 no-underline text-inherit border-l-2 border-l-transparent transition-[background,border-left-color] duration-[120ms] ease-[ease] cursor-pointer hover:bg-gray-50 dark:hover:bg-white/5 hover:border-l-gray-500 gap-2"
                    >
                      <span className="font-mono text-[11px] text-gray-400 dark:text-slate-500 tracking-[0.02em]">
                        #{coin.marketCapRank}
                      </span>
                      <span className="flex items-center min-w-0 gap-1.5">
                        <span className="truncate text-[13px] font-medium text-gray-900 dark:text-slate-200 tracking-[0.01em]">
                          {coin.name}
                        </span>
                        <span className="font-mono text-[11px] font-medium text-gray-500 dark:text-slate-400 tracking-[0.06em] flex-shrink-0">
                          {coin.symbol.toUpperCase()}
                        </span>
                      </span>
                      <span className="font-mono text-[11px] text-gray-700 dark:text-slate-300 text-right">
                        {formatPrice(coin.price)}
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
                    </RowHoverCard>
                  </li>
                ))}
          </ul>
        </div>
      </div>

      <div className="flex justify-end px-4 py-2 border-t border-gray-100 dark:border-white/5">
        <a
          className="text-[10px] text-gray-400 dark:text-slate-500 no-underline tracking-[0.03em] flex items-center gap-1 transition-colors duration-[120ms] hover:text-gray-500"
          href="https://www.coingecko.com"
          target="_blank"
          rel="noopener noreferrer"
        >
          <svg width="11" height="11" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="1.5" />
            <path d="M12 8v4M12 16h.01" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
          </svg>
          {t('market.poweredBy')}
        </a>
      </div>
    </div>
  )
}
