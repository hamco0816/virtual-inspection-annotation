// Package handler 处理 HTTP 请求：解析参数、调用存储层、返回统一格式响应。
package handler

import (
	"database/sql"
	"log/slog"
	"net/http"
	"time"

	"github.com/gin-gonic/gin"

	"inspection-server/internal/response"
)

const serviceStatusUp = "up"

// HealthHandler 服务运行状态检查
type HealthHandler struct {
	database *sql.DB
	logger   *slog.Logger
}

// healthStatus 运行状态检查返回的数据
type healthStatus struct {
	Status     string    `json:"status"` // 服务状态，up 表示服务和数据库都可用
	ServerTime time.Time `json:"serverTime"`
}

// NewHealthHandler 创建运行状态检查处理器
func NewHealthHandler(database *sql.DB, logger *slog.Logger) *HealthHandler {
	return &HealthHandler{database: database, logger: logger}
}

// Check 检查服务和数据库是否可用
func (healthHandler *HealthHandler) Check(requestContext *gin.Context) {
	if err := healthHandler.database.PingContext(requestContext.Request.Context()); err != nil {
		healthHandler.logger.Error("运行状态检查：数据库不可用", "clientIP", requestContext.ClientIP(), "error", err)
		response.Error(requestContext, http.StatusInternalServerError, "数据库不可用")
		return
	}
	response.Success(requestContext, healthStatus{Status: serviceStatusUp, ServerTime: time.Now()})
}
