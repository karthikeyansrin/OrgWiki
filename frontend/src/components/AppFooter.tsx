import { ExternalLink, GitBranch } from 'lucide-react'

const repositoryUrl = 'https://github.com/karthikeyansrin/OrgWiki'

export function AppFooter() {
  return <footer className="border-t border-slate-200 bg-white text-slate-700">
    <div className="mx-auto flex w-full max-w-6xl flex-col gap-7 px-5 py-9 sm:flex-row sm:items-end sm:justify-between sm:px-8">
      <div>
        <p className="text-lg font-semibold text-slate-950">OrgWiki</p>
        <p className="mt-1 text-sm font-medium text-teal-800">AI-powered Organizational Knowledge Platform</p>
        <p className="mt-3 max-w-md text-sm leading-6 text-slate-600">Transforming fragmented organizational knowledge into trusted, searchable documentation.</p>
      </div>
      <div className="flex flex-col gap-4 sm:items-end">
        <a href={repositoryUrl} target="_blank" rel="noreferrer" className="inline-flex w-fit items-center gap-2 text-sm font-semibold text-slate-700 transition hover:text-teal-800"><GitBranch size={17} />GitHub <ExternalLink size={14} /></a>
        <p className="text-sm text-slate-500">Built with React <span aria-hidden="true">•</span> .NET <span aria-hidden="true">•</span> PostgreSQL <span aria-hidden="true">•</span> OpenAI</p>
      </div>
    </div>
  </footer>
}
