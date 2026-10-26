// Vite 配置：开发服务器把 /api 请求转发到 Go 后端，后端地址通过环境变量 BACKEND_URL 修改。
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

const DEFAULT_BACKEND_URL = 'http://127.0.0.1:8080'
const DEV_SERVER_PORT = 5173

export default defineConfig(({ mode }) => {
  const environment = loadEnv(mode, process.cwd(), '')
  return {
    plugins: [react()],
    server: {
      port: DEV_SERVER_PORT,
      proxy: {
        '/api': {
          target: environment.BACKEND_URL || DEFAULT_BACKEND_URL,
          changeOrigin: true,
        },
      },
    },
  }
})
