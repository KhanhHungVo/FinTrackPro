import { Navigate } from 'react-router'
import { useAuthStore } from '@/features/auth'
import { authAdapter } from '@/shared/lib/auth'
import { useFadeUpAll } from '../lib/useFadeUp'
import styles from './LandingPage.module.css'
import { LandingNav } from './sections/LandingNav'
import { HeroSection } from './sections/HeroSection'
import { PainPointsSection } from './sections/PainPointsSection'
import { DashboardMockupSection } from './sections/DashboardMockupSection'
import { OutcomeSpotlightsSection } from './sections/OutcomeSpotlightsSection'
import { FeaturesSection } from './sections/FeaturesSection'
import { PricingSection } from './sections/PricingSection'
import { HowItWorksSection } from './sections/HowItWorksSection'
import { LandingFooter } from './sections/LandingFooter'

export function LandingPage() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const containerRef = useFadeUpAll()

  if (isAuthenticated) return <Navigate to="/dashboard" replace />

  function handleLogin() {
    authAdapter.login({ screen: 'login' })
  }

  function handleSignup() {
    authAdapter.login({ screen: 'signup' })
  }

  return (
    <div ref={containerRef} className={styles.root}>
      <div className={styles.bgMesh} />
      <LandingNav onLogin={handleLogin} onSignup={handleSignup} />
      <HeroSection onSignup={handleSignup} />
      <PainPointsSection />
      <DashboardMockupSection />
      <OutcomeSpotlightsSection />
      <FeaturesSection />
      <PricingSection onSignup={handleSignup} />
      <HowItWorksSection onSignup={handleSignup} />
      <LandingFooter />
    </div>
  )
}
