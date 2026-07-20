import { BookOpenText } from 'lucide-react'
import { Link } from 'react-router-dom'

export function PublicSpacesHeader() {
  return <header className="border-b border-slate-200 bg-white"><div className="mx-auto flex h-16 max-w-6xl items-center px-5 sm:px-8"><Link to="/" className="flex items-center gap-2.5 font-semibold text-slate-950"><span className="grid size-8 place-items-center rounded-md bg-teal-700 text-white"><BookOpenText size={18} /></span>OrgWiki</Link></div></header>
}
