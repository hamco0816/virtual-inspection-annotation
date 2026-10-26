// 列表筛选与页码状态：切换筛选条件时自动回到第一页。
import { useState } from 'react'
import type { IssuePriority, IssueStatus } from '../../types/issue'

export function useIssueFilters() {
  const [status, setStatus] = useState<IssueStatus>()
  const [priority, setPriority] = useState<IssuePriority>()
  const [pageNum, setPageNum] = useState(1)

  const changeStatus = (nextStatus?: IssueStatus) => {
    setStatus(nextStatus)
    setPageNum(1)
  }
  const changePriority = (nextPriority?: IssuePriority) => {
    setPriority(nextPriority)
    setPageNum(1)
  }
  const clearFilters = () => {
    setStatus(undefined)
    setPriority(undefined)
    setPageNum(1)
  }

  return {
    status,
    priority,
    pageNum,
    hasFilter: status !== undefined || priority !== undefined,
    setPageNum,
    changeStatus,
    changePriority,
    clearFilters,
  }
}
