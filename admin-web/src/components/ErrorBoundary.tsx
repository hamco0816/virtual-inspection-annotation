// 页面渲染异常兜底：组件出错时显示提示和重新加载按钮，避免整页白屏。
import { Component, type ErrorInfo, type ReactNode } from 'react'

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false }

  /** 捕获到子组件渲染异常时切换到兜底界面 */
  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true }
  }

  /** 把异常交给浏览器的错误上报通道，便于在开发者工具中排查 */
  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    reportError(new Error(`页面渲染异常: ${error.message}\n${errorInfo.componentStack ?? ''}`))
  }

  render(): ReactNode {
    if (!this.state.hasError) {
      return this.props.children
    }
    return (
      <div className="state-panel state-panel--error" role="alert">
        <h2 className="state-panel__title">页面出现异常</h2>
        <p className="state-panel__text">重新加载页面后再试一次。</p>
        <button type="button" className="button" onClick={() => window.location.reload()}>
          重新加载页面
        </button>
      </div>
    )
  }
}
