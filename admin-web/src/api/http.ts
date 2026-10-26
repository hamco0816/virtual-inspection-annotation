// 接口请求封装：处理超时、网络错误，并解析 { code, message, data } 统一响应格式。

const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? ''
const REQUEST_TIMEOUT_MS = 8000
const SUCCESS_CODE = 200
const NETWORK_ERROR_CODE = 0

export const BACKEND_UNREACHABLE_MESSAGE = '连接不上后端服务，请确认 Go 服务已启动'
const TIMEOUT_MESSAGE = '后端服务响应超时，请稍后重试'

interface ApiResponse<T> {
  code: number
  message: string
  data: T
}

/** 接口调用失败，message 可直接展示给用户 */
export class ApiError extends Error {
  readonly code: number

  constructor(code: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.code = code
  }
}

/** 发送请求并返回响应中的 data，失败时抛出 ApiError */
export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const controller = new AbortController()
  const timeoutId = window.setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS)

  let response: Response
  try {
    response = await fetch(API_BASE_URL + path, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...init.headers },
      signal: controller.signal,
    })
  } catch {
    throw new ApiError(NETWORK_ERROR_CODE, controller.signal.aborted ? TIMEOUT_MESSAGE : BACKEND_UNREACHABLE_MESSAGE)
  } finally {
    window.clearTimeout(timeoutId)
  }

  const body = await parseApiResponse<T>(response)
  if (body === null) {
    throw new ApiError(response.status, BACKEND_UNREACHABLE_MESSAGE)
  }
  if (body.code !== SUCCESS_CODE) {
    throw new ApiError(body.code, body.message)
  }
  return body.data
}

/** 把任意异常转换为可展示的错误文字 */
export function toErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }
  return '操作失败，请稍后重试'
}

/** 解析统一响应体，响应不是约定格式（例如代理在后端停止时返回的错误页）时返回 null */
async function parseApiResponse<T>(response: Response): Promise<ApiResponse<T> | null> {
  try {
    const body: unknown = await response.json()
    if (typeof body === 'object' && body !== null && 'code' in body && 'message' in body) {
      return body as ApiResponse<T>
    }
    return null
  } catch {
    return null
  }
}
