import { BrowserRouter, Routes, Route, Navigate, Outlet } from 'react-router'
import { QueryProvider, AuthProvider, ErrorBoundary, LocaleProvider } from './providers'
import { Toaster } from 'sonner'
import '@/shared/i18n'
import { Navbar } from '@/widgets/navbar'
import { DashboardPage } from '@/pages/dashboard'
import { TransactionsPage } from '@/pages/transactions'
import { BudgetsPage } from '@/pages/budgets'
import { TradesPage } from '@/pages/trades'
import { SettingsPage } from '@/pages/settings'
import { PricingPage } from '@/pages/pricing'
import { AboutPage } from '@/pages/about'
import { MarketPage } from '@/pages/market'
import { LandingPage } from '@/pages/landing'
import { PlanLimitModal, BankTransferModal } from '@/features/upgrade'
import { useAuthStore } from '@/features/auth'
import { NotFoundPage } from '@/shared/ui/NotFoundPage'
import { DonationFooter } from '@/shared/ui/DonationFooter'
import { useLocaleStore } from '@/features/locale/model/localeStore'
import './styles/globals.css'

function RequireAuth() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  return isAuthenticated ? <Outlet /> : <Navigate to="/" replace />
}

function AppLayout() {
  const theme = useLocaleStore((s) => s.theme)
  return (
    <div className={theme === 'dark' ? 'dark' : ''}>
      <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-[#0f1117] dark:text-slate-200">
        <Navbar />
        <main className="flex-1">
          <Outlet />
        </main>
        <DonationFooter />
      </div>
    </div>
  )
}

export function App() {
  const theme = useLocaleStore((s) => s.theme)
  return (
    <ErrorBoundary>
      <AuthProvider>
        <QueryProvider>
          <LocaleProvider>
            <Toaster position="top-right" richColors theme={theme === 'dark' ? 'dark' : 'light'} />
            <BrowserRouter>
              <PlanLimitModal />
              <BankTransferModal />
              <Routes>
                {/* Public route — no auth required */}
                <Route path="/" element={<LandingPage />} />

                {/* Protected routes — redirect to / if unauthenticated */}
                <Route element={<RequireAuth />}>
                  <Route element={<AppLayout />}>
                    <Route path="/dashboard"    element={<DashboardPage />} />
                    <Route path="/transactions" element={<TransactionsPage />} />
                    <Route path="/budgets"      element={<BudgetsPage />} />
                    <Route path="/trades"       element={<TradesPage />} />
                    <Route path="/settings"     element={<SettingsPage />} />
                    <Route path="/pricing"      element={<PricingPage />} />
                    <Route path="/about"        element={<AboutPage />} />
                    <Route path="/market"       element={<MarketPage />} />
                    <Route path="*"             element={<NotFoundPage />} />
                  </Route>
                </Route>
              </Routes>
            </BrowserRouter>
          </LocaleProvider>
        </QueryProvider>
      </AuthProvider>
    </ErrorBoundary>
  )
}
