// Package response 定义接口统一响应格式：{ "code": 200, "message": "...", "data": {} }。
package response

import (
	"net/http"

	"github.com/gin-gonic/gin"
)

const (
	successMessage = "success"

	InvalidBodyMessage   = "请求体格式错误，请检查 JSON 字段类型"
	InternalErrorMessage = "服务器内部错误"
	NotFoundMessage      = "接口不存在"
)

// Body 统一响应体
type Body struct {
	Code    int    `json:"code"`
	Message string `json:"message"`
	Data    any    `json:"data"`
}

// PageResult 分页查询结果
type PageResult[T any] struct {
	Total    int64 `json:"total"`
	List     []T   `json:"list"`
	PageNum  int   `json:"pageNum"`
	PageSize int   `json:"pageSize"`
}

// Success 返回成功响应
func Success(requestContext *gin.Context, data any) {
	requestContext.JSON(http.StatusOK, Body{Code: http.StatusOK, Message: successMessage, Data: data})
}

// Error 返回失败响应并终止后续处理，HTTP 状态码与 code 保持一致
func Error(requestContext *gin.Context, httpStatus int, message string) {
	requestContext.AbortWithStatusJSON(httpStatus, Body{Code: httpStatus, Message: message, Data: nil})
}
