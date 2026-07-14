import { CheckCircle2, FileWarning, LoaderCircle } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { getUpload } from '../api/client'

export function UploadDetailsPage() {
  const { uploadId = '' } = useParams()
  const query = useQuery({ queryKey: ['upload', uploadId], queryFn: () => getUpload(uploadId), enabled: Boolean(uploadId) })
  if (query.isLoading) return <div className="flex items-center gap-3 text-slate-600"><LoaderCircle className="animate-spin" size={20} />Loading processed documents…</div>
  if (query.isError) return <div className="rounded-xl bg-rose-50 p-5 text-rose-700">{query.error.message}</div>
  const result = query.data!
  return <section>
    <Link to="/import" className="text-sm font-semibold text-teal-700 hover:text-teal-900">← Import another archive</Link>
    <div className="mt-6 flex flex-wrap items-end justify-between gap-5"><div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-teal-700">Knowledge archive processed</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">{result.fileName}</h1></div><span className="rounded-full bg-teal-100 px-3 py-1.5 text-sm font-semibold text-teal-800">{result.status === 'CompletedWithErrors' ? 'Completed with attention' : result.status}</span></div>
    <div className="mt-8 grid gap-3 sm:grid-cols-4">{[['Discovered', result.totalFiles], ['Supported', result.supportedFiles], ['Parsed', result.parsedFiles], ['Needs attention', result.failedFiles]].map(([label, value]) => <div className="rounded-xl border border-slate-200 bg-white p-4" key={label as string}><p className="text-sm text-slate-500">{label}</p><p className="mt-1 text-2xl font-semibold">{value}</p></div>)}</div>
    <div className="mt-10 overflow-hidden rounded-xl border border-slate-200 bg-white"><div className="border-b border-slate-200 px-5 py-4"><h2 className="font-semibold">Documents in this archive</h2></div><div className="divide-y divide-slate-100">{result.documents.map((document) => <div className="flex flex-wrap items-center justify-between gap-4 px-5 py-4" key={document.id}><div className="min-w-0"><p className="truncate font-medium text-slate-900">{document.fileName}</p><p className="truncate text-sm text-slate-500">{document.originalPath}</p></div><div className="flex items-center gap-5 text-sm text-slate-500"><span>{document.wordCount.toLocaleString()} words</span>{document.processingStatus === 'Parsed' ? <span className="flex items-center gap-1.5 font-medium text-teal-700"><CheckCircle2 size={16} />Parsed</span> : <span className="flex items-center gap-1.5 font-medium text-amber-700" title={document.processingError ?? undefined}><FileWarning size={16} />Needs attention</span>}</div></div>)}</div></div>
    {result.isEligibleForAnalysis ? <div className="mt-8 rounded-xl border border-teal-200 bg-teal-50 p-5"><p className="font-semibold text-teal-900">Ready for knowledge analysis</p><p className="mt-1 text-sm text-teal-800">Your source material is prepared. Knowledge analysis will be available in the next phase.</p></div> : <div className="mt-8 rounded-xl border border-amber-200 bg-amber-50 p-5"><p className="font-semibold text-amber-900">Knowledge archive exceeds the current MVP analysis limit</p><p className="mt-1 text-sm leading-6 text-amber-800">Your documents were successfully extracted, but the archive contains {result.totalCharacterCount.toLocaleString()} characters of knowledge content. The current Build Week MVP supports up to 300,000 characters per analysis. Reduce the archive and upload again.</p></div>}
  </section>
}
