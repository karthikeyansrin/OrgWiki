import { useQuery } from '@tanstack/react-query'
import { Archive, ChevronRight, LoaderCircle } from 'lucide-react'
import { Link } from 'react-router-dom'
import { getUploads } from '../api/client'

function statusClass(status: string | null) {
  if (!status) return 'bg-slate-100 text-slate-600'
  if (status === 'Completed') return 'bg-teal-100 text-teal-800'
  if (status === 'Processing') return 'bg-sky-100 text-sky-800'
  if (status === 'Failed') return 'bg-rose-100 text-rose-800'
  return 'bg-amber-100 text-amber-800'
}

export function UploadsPage() {
  const query = useQuery({ queryKey: ['uploads'], queryFn: getUploads })
  if (query.isLoading) return <div className="flex items-center gap-3 text-slate-600"><LoaderCircle className="animate-spin" size={20} />Loading uploads…</div>
  if (query.isError) return <div className="rounded-xl bg-rose-50 p-5 text-rose-700">{query.error.message}</div>
  const uploads = query.data ?? []

  return <section>
    <div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-teal-700">Uploads</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Your uploads</h1><p className="mt-3 text-slate-600">Open an archive to review its documents and continue its knowledge workflow.</p></div>
    {uploads.length === 0 ? <div className="mt-8 rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center"><Archive className="mx-auto text-slate-400" size={28} /><h2 className="mt-4 font-semibold text-slate-900">No uploads yet</h2><p className="mt-2 text-sm text-slate-600">Import a knowledge archive to begin building your workspace.</p><Link to="/import" className="mt-5 inline-flex rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white">Import an archive</Link></div> : <div className="mt-8 overflow-hidden rounded-xl border border-slate-200 bg-white"><div className="hidden grid-cols-[minmax(0,1.8fr)_1fr_repeat(3,minmax(0,0.8fr))] gap-4 border-b border-slate-200 bg-slate-50 px-5 py-3 text-xs font-semibold uppercase tracking-wide text-slate-500 md:grid"><span>Archive</span><span>Uploaded</span><span>Status</span><span>Documents</span><span>Workflow</span></div><div className="divide-y divide-slate-100">{uploads.map(upload => <Link key={upload.uploadId} to={`/uploads/${upload.uploadId}`} className="grid gap-3 px-5 py-4 transition hover:bg-teal-50/50 md:grid-cols-[minmax(0,1.8fr)_1fr_repeat(3,minmax(0,0.8fr))] md:items-center md:gap-4"><div className="min-w-0"><p className="truncate font-semibold text-slate-900">{upload.fileName}</p><p className="mt-1 text-sm text-slate-500 md:hidden">Uploaded {new Date(upload.createdAtUtc).toLocaleDateString()}</p></div><p className="hidden text-sm text-slate-600 md:block">{new Date(upload.createdAtUtc).toLocaleDateString()}</p><span className={`w-fit rounded-full px-2.5 py-1 text-xs font-semibold ${statusClass(upload.status)}`}>{upload.status}</span><p className="text-sm text-slate-600">{upload.documentCount} documents</p><div className="flex items-center gap-2 text-sm"><span className={`rounded-full px-2 py-1 text-xs font-semibold ${statusClass(upload.analysisStatus)}`}>Analysis: {upload.analysisStatus ?? 'Not started'}</span><span className={`hidden rounded-full px-2 py-1 text-xs font-semibold lg:inline ${statusClass(upload.generationStatus)}`}>Generation: {upload.generationStatus ?? 'Not started'}</span><ChevronRight className="ml-auto text-slate-400" size={18} /></div></Link>)}</div></div>}
  </section>
}
