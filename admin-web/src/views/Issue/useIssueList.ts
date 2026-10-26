// 问题列表数据：按筛选条件和页码加载，支持手动刷新和修改状态后替换单条记录。
import { useCallback, useEffect, useState } from 'react'
import { fetchIssues, type IssueQuery } from '../../api/issueApi'
import { toErrorMessage } from '../../api/http'
import type { Issue, PageResult } from '../../types/issue'

export function useIssueList(query: IssueQuery) {
  const [page, setPage] = useState<PageResult<Issue> | null>(null)
  const [loading, setLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [reloadCount, setReloadCount] = useState(0)
  const { pageNum, pageSize, status, priority } = query

  // 查询条件变化或手动刷新时重新拉取列表，组件卸载或条件再次变化时丢弃过期结果
  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetchIssues({ pageNum, pageSize, status, priority })
      .then((result) => {
        if (cancelled) return
        setPage(result)
        setErrorMessage(null)
      })
      .catch((error: unknown) => {
        if (!cancelled) setErrorMessage(toErrorMessage(error))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [pageNum, pageSize, status, priority, reloadCount])

  const reload = useCallback(() => setReloadCount((count) => count + 1), [])

  const replaceIssue = useCallback((updatedIssue: Issue) => {
    setPage((currentPage) =>
      currentPage && {
        ...currentPage,
        list: currentPage.list.map((issue) => (issue.id === updatedIssue.id ? updatedIssue : issue)),
      },
    )
  }, [])

  return { page, loading, errorMessage, reload, replaceIssue }
}
