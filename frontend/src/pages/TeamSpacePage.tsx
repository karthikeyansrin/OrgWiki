import { useQuery } from '@tanstack/react-query'
import { CalendarDays, Download, Search } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useState } from 'react'
import { getPublicTeamSpace, getPublicTeamSpaceArticle } from '../api/client'
import { PageSkeleton } from '../components/LoadingSkeleton'
import { PublicSpacesHeader } from '../components/PublicSpacesHeader'
import { downloadArticleMarkdown } from '../components/downloadMarkdown'

function SpaceArticleActions({ slug, articleKey, onOpen }: { slug: string; articleKey: string; onOpen: () => void }) {
  const article = useQuery({ queryKey: ['public-team-space-article-download', slug, articleKey], queryFn: () => getPublicTeamSpaceArticle(slug, articleKey), enabled: false })
  const download = async () => {
    const result = await article.refetch()
    if (result.data) downloadArticleMarkdown(result.data)
  }
  return <div className="flex items-center gap-4"><button type="button" onClick={event => { event.stopPropagation(); void download() }} disabled={article.isFetching} className="inline-flex cursor-pointer items-center gap-1.5 text-sm font-semibold text-slate-600 hover:text-teal-800 disabled:cursor-not-allowed"><Download size={15} />{article.isFetching ? 'Preparing...' : 'Download Markdown'}</button><button type="button" onClick={event => { event.stopPropagation(); onOpen() }} className="cursor-pointer text-sm font-semibold text-teal-700 hover:text-teal-900">Read more →</button></div>
}

export function TeamSpacePage() {
  const { slug = '' } = useParams()
  const navigate = useNavigate()
  const query = useQuery({ queryKey: ['public-team-space', slug], queryFn: () => getPublicTeamSpace(slug), enabled: Boolean(slug) })
  const [filter, setFilter] = useState('')
  if (query.isLoading) return <div className="min-h-screen bg-stone-50"><PublicSpacesHeader /><main className="mx-auto max-w-6xl px-5 py-12 sm:px-8"><PageSkeleton cards={3} rows={2} /></main></div>
  if (query.isError) return <div className="min-h-screen bg-stone-50"><PublicSpacesHeader /><main className="mx-auto max-w-6xl px-5 py-12 text-rose-700">This Team Space could not be found.</main></div>
  const space = query.data!
  const term = filter.trim().toLowerCase()
  const articles = space.articles.filter(article => !term || `${article.title} ${article.summary}`.toLowerCase().includes(term))
  const openArticle = (articleKey: string) => navigate(`/spaces/${space.slug}/${articleKey}`)

  return <div className="min-h-screen bg-stone-50 text-slate-950"><PublicSpacesHeader /><main className="mx-auto max-w-6xl px-5 py-12 sm:px-8"><Link to="/spaces" className="text-sm font-semibold text-teal-700 hover:text-teal-900">← Team Spaces</Link><div className="mt-6 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8"><p className="text-sm font-semibold uppercase tracking-[0.18em] text-teal-700">Public Team Space</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">{space.name}</h1><p className="mt-4 max-w-3xl text-lg leading-8 text-slate-600">{space.description}</p><p className="mt-5 text-sm font-medium text-teal-800">{space.articles.length} published article{space.articles.length === 1 ? '' : 's'}</p></div><div className="relative mt-7"><Search size={18} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" /><input value={filter} onChange={event => setFilter(event.target.value)} placeholder={`Search ${space.name} knowledge`} className="w-full rounded-lg border border-slate-300 bg-white py-2.5 pl-10 pr-3 text-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100" /></div><div className="mt-6 grid gap-4">{articles.map(article => <article key={article.key} role="link" tabIndex={0} aria-label={`Read ${article.title}`} onClick={() => openArticle(article.key)} onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openArticle(article.key) } }} className="cursor-pointer rounded-xl border border-slate-200 bg-white p-6 shadow-sm transition hover:-translate-y-0.5 hover:border-teal-300 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-teal-500 focus:ring-offset-2"><h2 className="text-xl font-semibold">{article.title}</h2><p className="mt-2 max-w-3xl leading-7 text-slate-600">{article.summary}</p><div className="mt-5 flex flex-wrap items-center justify-between gap-4 border-t border-slate-100 pt-4"><span className="inline-flex items-center gap-2 text-sm text-slate-500"><CalendarDays size={15} />Updated {new Date(article.lastUpdatedAtUtc).toLocaleDateString()}</span><SpaceArticleActions slug={space.slug} articleKey={article.key} onOpen={() => openArticle(article.key)} /></div></article>)}</div>{articles.length === 0 && <div className="mt-6 rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-600">No articles in this Team Space match your search.</div>}</main></div>
}
