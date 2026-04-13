import { useTranslation } from 'react-i18next'
import { useTrendingCoins } from '@/entities/signal'

const COINGECKO_BASE = 'https://www.coingecko.com/en/coins'

function SkeletonRow() {
  return (
    <li className="grid [grid-template-columns:48px_1fr_68px_24px] items-center px-4 py-[9px] border-b border-gray-50 dark:border-white/5 last:border-b-0">
      <div className="h-2.5 w-7 rounded-[3px] bg-gradient-to-r from-gray-100 via-gray-200 to-gray-100 dark:from-white/5 dark:via-white/8 dark:to-white/5 bg-[length:800px_100%] animate-[tcw-shimmer_1.4s_infinite_linear]" />
      <div className="h-2.5 w-[60%] rounded-[3px] bg-gradient-to-r from-gray-100 via-gray-200 to-gray-100 dark:from-white/5 dark:via-white/8 dark:to-white/5 bg-[length:800px_100%] animate-[tcw-shimmer_1.4s_infinite_linear]" />
      <div className="h-2.5 w-9 ml-auto rounded-[3px] bg-gradient-to-r from-gray-100 via-gray-200 to-gray-100 dark:from-white/5 dark:via-white/8 dark:to-white/5 bg-[length:800px_100%] animate-[tcw-shimmer_1.4s_infinite_linear]" />
      <div />
    </li>
  )
}

export function TrendingCoinsWidget() {
  const { t } = useTranslation()
  const { data: coins, isLoading } = useTrendingCoins()

  return (
    <div className="glass-card overflow-hidden">
      <div className="flex items-baseline justify-between px-4 pt-3.5 pb-2.5 border-b border-gray-100 dark:border-white/5">
        <h2 className="text-sm font-medium text-gray-700 dark:text-slate-200 m-0">{t('market.trendingCoins')}</h2>
        <span className="font-mono text-[9px] text-gray-500 bg-gray-50 border border-gray-200 rounded px-1.5 py-0.5 tracking-[0.08em] uppercase dark:bg-green-500/10 dark:border-green-500/20 dark:text-green-400">
          {t('market.live')}
        </span>
      </div>

      <div className="grid [grid-template-columns:48px_1fr_68px_24px] px-4 py-1.5 border-b border-gray-100 dark:border-white/5">
        <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500">{t('market.rank')}</span>
        <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500">{t('market.name')}</span>
        <span className="font-mono text-[9px] tracking-[0.08em] uppercase text-gray-400 dark:text-slate-500 text-right">{t('trades.symbol')}</span>
        <span />
      </div>

      <ul className="list-none m-0 p-0">
        {isLoading
          ? Array.from({ length: 7 }, (_, i) => <SkeletonRow key={i} />)
          : coins?.map((coin) => (
              <li key={coin.id}>
                <a
                  className="group grid [grid-template-columns:48px_1fr_68px_24px] items-center px-4 py-[9px] border-b border-gray-50 dark:border-white/5 last:border-b-0 no-underline text-inherit border-l-2 border-l-transparent transition-[background,border-left-color] duration-[120ms] ease-[ease] cursor-pointer hover:bg-gray-50 dark:hover:bg-white/5 hover:border-l-gray-500"
                  href={`${COINGECKO_BASE}/${coin.id}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  title={`${coin.name} — CoinGecko`}
                >
                  <span className="font-mono text-[11px] text-gray-400 dark:text-slate-500 tracking-[0.02em]">
                    #{coin.marketCapRank}
                  </span>
                  <span className="text-[13px] font-medium text-gray-900 dark:text-slate-200 tracking-[0.01em] whitespace-nowrap overflow-hidden text-ellipsis pr-2">
                    {coin.name}
                  </span>
                  <span className="font-mono text-[11px] font-medium text-gray-500 dark:text-slate-400 tracking-[0.06em] text-right">
                    {coin.symbol.toUpperCase()}
                  </span>
                  <span className="flex justify-end opacity-0 group-hover:opacity-100 transition-opacity duration-[120ms]">
                    <svg width="11" height="11" viewBox="0 0 12 12" fill="none" aria-hidden="true">
                      <path
                        d="M2 10L10 2M10 2H5M10 2V7"
                        stroke="currentColor"
                        className="text-gray-400"
                        strokeWidth="1.5"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      />
                    </svg>
                  </span>
                </a>
              </li>
            ))}
      </ul>

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
