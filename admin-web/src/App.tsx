// 应用根组件：渲染异常兜底 + 巡检问题台账页。
import { ErrorBoundary } from './components/ErrorBoundary'
import { IssueListPage } from './views/Issue/IssueListPage'

export function App() {
  return (
    <ErrorBoundary>
      <IssueListPage />
    </ErrorBoundary>
  )
}
