import { useMobile } from '../../lib/useMobile'

const KPI_CARDS = [
  { label: 'Income · This Month', val: '+$4,280', color: '#10b981' },
  { label: 'Expenses · This Month', val: '−$1,840', color: '#ef4444' },
  { label: 'Trading P&L', val: '+$362', color: '#10b981' },
  { label: 'Unrealized P&L', val: '+$987', color: '#10b981' },
]

export function DashboardMockupSection() {
  const isMobile = useMobile()

  return (
    <section id="mockup" style={{ padding: '80px 0', textAlign: 'center' }}>
      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', position: 'relative', zIndex: 1 }}>
        <p style={{ fontSize: 12, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#3b82f6', marginBottom: 12 }}>
          Product
        </p>
        <h2 style={{ fontSize: 'clamp(28px, 4vw, 42px)', fontWeight: 900, letterSpacing: '-1px', lineHeight: 1.15, marginBottom: 14 }}>
          Your complete financial command center
        </h2>
        <p style={{ fontSize: 17, color: 'rgba(255,255,255,0.55)', maxWidth: 520, margin: '0 auto 48px', lineHeight: 1.65 }}>
          Everything — expenses, budgets, trades — unified in one real-time dashboard.
        </p>

        {/* Mobile KPI summary */}
        {isMobile && (
          <div className="lp-fade-up">
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 20 }}>
              {KPI_CARDS.map((k) => (
                <div key={k.label} style={{
                  borderRadius: 12, background: 'rgba(255,255,255,0.04)',
                  border: '1px solid rgba(255,255,255,0.08)',
                  borderLeft: `3px solid ${k.color}`,
                  padding: '16px 14px', textAlign: 'left',
                }}>
                  <div style={{ fontSize: 11, color: 'rgba(255,255,255,0.45)', marginBottom: 6, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>{k.label}</div>
                  <div style={{ fontSize: 20, fontWeight: 800, color: k.color, letterSpacing: '-0.5px' }}>{k.val}</div>
                </div>
              ))}
            </div>
            <a href="#how" style={{ fontSize: 14, fontWeight: 600, color: '#3b82f6', textDecoration: 'none' }}>
              See full dashboard →
            </a>
          </div>
        )}

        {/* Browser frame — desktop only */}
        {!isMobile && <div
          className="lp-fade-up"
          style={{
            borderRadius: 14, border: '1px solid rgba(255,255,255,0.12)',
            overflow: 'hidden',
            boxShadow: '0 0 0 1px rgba(255,255,255,0.04), 0 32px 80px rgba(0,0,0,0.7), 0 0 120px rgba(37,99,235,0.12)',
            background: '#0c0e14', maxWidth: 960, margin: '0 auto',
          }}
        >
          {/* Browser chrome */}
          <div style={{ background: '#161a25', borderBottom: '1px solid rgba(255,255,255,0.08)', padding: '10px 14px', display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{ display: 'flex', gap: 6 }}>
              <span style={{ width: 11, height: 11, borderRadius: '50%', background: '#ff5f57', display: 'inline-block' }} />
              <span style={{ width: 11, height: 11, borderRadius: '50%', background: '#febc2e', display: 'inline-block' }} />
              <span style={{ width: 11, height: 11, borderRadius: '50%', background: '#28c840', display: 'inline-block' }} />
            </div>
            <div style={{
              flex: 1, maxWidth: 360, margin: '0 auto',
              background: 'rgba(255,255,255,0.06)', border: '1px solid rgba(255,255,255,0.08)',
              borderRadius: 6, padding: '5px 12px', fontSize: 11, color: 'rgba(255,255,255,0.32)', textAlign: 'center',
            }}>
              app.fintrackpro.io/dashboard
            </div>
          </div>

          {/* Dashboard content */}
          <div style={{ fontSize: 11, background: '#0c0e14', color: 'rgba(255,255,255,0.85)', textAlign: 'left' }}>
            {/* Dash nav */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 16px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
              <span style={{ fontSize: 13, fontWeight: 700, letterSpacing: '-0.3px' }}>FinTrackPro</span>
              <div style={{ display: 'flex', gap: 3 }}>
                {['Dashboard', 'Transactions', 'Budgets', 'Trades'].map((l, i) => (
                  <span key={l} style={{ padding: '3px 9px', borderRadius: 5, fontSize: 10.5, fontWeight: 500, background: i === 0 ? '#2563eb' : 'transparent', color: i === 0 ? '#fff' : 'rgba(255,255,255,0.45)' }}>{l}</span>
                ))}
              </div>
              <div style={{ width: 26, height: 26, borderRadius: '50%', background: '#2563eb', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 700 }}>V</div>
            </div>

            <div style={{ padding: 16 }}>
              <div style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>Good morning, Alex ☀️</div>
              <div style={{ fontSize: 9.5, color: 'rgba(255,255,255,0.38)', marginBottom: 14 }}>Saturday, May 10</div>

              {/* KPI row */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 8, marginBottom: 12 }}>
                {[
                  { label: 'Income · This Month', val: '+$4,280.00', color: '#10b981' },
                  { label: 'Expenses · This Month', val: '−$1,840.50', color: '#ef4444' },
                  { label: 'Trading P&L · This Month', val: '+$362.40', color: '#3b82f6', valColor: '#10b981' },
                  { label: 'Unrealized P&L', val: '+$987.20', color: '#a855f7', valColor: '#10b981', sub: '3 open position(s)' },
                ].map((k) => (
                  <div key={k.label} style={{ borderRadius: 10, background: 'rgba(255,255,255,0.04)', borderLeft: `3px solid ${k.color}`, padding: '10px 12px' }}>
                    <div style={{ fontSize: 8, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.06em', color: k.color, marginBottom: 3 }}>{k.label}</div>
                    <div style={{ fontSize: 14, fontWeight: 800, letterSpacing: '-0.5px', color: k.valColor ?? k.color }}>{k.val}</div>
                    {k.sub && <div style={{ fontSize: 8, color: 'rgba(255,255,255,0.35)', marginTop: 2 }}>{k.sub}</div>}
                  </div>
                ))}
              </div>

              {/* Widgets row */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 12 }}>
                {/* Expense allocation */}
                <div style={{ borderRadius: 10, background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.06)', padding: 12 }}>
                  <div style={{ fontSize: 8.5, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(255,255,255,0.45)', marginBottom: 10 }}>Expense Allocation · This Month</div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                    <svg width="72" height="72" viewBox="0 0 72 72" style={{ flexShrink: 0 }}>
                      <circle cx="36" cy="36" r="26" fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth="12"/>
                      <circle cx="36" cy="36" r="26" fill="none" stroke="#f97316" strokeWidth="12" strokeDasharray="59.4 103.7" strokeDashoffset="0" transform="rotate(-90 36 36)"/>
                      <circle cx="36" cy="36" r="26" fill="none" stroke="#06b6d4" strokeWidth="12" strokeDasharray="49.4 113.7" strokeDashoffset="-59.4" transform="rotate(-90 36 36)"/>
                      <circle cx="36" cy="36" r="26" fill="none" stroke="#ef4444" strokeWidth="12" strokeDasharray="21.4 141.7" strokeDashoffset="-108.8" transform="rotate(-90 36 36)"/>
                      <circle cx="36" cy="36" r="26" fill="none" stroke="#eab308" strokeWidth="12" strokeDasharray="6.6 156.5" strokeDashoffset="-130.2" transform="rotate(-90 36 36)"/>
                      <text x="36" y="39" textAnchor="middle" fill="rgba(255,255,255,0.7)" fontSize="6" fontFamily="Inter" fontWeight="700">$1,840</text>
                    </svg>
                    <div style={{ flex: 1 }}>
                      {[
                        { color: '#f97316', label: 'Housing', val: '$980.00' },
                        { color: '#ef4444', label: 'Food & Dining', val: '$425.00' },
                        { color: '#06b6d4', label: 'Transport', val: '$172.00' },
                        { color: '#10b981', label: 'Health', val: '$115.00' },
                        { color: '#eab308', label: 'Entertainment', val: '$94.00' },
                      ].map((item) => (
                        <div key={item.label} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 5 }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: 8.5 }}>
                            <span style={{ width: 7, height: 7, borderRadius: 2, background: item.color, display: 'inline-block', flexShrink: 0 }} />
                            {item.label}
                          </div>
                          <span style={{ fontSize: 8, color: 'rgba(255,255,255,0.5)' }}>{item.val}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Budget health */}
                <div style={{ borderRadius: 10, background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.06)', padding: 12 }}>
                  <div style={{ fontSize: 8.5, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(255,255,255,0.45)', marginBottom: 10, display: 'flex', justifyContent: 'space-between' }}>
                    <span>Budget Health</span><span style={{ color: 'rgba(255,255,255,0.35)' }}>2 of 3 on track</span>
                  </div>
                  {[
                    { label: 'Food & Dining', pct: 87, used: '$425.00', limit: '$500.00', color: '#10b981' },
                    { label: 'Transport', pct: 84, used: '$172.00', limit: '$205.00', color: '#10b981' },
                    { label: 'Entertainment', pct: 115, used: '$94.00', limit: '$80.00', color: '#ef4444', over: true },
                  ].map((b) => (
                    <div key={b.label} style={{ marginBottom: 8 }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 8.5, marginBottom: 4 }}>
                        <span>{b.label}</span>
                        <span style={{ color: b.color, fontWeight: b.over ? 700 : 400 }}>{b.pct}%</span>
                      </div>
                      <div style={{ height: 5, borderRadius: 3, background: 'rgba(255,255,255,0.08)', overflow: 'hidden' }}>
                        <div style={{ width: `${Math.min(b.pct, 100)}%`, height: '100%', background: b.color, borderRadius: 3 }} />
                      </div>
                      <div style={{ fontSize: 7.5, color: b.over ? b.color : 'rgba(255,255,255,0.35)', marginTop: 2, textAlign: 'right' }}>
                        {b.used} / {b.limit}{b.over ? ' — over budget' : ''}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Trading intelligence */}
              <div style={{ borderRadius: 10, background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.06)', padding: 12, marginBottom: 12 }}>
                <div style={{ fontSize: 8.5, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(255,255,255,0.45)', marginBottom: 8 }}>Trading Intelligence</div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 8 }}>
                  <div style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.05)', borderRadius: 8, padding: 9 }}>
                    <div style={{ fontSize: 7.5, color: 'rgba(255,255,255,0.38)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 2 }}>Unrealized P&L</div>
                    <div style={{ fontSize: 13, fontWeight: 800, color: '#10b981' }}>+$987.20</div>
                  </div>
                  <div style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.05)', borderRadius: 8, padding: 9 }}>
                    <div style={{ fontSize: 7.5, color: 'rgba(255,255,255,0.38)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 2 }}>Open Positions</div>
                    <div style={{ fontSize: 13, fontWeight: 800 }}>3</div>
                  </div>
                </div>
                {[
                  { sym: 'VNM', dir: 'Long', pnl: '+$689.00 · +12.4%', alloc: '72% of portfolio' },
                  { sym: 'BTC', dir: 'Long', pnl: '+$183.00 · +6.1%', alloc: '19% of portfolio' },
                  { sym: 'HPG', dir: 'Short', pnl: '+$115.20 · +3.8%', alloc: '9% of portfolio' },
                ].map((pos) => (
                  <div key={pos.sym} style={{ padding: '7px 8px', background: 'rgba(255,255,255,0.03)', borderRadius: 7, display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 5 }}>
                    <div>
                      <div style={{ fontSize: 9, fontWeight: 700 }}>{pos.sym}</div>
                      <div style={{ fontSize: 8, color: 'rgba(255,255,255,0.35)' }}>{pos.dir} · Open</div>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                      <div style={{ fontSize: 9, color: '#10b981' }}>{pos.pnl}</div>
                      <div style={{ fontSize: 8, color: 'rgba(255,255,255,0.35)' }}>{pos.alloc}</div>
                    </div>
                  </div>
                ))}
              </div>

              {/* Recent activity */}
              <div style={{ borderRadius: 10, background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.06)', padding: 12 }}>
                <div style={{ fontSize: 8.5, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(255,255,255,0.45)', marginBottom: 8 }}>Recent Activity</div>
                {[
                  { dot: '#10b981', name: 'Monthly Salary', type: 'Income', amount: '+$4,280.00', amtColor: '#10b981' },
                  { dot: '#f97316', name: 'Housing', type: 'Expense', amount: '−$980.00', amtColor: '#ef4444' },
                  { dot: '#ef4444', name: 'Food & Dining', type: 'Expense', amount: '−$34.50', amtColor: '#ef4444' },
                  { dot: '#3b82f6', name: 'BTC', type: 'Long · Closed', amount: '+$503.00', amtColor: '#10b981' },
                  { dot: '#3b82f6', name: 'HPG', type: 'Short · Closed', amount: '−$141.00', amtColor: '#ef4444' },
                ].map((item, i) => (
                  <div key={i} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '7px 0', borderBottom: i < 4 ? '1px solid rgba(255,255,255,0.04)' : 'none' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
                      <span style={{ width: 6, height: 6, borderRadius: '50%', background: item.dot, display: 'inline-block', flexShrink: 0 }} />
                      <div>
                        <div style={{ fontSize: 9, fontWeight: 600 }}>{item.name}</div>
                        <div style={{ fontSize: 8, color: 'rgba(255,255,255,0.35)' }}>{item.type}</div>
                      </div>
                    </div>
                    <span style={{ fontSize: 9.5, fontWeight: 700, color: item.amtColor }}>{item.amount}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>}
      </div>
    </section>
  )
}
