import { X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Badge, Input, Spinner } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { labels } from '@/lib/labels'

export type Target = { id: string; name: string }

/** Seleção de vários usuários (destinatários de um comunicado específico). Requer a permissão users.view. */
export function UserPicker({ value, onChange }: { value: Target[]; onChange: (value: Target[]) => void }) {
  const [term, setTerm] = useState('')
  const [debounced, setDebounced] = useState('')
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => { const timer = setTimeout(() => setDebounced(term), 250); return () => clearTimeout(timer) }, [term])
  useEffect(() => {
    const onDown = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false) }
    document.addEventListener('mousedown', onDown)
    return () => document.removeEventListener('mousedown', onDown)
  }, [])
  const results = useQuery({
    queryKey: ['usuarios', 'busca', debounced],
    queryFn: () => api.get<Paged<Schema<'UserDto'>>>('/api/usuarios', { search: debounced, pageSize: 8 }),
    enabled: open && debounced.trim().length >= 2,
  })
  const available = (results.data?.items ?? []).filter((user) => !value.some((target) => target.id === user.id))

  return (
    <div>
      <div className="relative" ref={ref}>
        <Input value={term} placeholder="Buscar usuário por nome ou e-mail…" onFocus={() => setOpen(true)} onChange={(e) => { setTerm(e.target.value); setOpen(true) }} />
        {open && debounced.trim().length >= 2 && (
          <div className="animate-pop absolute z-30 mt-1 max-h-64 w-full overflow-y-auto rounded-xl border border-line bg-surface p-1 shadow-(--shadow-pop)">
            {results.isLoading ? <div className="flex justify-center py-4"><Spinner /></div>
              : results.error ? <p className="px-3 py-3 text-sm text-danger">Não foi possível buscar os usuários.</p>
              : available.length === 0 ? <p className="px-3 py-3 text-sm text-muted">Nenhum usuário encontrado.</p>
              : available.map((user) => (
                <button type="button" key={user.id} onClick={() => { onChange([...value, { id: user.id, name: user.name }]); setTerm(''); setOpen(false) }}
                  className="flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-sm hover:bg-canvas">
                  <span className="min-w-0"><span className="block truncate font-medium text-ink">{user.name}</span><span className="block truncate text-xs text-muted">{user.email}</span></span>
                  <Badge tone={labels.userKind[user.kind].tone}>{labels.userKind[user.kind].label}</Badge>
                </button>
              ))}
          </div>
        )}
      </div>
      {value.length > 0 && (
        <ul className="mt-2 flex flex-wrap gap-1.5">
          {value.map((target) => (
            <li key={target.id} className="inline-flex items-center gap-1 rounded-full bg-brand-50 py-1 pr-1.5 pl-2.5 text-xs font-medium text-brand-700">
              {target.name || 'Usuário'}
              <button type="button" aria-label={`Remover ${target.name}`} className="rounded-full p-0.5 hover:bg-black/10" onClick={() => onChange(value.filter((t) => t.id !== target.id))}><X className="size-3" /></button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
