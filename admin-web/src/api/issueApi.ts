// 巡检问题接口：分页查询、修改处理状态、后端运行状态检查。
import type { Issue, IssuePriority, IssueStatus, PageResult } from '../types/issue'
import { request } from './http'

export interface IssueQuery {
  pageNum: number
  pageSize: number
  status?: IssueStatus
  priority?: IssuePriority
}

export interface HealthStatus {
  status: string
  serverTime: string
}

/** 分页查询问题列表，按上报时间倒序 */
export function fetchIssues(query: IssueQuery): Promise<PageResult<Issue>> {
  const params = new URLSearchParams({
    pageNum: String(query.pageNum),
    pageSize: String(query.pageSize),
  })
  if (query.status) {
    params.set('status', query.status)
  }
  if (query.priority) {
    params.set('priority', query.priority)
  }
  return request<PageResult<Issue>>(`/api/issues?${params.toString()}`)
}

/** 修改问题处理状态，返回修改后的问题 */
export function updateIssueStatus(issueId: number, status: IssueStatus): Promise<Issue> {
  return request<Issue>(`/api/issues/${issueId}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  })
}

/** 检查后端服务是否可用 */
export function checkHealth(): Promise<HealthStatus> {
  return request<HealthStatus>('/api/health')
}
