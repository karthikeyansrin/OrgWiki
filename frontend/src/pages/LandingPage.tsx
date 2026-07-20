import { BookOpenText, UsersRound } from 'lucide-react'
import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { OverviewPage } from './OverviewPage'

export function LandingPage() {
  const { user, isLoading } = useAuth()
  if (isLoading) return <div className="grid min-h-screen place-items-center bg-stone-50 text-slate-600">Loading…</div>
  if (user) return <Navigate to="/import" replace />
  return <div className="min-h-screen bg-stone-50 text-slate-950"><header className="border-b border-slate-200 bg-white"><div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-5 sm:px-8"><Link to="/" className="flex items-center gap-2.5 font-semibold"><span className="grid size-8 place-items-center rounded-md bg-teal-700 text-white"><BookOpenText size={18} aria-hidden="true" /></span><span>OrgWiki</span></Link><div className="flex items-center gap-2"><Link to="/spaces" className="inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-semibold text-slate-700 hover:bg-slate-100"><UsersRound size={16} />Team Spaces</Link><Link to="/auth?mode=login" className="rounded-md px-3 py-1.5 text-sm font-semibold text-slate-700 hover:bg-slate-100">Login</Link><Link to="/auth?mode=register" className="rounded-md bg-teal-700 px-3 py-1.5 text-sm font-semibold text-white hover:bg-teal-800">Register</Link></div></div></header><main className="mx-auto w-full max-w-6xl px-5 py-10 sm:px-8 sm:py-14"><OverviewPage /></main></div>
}
