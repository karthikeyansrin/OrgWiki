import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, FolderPlus, Plus, Save, Trash2, UsersRound } from 'lucide-react'
import { createTeamSpace, deleteTeamSpace, getArticleTeamSpaces, getTeamSpaces, updateArticleTeamSpaces } from '../api/client'

function slugFromName(name: string) {
  return name.toLowerCase().trim().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
}

export function TeamSpaceManager({ articleKey }: { articleKey: string }) {
  const client = useQueryClient()
  const spaces = useQuery({ queryKey: ['team-spaces'], queryFn: getTeamSpaces })
  const assignments = useQuery({ queryKey: ['article-team-spaces', articleKey], queryFn: () => getArticleTeamSpaces(articleKey) })
  const [selected, setSelected] = useState<string[]>([])
  const [assignedSpaceIds, setAssignedSpaceIds] = useState<string[]>([])
  const [showCreate, setShowCreate] = useState(false)
  const [spaceToDelete, setSpaceToDelete] = useState<{ id: string; name: string } | null>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  useEffect(() => {
    if (assignments.data) setAssignedSpaceIds(assignments.data.teamSpaces.map(space => space.id))
  }, [assignments.data])

  const refresh = async () => {
    await client.invalidateQueries({ queryKey: ['article-team-spaces', articleKey] })
    await client.invalidateQueries({ queryKey: ['team-spaces'] })
  }
  const save = useMutation({
    mutationFn: () => updateArticleTeamSpaces(articleKey, [...new Set([...assignedSpaceIds, ...selected])]),
    onSuccess: async assignments => {
      setAssignedSpaceIds(assignments.teamSpaces.map(space => space.id))
      setSelected([])
      await refresh()
    }
  })
  const create = useMutation({
    mutationFn: () => createTeamSpace({ name, slug: slugFromName(name), description }),
    onSuccess: async space => {
      await client.invalidateQueries({ queryKey: ['team-spaces'] })
      setSelected(current => current.includes(space.id) ? current : [...current, space.id])
      setName('')
      setDescription('')
      setShowCreate(false)
    }
  })
  const remove = useMutation({
    mutationFn: (id: string) => deleteTeamSpace(id),
    onSuccess: async (_, id) => {
      setSelected(current => current.filter(value => value !== id))
      setAssignedSpaceIds(current => current.filter(value => value !== id))
      setSpaceToDelete(null)
      await refresh()
    }
  })
  const toggle = (id: string) => setSelected(current => current.includes(id) ? current.filter(value => value !== id) : [...current, id])
  const closeDeleteDialog = () => {
    if (remove.isPending) return
    remove.reset()
    setSpaceToDelete(null)
  }

  return <>
    <section className="mt-8 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex gap-3"><span className="grid size-10 place-items-center rounded-lg bg-teal-50 text-teal-700"><UsersRound size={20} /></span><div><h2 className="font-semibold">Manage Team Spaces</h2><p className="mt-1 max-w-xl text-sm leading-6 text-slate-600">Select the public collections to add this article to. Existing Team Space assignments are preserved.</p>{assignedSpaceIds.length > 0 && <p className="mt-1 text-xs font-medium text-teal-800">Already shared in {assignedSpaceIds.length} space{assignedSpaceIds.length === 1 ? '' : 's'}.</p>}</div></div>
        <button type="button" onClick={() => setShowCreate(value => !value)} className="inline-flex cursor-pointer items-center gap-2 rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"><FolderPlus size={16} />New space</button>
      </div>
      {showCreate && <div className="mt-5 rounded-xl border border-slate-200 bg-slate-50 p-4"><p className="font-semibold text-slate-800">Create a Team Space</p><div className="mt-4 grid gap-3 sm:grid-cols-2"><label className="text-sm font-medium text-slate-700">Name<input value={name} onChange={event => setName(event.target.value)} placeholder="Technical" className="mt-1.5 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100" /></label><label className="text-sm font-medium text-slate-700">Slug<input value={slugFromName(name)} readOnly placeholder="technical" className="mt-1.5 w-full rounded-lg border border-slate-200 bg-slate-100 px-3 py-2 text-slate-600" /></label></div><label className="mt-3 block text-sm font-medium text-slate-700">Description<textarea value={description} onChange={event => setDescription(event.target.value)} placeholder="Engineering standards, architecture, and development guides." className="mt-1.5 min-h-20 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100" /></label><div className="mt-4 flex gap-3"><button type="button" disabled={create.isPending || !name.trim() || !description.trim()} onClick={() => create.mutate()} className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-teal-700 px-3 py-2 text-sm font-semibold text-white hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-50"><Plus size={16} />{create.isPending ? 'Creating...' : 'Create space'}</button><button type="button" onClick={() => setShowCreate(false)} className="cursor-pointer px-3 py-2 text-sm font-semibold text-slate-600">Cancel</button></div>{create.isError && <p className="mt-3 text-sm text-rose-700">{create.error.message}</p>}</div>}
      <div className="mt-5 grid gap-2 sm:grid-cols-2">{spaces.isLoading ? <p className="text-sm text-slate-500">Loading Team Spaces...</p> : (spaces.data ?? []).map(space => <div key={space.id} className={`flex items-start gap-3 rounded-lg border p-4 transition ${selected.includes(space.id) ? 'border-teal-300 bg-teal-50' : 'border-slate-200 hover:border-slate-300'}`}><label className="flex min-w-0 flex-1 cursor-pointer items-start gap-3"><input type="checkbox" checked={selected.includes(space.id)} onChange={() => toggle(space.id)} className="mt-1 size-4 accent-teal-700" /><span className="min-w-0"><span className="flex items-center gap-2 font-semibold text-slate-900">{space.name}{selected.includes(space.id) && <Check size={15} className="text-teal-700" />}</span><span className="mt-1 block text-sm leading-5 text-slate-600">{space.description}</span></span></label><button type="button" aria-label={`Delete ${space.name}`} title={`Delete ${space.name}`} disabled={remove.isPending} onClick={() => setSpaceToDelete({ id: space.id, name: space.name })} className="shrink-0 cursor-pointer rounded-md p-1.5 text-slate-400 transition hover:bg-rose-50 hover:text-rose-700 disabled:cursor-not-allowed disabled:opacity-50"><Trash2 size={16} /></button></div>)}</div>
      {!spaces.isLoading && spaces.data?.length === 0 && <p className="mt-5 rounded-lg border border-dashed border-slate-300 p-4 text-sm text-slate-600">Create the first Team Space, then add this article to it.</p>}
      <div className="mt-5 flex flex-wrap items-center gap-3"><button type="button" disabled={save.isPending || spaces.isLoading || assignments.isLoading || selected.length === 0} onClick={() => save.mutate()} className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"><Save size={16} />{save.isPending ? 'Saving...' : 'Save Team Spaces'}</button><span className="text-sm text-slate-500">{selected.length} space{selected.length === 1 ? '' : 's'} selected</span></div>
      {save.isError && <p className="mt-3 text-sm text-rose-700">{save.error.message}</p>}
    </section>
    {spaceToDelete && <div className="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-5" role="presentation"><div role="dialog" aria-modal="true" aria-labelledby="delete-team-space-title" className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-2xl"><div className="flex size-11 items-center justify-center rounded-full bg-rose-50 text-rose-700"><Trash2 size={20} /></div><h2 id="delete-team-space-title" className="mt-4 text-xl font-semibold text-slate-950">Delete Team Space?</h2><p className="mt-3 leading-6 text-slate-600">Delete <strong className="font-semibold text-slate-900">{spaceToDelete.name}</strong>? Its article assignments will be removed, but the published articles themselves will remain available in your Knowledge Base.</p>{remove.isError && <p className="mt-4 rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{remove.error.message}</p>}<div className="mt-6 flex flex-wrap justify-end gap-3"><button type="button" disabled={remove.isPending} onClick={closeDeleteDialog} className="cursor-pointer rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50">Cancel</button><button type="button" disabled={remove.isPending} onClick={() => remove.mutate(spaceToDelete.id)} className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-rose-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-rose-800 disabled:cursor-not-allowed disabled:opacity-50"><Trash2 size={16} />{remove.isPending ? 'Deleting...' : 'Delete Team Space'}</button></div></div></div>}
  </>
}
