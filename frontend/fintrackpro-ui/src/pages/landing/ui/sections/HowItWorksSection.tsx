interface Props {
  onSignup: () => void
}

const steps = [
  {
    num: '1',
    title: 'Sign up in 30 seconds',
    desc: 'Google or email — no forms, no friction. Your dashboard is ready instantly.',
  },
  {
    num: '2',
    title: 'Log your first transaction',
    desc: 'Add a transaction and watch it auto-categorize. Set a budget in one tap.',
  },
  {
    num: '3',
    title: 'See your full picture',
    desc: 'Expenses, budgets, and trades — all updated in real time. Always in control.',
  },
]

export function HowItWorksSection({ onSignup }: Props) {
  return (
    <section id="how" style={{ padding: '96px 0' }}>
      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', position: 'relative', zIndex: 1 }}>
        <p style={{ fontSize: 12, fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#3b82f6', marginBottom: 12, textAlign: 'center' }}>How It Works</p>
        <h2 style={{ fontSize: 'clamp(28px, 4vw, 42px)', fontWeight: 900, letterSpacing: '-1px', lineHeight: 1.15, marginBottom: 14, textAlign: 'center' }}>Up and running in minutes</h2>
        <p style={{ fontSize: 17, color: 'rgba(255,255,255,0.55)', maxWidth: 520, margin: '0 auto', lineHeight: 1.65, textAlign: 'center' }}>No complicated setup. Just clarity.</p>

        <div
          className="lp-fade-up"
          style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 32, marginTop: 56, position: 'relative' }}
        >
          {steps.map((step) => (
            <div key={step.num} style={{ textAlign: 'center' }}>
              <div style={{ width: 44, height: 44, borderRadius: '50%', background: 'rgba(37,99,235,0.15)', border: '1px solid #2563eb', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 16, fontWeight: 800, color: '#3b82f6', margin: '0 auto 20px', position: 'relative', zIndex: 1 }}>
                {step.num}
              </div>
              <h3 style={{ fontSize: 17, fontWeight: 800, marginBottom: 8 }}>{step.title}</h3>
              <p style={{ fontSize: 14, color: 'rgba(255,255,255,0.55)', maxWidth: 240, margin: '0 auto', lineHeight: 1.6 }}>{step.desc}</p>
            </div>
          ))}
        </div>

        {/* Final CTA */}
        <div style={{ textAlign: 'center', marginTop: 80 }}>
          <h3 style={{ fontSize: 'clamp(24px, 4vw, 38px)', fontWeight: 800, letterSpacing: '-0.8px', marginBottom: 16 }}>
            Take control of your finances today.
          </h3>
          <p style={{ color: 'rgba(255,255,255,0.55)', fontSize: 17, marginBottom: 36, maxWidth: 440, marginLeft: 'auto', marginRight: 'auto' }}>
            Join investors who track smarter with FinTrackPro.
          </p>
          <button
            onClick={onSignup}
            style={{ padding: '16px 40px', borderRadius: 10, background: '#2563eb', color: '#fff', fontSize: 17, fontWeight: 700, border: 'none', cursor: 'pointer', boxShadow: '0 4px 24px rgba(37,99,235,0.35)' }}
          >
            Start for Free — It's $0
          </button>
          <p style={{ marginTop: 14, fontSize: 13, color: 'rgba(255,255,255,0.32)' }}>
            No credit card required<span style={{ margin: '0 6px', opacity: 0.4 }}>·</span>Free plan available
          </p>
        </div>
      </div>
    </section>
  )
}
