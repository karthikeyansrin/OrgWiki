import { Archive, BookOpenText, ClipboardCheck, Library, List, UserRound } from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { flushSync } from 'react-dom'
import { useAuth } from '../auth/AuthContext'

const navigation = [
  { to: '/import', label: 'Import', icon: Archive },
  { to: '/uploads', label: 'Uploads', icon: List },
  { to: '/review', label: 'Review', icon: ClipboardCheck },
  { to: '/knowledge', label: 'Knowledge Base', icon: Library },
]

export function AppLayout() {
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  return (
    <div className="min-h-screen bg-stone-50 text-slate-950">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-5 sm:px-8">
          <NavLink to="/import" className="flex items-center gap-2.5 font-semibold tracking-normal">
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
          <div className="ml-3 flex items-center gap-3">
            <span className="flex items-center gap-1.5 text-sm font-medium text-slate-600"><UserRound size={16} aria-hidden="true" />{user?.fullName}</span>
            <button type="button" onClick={() => { flushSync(logout); navigate('/', { replace: true }) }} className="rounded-md bg-slate-100 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-200 hover:text-slate-950">Logout</button>
          </div>
        </div>
      </header>
      <main className="mx-auto w-full max-w-6xl px-5 py-10 sm:px-8 sm:py-14">
        <Outlet />
      </main>
    </div>
  )
}
