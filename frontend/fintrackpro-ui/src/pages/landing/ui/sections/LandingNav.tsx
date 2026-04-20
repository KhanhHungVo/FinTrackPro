import { useState } from 'react'
import { useMobile } from '../../lib/useMobile'

interface Props {
  onLogin: () => void
  onSignup: () => void
}

const NAV_LINKS = [
  { label: 'Features', href: '#features' },
  { label: 'Pricing', href: '#pricing' },
  { label: 'How it works', href: '#how' },
] as const

export function LandingNav({ onLogin, onSignup }: Props) {
  const isMobile = useMobile()
  const [drawerOpen, setDrawerOpen] = useState(false)

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
        {/* Logo */}
        <span style={{ fontSize: 17, fontWeight: 700, letterSpacing: '-0.3px', color: 'rgba(255,255,255,0.92)', flexShrink: 0 }}>
          FinTrackPro
        </span>

        {/* Desktop nav links */}
        {!isMobile && (
          <ul style={{ display: 'flex', gap: 4, listStyle: 'none', margin: 0, padding: 0 }}>
            {NAV_LINKS.map(({ label, href }) => (
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
            ))}
          </ul>
        )}

        {/* Right side */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <button
            onClick={onLogin}
            style={{
              padding: '7px 16px', borderRadius: 8, border: '1px solid rgba(255,255,255,0.12)',
              background: 'transparent', color: 'rgba(255,255,255,0.7)',
              fontSize: 14, fontWeight: 600, cursor: 'pointer', whiteSpace: 'nowrap',
            }}
          >
            Log in
          </button>

          {/* Desktop: Start for Free */}
          {!isMobile && (
            <button
              onClick={onSignup}
              style={{
                padding: '8px 18px', borderRadius: 8,
                background: '#2563eb', color: '#fff',
                fontSize: 14, fontWeight: 600, cursor: 'pointer', border: 'none',
                whiteSpace: 'nowrap',
              }}
            >
              Start for Free
            </button>
          )}

          {/* Mobile: hamburger */}
          {isMobile && (
            <button
              onClick={() => setDrawerOpen((o) => !o)}
              aria-label={drawerOpen ? 'Close menu' : 'Open menu'}
              style={{
                width: 36, height: 36, borderRadius: 8,
                border: '1px solid rgba(255,255,255,0.12)',
                background: drawerOpen ? 'rgba(255,255,255,0.07)' : 'transparent',
                color: 'rgba(255,255,255,0.7)',
                fontSize: 18, cursor: 'pointer',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                flexShrink: 0,
              }}
            >
              {drawerOpen ? '✕' : '☰'}
            </button>
          )}
        </div>
      </div>

      {/* Mobile drawer */}
      {isMobile && drawerOpen && (
        <div style={{
          borderTop: '1px solid rgba(255,255,255,0.07)',
          background: 'rgba(12,14,20,0.97)',
          padding: '8px 0 16px',
        }}>
          {NAV_LINKS.map(({ label, href }) => (
            <a
              key={label}
              href={href}
              onClick={() => setDrawerOpen(false)}
              style={{
                display: 'block', padding: '12px 24px',
                fontSize: 15, fontWeight: 500, color: 'rgba(255,255,255,0.7)',
                textDecoration: 'none',
              }}
            >
              {label}
            </a>
          ))}
        </div>
      )}
    </nav>
  )
}
