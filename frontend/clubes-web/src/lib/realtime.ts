import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useEffect, useRef } from 'react'
import { API_URL, refreshSession, session } from './api'

/**
 * Eventos em tempo real publicados pela API:
 * - "notification": nova notificação para o usuário
 * - "availabilityChanged": disponibilidade de espaços mudou ({ spaceId? })
 * - "occupancyChanged": entrada/saída registrada na portaria
 */
export type RealtimeEvent = 'notification' | 'availabilityChanged' | 'occupancyChanged'

let connection: HubConnection | null = null
let starting: Promise<void> | null = null

function ensureConnection() {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/clube`, {
        accessTokenFactory: async () => session.accessToken ?? (await refreshSession())?.accessToken ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
  }
  if (connection.state === HubConnectionState.Disconnected && !starting) {
    starting = connection.start().catch(() => { /* a interface continua funcionando por consulta; a reconexão é tentada na próxima assinatura */ }).finally(() => { starting = null })
  }
  return connection
}

export function stopRealtime() {
  void connection?.stop()
  connection = null
}

/** Assina um evento em tempo real enquanto o componente estiver montado. */
export function useRealtime(event: RealtimeEvent, handler: (payload: unknown) => void) {
  const handlerRef = useRef(handler)
  handlerRef.current = handler
  useEffect(() => {
    const hub = ensureConnection()
    const listener = (payload: unknown) => handlerRef.current(payload)
    hub.on(event, listener)
    return () => hub.off(event, listener)
  }, [event])
}
