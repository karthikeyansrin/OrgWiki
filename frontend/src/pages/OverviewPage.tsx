import { ArrowRight, Files, GitMerge, ScanText, ShieldCheck } from 'lucide-react'
import { BackendStatus } from '../components/BackendStatus'

const stages = [
  {
    title: 'Source material',
    detail: 'Bring fragmented organizational files into one transformation workflow.',
    icon: Files,
  },
  {
    title: 'Structured knowledge',
    detail: 'Extract durable concepts, relationships, and evidence from the material.',
    icon: ScanText,
  },
  {
    title: 'Knowledge refinement',
    detail: 'Surface duplicates and conflicts before information becomes organizational truth.',
    icon: GitMerge,
  },
  {
    title: 'Reviewed knowledge base',
    detail: 'Publish knowledge people can find, trust, and continue to improve.',
    icon: ShieldCheck,
  },
]

export function OverviewPage() {
  return (
    <div className="space-y-14">
      <section className="grid gap-8 border-b border-slate-200 pb-12 lg:grid-cols-[1.2fr_0.8fr] lg:items-end">
        <div>
          <p className="mb-4 text-sm font-semibold uppercase tracking-[0.12em] text-teal-700">
            Organizational knowledge operations
          </p>
          <h1 className="max-w-3xl text-4xl font-semibold leading-tight tracking-normal text-slate-950 sm:text-5xl">
            Transform scattered documents into knowledge your organization can maintain.
          </h1>
        </div>
        <div className="border-l-2 border-amber-400 pl-5 text-base leading-7 text-slate-600">
          OrgWiki turns source material into reviewed, structured knowledge with traceable evidence.
        </div>
      </section>

      <section aria-labelledby="workflow-heading">
        <div className="mb-6 flex items-center justify-between gap-4">
          <div>
            <p className="text-sm font-medium text-teal-700">Knowledge transformation</p>
            <h2 id="workflow-heading" className="mt-1 text-2xl font-semibold tracking-normal">
              From messy documents to structured knowledge
            </h2>
          </div>
          <BackendStatus />
        </div>
        <div className="grid gap-px overflow-hidden border border-slate-200 bg-slate-200 sm:grid-cols-2 xl:grid-cols-4">
          {stages.map(({ title, detail, icon: Icon }, index) => (
            <article key={title} className="min-h-56 bg-white p-6">
              <div className="mb-8 flex items-center justify-between">
                <span className="grid size-10 place-items-center rounded-md bg-teal-50 text-teal-800">
                  <Icon size={20} aria-hidden="true" />
                </span>
                {index < stages.length - 1 && <ArrowRight className="hidden text-slate-300 xl:block" size={18} />}
              </div>
              <h3 className="text-base font-semibold text-slate-900">{title}</h3>
              <p className="mt-3 text-sm leading-6 text-slate-600">{detail}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="grid gap-6 border-t border-slate-200 pt-8 md:grid-cols-3">
        <div>
          <p className="text-sm font-medium text-slate-500">System state</p>
          <p className="mt-2 text-lg font-semibold">Foundation ready</p>
        </div>
        <div>
          <p className="text-sm font-medium text-slate-500">Next capability</p>
          <p className="mt-2 text-lg font-semibold">Document intake</p>
        </div>
        <div>
          <p className="text-sm font-medium text-slate-500">Knowledge model</p>
          <p className="mt-2 text-lg font-semibold">Pending source analysis</p>
        </div>
      </section>
    </div>
  )
}
