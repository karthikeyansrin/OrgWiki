import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useRef, useState } from 'react'
import { AlertTriangle, ArrowDown, CheckCircle2, Clock3, Eye, FileText, Save, Send, ShieldCheck, XCircle } from 'lucide-react'
import { approveReviewArticle, getReviewArticle, publishReviewArticle, rejectReviewArticle, updateReviewArticle } from '../api/client'
import { PageSkeleton } from '../components/LoadingSkeleton'
import { SafeMarkdown } from '../components/SafeMarkdown'

type Draft = { title: string; summary: string; markdownContent: string; difficulty: string; estimatedReadingMinutes: number; tags: string[]; relatedArticleKeys: string[] }

function statusStyle(status: string) {
  if (status === 'Published') return 'bg-emerald-100 text-emerald-800'
  if (status === 'Approved') return 'bg-sky-100 text-sky-800'
  if (status === 'Rejected') return 'bg-rose-100 text-rose-800'
  return 'bg-amber-100 text-amber-800'
}

export function ReviewArticlePage() {
  const { articleId = '' } = useParams()
  const navigate = useNavigate()
  const client = useQueryClient()
  const query = useQuery({ queryKey: ['review-article', articleId], queryFn: () => getReviewArticle(articleId), enabled: Boolean(articleId) })
  const [draft, setDraft] = useState<Draft | null>(null)
  const [notes, setNotes] = useState('')
  const [notice, setNotice] = useState<string | null>(null)
  const [showActionJump, setShowActionJump] = useState(true)
  const actionRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!query.data) return
    const article = query.data
    setDraft({ title: article.title, summary: article.summary, markdownContent: article.markdownContent, difficulty: article.difficulty, estimatedReadingMinutes: article.estimatedReadingMinutes, tags: article.tags, relatedArticleKeys: article.relatedArticleKeys })
    setNotes(article.reviewNotes ?? '')
  }, [query.data])

  useEffect(() => {
    const updateActionJump = () => setShowActionJump(window.scrollY < 280)
    updateActionJump()
    window.addEventListener('scroll', updateActionJump, { passive: true })
    return () => window.removeEventListener('scroll', updateActionJump)
  }, [])

  const refresh = async () => {
    await client.invalidateQueries({ queryKey: ['review-article', articleId] })
    await client.invalidateQueries({ queryKey: ['review-dashboard'] })
  }
  const save = useMutation({ mutationFn: () => updateReviewArticle(articleId, draft!), onSuccess: async () => { await refresh(); setNotice('Article changes saved.') } })
  const approve = useMutation({ mutationFn: () => approveReviewArticle(articleId, notes), onSuccess: async () => { await refresh(); setNotice('Article approved and ready to publish.') } })
  const reject = useMutation({ mutationFn: () => rejectReviewArticle(articleId, notes), onSuccess: () => navigate('/review') })
  const publish = useMutation({ mutationFn: () => publishReviewArticle(articleId), onSuccess: () => navigate('/review') })

  if (query.isLoading || !draft) return <PageSkeleton cards={3} rows={2} />
  if (query.isError) return <div className="rounded-xl bg-rose-50 p-5 text-rose-700">{query.error.message}</div>

  const article = query.data!
  const locked = article.status === 'Published'
  const rejected = article.status === 'Rejected'
  const busy = save.isPending || approve.isPending || reject.isPending || publish.isPending
  const update = <K extends keyof Draft>(key: K, value: Draft[K]) => setDraft(current => current ? { ...current, [key]: value } : current)
  const reset = () => { setDraft({ title: article.title, summary: article.summary, markdownContent: article.markdownContent, difficulty: article.difficulty, estimatedReadingMinutes: article.estimatedReadingMinutes, tags: article.tags, relatedArticleKeys: article.relatedArticleKeys }); setNotes(article.reviewNotes ?? ''); setNotice('Unsaved changes discarded.') }
  const error = save.error?.message ?? approve.error?.message ?? reject.error?.message ?? publish.error?.message

  return <section className="animate-fade-in">
    <Link to="/review" className="text-sm font-semibold text-teal-700 hover:text-teal-900">← Review</Link>
    <div className="mt-6 flex flex-wrap items-start justify-between gap-4">
      <div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-teal-700">Human review</p><h1 className="mt-2 text-3xl font-semibold tracking-tight">Review generated knowledge</h1><p className="mt-3 max-w-2xl text-slate-600">Refine the draft while keeping its verified source evidence visible.</p></div>
      <span className={`rounded-full px-3 py-1.5 text-sm font-semibold ${statusStyle(article.status)}`}>{article.status === 'PendingReview' ? 'Pending review' : article.status}</span>
    </div>

    {locked ? <div className="mt-6 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-emerald-900"><span>Published articles are read-only to preserve the reviewed knowledge record.</span><Link to={`/knowledge/articles/${article.key}`} className="font-semibold underline underline-offset-2">View in Knowledge Base</Link></div> : <div className="mt-6 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950"><AlertTriangle size={16} className="mr-2 inline" />This article contains AI-generated content. Confirm the draft against the immutable source evidence before approval.</div>}
    {notice && <p className="mt-4 rounded-lg border border-teal-200 bg-teal-50 px-4 py-3 text-sm font-medium text-teal-900">{notice}</p>}

    <div className="mt-8 grid gap-8 xl:grid-cols-[minmax(0,0.9fr)_minmax(30rem,1.1fr)]">
      <div className="space-y-5">
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-2"><FileText size={18} className="text-teal-700" /><h2 className="font-semibold">Editable article details</h2></div>
          <div className="mt-5 space-y-5">
            <label className="block text-sm font-semibold text-slate-700">Title<input disabled={locked} value={draft.title} onChange={event => update('title', event.target.value)} className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100 disabled:bg-slate-100" /></label>
            <label className="block text-sm font-semibold text-slate-700">Summary<textarea disabled={locked} value={draft.summary} onChange={event => update('summary', event.target.value)} className="mt-2 min-h-28 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100 disabled:bg-slate-100" /></label>
            <div><div className="flex items-center justify-between"><label className="text-sm font-semibold text-slate-700" htmlFor="article-markdown">Markdown content</label><span className="text-xs text-slate-500">Live preview on the right</span></div><textarea id="article-markdown" disabled={locked} value={draft.markdownContent} onChange={event => update('markdownContent', event.target.value)} className="mt-2 min-h-[32rem] w-full rounded-lg border border-slate-300 bg-slate-950 px-4 py-3 font-mono text-sm leading-6 text-slate-100 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-100 disabled:bg-slate-800" /></div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><h2 className="font-semibold">Article metadata</h2><div className="mt-5 grid gap-4 sm:grid-cols-2"><label className="text-sm font-semibold text-slate-700">Difficulty<select disabled={locked} value={draft.difficulty} onChange={event => update('difficulty', event.target.value)} className="mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 disabled:bg-slate-100">{['Beginner', 'Intermediate', 'Advanced'].map(value => <option key={value}>{value}</option>)}</select></label><label className="text-sm font-semibold text-slate-700">Estimated reading time<input disabled={locked} type="number" min="1" value={draft.estimatedReadingMinutes} onChange={event => update('estimatedReadingMinutes', Number(event.target.value))} className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 disabled:bg-slate-100" /></label></div><label className="mt-4 block text-sm font-semibold text-slate-700">Tags<input disabled={locked} value={draft.tags.join(', ')} onChange={event => update('tags', event.target.value.split(',').map(tag => tag.trim()).filter(Boolean))} className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 disabled:bg-slate-100" /></label><label className="mt-4 block text-sm font-semibold text-slate-700">Related articles<select disabled={locked} multiple value={draft.relatedArticleKeys} onChange={event => update('relatedArticleKeys', Array.from(event.target.selectedOptions).map(option => option.value))} className="mt-2 min-h-28 w-full rounded-lg border border-slate-300 px-3 py-2.5 disabled:bg-slate-100">{article.availableRelatedArticles.map(item => <option key={item.id} value={item.key}>{item.title}</option>)}</select></label><label className="mt-4 block text-sm font-semibold text-slate-700">Review notes<textarea disabled={locked} value={notes} onChange={event => setNotes(event.target.value)} className="mt-2 min-h-24 w-full rounded-lg border border-slate-300 px-3 py-2.5 disabled:bg-slate-100" /></label></div>

        {!locked && <div ref={actionRef} className="flex flex-wrap gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm"><button type="button" disabled={busy} onClick={() => save.mutate()} className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"><Save size={16} />{save.isPending ? 'Saving…' : 'Save changes'}</button><button type="button" disabled={busy} onClick={reset} className="cursor-pointer rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50">Undo unsaved changes</button>{article.status === 'PendingReview' && <><button type="button" disabled={busy} onClick={() => approve.mutate()} className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-50"><CheckCircle2 size={16} />{approve.isPending ? 'Approving…' : 'Approve'}</button><button type="button" disabled={busy} onClick={() => reject.mutate()} className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-rose-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-rose-800 disabled:cursor-not-allowed disabled:opacity-50"><XCircle size={16} />{reject.isPending ? 'Rejecting…' : 'Reject'}</button></>}{rejected && <button type="button" disabled={busy} onClick={() => approve.mutate()} className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"><CheckCircle2 size={16} />Approve</button>}{article.status === 'Approved' && <button type="button" disabled={busy} onClick={() => publish.mutate()} className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"><Send size={16} />{publish.isPending ? 'Publishing…' : 'Publish'}</button>}<button type="button" disabled={busy} onClick={() => navigate('/review')} className="cursor-pointer px-4 py-2.5 text-sm font-semibold text-slate-600 hover:text-slate-950 disabled:cursor-not-allowed disabled:opacity-50">Cancel</button></div>}
        {error && <p className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{String(error)}</p>}
      </div>

      <aside className="space-y-5 xl:sticky xl:top-5 xl:max-h-[calc(100vh-2.5rem)] xl:overflow-y-auto xl:pr-1">
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm"><div className="flex items-center gap-2"><Eye size={18} className="text-teal-700" /><h2 className="font-semibold">Live article preview</h2></div><p className="mt-1 text-sm text-slate-500">Rendered exactly as readers will experience it.</p><div className="mt-6 border-t border-slate-100 pt-6"><p className="text-sm font-semibold uppercase tracking-[0.16em] text-teal-700">{article.domain}</p><h2 className="mt-3 text-3xl font-semibold tracking-tight">{draft.title}</h2><p className="mt-3 text-lg leading-8 text-slate-600">{draft.summary}</p><div className="mt-4 flex items-center gap-2 text-sm text-slate-500"><Clock3 size={15} />{draft.estimatedReadingMinutes} min read <span>·</span><span>{draft.difficulty}</span></div><div className="mt-7"><SafeMarkdown content={draft.markdownContent} /></div></div></div>
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><div className="flex items-center gap-2"><ShieldCheck size={18} className="text-teal-700" /><h2 className="font-semibold">Knowledge Quality</h2></div><div className="mt-4 grid gap-2 text-sm text-slate-700"><p>✓ {article.quality.citationCount} verified citations</p><p>✓ {article.quality.sourceDocumentCount} source documents</p><p>✓ {Math.round(article.quality.confidence * 100)}% confidence</p><p>✓ {article.quality.relatedArticleCount} related articles</p><p className={article.quality.linkedConflictCount ? 'text-amber-700' : ''}>{article.quality.linkedConflictCount ? '⚠' : '✓'} {article.quality.linkedConflictCount} linked conflicts</p><p className={article.quality.potentiallyOutdatedCount ? 'text-amber-700' : ''}>{article.quality.potentiallyOutdatedCount ? '⚠' : '✓'} {article.quality.potentiallyOutdatedCount} potentially outdated findings</p></div></div>
        <div className="rounded-xl border border-teal-200 bg-teal-50 p-5"><h2 className="font-semibold text-teal-950">Verified source evidence</h2><p className="mt-1 text-sm text-teal-900">Read-only snippets preserved exactly from the source documents.</p><div className="mt-4 space-y-4">{article.citations.map(citation => <div key={`${citation.sourceDocumentId}-${citation.evidenceSnippet}`} className="rounded-lg border border-teal-100 bg-white p-4 text-sm"><p className="font-semibold text-slate-900">{citation.fileName}</p><p className="mt-1 text-xs text-slate-500">{citation.originalPath}</p><blockquote className="mt-3 max-h-36 overflow-auto whitespace-pre-wrap border-l-2 border-teal-500 pl-3 leading-6 text-slate-700">{citation.evidenceSnippet}</blockquote></div>)}</div></div>
      </aside>
    </div>
    {!locked && showActionJump && <button type="button" onClick={() => actionRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })} className="fixed bottom-6 right-6 z-20 inline-flex cursor-pointer items-center gap-2 rounded-full border border-slate-300/80 bg-white/70 px-4 py-2.5 text-sm font-semibold text-slate-700 shadow-lg backdrop-blur transition hover:bg-white/95 hover:text-teal-800"><span>Actions</span><ArrowDown size={16} /></button>}
  </section>
}
