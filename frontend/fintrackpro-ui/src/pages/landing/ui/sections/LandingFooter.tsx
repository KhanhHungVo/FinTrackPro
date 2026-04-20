export function LandingFooter() {
  return (
    <footer style={{ borderTop: '1px solid rgba(255,255,255,0.07)', padding: '40px 0' }}>
      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 16, position: 'relative', zIndex: 1 }}>
        <div>
          <div style={{ fontSize: 15, fontWeight: 700 }}>FinTrackPro</div>
          <div style={{ fontSize: 13, color: 'rgba(255,255,255,0.32)', marginTop: 3 }}>© 2026 FinTrackPro · Built for serious investors · Free plan available</div>
        </div>
        <nav style={{ display: 'flex', gap: 24 }}>
          {[
            { label: 'Dashboard', href: '/dashboard' },
            { label: 'Pricing', href: '#pricing' },
            { label: 'Sign Up', href: '#' },
          ].map((link) => (
            <a
              key={link.label}
              href={link.href}
              style={{ fontSize: 14, color: 'rgba(255,255,255,0.32)', textDecoration: 'none' }}
            >
              {link.label}
            </a>
          ))}
        </nav>
      </div>
    </footer>
  )
}
