// Package middleware 提供 HTTP 中间件：异常恢复、访问日志、跨域访问控制。
package middleware

import (
	"log/slog"
	"net/http"
	"runtime/debug"
	"slices"
	"time"

	"github.com/gin-gonic/gin"

	"inspection-server/internal/response"
)

const (
	allowAllOrigins = "*"
	allowedMethods  = "GET, POST, PATCH, OPTIONS"
	allowedHeaders  = "Content-Type"
)

// Recovery 捕获请求处理中的 panic，记录日志后返回 500，不向调用方暴露堆栈
func Recovery(logger *slog.Logger) gin.HandlerFunc {
	return func(requestContext *gin.Context) {
		defer func() {
			recovered := recover()
			if recovered == nil {
				return
			}
			logger.Error("请求处理发生异常",
				"method", requestContext.Request.Method,
				"path", requestContext.Request.URL.Path,
				"clientIP", requestContext.ClientIP(),
				"panic", recovered,
				"stack", string(debug.Stack()))
			response.Error(requestContext, http.StatusInternalServerError, response.InternalErrorMessage)
		}()
		requestContext.Next()
	}
}

// AccessLog 以 DEBUG 级别记录每个请求的方法、路径、状态码和耗时
func AccessLog(logger *slog.Logger) gin.HandlerFunc {
	return func(requestContext *gin.Context) {
		startTime := time.Now()
		requestContext.Next()
		logger.Debug("HTTP 请求",
			"method", requestContext.Request.Method,
			"path", requestContext.Request.URL.Path,
			"status", requestContext.Writer.Status(),
			"duration", time.Since(startTime),
			"clientIP", requestContext.ClientIP())
	}
}

// CORS 允许配置中的前端来源跨域访问接口，并直接响应浏览器的预检请求
func CORS(allowedOrigins []string) gin.HandlerFunc {
	allowAll := slices.Contains(allowedOrigins, allowAllOrigins)
	return func(requestContext *gin.Context) {
		origin := requestContext.GetHeader("Origin")
		if origin != "" && (allowAll || slices.Contains(allowedOrigins, origin)) {
			header := requestContext.Writer.Header()
			header.Set("Access-Control-Allow-Origin", origin)
			header.Set("Access-Control-Allow-Methods", allowedMethods)
			header.Set("Access-Control-Allow-Headers", allowedHeaders)
			header.Add("Vary", "Origin")
		}
		if requestContext.Request.Method == http.MethodOptions {
			requestContext.AbortWithStatus(http.StatusNoContent)
			return
		}
		requestContext.Next()
	}
}
