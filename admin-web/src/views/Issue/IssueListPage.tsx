// 巡检问题台账页：页头、筛选、问题列表、状态修改结果提示和分页。
import { useCallback, useState } from 'react'
import { Notice, type NoticeMessage } from '../../components/Notice'
import { StatePanel } from '../../components/StatePanel'
import { STATUS_LABELS, type Issue, type PageResult } from '../../types/issue'
import { IssueFilterBar } from './IssueFilterBar'
import { IssueTable } from './IssueTable'
import { LedgerHeader } from './LedgerHeader'
import { Pagination } from './Pagination'
import { useIssueFilters } from './useIssueFilters'
import { useIssueList } from './useIssueList'
import './issue.css'

const PAGE_SIZE = 20

export function IssueListPage() {
  const filters = useIssueFilters()
  const { status, priority, pageNum } = filters
  const { page, loading, errorMessage, reload, replaceIssue } = useIssueList({ pageNum, pageSize: PAGE_SIZE, status, priority })
  const [notice, setNotice] = useState<NoticeMessage | null>(null)

  const handleIssueUpdated = useCallback((updatedIssue: Issue) => {
    replaceIssue(updatedIssue)
    setNotice({ id: Date.now(), tone: 'success', text: `「${updatedIssue.title}」已改为${STATUS_LABELS[updatedIssue.status]}` })
  }, [replaceIssue])
  const handleUpdateFailed = useCallback((issue: Issue, message: string) => {
    setNotice({ id: Date.now(), tone: 'error', text: `「${issue.title}」状态修改失败：${message}` })
  }, [])
  const dismissNotice = useCallback(() => setNotice(null), [])

  return (
    <main className="ledger">
      <LedgerHeader loading={loading} onRefresh={reload} />
      <IssueFilterBar
        status={status}
        priority={priority}
        total={errorMessage ? null : (page?.total ?? null)}
        onStatusChange={filters.changeStatus}
        onPriorityChange={filters.changePriority}
      />
      <section className="ledger__sheet" aria-busy={loading}>
        <IssueListContent
          page={page}
          loading={loading}
          errorMessage={errorMessage}
          hasFilter={filters.hasFilter}
          onRetry={reload}
          onClearFilters={filters.clearFilters}
          onIssueUpdated={handleIssueUpdated}
          onUpdateFailed={handleUpdateFailed}
        />
      </section>
      {page && !errorMessage && (
        <Pagination pageNum={pageNum} pageSize={PAGE_SIZE} total={page.total} disabled={loading} onPageChange={filters.setPageNum} />
      )}
      <Notice notice={notice} onDismiss={dismissNotice} />
    </main>
  )
}

interface IssueListContentProps {
  page: PageResult<Issue> | null
  loading: boolean
  errorMessage: string | null
  hasFilter: boolean
  onRetry: () => void
  onClearFilters: () => void
  onIssueUpdated: (updatedIssue: Issue) => void
  onUpdateFailed: (issue: Issue, errorMessage: string) => void
}

/** 根据加载结果显示错误提示、加载中、空状态或问题表格 */
function IssueListContent({ page, loading, errorMessage, hasFilter, onRetry, onClearFilters, onIssueUpdated, onUpdateFailed }: IssueListContentProps) {
  if (errorMessage) {
    const retryButton = <button type="button" className="button" onClick={onRetry} disabled={loading}>{loading ? '正在重新加载' : '重新加载'}</button>
    return <StatePanel tone="error" title="问题列表加载失败" description={errorMessage} action={retryButton} />
  }
  if (!page) {
    return <p className="ledger__loading">正在加载问题列表</p>
  }
  if (page.list.length === 0 && hasFilter) {
    const clearButton = <button type="button" className="button button--quiet" onClick={onClearFilters}>清除筛选</button>
    return <StatePanel tone="empty" title="没有符合筛选条件的问题" description="换一个状态或优先级，或清除筛选查看全部问题。" action={clearButton} />
  }
  if (page.list.length === 0) {
    return <StatePanel tone="empty" title="还没有上报的问题" description="在 Unity 客户端点击设备放置标记并提交后，点「刷新列表」查看。" />
  }
  return <IssueTable issues={page.list} onIssueUpdated={onIssueUpdated} onUpdateFailed={onUpdateFailed} />
}
