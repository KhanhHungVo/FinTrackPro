export function OutcomeSpotlightsSection() {
  return (
    <section style={{ padding: '72px 0 0' }}>
      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', position: 'relative', zIndex: 1 }}>
        <p style={{ fontSize: 12, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#3b82f6', marginBottom: 12, textAlign: 'center' }}>Results</p>
        <h2 className="lp-fade-up" style={{ fontSize: 'clamp(28px, 4vw, 42px)', fontWeight: 900, letterSpacing: '-1px', lineHeight: 1.15, marginBottom: 14, textAlign: 'center' }}>
          What clarity actually looks like
        </h2>
        <p className="lp-fade-up lp-fade-up-d1" style={{ fontSize: 17, color: 'rgba(255,255,255,0.55)', maxWidth: 520, margin: '0 auto', lineHeight: 1.65, textAlign: 'center' }}>
          Real outcomes — not just features.
        </p>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(280px, 100%), 1fr))', gap: 20, marginTop: 52 }}>

          {/* Spending clarity */}
          <div className="lp-fade-up lp-fade-up-d1" style={{ borderRadius: 18, border: '1px solid rgba(255,255,255,0.07)', padding: '32px 28px', display: 'flex', flexDirection: 'column', position: 'relative', overflow: 'hidden', background: 'rgba(16,185,129,0.04)' }}>
            <div style={{ position: 'absolute', inset: 0, borderRadius: 18, opacity: 0.6, pointerEvents: 'none', background: 'radial-gradient(ellipse 220px 140px at 50% -10px, rgba(16,185,129,0.16), transparent)' }} />
            <div style={{ fontSize: 10, fontWeight: 600, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#10b981', marginBottom: 20, position: 'relative', zIndex: 1 }}>Spending Clarity</div>
            <div style={{ fontFamily: "'JetBrains Mono', monospace", fontSize: 'clamp(30px, 3.5vw, 46px)', fontWeight: 700, letterSpacing: '-1px', lineHeight: 1, marginBottom: 10, color: '#10b981', position: 'relative', zIndex: 1 }}>$3,750</div>
            <p style={{ fontSize: 15, color: 'rgba(255,255,255,0.55)', lineHeight: 1.6, marginBottom: 24, flex: 1, position: 'relative', zIndex: 1 }}>in untracked spending surfaced automatically — in the first week.</p>
            <div style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 10, padding: 14, fontSize: 11, position: 'relative', zIndex: 1 }}>
              <div style={{ fontSize: 9, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(255,255,255,0.3)', marginBottom: 10 }}>Expense breakdown · May</div>
              <div style={{ padding: '7px 9px', background: 'rgba(16,185,129,0.08)', border: '1px solid rgba(16,185,129,0.18)', borderRadius: 6, color: '#10b981', fontSize: 9, fontWeight: 600 }}>↑ $3,750 more than last month identified</div>
            </div>
          </div>

          {/* Budget control */}
          <div className="lp-fade-up lp-fade-up-d2" style={{ borderRadius: 18, border: '1px solid rgba(255,255,255,0.07)', padding: '32px 28px', display: 'flex', flexDirection: 'column', position: 'relative', overflow: 'hidden', background: 'rgba(245,158,11,0.04)' }}>
            <div style={{ position: 'absolute', inset: 0, borderRadius: 18, opacity: 0.6, pointerEvents: 'none', background: 'radial-gradient(ellipse 220px 140px at 50% -10px, rgba(245,158,11,0.16), transparent)' }} />
            <div style={{ fontSize: 10, fontWeight: 600, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#f59e0b', marginBottom: 20, position: 'relative', zIndex: 1 }}>Budget Control</div>
            <div style={{ fontFamily: "'JetBrains Mono', monospace", fontSize: 'clamp(30px, 3.5vw, 46px)', fontWeight: 700, letterSpacing: '-1px', lineHeight: 1, marginBottom: 10, color: '#f59e0b', position: 'relative', zIndex: 1 }}>3 alerts</div>
            <p style={{ fontSize: 15, color: 'rgba(255,255,255,0.55)', lineHeight: 1.6, marginBottom: 24, flex: 1, position: 'relative', zIndex: 1 }}>sent before the month ended — overspend caught and corrected in time.</p>
            <div style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 10, padding: 14, fontSize: 11, position: 'relative', zIndex: 1 }}>
              <div style={{ fontSize: 9, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(255,255,255,0.3)', marginBottom: 10 }}>Budget health · May</div>
              <div style={{ marginBottom: 9 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 9, marginBottom: 4 }}><span>Entertainment</span><span style={{ color: '#ef4444', fontWeight: 700 }}>115%</span></div>
                <div style={{ height: 5, borderRadius: 3, background: 'rgba(255,255,255,0.08)', overflow: 'hidden' }}><div style={{ width: '100%', height: '100%', background: '#ef4444', borderRadius: 3 }} /></div>
              </div>
              <div style={{ padding: '7px 9px', background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.2)', borderRadius: 6, color: '#f59e0b', fontSize: 9, fontWeight: 600 }}>⚡ Alert sent · Entertainment over by $14.00</div>
            </div>
          </div>

          {/* Trade edge */}
          <div className="lp-fade-up lp-fade-up-d3" style={{ borderRadius: 18, border: '1px solid rgba(255,255,255,0.07)', padding: '32px 28px', display: 'flex', flexDirection: 'column', position: 'relative', overflow: 'hidden', background: 'rgba(168,85,247,0.04)' }}>
            <div style={{ position: 'absolute', inset: 0, borderRadius: 18, opacity: 0.6, pointerEvents: 'none', background: 'radial-gradient(ellipse 220px 140px at 50% -10px, rgba(168,85,247,0.16), transparent)' }} />
            <div style={{ fontSize: 10, fontWeight: 600, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#a855f7', marginBottom: 20, position: 'relative', zIndex: 1 }}>Trade Edge</div>
            <div style={{ fontFamily: "'JetBrains Mono', monospace", fontSize: 'clamp(30px, 3.5vw, 46px)', fontWeight: 700, letterSpacing: '-1px', lineHeight: 1, marginBottom: 10, color: '#10b981', position: 'relative', zIndex: 1 }}>+$9,830</div>
            <p style={{ fontSize: 15, color: 'rgba(255,255,255,0.55)', lineHeight: 1.6, marginBottom: 24, flex: 1, position: 'relative', zIndex: 1 }}>in unrealized gains visible across open positions — at a glance.</p>
            <div style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 10, padding: 14, fontSize: 11, position: 'relative', zIndex: 1 }}>
              <div style={{ fontSize: 9, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(255,255,255,0.3)', marginBottom: 10 }}>Open positions · Live</div>
              {[
                { sym: 'VNM', detail: 'Long · 200 shares', pnl: '+$689.00', pct: '+12.4%' },
                { sym: 'BTC', detail: 'Long · 1 unit', pnl: '+$183.00', pct: '+6.1%' },
                { sym: 'HPG', detail: 'Short · 500 shares', pnl: '+$115.20', pct: '+3.8%' },
              ].map((p) => (
                <div key={p.sym} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '6px 8px', background: 'rgba(255,255,255,0.03)', borderRadius: 6, marginBottom: 6 }}>
                  <div><div style={{ fontSize: 9.5, fontWeight: 700 }}>{p.sym}</div><div style={{ fontSize: 8, color: 'rgba(255,255,255,0.35)' }}>{p.detail}</div></div>
                  <div style={{ textAlign: 'right' }}><div style={{ fontSize: 9.5, fontWeight: 700, color: '#10b981' }}>{p.pnl}</div><div style={{ fontSize: 8, color: 'rgba(255,255,255,0.35)' }}>{p.pct}</div></div>
                </div>
              ))}
            </div>
          </div>

        </div>
      </div>
    </section>
  )
}
