import { env } from '@/shared/config/env'

interface Props {
  onSignup: () => void
}

export function PricingSection({ onSignup }: Props) {
  const free = {
    transactions: env.FREE_TRANSACTIONS_LIMIT,
    historyDays: env.FREE_HISTORY_DAYS,
    budgets: env.FREE_BUDGETS_LIMIT,
    trades: env.FREE_TRADES_LIMIT,
    watchlist: env.FREE_WATCHLIST_LIMIT,
  }
  const pro = {
    transactions: env.PRO_TRANSACTIONS_LIMIT,
    budgets: env.PRO_BUDGETS_LIMIT,
    trades: env.PRO_TRADES_LIMIT,
    watchlist: env.PRO_WATCHLIST_LIMIT,
  }
  const amount = env.BANK_TRANSFER_AMOUNT

  return (
    <section id="pricing" style={{ padding: '96px 0', background: '#111420' }}>
      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', position: 'relative', zIndex: 1 }}>
        <p style={{ fontSize: 12, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#3b82f6', marginBottom: 12, textAlign: 'center' }}>Pricing</p>
        <h2 style={{ fontSize: 'clamp(28px, 4vw, 42px)', fontWeight: 900, letterSpacing: '-1px', lineHeight: 1.15, marginBottom: 14, textAlign: 'center' }}>Simple, honest pricing</h2>
        <p style={{ fontSize: 17, color: 'rgba(255,255,255,0.55)', maxWidth: 520, margin: '0 auto', lineHeight: 1.65, textAlign: 'center' }}>Start free. Upgrade when you're ready.</p>

        <div
          className="lp-fade-up"
          style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: 20, marginTop: 56, maxWidth: 800, marginLeft: 'auto', marginRight: 'auto' }}
        >
          {/* Free plan */}
          <div style={{ borderRadius: 14, background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.07)', padding: 32, display: 'flex', flexDirection: 'column' }}>
            <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 6 }}>Free</div>
            <div style={{ fontSize: 38, fontWeight: 900, letterSpacing: '-1.5px', marginBottom: 4 }}>$0<span style={{ fontSize: 15, fontWeight: 500, color: 'rgba(255,255,255,0.32)', letterSpacing: 0 }}>/month</span></div>
            <div style={{ fontSize: 12, color: 'rgba(255,255,255,0.32)', marginBottom: 24 }}>Always free. No credit card needed.</div>
            <div style={{ height: 1, background: 'rgba(255,255,255,0.07)', marginBottom: 24 }} />
            <ul style={{ listStyle: 'none', margin: 0, padding: 0, flex: 1, marginBottom: 28 }}>
              {[
                `${free.transactions} transactions / month`,
                `${free.historyDays}-day history`,
                `${free.budgets} budgets`,
                `${free.trades} trades stored`,
                `${free.watchlist} watchlist symbol`,
              ].map((item) => (
                <li key={item} style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 14, color: 'rgba(255,255,255,0.55)', padding: '7px 0', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                  <span style={{ color: '#10b981', fontSize: 15, flexShrink: 0 }}>✓</span> {item}
                </li>
              ))}
              {['Telegram alerts', 'Ad-free dashboard'].map((item) => (
                <li key={item} style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 14, color: 'rgba(255,255,255,0.32)', padding: '7px 0', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                  <span style={{ color: 'rgba(255,255,255,0.2)', fontSize: 15, flexShrink: 0 }}>✗</span> {item}
                </li>
              ))}
            </ul>
            <button
              onClick={onSignup}
              style={{ width: '100%', padding: 13, borderRadius: 9, border: '1px solid rgba(255,255,255,0.12)', background: 'transparent', color: 'rgba(255,255,255,0.55)', fontSize: 15, fontWeight: 600, cursor: 'pointer' }}
            >
              Get started free
            </button>
          </div>

          {/* Pro plan */}
          <div style={{ borderRadius: 14, background: 'rgba(37,99,235,0.06)', border: '1px solid #2563eb', padding: 32, display: 'flex', flexDirection: 'column', boxShadow: '0 0 40px rgba(37,99,235,0.1)' }}>
            <div style={{ display: 'inline-block', padding: '3px 10px', borderRadius: 99, background: 'rgba(37,99,235,0.2)', color: '#3b82f6', fontSize: 11, fontWeight: 700, marginBottom: 16, letterSpacing: '0.04em', textTransform: 'uppercase', width: 'fit-content' }}>Most Popular</div>
            <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 6 }}>Pro</div>
            <div style={{ fontSize: 38, fontWeight: 900, letterSpacing: '-1.5px', marginBottom: 4 }}>$4<span style={{ fontSize: 15, fontWeight: 500, color: 'rgba(255,255,255,0.32)', letterSpacing: 0 }}>/month</span></div>
            <div style={{ fontSize: 12, color: 'rgba(255,255,255,0.32)', marginBottom: 24 }}>≈ ₫{Number(amount).toLocaleString()} · Bank transfer available</div>
            <div style={{ height: 1, background: 'rgba(255,255,255,0.07)', marginBottom: 24 }} />
            <ul style={{ listStyle: 'none', margin: 0, padding: 0, flex: 1, marginBottom: 28 }}>
              {[
                `${pro.transactions} transactions / month`,
                '1-year history',
                `${pro.budgets} budgets`,
                `${pro.trades} trades stored`,
                `${pro.watchlist} watchlist symbols`,
                'Telegram alerts',
                'Ad-free dashboard',
              ].map((item) => (
                <li key={item} style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 14, color: 'rgba(255,255,255,0.55)', padding: '7px 0', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                  <span style={{ color: '#10b981', fontSize: 15, flexShrink: 0 }}>✓</span> {item}
                </li>
              ))}
            </ul>
            <button
              onClick={onSignup}
              style={{ width: '100%', padding: 13, borderRadius: 9, background: '#2563eb', color: '#fff', fontSize: 15, fontWeight: 700, border: 'none', cursor: 'pointer' }}
            >
              Upgrade to Pro
            </button>
          </div>
        </div>

        <p style={{ textAlign: 'center', marginTop: 24, fontSize: 14, color: 'rgba(255,255,255,0.32)' }}>
          Already have an account?{' '}
          <a href="/pricing" style={{ color: '#3b82f6', fontWeight: 600, textDecoration: 'none' }}>View full pricing →</a>
        </p>
      </div>
    </section>
  )
}
