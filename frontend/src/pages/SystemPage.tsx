import { BackendStatus } from '../components/BackendStatus'

export function SystemPage() {
  return (
    <section className="max-w-2xl">
      <p className="text-sm font-medium text-teal-700">System</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-normal">Platform connection</h1>
      <div className="mt-8 border border-slate-200 bg-white p-6">
        <p className="text-sm text-slate-500">Backend API</p>
        <div className="mt-3">
          <BackendStatus />
        </div>
      </div>
    </section>
  )
}
