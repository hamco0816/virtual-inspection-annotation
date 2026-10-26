// 单条问题的处理状态下拉框：选择后立即提交，保存期间禁用以防重复提交。
import { useState, type ChangeEvent } from 'react'
import { updateIssueStatus } from '../../api/issueApi'
import { toErrorMessage } from '../../api/http'
import { STATUS_LABELS, STATUS_OPTIONS, type Issue, type IssueStatus } from '../../types/issue'

interface StatusSelectProps {
  issue: Issue
  onUpdated: (updatedIssue: Issue) => void
  onFailed: (issue: Issue, errorMessage: string) => void
}

export function StatusSelect({ issue, onUpdated, onFailed }: StatusSelectProps) {
  const [saving, setSaving] = useState(false)

  const handleChange = async (event: ChangeEvent<HTMLSelectElement>) => {
    const nextStatus = event.target.value as IssueStatus
    setSaving(true)
    try {
      onUpdated(await updateIssueStatus(issue.id, nextStatus))
    } catch (error) {
      onFailed(issue, toErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <span className="status-select" data-status={issue.status}>
      <span className="status-glyph" aria-hidden="true" />
      <select
        value={issue.status}
        disabled={saving}
        onChange={handleChange}
        aria-label={`修改「${issue.title}」的处理状态`}
      >
        {STATUS_OPTIONS.map((status) => (
          <option key={status} value={status}>
            {STATUS_LABELS[status]}（{status}）
          </option>
        ))}
      </select>
      {saving && <span className="status-select__saving">保存中</span>}
    </span>
  )
}
