const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const number = new Intl.NumberFormat('pt-BR')
const dateFmt = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' })
const dateTimeFmt = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
const timeFmt = new Intl.DateTimeFormat('pt-BR', { timeStyle: 'short' })
const longDateFmt = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })

type Nullable<T> = T | null | undefined

export const fmt = {
  money: (value: Nullable<number>) => (value == null ? '—' : money.format(value)),
  number: (value: Nullable<number>) => (value == null ? '—' : number.format(value)),
  percent: (value: Nullable<number>) => (value == null ? '—' : `${number.format(value)}%`),
  /** Data-hora ISO (UTC) → dd/mm/aaaa no fuso local. */
  date: (iso: Nullable<string>) => (iso ? dateFmt.format(new Date(iso)) : '—'),
  dateTime: (iso: Nullable<string>) => (iso ? dateTimeFmt.format(new Date(iso)) : '—'),
  time: (iso: Nullable<string>) => (iso ? timeFmt.format(new Date(iso)) : '—'),
  longDate: (iso: Nullable<string | Date>) => (iso ? longDateFmt.format(new Date(iso)) : '—'),
  /** DateOnly da API ("aaaa-mm-dd") → dd/mm/aaaa, sem deslocamento de fuso. */
  day: (value: Nullable<string>) => {
    if (!value) return '—'
    const [y, m, d] = value.slice(0, 10).split('-')
    return `${d}/${m}/${y}`
  },
  /** Competência "aaaa-mm" → mm/aaaa. */
  competence: (value: Nullable<string>) => (value ? `${value.slice(5, 7)}/${value.slice(0, 4)}` : '—'),
  /** Minutos desde 00:00 → "08:00" (1440 → "00:00"). */
  minutes: (value: Nullable<number>) => (value == null ? '—' : `${String(Math.floor(value / 60) % 24).padStart(2, '0')}:${String(value % 60).padStart(2, '0')}`),
  cpf: (value: Nullable<string>) => {
    const d = (value ?? '').replace(/\D/g, '')
    return d.length === 11 ? `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}` : value || '—'
  },
  cnpj: (value: Nullable<string>) => {
    const d = (value ?? '').replace(/\D/g, '')
    return d.length === 14 ? `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}` : value || '—'
  },
  phone: (value: Nullable<string>) => {
    const d = (value ?? '').replace(/\D/g, '')
    if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`
    if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`
    return value || '—'
  },
  fileSize: (bytes: number) => (bytes >= 1048576 ? `${(bytes / 1048576).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`),
}

/** Máscaras aplicadas enquanto o usuário digita. */
export const masks = {
  cpf: (v: string) => v.replace(/\D/g, '').slice(0, 11).replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d{1,2})$/, '$1-$2'),
  cnpj: (v: string) => v.replace(/\D/g, '').slice(0, 14).replace(/^(\d{2})(\d)/, '$1.$2').replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3').replace(/\.(\d{3})(\d)/, '.$1/$2').replace(/(\d{4})(\d)/, '$1-$2'),
  phone: (v: string) => {
    const d = v.replace(/\D/g, '').slice(0, 11)
    if (d.length <= 2) return d
    if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`
    if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`
    return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`
  },
  cep: (v: string) => v.replace(/\D/g, '').slice(0, 8).replace(/(\d{5})(\d)/, '$1-$2'),
}

export const dates = {
  /** Hoje como "aaaa-mm-dd" (fuso local). */
  today: () => dates.toDay(new Date()),
  toDay: (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`,
  addDays: (day: string, amount: number) => {
    const [y, m, d] = day.split('-').map(Number)
    return dates.toDay(new Date(y, m - 1, d + amount))
  },
  firstOfMonth: (d = new Date()) => dates.toDay(new Date(d.getFullYear(), d.getMonth(), 1)),
  lastOfMonth: (d = new Date()) => dates.toDay(new Date(d.getFullYear(), d.getMonth() + 1, 0)),
  /** ISO (UTC) → valor para <input type="datetime-local"> no fuso local. */
  toInput: (iso: Nullable<string>) => {
    if (!iso) return ''
    const d = new Date(iso)
    return `${dates.toDay(d)}T${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
  },
  /** Valor de <input type="datetime-local"> → ISO (UTC) para a API. */
  fromInput: (value: Nullable<string>) => (value ? new Date(value).toISOString() : null),
  /** Início do dia local (aaaa-mm-dd) → ISO (UTC). */
  startOfDay: (day: string) => new Date(`${day}T00:00:00`).toISOString(),
  endOfDay: (day: string) => new Date(`${day}T23:59:59.999`).toISOString(),
}

/** Converte texto digitado ("1.234,56") em número; vazio → null. */
export function parseMoney(value: string): number | null {
  const cleaned = value.replace(/[^\d,.-]/g, '').replace(/\./g, '').replace(',', '.')
  if (cleaned === '' || cleaned === '-') return null
  const parsed = Number(cleaned)
  return Number.isFinite(parsed) ? parsed : null
}

export const emptyToNull = (value: string | null | undefined) => (value == null || value.trim() === '' ? null : value.trim())
