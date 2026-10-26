/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** 后端接口地址前缀，不设置时请求同源的 /api（开发时由 Vite 转发到 Go 后端） */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
