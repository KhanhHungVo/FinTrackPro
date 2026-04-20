const cards = [
  {
    iconBg: 'rgba(239,68,68,0.1)',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#ef4444" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/>
        <line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/>
      </svg>
    ),
    title: 'Spreadsheets break. Memory lies.',
    body: 'Stop juggling tabs. Every transaction is captured and auto-categorized so nothing slips through the cracks.',
  },
  {
    iconBg: 'rgba(245,158,11,0.1)',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#f59e0b" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
      </svg>
    ),
    title: 'Budgets you set and forget.',
    body: 'Get notified the moment your budget runs over — before it\'s too late to course-correct.',
  },
  {
    iconBg: 'rgba(168,85,247,0.1)',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#a855f7" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <polyline points="22 7 13.5 15.5 8.5 10.5 2 17"/><polyline points="16 7 22 7 22 13"/>
      </svg>
    ),
    title: 'Trading blind.',
    body: 'See your unrealized P&L and full portfolio allocation at a glance. Know exactly where you stand, right now.',
  },
]

export function PainPointsSection() {
  return (
    <section style={{ padding: '72px 0' }}>
      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', position: 'relative', zIndex: 1 }}>
        <div
          className="lp-fade-up"
          style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 20 }}
        >
          {cards.map((c) => (
            <div
              key={c.title}
              style={{
                padding: 28, borderRadius: 14,
                background: 'rgba(255,255,255,0.04)',
                border: '1px solid rgba(255,255,255,0.07)',
              }}
            >
              <div style={{
                width: 40, height: 40, borderRadius: 10,
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                background: c.iconBg, marginBottom: 16,
              }}>
                {c.icon}
              </div>
              <h3 style={{ fontSize: 16, fontWeight: 800, marginBottom: 8 }}>{c.title}</h3>
              <p style={{ fontSize: 14, color: 'rgba(255,255,255,0.55)', lineHeight: 1.6, margin: 0 }}
                dangerouslySetInnerHTML={{ __html: c.body }}
              />
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
