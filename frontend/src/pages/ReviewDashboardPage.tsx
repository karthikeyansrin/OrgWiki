import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { LoaderCircle } from 'lucide-react'
import { getReviewDashboard } from '../api/client'
import { useState } from 'react'

const statuses = ['All', 'PendingReview', 'Approved', 'Rejected', 'Published']
export function ReviewDashboardPage() {
  const query = useQuery({ queryKey: ['review-dashboard'], queryFn: getReviewDashboard })
  const [status, setStatus] = useState('All'); const [search, setSearch] = useState('')
  const articles = (query.data?.articles ?? []).filter(article => (status === 'All' || article.status === status) && `${article.title} ${article.tags.join(' ')} ${article.domain}`.toLowerCase().includes(search.toLowerCase()))
  if (query.isLoading) return <div className="flex items-center gap-3 text-slate-600"><LoaderCircle className="animate-spin" size={20} />Loading review queue…</div>
  if (query.isError) return <div className="rounded-xl bg-rose-50 p-5 text-rose-700">{query.error.message}</div>
  const dashboard = query.data!
  return <section><div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-teal-700">Human review</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Knowledge review queue</h1><p className="mt-3 text-slate-600">AI-generated articles require human approval before publishing.</p></div><div className="mt-8 grid gap-3 sm:grid-cols-4">{[['Pending review', dashboard.pendingReview], ['Approved', dashboard.approved], ['Rejected', dashboard.rejected], ['Published', dashboard.published]].map(([label, value]) => <div key={label as string} className="rounded-xl border border-slate-200 bg-white p-4"><p className="text-sm text-slate-500">{label}</p><p className="mt-1 text-2xl font-semibold">{value}</p></div>)}</div><div className="mt-8 flex flex-wrap gap-3"><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Filter by title, tag, or domain" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" />{statuses.map(value => <button key={value} onClick={() => setStatus(value)} className={`rounded-lg px-3 py-2 text-sm font-medium ${status === value ? 'bg-teal-700 text-white' : 'bg-white text-slate-700 ring-1 ring-slate-200'}`}>{value === 'PendingReview' ? 'Pending review' : value}</button>)}</div><div className="mt-6 grid gap-4">{articles.map(article => <Link to={`/review/articles/${article.id}`} key={article.id} className="rounded-xl border border-slate-200 bg-white p-5 transition hover:border-teal-300"><div className="flex flex-wrap items-start justify-between gap-3"><div><h2 className="font-semibold">{article.title}</h2><p className="mt-1 text-sm text-slate-600">{article.summary}</p></div><span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold">{article.status}</span></div><div className="mt-4 flex flex-wrap gap-4 text-sm text-slate-500"><span>{Math.round(article.confidence * 100)}% confidence</span><span>{article.citationCount} citations</span><span>{article.estimatedReadingMinutes} min read</span><span>{article.domain}</span></div></Link>)}</div></section>
}
