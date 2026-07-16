import { Link } from 'react-router-dom'

export function LandingPage() {
  return <main className="grid min-h-screen place-items-center bg-stone-50 px-6 text-center text-slate-950"><section><p className="text-4xl font-semibold">OrgWiki</p><p className="mt-4 text-lg text-slate-600">AI-powered Organizational Knowledge Platform</p><Link to="/auth" className="mt-8 inline-block rounded-lg bg-teal-700 px-5 py-3 font-semibold text-white">Get Started</Link></section></main>
}
