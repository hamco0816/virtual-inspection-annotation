// 页头的后端连接状态：定时调用运行状态检查接口，显示已连接 / 未连接。
import { useEffect, useState } from 'react'
import { checkHealth } from '../api/issueApi'

const HEALTH_CHECK_INTERVAL_MS = 10_000

type ConnectionState = 'checking' | 'online' | 'offline'

const CONNECTION_LABELS: Record<ConnectionState, string> = {
  checking: '正在检查后端',
  online: '后端已连接',
  offline: '后端未连接',
}

export function BackendStatus() {
  const [connectionState, setConnectionState] = useState<ConnectionState>('checking')

  useEffect(() => {
    let cancelled = false
    const runCheck = () => {
      checkHealth()
        .then(() => !cancelled && setConnectionState('online'))
        .catch(() => !cancelled && setConnectionState('offline'))
    }
    runCheck()
    const intervalId = window.setInterval(runCheck, HEALTH_CHECK_INTERVAL_MS)
    return () => {
      cancelled = true
      window.clearInterval(intervalId)
    }
  }, [])

  return (
    <span className={`backend-status backend-status--${connectionState}`} role="status">
      <span className="backend-status__dot" aria-hidden="true" />
      {CONNECTION_LABELS[connectionState]}
    </span>
  )
}
