import { useButtonState, primaryBtnStyle, ghostLinkStyle } from '../../lib/buttonStyles'

interface Props {
  onSignup: () => void
}

export function HeroSection({ onSignup }: Props) {
  const startBtn = useButtonState()
  const seeBtn = useButtonState()

  return (
    <section style={{
      padding: '112px 0 80px',
      textAlign: 'center',
      position: 'relative',
      zIndex: 1,
      overflow: 'hidden',
    }}>
      {/* Blue radial glow */}
      <div style={{
        position: 'absolute', inset: 0, pointerEvents: 'none',
        background: 'radial-gradient(ellipse 800px 500px at 50% -80px, rgba(37,99,235,0.18) 0%, transparent 70%)',
      }} />

      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', position: 'relative', zIndex: 1 }}>
        {/* Eyebrow */}
        <div style={{
          display: 'inline-flex', alignItems: 'center', gap: 8,
          padding: '5px 14px', borderRadius: 99,
          border: '1px solid rgba(255,255,255,0.12)',
          background: 'rgba(255,255,255,0.04)',
          fontSize: 12, fontWeight: 600,
          color: '#3b82f6', letterSpacing: '0.04em', textTransform: 'uppercase',
          marginBottom: 28,
        }}>
          <span style={{ width: 6, height: 6, borderRadius: '50%', background: '#3b82f6', display: 'inline-block' }} />
          Personal finance meets trading intelligence
        </div>

        <h1 style={{
          fontSize: 'clamp(38px, 6vw, 68px)',
          fontWeight: 900,
          lineHeight: 1.08,
          letterSpacing: '-2px',
          color: 'rgba(255,255,255,0.92)',
          maxWidth: 860, margin: '0 auto 20px',
        }}>
          You're earning.<br />
          <span style={{
            fontStyle: 'normal',
            background: 'linear-gradient(135deg, #60a5fa 0%, #818cf8 100%)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            backgroundClip: 'text',
          }}>
            But where is it all going?
          </span>
        </h1>

        <p style={{
          fontSize: 'clamp(16px, 2.2vw, 20px)',
          color: 'rgba(255,255,255,0.55)',
          maxWidth: 600, margin: '0 auto 40px',
          lineHeight: 1.65,
        }}>
          FinTrackPro connects your expenses, budgets, and trades in one command center — so you always know your real financial position.
        </p>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 14, flexWrap: 'wrap' }}>
          <button
            onClick={onSignup}
            {...startBtn.handlers}
            style={primaryBtnStyle(startBtn.isHovered, startBtn.isPressed, {
              padding: '14px 32px',
              fontSize: 16,
            })}
          >
            Start for Free
          </button>
          <a
            href="#mockup"
            {...seeBtn.handlers}
            style={ghostLinkStyle(seeBtn.isHovered, seeBtn.isPressed)}
          >
            See it in action{' '}
            <span style={{
              display: 'inline-block',
              transition: `transform 0.2s ease-out`,
              transform: seeBtn.isHovered ? 'translateX(4px)' : 'translateX(0)',
            }}>→</span>
          </a>
        </div>

        <p style={{ marginTop: 18, fontSize: 13, color: 'rgba(255,255,255,0.32)' }}>
          Free plan available<span style={{ margin: '0 6px', opacity: 0.4 }}>·</span>No credit card required
        </p>
      </div>
    </section>
  )
}
