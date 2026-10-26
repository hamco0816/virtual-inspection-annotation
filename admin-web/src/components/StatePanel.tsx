// 列表区域的空状态 / 错误状态提示块。
import type { ReactNode } from 'react'

interface StatePanelProps {
  tone: 'empty' | 'error'
  title: string
  description: string
  action?: ReactNode
}

export function StatePanel({ tone, title, description, action }: StatePanelProps) {
  return (
    <div className={`state-panel state-panel--${tone}`} role={tone === 'error' ? 'alert' : undefined}>
      <h2 className="state-panel__title">{title}</h2>
      <p className="state-panel__text">{description}</p>
      {action}
    </div>
  )
}
