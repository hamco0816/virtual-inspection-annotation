// 巡检问题的数据结构和枚举显示文字，字段名与 Go 后端、Unity 客户端保持一致。

export type IssuePriority = 'low' | 'medium' | 'high'

export type IssueStatus = 'open' | 'in_progress' | 'resolved'

export interface IssuePosition {
  x: number
  y: number
  z: number
}

export interface Issue {
  id: number
  title: string
  description: string
  priority: IssuePriority
  status: IssueStatus
  /** 问题标记在 Unity 场景中的世界坐标 */
  position: IssuePosition
  createdAt: string
  updatedAt: string
}

export interface PageResult<T> {
  total: number
  list: T[]
  pageNum: number
  pageSize: number
}

export const PRIORITY_OPTIONS: IssuePriority[] = ['high', 'medium', 'low']

export const STATUS_OPTIONS: IssueStatus[] = ['open', 'in_progress', 'resolved']

export const PRIORITY_LABELS: Record<IssuePriority, string> = {
  high: '高',
  medium: '中',
  low: '低',
}

export const STATUS_LABELS: Record<IssueStatus, string> = {
  open: '待处理',
  in_progress: '处理中',
  resolved: '已解决',
}
