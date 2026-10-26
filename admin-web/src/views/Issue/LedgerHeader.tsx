// 台账页头：标题、说明、后端连接状态和刷新按钮。
import { BackendStatus } from '../../components/BackendStatus'

interface LedgerHeaderProps {
  loading: boolean
  onRefresh: () => void
}

export function LedgerHeader({ loading, onRefresh }: LedgerHeaderProps) {
  return (
    <header className="ledger__header">
      <div>
        <h1 className="ledger__title">巡检问题台账</h1>
        <p className="ledger__subtitle">现场上报的问题汇总在这里，按优先级跟进并更新处理状态。</p>
      </div>
      <div className="ledger__actions">
        <BackendStatus />
        <button type="button" className="button" onClick={onRefresh} disabled={loading}>
          {loading ? '刷新中' : '刷新列表'}
        </button>
      </div>
    </header>
  )
}
