type Props = { cards?: number; rows?: number }

export function PageSkeleton({ cards = 4, rows = 3 }: Props) {
  return <div className="animate-fade-in" aria-busy="true" aria-label="Loading content">
    <div className="skeleton h-3 w-28 rounded" />
    <div className="skeleton mt-4 h-10 w-80 max-w-full rounded" />
    <div className="skeleton mt-4 h-5 w-full max-w-xl rounded" />
    <div className="mt-9 grid gap-3 sm:grid-cols-4">{Array.from({ length: cards }, (_, index) => <div key={index} className="rounded-xl border border-slate-200 bg-white p-5"><div className="skeleton h-3 w-20 rounded" /><div className="skeleton mt-3 h-8 w-12 rounded" /></div>)}</div>
    <div className="mt-8 space-y-3">{Array.from({ length: rows }, (_, index) => <div key={index} className="rounded-xl border border-slate-200 bg-white p-5"><div className="skeleton h-4 w-2/5 rounded" /><div className="skeleton mt-3 h-3 w-full rounded" /><div className="skeleton mt-2 h-3 w-3/4 rounded" /></div>)}</div>
  </div>
}
