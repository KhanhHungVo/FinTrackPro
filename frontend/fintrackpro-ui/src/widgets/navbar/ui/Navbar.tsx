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
  const displayName = useAuthStore((s) => s.displayName)
  const email = useAuthStore((s) => s.email)
  const logout = useAuthStore((s) => s.logout)

  const initial = displayName?.charAt(0).toUpperCase() ?? '?'

  return (
    <nav className="flex items-center justify-between border-b bg-white px-6 py-3">
      <span className="font-bold text-lg tracking-tight">FinTrackPro</span>
      <ul className="flex gap-1">
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

      <div className="relative ml-4">
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
    </nav>
  )
}
