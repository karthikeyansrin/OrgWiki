import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useState } from 'react'
import { LoaderCircle } from 'lucide-react'
import { getKnowledgeBase } from '../api/client'

export function KnowledgeBasePage() {
  const [filter, setFilter] = useState('')
  const [domain, setDomain] = useState('')
  const [tag, setTag] = useState('')
  const home = useQuery({ queryKey: ['knowledge-home'], queryFn: getKnowledgeBase })

  if (home.isLoading) return <div className="flex items-center gap-3 text-slate-600"><LoaderCircle className="animate-spin" size={20} />Loading Knowledge Base…</div>
  if (home.isError) return <div className="rounded-xl bg-rose-50 p-5 text-rose-700">{home.error.message}</div>

  const data = home.data!
  const term = filter.trim().toLowerCase()
  const articles = data.articles.filter(article => {
    const matchesFilter = !term || `${article.title} ${article.summary} ${article.domain} ${article.tags.join(' ')}`.toLowerCase().includes(term)
    return matchesFilter && (!domain || article.domain === domain) && (!tag || article.tags.includes(tag))
  })

  return <section><div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-teal-700">Knowledge Base</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Trusted organizational knowledge</h1><p className="mt-3 max-w-2xl text-slate-600">Consolidated from reviewed source material with verifiable evidence.</p></div><div className="mt-8 grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto]"><input value={filter} onChange={event => setFilter(event.target.value)} placeholder="Filter by title, summary, tag, or domain" className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm" /><span className="rounded-lg bg-teal-100 px-3 py-2 text-sm font-semibold text-teal-800">Search</span></div><div className="mt-4 flex flex-wrap gap-3 text-sm">{data.domains.map(value => <button key={value} onClick={() => setDomain(domain === value ? '' : value)} className={`rounded-lg px-3 py-2 font-medium ${domain === value ? 'bg-slate-900 text-white' : 'bg-white text-slate-700 ring-1 ring-slate-200'}`}>{value}</button>)}{data.tags.map(value => <button key={value} onClick={() => setTag(tag === value ? '' : value)} className={`rounded-lg px-3 py-2 font-medium ${tag === value ? 'bg-teal-700 text-white' : 'bg-teal-50 text-teal-800 ring-1 ring-teal-100'}`}>#{value}</button>)}</div>{(filter || domain || tag) && <p className="mt-6 text-sm text-slate-600">{articles.length} matching article{articles.length === 1 ? '' : 's'}</p>}<div className="mt-6 grid gap-4 md:grid-cols-2">{articles.map(article => <Link key={article.key} to={`/knowledge/articles/${article.key}`} className="rounded-xl border border-slate-200 bg-white p-5 transition hover:border-teal-400"><p className="text-sm font-semibold text-teal-700">{article.domain}</p><h2 className="mt-2 text-xl font-semibold">{article.title}</h2><p className="mt-2 text-sm leading-6 text-slate-600">{article.summary}</p><div className="mt-4 flex flex-wrap gap-2 text-xs text-slate-500"><span>{article.difficulty}</span><span>•</span><span>{article.estimatedReadingMinutes} min read</span>{article.tags.map(value => <span key={value} className="rounded bg-slate-100 px-2 py-1">{value}</span>)}</div></Link>)}</div>{articles.length === 0 && <div className="mt-8 rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-600">No published knowledge matches this filter. Approved articles must be explicitly published before they appear here.</div>}</section>
}
