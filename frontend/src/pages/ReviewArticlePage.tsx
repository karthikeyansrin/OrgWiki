import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { AlertTriangle, CheckCircle2, LoaderCircle, Save, Send, XCircle } from 'lucide-react'
import { approveReviewArticle, getReviewArticle, publishReviewArticle, rejectReviewArticle, updateReviewArticle } from '../api/client'

type Draft = {
  title: string
  summary: string
  markdownContent: string
  difficulty: string
  estimatedReadingMinutes: number
  tags: string[]
  relatedArticleKeys: string[]
}

export function ReviewArticlePage() {
  const { articleId = '' } = useParams()
  const navigate = useNavigate()
  const client = useQueryClient()
  const query = useQuery({ queryKey: ['review-article', articleId], queryFn: () => getReviewArticle(articleId), enabled: Boolean(articleId) })
  const [draft, setDraft] = useState<Draft | null>(null)
  const [notes, setNotes] = useState('')

  useEffect(() => {
    if (!query.data) return
    const article = query.data
    setDraft({
      title: article.title,
      summary: article.summary,
      markdownContent: article.markdownContent,
      difficulty: article.difficulty,
      estimatedReadingMinutes: article.estimatedReadingMinutes,
      tags: article.tags,
      relatedArticleKeys: article.relatedArticleKeys
    })
    setNotes(article.reviewNotes ?? '')
  }, [query.data])

  const refresh = async () => {
    await client.invalidateQueries({ queryKey: ['review-article', articleId] })
    await client.invalidateQueries({ queryKey: ['review-dashboard'] })
  }

  const save = useMutation({ mutationFn: () => updateReviewArticle(articleId, draft!), onSuccess: refresh })
  const approve = useMutation({ mutationFn: () => approveReviewArticle(articleId, notes), onSuccess: refresh })
  const reject = useMutation({ mutationFn: () => rejectReviewArticle(articleId, notes), onSuccess: () => navigate('/review') })
  const publish = useMutation({ mutationFn: () => publishReviewArticle(articleId), onSuccess: () => navigate('/review') })

  if (query.isLoading || !draft) return <div className="flex items-center gap-3 text-slate-600"><LoaderCircle className="animate-spin" size={20} />Loading article review...</div>
  if (query.isError) return <div className="rounded-xl bg-rose-50 p-5 text-rose-700">{query.error.message}</div>

  const article = query.data!
  const locked = article.status === 'Published'
  const rejected = article.status === 'Rejected'
  const update = <K extends keyof Draft>(key: K, value: Draft[K]) => setDraft(current => current ? { ...current, [key]: value } : current)
  const reset = () => setDraft({
    title: article.title,
    summary: article.summary,
    markdownContent: article.markdownContent,
    difficulty: article.difficulty,
    estimatedReadingMinutes: article.estimatedReadingMinutes,
    tags: article.tags,
    relatedArticleKeys: article.relatedArticleKeys
  })
  const error = save.error?.message ?? approve.error?.message ?? reject.error?.message ?? publish.error?.message

  return <section>
    <Link to="/review" className="text-sm font-semibold text-teal-700">← Review</Link>
    <div className="mt-6 flex flex-wrap items-start justify-between gap-4">
      <div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-teal-700">Article review</p><h1 className="mt-2 text-3xl font-semibold">{article.title}</h1></div>
      <span className="rounded-full bg-amber-100 px-3 py-1 text-sm font-semibold text-amber-800">{article.status}</span>
    </div>

    {locked ? <div className="mt-4 rounded-lg border border-teal-200 bg-teal-50 p-4 text-teal-900">Published articles are read-only. <Link to={`/knowledge/articles/${article.key}`} className="font-semibold underline">View in Knowledge Base</Link></div> : <p className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900"><AlertTriangle size={16} className="mr-2 inline" />This article has AI-generated content and requires human approval before publishing.</p>}

    <div className="mt-8 grid gap-8 lg:grid-cols-[minmax(0,1fr)_22rem]">
      <div className="space-y-5">
        <label className="block text-sm font-medium">Title<input disabled={locked} value={draft.title} onChange={event => update('title', event.target.value)} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 disabled:bg-slate-100" /></label>
        <label className="block text-sm font-medium">Summary<textarea disabled={locked} value={draft.summary} onChange={event => update('summary', event.target.value)} className="mt-1 min-h-24 w-full rounded-lg border border-slate-300 px-3 py-2 disabled:bg-slate-100" /></label>
        <label className="block text-sm font-medium">Markdown<textarea disabled={locked} value={draft.markdownContent} onChange={event => update('markdownContent', event.target.value)} className="mt-1 min-h-80 w-full rounded-lg border border-slate-300 px-3 py-2 font-mono text-sm disabled:bg-slate-100" /></label>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="text-sm font-medium">Difficulty<select disabled={locked} value={draft.difficulty} onChange={event => update('difficulty', event.target.value)} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 disabled:bg-slate-100">{['Beginner', 'Intermediate', 'Advanced'].map(value => <option key={value}>{value}</option>)}</select></label>
          <label className="text-sm font-medium">Estimated reading time<input disabled={locked} type="number" min="1" value={draft.estimatedReadingMinutes} onChange={event => update('estimatedReadingMinutes', Number(event.target.value))} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 disabled:bg-slate-100" /></label>
        </div>
        <label className="block text-sm font-medium">Tags<input disabled={locked} value={draft.tags.join(', ')} onChange={event => update('tags', event.target.value.split(',').map(tag => tag.trim()).filter(Boolean))} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 disabled:bg-slate-100" /></label>
        <label className="block text-sm font-medium">Related articles<select disabled={locked} multiple value={draft.relatedArticleKeys} onChange={event => update('relatedArticleKeys', Array.from(event.target.selectedOptions).map(option => option.value))} className="mt-1 min-h-28 w-full rounded-lg border border-slate-300 px-3 py-2 disabled:bg-slate-100">{article.availableRelatedArticles.map(item => <option key={item.id} value={item.key}>{item.title}</option>)}</select></label>
        <label className="block text-sm font-medium">Review notes<textarea disabled={locked} value={notes} onChange={event => setNotes(event.target.value)} className="mt-1 min-h-24 w-full rounded-lg border border-slate-300 px-3 py-2 disabled:bg-slate-100" /></label>

        {!locked && <div className="flex flex-wrap gap-3">
          <button type="button" onClick={() => save.mutate()} className="inline-flex items-center gap-2 rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white"><Save size={16} />Save</button>
          <button type="button" onClick={reset} className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-semibold">Undo unsaved changes</button>
          {article.status === 'PendingReview' && <>
            <button type="button" onClick={() => approve.mutate()} className="inline-flex items-center gap-2 rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white"><CheckCircle2 size={16} />Approve</button>
            <button type="button" onClick={() => reject.mutate()} className="inline-flex items-center gap-2 rounded-lg bg-rose-700 px-4 py-2.5 text-sm font-semibold text-white"><XCircle size={16} />Reject</button>
          </>}
          {rejected && <button type="button" onClick={() => approve.mutate()} className="inline-flex items-center gap-2 rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white"><CheckCircle2 size={16} />Approve</button>}
          {article.status === 'Approved' && <button type="button" onClick={() => publish.mutate()} className="inline-flex items-center gap-2 rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white"><Send size={16} />Publish</button>}
          <button type="button" onClick={() => navigate('/review')} className="rounded-lg px-4 py-2.5 text-sm font-semibold text-slate-600">Cancel</button>
        </div>}
        {error && <p className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{String(error)}</p>}
      </div>

      <aside className="space-y-5">
        <div className="rounded-xl border border-slate-200 bg-white p-5"><h2 className="font-semibold">Knowledge Quality</h2><div className="mt-4 space-y-2 text-sm"><p>✓ {article.quality.citationCount} citations</p><p>✓ {article.quality.sourceDocumentCount} source documents</p><p>✓ {Math.round(article.quality.confidence * 100)}% confidence</p><p>✓ {article.quality.relatedArticleCount} related articles</p><p className={article.quality.linkedConflictCount ? 'text-amber-700' : ''}>{article.quality.linkedConflictCount ? '⚠' : '✓'} {article.quality.linkedConflictCount} linked conflicts</p><p className={article.quality.potentiallyOutdatedCount ? 'text-amber-700' : ''}>{article.quality.potentiallyOutdatedCount ? '⚠' : '✓'} {article.quality.potentiallyOutdatedCount} potentially outdated findings</p></div></div>
        <div className="rounded-xl border border-slate-200 bg-white p-5"><h2 className="font-semibold">Verified citations</h2><div className="mt-4 space-y-4">{article.citations.map(citation => <div key={`${citation.sourceDocumentId}-${citation.evidenceSnippet}`} className="text-sm"><p className="font-medium">{citation.fileName}</p><p className="text-xs text-slate-500">{citation.originalPath}</p><blockquote className="mt-2 border-l-2 border-teal-500 pl-3 text-slate-600">{citation.evidenceSnippet}</blockquote></div>)}</div></div>
      </aside>
    </div>
  </section>
}
