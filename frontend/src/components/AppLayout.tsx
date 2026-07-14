import { Archive, BookOpenText, HeartPulse, Network } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'

const navigation = [
  { to: '/', label: 'Overview', icon: Network },
  { to: '/system', label: 'System', icon: HeartPulse },
  { to: '/import', label: 'Import', icon: Archive },
]

export function AppLayout() {
  return (
    <div className="min-h-screen bg-stone-50 text-slate-950">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-5 sm:px-8">
          <NavLink to="/" className="flex items-center gap-2.5 font-semibold tracking-normal">
            <span className="grid size-8 place-items-center rounded-md bg-teal-700 text-white">
              <BookOpenText size={18} aria-hidden="true" />
            </span>
            <span>OrgWiki</span>
          </NavLink>
          <nav aria-label="Primary navigation" className="flex items-center gap-1">
            {navigation.map(({ to, label, icon: Icon }) => (
              <NavLink
                key={to}
                to={to}
                className={({ isActive }) =>
                  `flex h-9 items-center gap-2 px-3 text-sm font-medium transition-colors ${
                    isActive
                      ? 'border-b-2 border-teal-700 text-teal-800'
                      : 'text-slate-600 hover:text-slate-950'
                  }`
                }
              >
                <Icon size={16} aria-hidden="true" />
                {label}
              </NavLink>
            ))}
          </nav>
        </div>
      </header>
      <main className="mx-auto w-full max-w-6xl px-5 py-10 sm:px-8 sm:py-14">
        <Outlet />
      </main>
    </div>
  )
}
