import type { components } from './api-types.gen'

/** Tipo de um schema do contrato OpenAPI. Ex.: Schema<'MemberDto'>. */
export type Schema<K extends keyof components['schemas']> = components['schemas'][K]
export type Paged<T> = { items: T[]; total: number; page: number; pageSize: number }

export const API_URL: string = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5080'

const REFRESH_KEY = 'clubes.refresh'
const CLUB_KEY = 'clubes.club'

export class ApiError extends Error {
  readonly code: string
  readonly status: number
  readonly errors?: Record<string, string[]>

  constructor(status: number, code: string, message: string, errors?: Record<string, string[]>) {
    super(message)
    this.status = status
    this.code = code
    this.errors = errors
  }
}

/** Mensagem amigável para qualquer erro (da API, de rede ou inesperado). */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message
  if (error instanceof TypeError) return 'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.'
  return 'Ocorreu um erro inesperado. Tente novamente.'
}

// O token de acesso fica apenas em memória. O token de renovação fica em sessionStorage:
// é descartado ao fechar a aba e nunca é enviado automaticamente pelo navegador (sem cookies → sem CSRF).
let accessToken: string | null = null
let onSessionLost: (() => void) | null = null
let refreshing: Promise<Schema<'AuthResponse'> | null> | null = null

export const session = {
  get accessToken() { return accessToken },
  get refreshToken() { return sessionStorage.getItem(REFRESH_KEY) },
  /** Clube em que o superadministrador está operando (validado pelo servidor a cada requisição). */
  get clubId() { return sessionStorage.getItem(CLUB_KEY) },
  set clubId(value: string | null) { if (value) sessionStorage.setItem(CLUB_KEY, value); else sessionStorage.removeItem(CLUB_KEY) },
  store(auth: Schema<'AuthResponse'>) {
    accessToken = auth.accessToken
    sessionStorage.setItem(REFRESH_KEY, auth.refreshToken)
  },
  clear() {
    accessToken = null
    sessionStorage.removeItem(REFRESH_KEY)
    sessionStorage.removeItem(CLUB_KEY)
  },
  onLost(handler: () => void) { onSessionLost = handler },
}

/** Renovação com rotação: uma única chamada por vez, compartilhada por todas as requisições pendentes. */
export function refreshSession(): Promise<Schema<'AuthResponse'> | null> {
  const token = session.refreshToken
  if (!token) return Promise.resolve(null)
  refreshing ??= (async () => {
    try {
      const response = await fetch(`${API_URL}/api/auth/refresh`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refreshToken: token }),
      })
      if (!response.ok) { session.clear(); return null }
      const auth = (await response.json()) as Schema<'AuthResponse'>
      session.store(auth)
      return auth
    } catch { return null } finally { refreshing = null }
  })()
  return refreshing
}

type Query = Record<string, string | number | boolean | null | undefined | Array<string | number>>
type Options = { query?: Query; body?: unknown; signal?: AbortSignal; form?: FormData }

function buildUrl(path: string, query?: Query) {
  const url = new URL(API_URL + path)
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value === null || value === undefined || value === '') continue
    if (Array.isArray(value)) value.forEach((v) => url.searchParams.append(key, String(v)))
    else url.searchParams.set(key, String(value))
  }
  return url.toString()
}

async function send(method: string, path: string, options: Options, retry = true): Promise<Response> {
  const headers: Record<string, string> = {}
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`
  if (session.clubId) headers['X-Club-Id'] = session.clubId
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'

  const response = await fetch(buildUrl(path, options.query), {
    method, headers, signal: options.signal,
    body: options.form ?? (options.body !== undefined ? JSON.stringify(options.body) : undefined),
  })

  if (response.status === 401 && retry && !path.startsWith('/api/auth/login')) {
    const renewed = await refreshSession()
    if (renewed) return send(method, path, options, false)
    onSessionLost?.()
  }
  if (!response.ok) {
    let payload: { code?: string; message?: string; errors?: Record<string, string[]> } = {}
    try { payload = await response.json() } catch { /* resposta sem corpo JSON */ }
    throw new ApiError(response.status, payload.code ?? 'erro', payload.message ?? 'Não foi possível concluir a operação.', payload.errors)
  }
  return response
}

async function json<T>(method: string, path: string, options: Options = {}): Promise<T> {
  const response = await send(method, path, options)
  if (response.status === 204) return undefined as T
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const api = {
  get: <T>(path: string, query?: Query, signal?: AbortSignal) => json<T>('GET', path, { query, signal }),
  post: <T = void>(path: string, body?: unknown, query?: Query) => json<T>('POST', path, { body: body ?? {}, query }),
  put: <T = void>(path: string, body?: unknown, query?: Query) => json<T>('PUT', path, { body: body ?? {}, query }),
  delete: <T = void>(path: string, query?: Query) => json<T>('DELETE', path, { query }),

  /** Envia um arquivo (imagem ou PDF até 10 MB). `scope`: ClubPublic (visível ao clube), Staff (equipe e titular) ou Restricted (documentos). */
  async upload(file: File, scope: Schema<'FileScope'> = 'Restricted', ownerMemberId?: string | null): Promise<Schema<'FileDto'>> {
    const form = new FormData()
    form.append('File', file)
    form.append('Scope', scope)
    if (ownerMemberId) form.append('OwnerMemberId', ownerMemberId)
    return (await send('POST', '/api/arquivos', { form })).json()
  },

  /** Baixa um recurso autenticado (arquivos, PDFs, planilhas) como Blob. */
  async blob(path: string, query?: Query): Promise<{ blob: Blob; fileName?: string }> {
    const response = await send('GET', path, { query })
    const disposition = response.headers.get('Content-Disposition') ?? ''
    const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(disposition)
    return { blob: await response.blob(), fileName: match ? decodeURIComponent(match[1] ?? match[2]) : undefined }
  },

  /** Baixa e salva/abre um arquivo autenticado no navegador. */
  async download(path: string, query?: Query, fallbackName = 'arquivo') {
    const { blob, fileName } = await api.blob(path, query)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = fileName ?? fallbackName
    link.click()
    setTimeout(() => URL.revokeObjectURL(url), 10_000)
  },
}

/** Chave única por tentativa de operação financeira; reutilize a mesma chave ao repetir a mesma tentativa. */
export const newIdempotencyKey = () => crypto.randomUUID()
