const features = [
  {
    accentColor: '#10b981',
    iconBg: 'rgba(16,185,129,0.12)',
    icon: (
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#10b981" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M21.21 15.89A10 10 0 1 1 8 2.83"/><path d="M22 12A10 10 0 0 0 12 2v10z"/>
      </svg>
    ),
    title: 'Expense Allocation',
    desc: 'Auto-categorized transactions surface as a real-time donut chart. See exactly where every dollar goes, broken down by category and month.',
    tags: ['Auto-categorize', 'Monthly breakdown', 'Category management'],
  },
  {
    accentColor: '#3b82f6',
    iconBg: 'rgba(37,99,235,0.12)',
    icon: (
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#3b82f6" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/>
      </svg>
    ),
    title: 'Budget Health',
    desc: 'Set budgets per category. Get Telegram alerts before you overspend. Track progress with live progress bars and overrun warnings.',
    tags: ['Progress bars', 'Telegram alerts', 'Overrun detection'],
  },
  {
    accentColor: '#a855f7',
    iconBg: 'rgba(168,85,247,0.12)',
    icon: (
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#a855f7" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <polyline points="22 7 13.5 15.5 8.5 10.5 2 17"/><polyline points="16 7 22 7 22 13"/>
      </svg>
    ),
    title: 'Trading Intelligence',
    desc: 'Open positions, realized vs unrealized P&L, win rate, exposure concentration alerts — all in one live trading command panel.',
    tags: ['Unrealized P&L', 'Open positions', 'Risk signals'],
  },
]

export function FeaturesSection() {
  return (
    <section id="features" style={{ padding: '96px 0' }}>
      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', position: 'relative', zIndex: 1 }}>
        <p style={{ fontSize: 12, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#3b82f6', marginBottom: 12 }}>Features</p>
        <h2 style={{ fontSize: 'clamp(28px, 4vw, 42px)', fontWeight: 900, letterSpacing: '-1px', lineHeight: 1.15, marginBottom: 14 }}>Every tool a serious investor needs</h2>
        <p style={{ fontSize: 17, color: 'rgba(255,255,255,0.55)', maxWidth: 520, lineHeight: 1.65 }}>Three pillars. One dashboard. Full clarity.</p>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(280px, 100%), 1fr))', gap: 24, marginTop: 56 }}>
          {features.map((f, i) => (
            <div
              key={f.title}
              className={`lp-fade-up lp-fade-up-d${i + 1}`}
              style={{ padding: '32px 28px', borderRadius: 14, background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.07)', position: 'relative', overflow: 'hidden' }}
            >
              {/* Top accent bar */}
              <div style={{ position: 'absolute', top: 0, left: 0, right: 0, height: 2, background: `linear-gradient(90deg, ${f.accentColor}, transparent)` }} />
              <div style={{ width: 44, height: 44, borderRadius: 11, display: 'flex', alignItems: 'center', justifyContent: 'center', background: f.iconBg, marginBottom: 20 }}>
                {f.icon}
              </div>
              <h3 style={{ fontSize: 18, fontWeight: 800, marginBottom: 10 }}>{f.title}</h3>
              <p style={{ fontSize: 14, color: 'rgba(255,255,255,0.55)', lineHeight: 1.65, marginBottom: 20 }}>{f.desc}</p>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                {f.tags.map((tag) => (
                  <span key={tag} style={{ padding: '3px 10px', borderRadius: 99, fontSize: 11, fontWeight: 600, border: '1px solid rgba(255,255,255,0.07)', color: 'rgba(255,255,255,0.32)' }}>{tag}</span>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
