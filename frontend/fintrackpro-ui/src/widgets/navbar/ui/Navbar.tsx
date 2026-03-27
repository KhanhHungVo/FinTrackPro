import { useState } from 'react'
import { NavLink } from 'react-router'
import { useAuthStore } from '@/features/auth'
import { cn } from '@/shared/lib/cn'

const links = [
  { to: '/dashboard',    label: 'Dashboard'     },
  { to: '/transactions', label: 'Transactions'  },
  { to: '/budgets',      label: 'Budgets'       },
  { to: '/trades',       label: 'Trades'        },
  { to: '/settings',     label: 'Settings'      },
]

export function Navbar() {
  const [open, setOpen] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const displayName = useAuthStore((s) => s.displayName)
  const email = useAuthStore((s) => s.email)
  const logout = useAuthStore((s) => s.logout)

  const initial = displayName?.charAt(0).toUpperCase() ?? '?'

  return (
    <nav className="border-b bg-white">
      <div className="flex items-center justify-between px-4 py-3 md:px-6">
        <span className="font-bold text-lg tracking-tight">FinTrackPro</span>

        <ul className="hidden md:flex gap-1">
          {links.map(({ to, label }) => (
            <li key={to}>
              <NavLink
                to={to}
                className={({ isActive }) =>
                  cn(
                    'rounded-md px-3 py-1.5 text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-blue-600 text-white'
                      : 'text-gray-600 hover:bg-gray-100',
                  )
                }
              >
                {label}
              </NavLink>
            </li>
          ))}
        </ul>

        <div className="flex items-center gap-2">
          <button
            onClick={() => setMobileOpen((v) => !v)}
            className="md:hidden flex h-8 w-8 items-center justify-center rounded-md text-gray-600 hover:bg-gray-100"
            aria-label="Toggle navigation"
            aria-expanded={mobileOpen}
          >
            {mobileOpen ? (
              <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
                <path d="M2 2L16 16M16 2L2 16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
              </svg>
            ) : (
              <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
                <path d="M2 4h14M2 9h14M2 14h14" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
              </svg>
            )}
          </button>

          <div className="relative">
            <button
              onClick={() => setOpen((v) => !v)}
              className="flex h-8 w-8 items-center justify-center rounded-full bg-blue-600 text-sm font-semibold text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              aria-label="User menu"
            >
              {initial}
            </button>

            {open && (
              <>
                <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
                <div className="absolute right-0 z-20 mt-2 w-56 rounded-md border bg-white py-1 shadow-lg">
                  <div className="border-b px-4 py-2">
                    <p className="truncate text-sm font-medium text-gray-900">{displayName}</p>
                    <p className="truncate text-xs text-gray-500">{email}</p>
                  </div>
                  <button
                    onClick={() => { setOpen(false); logout() }}
                    className="w-full px-4 py-2 text-left text-sm text-gray-700 hover:bg-gray-100"
                  >
                    Sign out
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      </div>

      {mobileOpen && (
        <div className="md:hidden border-t bg-white px-4 pb-3 space-y-1">
          {links.map(({ to, label }) => (
            <NavLink
              key={to}
              to={to}
              onClick={() => setMobileOpen(false)}
              className={({ isActive }) =>
                cn(
                  'block w-full rounded-md px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-600 hover:bg-gray-100',
                )
              }
            >
              {label}
            </NavLink>
          ))}
        </div>
      )}
    </nav>
  )
}
