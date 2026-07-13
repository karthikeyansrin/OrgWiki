import { useQuery } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, LoaderCircle } from 'lucide-react'
import { getHealth } from '../api/client'

export function BackendStatus() {
  const healthQuery = useQuery({ queryKey: ['health'], queryFn: getHealth })

  if (healthQuery.isPending) {
    return (
      <span className="flex items-center gap-2 text-sm text-slate-500">
        <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
        Checking service
      </span>
    )
  }

  if (healthQuery.isError) {
    return (
      <span className="flex items-center gap-2 text-sm text-rose-700">
        <AlertCircle className="size-4" aria-hidden="true" />
        Service unavailable
      </span>
    )
  }

  return (
    <span className="flex items-center gap-2 text-sm text-emerald-700">
      <CheckCircle2 className="size-4" aria-hidden="true" />
      Service connected
    </span>
  )
}
