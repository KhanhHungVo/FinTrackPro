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
import { PlanLimitModal, BankTransferModal } from '@/features/upgrade'
import { NotFoundPage } from '@/shared/ui/NotFoundPage'
import './styles/globals.css'

function AppLayout() {
  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <main>
        <Outlet />
      </main>
    </div>
  )
}

export function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <QueryProvider>
          <LocaleProvider>
            <Toaster position="top-right" richColors />
            <BrowserRouter>
              <PlanLimitModal />
              <BankTransferModal />
              <Routes>
                <Route element={<AppLayout />}>
                  <Route index element={<Navigate to="/dashboard" replace />} />
                  <Route path="/dashboard"    element={<DashboardPage />} />
                  <Route path="/transactions" element={<TransactionsPage />} />
                  <Route path="/budgets"      element={<BudgetsPage />} />
                  <Route path="/trades"       element={<TradesPage />} />
                  <Route path="/settings"     element={<SettingsPage />} />
                  <Route path="/pricing"      element={<PricingPage />} />
                  <Route path="*"             element={<NotFoundPage />} />
                </Route>
              </Routes>
            </BrowserRouter>
          </LocaleProvider>
        </QueryProvider>
      </AuthProvider>
    </ErrorBoundary>
  )
}
