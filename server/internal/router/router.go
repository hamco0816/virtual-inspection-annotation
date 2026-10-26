// Package router 组装中间件和接口路由。
package router

import (
	"database/sql"
	"log/slog"
	"net/http"

	"github.com/gin-gonic/gin"

	"inspection-server/internal/handler"
	"inspection-server/internal/middleware"
	"inspection-server/internal/response"
	"inspection-server/internal/store"
)

// New 创建 HTTP 路由，所有接口挂在 /api 下
func New(database *sql.DB, allowedOrigins []string, logger *slog.Logger) *gin.Engine {
	gin.SetMode(gin.ReleaseMode)
	engine := gin.New()
	// 服务直接对外，不经过反向代理，客户端 IP 取 TCP 连接地址
	if err := engine.SetTrustedProxies(nil); err != nil {
		logger.Warn("设置可信代理失败", "error", err)
	}
	engine.Use(middleware.Recovery(logger), middleware.AccessLog(logger), middleware.CORS(allowedOrigins))

	healthHandler := handler.NewHealthHandler(database, logger)
	issueHandler := handler.NewIssueHandler(store.NewIssueStore(database), logger)

	api := engine.Group("/api")
	api.GET("/health", healthHandler.Check)
	api.GET("/issues", issueHandler.List)
	api.POST("/issues", issueHandler.Create)
	api.GET("/issues/:id", issueHandler.Get)
	api.PATCH("/issues/:id/status", issueHandler.UpdateStatus)

	engine.NoRoute(func(requestContext *gin.Context) {
		response.Error(requestContext, http.StatusNotFound, response.NotFoundMessage)
	})
	return engine
}
