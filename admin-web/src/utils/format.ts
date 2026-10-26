// 列表展示用的日期和坐标格式化。

const COORDINATE_DECIMALS = 2

/** 把后端返回的时间格式化为 "2026-10-26 14:35" */
export function formatDateTime(isoTime: string): string {
  const date = new Date(isoTime)
  if (Number.isNaN(date.getTime())) {
    return isoTime
  }
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/** 坐标保留两位小数 */
export function formatCoordinate(value: number): string {
  return value.toFixed(COORDINATE_DECIMALS)
}
