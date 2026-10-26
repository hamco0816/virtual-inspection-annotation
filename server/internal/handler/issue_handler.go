// 巡检问题接口：上报、分页列表、详情、修改处理状态。
package handler

import (
	"errors"
	"fmt"
	"log/slog"
	"net/http"
	"strconv"

	"github.com/gin-gonic/gin"

	"inspection-server/internal/model"
	"inspection-server/internal/response"
	"inspection-server/internal/store"
)

const (
	defaultPageNum  = 1
	defaultPageSize = 20
	maxPageSize     = 100

	issueNotFoundMessage = "问题不存在"
	invalidIDMessage     = "id 必须是正整数"
)

var errNotPositiveInteger = errors.New("not a positive integer")

// IssueHandler 巡检问题接口处理器
type IssueHandler struct {
	issueStore *store.IssueStore
	logger     *slog.Logger
}

// NewIssueHandler 创建巡检问题接口处理器
func NewIssueHandler(issueStore *store.IssueStore, logger *slog.Logger) *IssueHandler {
	return &IssueHandler{issueStore: issueStore, logger: logger}
}

// Create 接收 Unity 上报的问题并保存，新问题默认状态为 open
func (issueHandler *IssueHandler) Create(requestContext *gin.Context) {
	var request model.CreateIssueRequest
	if err := requestContext.ShouldBindJSON(&request); err != nil {
		response.Error(requestContext, http.StatusBadRequest, response.InvalidBodyMessage)
		return
	}
	if err := request.Validate(); err != nil {
		response.Error(requestContext, http.StatusBadRequest, err.Error())
		return
	}

	issue, err := issueHandler.issueStore.Create(requestContext.Request.Context(), request)
	if err != nil {
		issueHandler.logger.Error("上报巡检问题失败", "clientIP", requestContext.ClientIP(), "title", request.Title, "error", err)
		response.Error(requestContext, http.StatusInternalServerError, response.InternalErrorMessage)
		return
	}
	issueHandler.logger.Info("新增巡检问题", "id", issue.ID, "priority", issue.Priority, "clientIP", requestContext.ClientIP())
	response.Success(requestContext, issue)
}

// List 分页查询问题列表，可按 status、priority 筛选，按创建时间倒序
func (issueHandler *IssueHandler) List(requestContext *gin.Context) {
	filter, err := parseListFilter(requestContext)
	if err != nil {
		response.Error(requestContext, http.StatusBadRequest, err.Error())
		return
	}

	issues, total, err := issueHandler.issueStore.List(requestContext.Request.Context(), filter)
	if err != nil {
		issueHandler.logger.Error("查询巡检问题列表失败", "clientIP", requestContext.ClientIP(), "error", err)
		response.Error(requestContext, http.StatusInternalServerError, response.InternalErrorMessage)
		return
	}
	response.Success(requestContext, response.PageResult[model.Issue]{
		Total: total, List: issues, PageNum: filter.PageNum, PageSize: filter.PageSize,
	})
}

// Get 查询单个问题详情
func (issueHandler *IssueHandler) Get(requestContext *gin.Context) {
	issueID, ok := parseIssueID(requestContext)
	if !ok {
		return
	}

	issue, err := issueHandler.issueStore.GetByID(requestContext.Request.Context(), issueID)
	if errors.Is(err, store.ErrIssueNotFound) {
		response.Error(requestContext, http.StatusNotFound, issueNotFoundMessage)
		return
	}
	if err != nil {
		issueHandler.logger.Error("查询巡检问题详情失败", "clientIP", requestContext.ClientIP(), "id", issueID, "error", err)
		response.Error(requestContext, http.StatusInternalServerError, response.InternalErrorMessage)
		return
	}
	response.Success(requestContext, issue)
}

// UpdateStatus 根据问题 ID 修改处理状态
func (issueHandler *IssueHandler) UpdateStatus(requestContext *gin.Context) {
	issueID, ok := parseIssueID(requestContext)
	if !ok {
		return
	}
	var request model.UpdateStatusRequest
	if err := requestContext.ShouldBindJSON(&request); err != nil {
		response.Error(requestContext, http.StatusBadRequest, response.InvalidBodyMessage)
		return
	}
	if !request.Status.IsValid() {
		response.Error(requestContext, http.StatusBadRequest, model.InvalidStatusMessage)
		return
	}

	issue, err := issueHandler.issueStore.UpdateStatus(requestContext.Request.Context(), issueID, request.Status)
	if errors.Is(err, store.ErrIssueNotFound) {
		response.Error(requestContext, http.StatusNotFound, issueNotFoundMessage)
		return
	}
	if err != nil {
		issueHandler.logger.Error("修改巡检问题状态失败", "clientIP", requestContext.ClientIP(), "id", issueID, "status", request.Status, "error", err)
		response.Error(requestContext, http.StatusInternalServerError, response.InternalErrorMessage)
		return
	}
	issueHandler.logger.Info("修改巡检问题状态", "id", issueID, "status", issue.Status, "clientIP", requestContext.ClientIP())
	response.Success(requestContext, issue)
}

// parseListFilter 解析并校验分页和筛选参数
func parseListFilter(requestContext *gin.Context) (store.ListFilter, error) {
	pageNum, err := parsePositiveInt(requestContext.Query("pageNum"), defaultPageNum)
	if err != nil {
		return store.ListFilter{}, errors.New("pageNum 必须是大于 0 的整数")
	}
	pageSize, err := parsePositiveInt(requestContext.Query("pageSize"), defaultPageSize)
	if err != nil || pageSize > maxPageSize {
		return store.ListFilter{}, fmt.Errorf("pageSize 必须是 1 到 %d 之间的整数", maxPageSize)
	}

	status := model.Status(requestContext.Query("status"))
	if status != "" && !status.IsValid() {
		return store.ListFilter{}, errors.New(model.InvalidStatusMessage)
	}
	priority := model.Priority(requestContext.Query("priority"))
	if priority != "" && !priority.IsValid() {
		return store.ListFilter{}, errors.New(model.InvalidPriorityMessage)
	}
	return store.ListFilter{Status: status, Priority: priority, PageNum: pageNum, PageSize: pageSize}, nil
}

// parsePositiveInt 把查询参数转换为正整数，参数为空时使用默认值
func parsePositiveInt(rawValue string, defaultValue int) (int, error) {
	if rawValue == "" {
		return defaultValue, nil
	}
	value, err := strconv.Atoi(rawValue)
	if err != nil || value < 1 {
		return 0, errNotPositiveInteger
	}
	return value, nil
}

// parseIssueID 解析路径中的问题 ID，格式错误时直接返回 400
func parseIssueID(requestContext *gin.Context) (int64, bool) {
	issueID, err := strconv.ParseInt(requestContext.Param("id"), 10, 64)
	if err != nil || issueID < 1 {
		response.Error(requestContext, http.StatusBadRequest, invalidIDMessage)
		return 0, false
	}
	return issueID, true
}
