import jsQR from 'jsqr'
import { Camera, CameraOff } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Alert, Button, cn, Spinner } from '@/components/ui'

// BarcodeDetector (Chrome/Edge em Android, macOS e ChromeOS) ainda não faz parte das definições do TypeScript.
type Detector = { detect: (source: HTMLVideoElement) => Promise<{ rawValue: string }[]> }
type DetectorConstructor = new (options?: { formats: string[] }) => Detector

function cameraError(error: unknown) {
  const name = error instanceof DOMException ? error.name : ''
  if (name === 'NotAllowedError' || name === 'SecurityError') return 'Permissão de câmera negada. Autorize o uso da câmera no navegador ou digite o código numérico.'
  if (name === 'NotFoundError' || name === 'OverconstrainedError') return 'Nenhuma câmera foi encontrada neste dispositivo. Digite o código numérico.'
  if (name === 'NotReadableError') return 'A câmera está em uso por outro aplicativo.'
  return 'Não foi possível acessar a câmera neste navegador. Digite o código numérico.'
}

/**
 * Leitor de QR Code pela câmera. Usa BarcodeDetector quando o navegador oferece e jsQR como alternativa.
 * Enquanto `paused` for verdadeiro (requisição em andamento ou intervalo após um resultado), nenhum código é emitido.
 */
export function QrScanner({ onCode, paused }: { onCode: (code: string) => void; paused: boolean }) {
  const video = useRef<HTMLVideoElement>(null)
  const stream = useRef<MediaStream | null>(null)
  const pausedRef = useRef(paused)
  pausedRef.current = paused
  const onCodeRef = useRef(onCode)
  onCodeRef.current = onCode
  const [active, setActive] = useState(false)
  const [starting, setStarting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const stop = useCallback(() => {
    stream.current?.getTracks().forEach((track) => track.stop())
    stream.current = null
    setActive(false)
  }, [])
  useEffect(() => stop, [stop])

  const start = async () => {
    setError(null)
    setStarting(true)
    try {
      if (!navigator.mediaDevices?.getUserMedia) throw new Error('unsupported')
      // Câmera traseira quando existir (tablets e celulares); em notebooks o navegador usa a disponível.
      stream.current = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: 'environment' }, width: { ideal: 1280 }, height: { ideal: 720 } }, audio: false })
      setActive(true)
    } catch (cause) { setError(cameraError(cause)) } finally { setStarting(false) }
  }

  useEffect(() => {
    const element = video.current
    if (!active || !element || !stream.current) return
    element.srcObject = stream.current
    void element.play().catch(() => { /* o navegador inicia a reprodução assim que o fluxo estiver pronto */ })

    const NativeDetector = (window as unknown as { BarcodeDetector?: DetectorConstructor }).BarcodeDetector
    let detector: Detector | null = null
    try { detector = NativeDetector ? new NativeDetector({ formats: ['qr_code'] }) : null } catch { detector = null }
    const canvas = document.createElement('canvas')
    const context = canvas.getContext('2d', { willReadFrequently: true })
    let cancelled = false
    let busy = false

    const timer = window.setInterval(async () => {
      if (cancelled || busy || pausedRef.current || element.readyState < 2 || element.videoWidth === 0) return
      busy = true
      try {
        let code: string | undefined
        if (detector) code = (await detector.detect(element))[0]?.rawValue
        else if (context) {
          const scale = Math.min(1, 720 / element.videoWidth)
          canvas.width = Math.round(element.videoWidth * scale)
          canvas.height = Math.round(element.videoHeight * scale)
          context.drawImage(element, 0, 0, canvas.width, canvas.height)
          const image = context.getImageData(0, 0, canvas.width, canvas.height)
          code = jsQR(image.data, image.width, image.height, { inversionAttempts: 'dontInvert' })?.data
        }
        if (code && !cancelled && !pausedRef.current) onCodeRef.current(code)
      } catch { detector = null /* detector nativo indisponível: segue com jsQR */ } finally { busy = false }
    }, 180)
    return () => { cancelled = true; window.clearInterval(timer); element.srcObject = null }
  }, [active])

  return (
    <div className="space-y-3">
      <div className={cn('relative overflow-hidden rounded-(--radius-card) bg-ink', active ? 'aspect-video' : 'hidden')}>
        <video ref={video} className="size-full object-cover" playsInline muted />
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
          <div className={cn('size-2/5 min-w-32 rounded-2xl border-2 transition-colors', paused ? 'border-white/30' : 'border-white/90')} />
        </div>
        {paused && <div className="absolute inset-x-0 bottom-0 flex items-center justify-center gap-2 bg-ink/70 py-2 text-[13px] text-white"><Spinner className="size-4 text-white" />Leitura em pausa…</div>}
      </div>
      {error && <Alert tone="warning">{error}</Alert>}
      {active
        ? <Button className="w-full" icon={CameraOff} onClick={stop}>Desligar câmera</Button>
        : <Button className="w-full" variant="primary" icon={Camera} loading={starting} onClick={() => void start()}>Ler QR Code com a câmera</Button>}
    </div>
  )
}
