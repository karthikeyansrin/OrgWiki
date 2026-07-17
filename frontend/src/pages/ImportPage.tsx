import { useRef, useState } from 'react'
import { Archive, ArrowUp, CheckCircle2, FileWarning, UploadCloud } from 'lucide-react'
import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { uploadArchive } from '../api/client'

export function ImportPage() {
  const inputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const mutation = useMutation({
    mutationFn: uploadArchive,
    onSuccess: (result) => navigate(`/uploads/${result.uploadId}`),
    onError: (err) => setError(err instanceof Error ? err.message : 'The archive could not be processed.'),
  })

  function choose(next: File | undefined) {
    setError(null)
    if (!next) return
    if (!next.name.toLowerCase().endsWith('.zip')) {
      setFile(null)
      setError('Please choose a ZIP archive.')
      return
    }
    setFile(next)
  }

  return <section className="mx-auto max-w-3xl">
    <div className="mb-10 max-w-2xl">
      <p className="mb-3 text-sm font-semibold uppercase tracking-[0.18em] text-teal-700">Import</p>
      <h1 className="text-4xl font-semibold tracking-tight text-slate-950 sm:text-5xl">Import organizational knowledge</h1>
      <p className="mt-5 text-lg leading-8 text-slate-600">Upload a ZIP archive containing your organization&apos;s PDF, DOCX, Markdown, and text documents. OrgWiki will extract and prepare them for knowledge analysis.</p>
    </div>
    <div
      className="rounded-2xl border-2 border-dashed border-slate-300 bg-white p-10 text-center transition hover:border-teal-500 hover:bg-teal-50/30"
      onDragOver={(event) => event.preventDefault()}
      onDrop={(event) => { event.preventDefault(); choose(event.dataTransfer.files[0]) }}
    >
      <div className="mx-auto grid size-14 place-items-center rounded-full bg-teal-100 text-teal-800"><UploadCloud size={26} /></div>
      <h2 className="mt-5 text-xl font-semibold text-slate-900">Drop your knowledge archive here</h2>
      <p className="mt-2 text-sm text-slate-500">ZIP files only · PDF, DOCX, Markdown, and text documents</p>
      <button type="button" onClick={() => inputRef.current?.click()} className="mt-6 rounded-lg bg-slate-900 px-5 py-2.5 text-sm font-semibold text-white hover:bg-slate-700">Choose ZIP archive</button>
      <input ref={inputRef} type="file" accept=".zip,application/zip" className="hidden" onChange={(event) => choose(event.target.files?.[0])} />
    </div>
    {file && <div className="mt-5 flex items-center justify-between rounded-xl border border-slate-200 bg-white p-4"><div className="flex items-center gap-3"><Archive className="text-teal-700" size={20} /><div><p className="font-medium text-slate-900">{file.name}</p><p className="text-sm text-slate-500">{(file.size / 1024 / 1024).toFixed(2)} MB</p></div></div><button type="button" onClick={() => mutation.mutate(file)} disabled={mutation.isPending} className="flex items-center gap-2 rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"><ArrowUp size={16} />{mutation.isPending ? 'Processing…' : 'Import archive'}</button></div>}
    {error && <p className="mt-4 flex items-center gap-2 rounded-lg bg-rose-50 p-3 text-sm text-rose-700"><FileWarning size={17} />{error}</p>}
    <p className="mt-8 flex items-center justify-center gap-2 text-sm text-slate-500"><CheckCircle2 size={16} className="text-teal-700" />Your original archive is kept securely for this workspace.</p>
    <div className="mt-8 rounded-xl border border-slate-200 bg-white p-5"><p className="font-semibold text-slate-900">MVP limits</p><ul className="mt-3 grid gap-2 text-sm text-slate-600 sm:grid-cols-2"><li>Up to 10 supported documents</li><li>Maximum archive size: 10 MB</li><li>PDF, DOCX, Markdown, and TXT</li><li>PDFs up to 50 pages</li></ul></div>
  </section>
}
