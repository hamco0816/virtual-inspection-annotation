// 问题列表表格：优先级用色块标签区分，状态列可直接修改。
import { PRIORITY_LABELS, type Issue } from '../../types/issue'
import { formatCoordinate, formatDateTime } from '../../utils/format'
import { StatusSelect } from './StatusSelect'

interface IssueTableProps {
  issues: Issue[]
  onIssueUpdated: (updatedIssue: Issue) => void
  onUpdateFailed: (issue: Issue, errorMessage: string) => void
}

export function IssueTable({ issues, onIssueUpdated, onUpdateFailed }: IssueTableProps) {
  return (
    <table className="issue-table">
      <thead>
        <tr>
          <th scope="col">编号</th>
          <th scope="col">问题</th>
          <th scope="col">优先级</th>
          <th scope="col">处理状态</th>
          <th scope="col">标记坐标</th>
          <th scope="col">上报时间</th>
          <th scope="col">更新时间</th>
        </tr>
      </thead>
      <tbody>
        {issues.map((issue) => (
          <tr key={issue.id} className="issue-row" data-priority={issue.priority} data-status={issue.status}>
            <td className="issue-row__id figure" data-label="编号">#{issue.id}</td>
            <td className="issue-row__subject" data-label="问题">
              <span className="issue-row__title">{issue.title}</span>
              {issue.description && <span className="issue-row__description">{issue.description}</span>}
            </td>
            <td data-label="优先级">
              <span className="priority-tag">{PRIORITY_LABELS[issue.priority]}</span>
            </td>
            <td data-label="处理状态">
              <StatusSelect issue={issue} onUpdated={onIssueUpdated} onFailed={onUpdateFailed} />
            </td>
            <td className="figure" data-label="标记坐标">
              <span className="coordinate">
                <span>x {formatCoordinate(issue.position.x)}</span>
                <span>y {formatCoordinate(issue.position.y)}</span>
                <span>z {formatCoordinate(issue.position.z)}</span>
              </span>
            </td>
            <td className="figure" data-label="上报时间">{formatDateTime(issue.createdAt)}</td>
            <td className="figure" data-label="更新时间">{formatDateTime(issue.updatedAt)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
