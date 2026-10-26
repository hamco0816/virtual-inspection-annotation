// 列表分页：上一页 / 下一页和当前页码。
interface PaginationProps {
  pageNum: number
  pageSize: number
  total: number
  disabled: boolean
  onPageChange: (pageNum: number) => void
}

export function Pagination({ pageNum, pageSize, total, disabled, onPageChange }: PaginationProps) {
  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  if (pageCount <= 1) {
    return null
  }
  return (
    <nav className="pagination" aria-label="分页">
      <button type="button" className="button button--quiet" disabled={disabled || pageNum <= 1} onClick={() => onPageChange(pageNum - 1)}>
        上一页
      </button>
      <span className="pagination__current">
        第 <span className="figure">{pageNum}</span> / <span className="figure">{pageCount}</span> 页
      </span>
      <button type="button" className="button button--quiet" disabled={disabled || pageNum >= pageCount} onClick={() => onPageChange(pageNum + 1)}>
        下一页
      </button>
    </nav>
  )
}
