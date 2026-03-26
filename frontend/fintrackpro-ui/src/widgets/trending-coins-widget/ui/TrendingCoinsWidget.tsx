import { useTrendingCoins } from '@/entities/signal'

const COINGECKO_BASE = 'https://www.coingecko.com/en/coins'

const styles = `
  @import url('https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500&display=swap');

  .tcw-root {
    background: #ffffff;
    border-radius: 8px;
    border: 1px solid #e5e7eb;
    overflow: hidden;
    font-family: system-ui, sans-serif;
  }

  .tcw-header {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    padding: 14px 16px 10px;
    border-bottom: 1px solid #f3f4f6;
  }

  .tcw-title {
    font-size: 14px;
    font-weight: 500;
    color: #374151;
    margin: 0;
  }

  .tcw-badge {
    font-family: 'JetBrains Mono', monospace;
    font-size: 9px;
    color: #6b7280;
    background: #f9fafb;
    border: 1px solid #e5e7eb;
    border-radius: 4px;
    padding: 2px 6px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
  }

  .tcw-col-headers {
    display: grid;
    grid-template-columns: 48px 1fr 68px 24px;
    padding: 6px 16px 5px;
    border-bottom: 1px solid #f3f4f6;
  }

  .tcw-col-label {
    font-family: 'JetBrains Mono', monospace;
    font-size: 9px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: #9ca3af;
  }

  .tcw-col-label-right {
    text-align: right;
  }

  .tcw-list {
    list-style: none;
    margin: 0;
    padding: 0;
  }

  .tcw-row {
    display: grid;
    grid-template-columns: 48px 1fr 68px 24px;
    align-items: center;
    padding: 9px 16px;
    border-bottom: 1px solid #f9fafb;
    text-decoration: none;
    color: inherit;
    border-left: 2px solid transparent;
    transition: background 0.12s ease, border-left-color 0.12s ease;
    cursor: pointer;
  }

  .tcw-row:last-child {
    border-bottom: none;
  }

  .tcw-row:hover {
    background: #f9fafb;
    border-left-color: #6b7280;
  }

  .tcw-row:hover .tcw-link-icon {
    opacity: 1;
  }

  .tcw-rank {
    font-family: 'JetBrains Mono', monospace;
    font-size: 11px;
    color: #9ca3af;
    letter-spacing: 0.02em;
  }

  .tcw-name {
    font-size: 13px;
    font-weight: 500;
    color: #111827;
    letter-spacing: 0.01em;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    padding-right: 8px;
  }

  .tcw-symbol {
    font-family: 'JetBrains Mono', monospace;
    font-size: 11px;
    font-weight: 500;
    color: #6b7280;
    letter-spacing: 0.06em;
    text-align: right;
  }

  .tcw-link-icon {
    display: flex;
    justify-content: flex-end;
    opacity: 0;
    transition: opacity 0.12s ease;
  }

  .tcw-skeleton-row {
    display: grid;
    grid-template-columns: 48px 1fr 68px 24px;
    align-items: center;
    padding: 9px 16px;
    border-bottom: 1px solid #f9fafb;
    gap: 0;
  }

  .tcw-skeleton-row:last-child {
    border-bottom: none;
  }

  @keyframes tcw-shimmer {
    0%   { background-position: -400px 0; }
    100% { background-position: 400px 0; }
  }

  .tcw-skel {
    height: 10px;
    border-radius: 3px;
    background: linear-gradient(90deg, #f3f4f6 25%, #e5e7eb 50%, #f3f4f6 75%);
    background-size: 800px 100%;
    animation: tcw-shimmer 1.4s infinite linear;
  }

  .tcw-skel-rank  { width: 28px; }
  .tcw-skel-name  { width: 60%; }
  .tcw-skel-sym   { width: 36px; margin-left: auto; }

  .tcw-footer {
    display: flex;
    justify-content: flex-end;
    padding: 8px 16px;
    border-top: 1px solid #f3f4f6;
  }

  .tcw-attribution {
    font-size: 10px;
    color: #9ca3af;
    text-decoration: none;
    letter-spacing: 0.03em;
    display: flex;
    align-items: center;
    gap: 4px;
    transition: color 0.12s ease;
  }

  .tcw-attribution:hover {
    color: #6b7280;
  }
`

function SkeletonRow() {
  return (
    <li className="tcw-skeleton-row">
      <div className="tcw-skel tcw-skel-rank" />
      <div className="tcw-skel tcw-skel-name" />
      <div className="tcw-skel tcw-skel-sym" />
      <div />
    </li>
  )
}

export function TrendingCoinsWidget() {
  const { data: coins, isLoading } = useTrendingCoins()

  return (
    <>
      <style>{styles}</style>
      <div className="tcw-root">
        <div className="tcw-header">
          <h2 className="tcw-title">Trending Coins</h2>
          <span className="tcw-badge">Live</span>
        </div>

        <div className="tcw-col-headers">
          <span className="tcw-col-label">Rank</span>
          <span className="tcw-col-label">Name</span>
          <span className="tcw-col-label tcw-col-label-right">Symbol</span>
          <span />
        </div>

        <ul className="tcw-list">
          {isLoading
            ? Array.from({ length: 7 }, (_, i) => <SkeletonRow key={i} />)
            : coins?.map((coin) => (
                <li key={coin.id}>
                  <a
                    className="tcw-row"
                    href={`${COINGECKO_BASE}/${coin.id}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    title={`View ${coin.name} on CoinGecko`}
                  >
                    <span className="tcw-rank">#{coin.marketCapRank}</span>
                    <span className="tcw-name">{coin.name}</span>
                    <span className="tcw-symbol">{coin.symbol.toUpperCase()}</span>
                    <span className="tcw-link-icon">
                      <svg width="11" height="11" viewBox="0 0 12 12" fill="none" aria-hidden="true">
                        <path
                          d="M2 10L10 2M10 2H5M10 2V7"
                          stroke="#9ca3af"
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

        <div className="tcw-footer">
          <a
            className="tcw-attribution"
            href="https://www.coingecko.com"
            target="_blank"
            rel="noopener noreferrer"
          >
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <circle cx="12" cy="12" r="10" stroke="#9ca3af" strokeWidth="1.5" />
              <path d="M12 8v4M12 16h.01" stroke="#9ca3af" strokeWidth="1.5" strokeLinecap="round" />
            </svg>
            Powered by CoinGecko
          </a>
        </div>
      </div>
    </>
  )
}
