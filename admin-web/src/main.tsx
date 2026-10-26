// 管理端入口：加载字体和全局样式，挂载根组件。
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import '@fontsource/barlow-semi-condensed/500.css'
import '@fontsource/barlow-semi-condensed/600.css'
import './styles/base.css'
import { App } from './App'

const rootElement = document.getElementById('root')
if (!rootElement) {
  throw new Error('页面缺少 #root 挂载节点')
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
