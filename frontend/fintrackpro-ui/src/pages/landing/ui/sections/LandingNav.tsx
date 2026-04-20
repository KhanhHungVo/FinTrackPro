interface Props {
  onLogin: () => void
  onSignup: () => void
}

export function LandingNav({ onLogin, onSignup }: Props) {
  return (
    <nav style={{
      position: 'sticky', top: 0, zIndex: 100,
      background: 'rgba(12,14,20,0.85)',
      backdropFilter: 'blur(16px)',
      borderBottom: '1px solid rgba(255,255,255,0.07)',
    }}>
      <div style={{
        maxWidth: 1200, margin: '0 auto', padding: '0 24px',
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        height: 56,
      }}>
        <span style={{ fontSize: 17, fontWeight: 700, letterSpacing: '-0.3px', color: 'rgba(255,255,255,0.92)' }}>
          FinTrackPro
        </span>

        <ul style={{ display: 'flex', gap: 4, listStyle: 'none', margin: 0, padding: 0 }}>
          {(['Features', 'Pricing', 'How it works'] as const).map((label) => {
            const href = label === 'Features' ? '#features' : label === 'Pricing' ? '#pricing' : '#how'
            return (
              <li key={label}>
                <a
                  href={href}
                  style={{
                    padding: '6px 12px', borderRadius: 8,
                    fontSize: 14, fontWeight: 500, color: 'rgba(255,255,255,0.55)',
                    textDecoration: 'none', display: 'block',
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.color = 'rgba(255,255,255,0.92)'
                    e.currentTarget.style.background = 'rgba(255,255,255,0.05)'
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.color = 'rgba(255,255,255,0.55)'
                    e.currentTarget.style.background = 'transparent'
                  }}
                >
                  {label}
                </a>
              </li>
            )
          })}
        </ul>

        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <button
            onClick={onLogin}
            style={{
              padding: '7px 16px', borderRadius: 8, border: '1px solid rgba(255,255,255,0.12)',
              background: 'transparent', color: 'rgba(255,255,255,0.7)',
              fontSize: 14, fontWeight: 600, cursor: 'pointer',
            }}
          >
            Log in
          </button>
          <button
            onClick={onSignup}
            style={{
              padding: '8px 18px', borderRadius: 8,
              background: '#2563eb', color: '#fff',
              fontSize: 14, fontWeight: 600, cursor: 'pointer', border: 'none',
            }}
          >
            Start for Free
          </button>
        </div>
      </div>
    </nav>
  )
}
