import { useEffect, useState, type FormEvent } from 'react'
import { BookOpenText } from 'lucide-react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { login, register } from '../api/client'
import { useAuth } from '../auth/AuthContext'

export function AuthPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const { user, isLoading, completeAuthentication } = useAuth()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    setMode(searchParams.get('mode') === 'register' ? 'register' : 'login')
  }, [searchParams])

  if (isLoading) return <div className="grid min-h-screen place-items-center text-slate-600">Loading...</div>
  if (user) return <Navigate to="/import" replace />

  const switchMode = (next: 'login' | 'register') => { setSearchParams({ mode: next }); setError('') }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    if (!email.trim() || !password) return setError('Email and password are required.')
    if (mode === 'register') {
      if (!fullName.trim()) return setError('Full name is required.')
      if (password.length < 8) return setError('Password must be at least 8 characters.')
      if (password !== confirmPassword) return setError('Password confirmation does not match.')
    }
    try {
      setSubmitting(true)
      const response = mode === 'login' ? await login(email, password) : await register(fullName, email, password, confirmPassword)
      completeAuthentication(response)
      navigate('/import', { replace: true })
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Authentication could not be completed.')
    } finally {
      setSubmitting(false)
    }
  }

  return <main className="grid min-h-screen place-items-center bg-stone-50 px-5"><section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-7 shadow-sm"><Link to="/" className="mx-auto flex w-fit items-center gap-2.5 text-2xl font-semibold text-slate-950 hover:text-teal-800"><span className="grid size-8 place-items-center rounded-md bg-teal-700 text-white"><BookOpenText size={18} aria-hidden="true" /></span><span>OrgWiki</span></Link><div className="mt-6 grid grid-cols-2 rounded-lg bg-slate-100 p-1"><button type="button" onClick={() => switchMode('login')} className={`rounded-md px-3 py-2 text-sm font-semibold ${mode === 'login' ? 'bg-white text-slate-950 shadow-sm' : 'text-slate-600'}`}>Login</button><button type="button" onClick={() => switchMode('register')} className={`rounded-md px-3 py-2 text-sm font-semibold ${mode === 'register' ? 'bg-white text-slate-950 shadow-sm' : 'text-slate-600'}`}>Register</button></div><form className="mt-6 space-y-4" onSubmit={submit}>{mode === 'register' && <label className="block text-sm font-medium">Full Name<input value={fullName} onChange={event => setFullName(event.target.value)} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2" autoComplete="name" /></label>}<label className="block text-sm font-medium">Email<input type="email" value={email} onChange={event => setEmail(event.target.value)} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2" autoComplete="email" /></label><label className="block text-sm font-medium">Password<input type="password" value={password} onChange={event => setPassword(event.target.value)} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2" autoComplete={mode === 'login' ? 'current-password' : 'new-password'} /></label>{mode === 'register' && <label className="block text-sm font-medium">Confirm Password<input value={confirmPassword} onChange={event => setConfirmPassword(event.target.value)} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2" autoComplete="new-password" /></label>}{error && <p className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{error}</p>}<button disabled={submitting} className="w-full rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{submitting ? 'Please wait...' : mode === 'login' ? 'Login' : 'Register'}</button></form></section></main>
}
