import { NavLink } from 'react-router'
import { cn } from '@/shared/lib/cn'

const links = [
  { to: '/dashboard',    label: 'Dashboard'     },
  { to: '/transactions', label: 'Transactions'  },
  { to: '/budgets',      label: 'Budgets'       },
  { to: '/trades',       label: 'Trades'        },
  { to: '/settings',     label: 'Settings'      },
]

export function Navbar() {
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
    </nav>
  )
}
