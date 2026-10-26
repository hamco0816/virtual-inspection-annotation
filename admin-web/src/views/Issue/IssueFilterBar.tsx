// 列表筛选栏：按处理状态和优先级筛选，右侧显示符合条件的总条数。
import {
  PRIORITY_LABELS,
  PRIORITY_OPTIONS,
  STATUS_LABELS,
  STATUS_OPTIONS,
  type IssuePriority,
  type IssueStatus,
} from '../../types/issue'

interface IssueFilterBarProps {
  status?: IssueStatus
  priority?: IssuePriority
  total: number | null
  onStatusChange: (status?: IssueStatus) => void
  onPriorityChange: (priority?: IssuePriority) => void
}

interface SegmentedFilterProps<T extends string> {
  label: string
  value?: T
  options: T[]
  labels: Record<T, string>
  onChange: (value?: T) => void
}

/** 一组互斥的筛选按钮，第一个按钮"全部"表示不筛选 */
function SegmentedFilter<T extends string>({ label, value, options, labels, onChange }: SegmentedFilterProps<T>) {
  return (
    <div className="segmented" role="group" aria-label={`按${label}筛选`}>
      <span className="segmented__label">{label}</span>
      <button type="button" className="segmented__option" aria-pressed={value === undefined} onClick={() => onChange(undefined)}>
        全部
      </button>
      {options.map((option) => (
        <button
          key={option}
          type="button"
          className="segmented__option"
          data-value={option}
          aria-pressed={value === option}
          onClick={() => onChange(option)}
        >
          {labels[option]}
        </button>
      ))}
    </div>
  )
}

export function IssueFilterBar({ status, priority, total, onStatusChange, onPriorityChange }: IssueFilterBarProps) {
  return (
    <div className="filter-bar">
      <SegmentedFilter label="状态" value={status} options={STATUS_OPTIONS} labels={STATUS_LABELS} onChange={onStatusChange} />
      <SegmentedFilter label="优先级" value={priority} options={PRIORITY_OPTIONS} labels={PRIORITY_LABELS} onChange={onPriorityChange} />
      {total !== null && <span className="filter-bar__total">共 <span className="figure">{total}</span> 条</span>}
    </div>
  )
}
