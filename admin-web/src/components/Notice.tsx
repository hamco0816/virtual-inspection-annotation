// 操作结果提示条：显示状态修改成功或失败，几秒后自动消失。
import { useEffect } from 'react'

const NOTICE_DURATION_MS = 4000

export interface NoticeMessage {
  id: number
  tone: 'success' | 'error'
  text: string
}

interface NoticeProps {
  notice: NoticeMessage | null
  onDismiss: () => void
}

export function Notice({ notice, onDismiss }: NoticeProps) {
  useEffect(() => {
    if (!notice) {
      return
    }
    const timeoutId = window.setTimeout(onDismiss, NOTICE_DURATION_MS)
    return () => window.clearTimeout(timeoutId)
  }, [notice, onDismiss])

  return (
    <div className="notice-region" aria-live="polite">
      {notice && (
        <div key={notice.id} className={`notice notice--${notice.tone}`}>
          <span>{notice.text}</span>
          <button type="button" className="notice__close" onClick={onDismiss} aria-label="关闭提示">
            关闭
          </button>
        </div>
      )}
    </div>
  )
}
