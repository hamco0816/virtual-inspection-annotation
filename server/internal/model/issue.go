// Package model 定义巡检问题的数据结构、枚举值和参数校验规则。
package model

import (
	"errors"
	"fmt"
	"strings"
	"time"
	"unicode/utf8"
)

// Priority 问题优先级
type Priority string

// Status 问题处理状态
type Status string

const (
	PriorityLow    Priority = "low"
	PriorityMedium Priority = "medium"
	PriorityHigh   Priority = "high"

	StatusOpen       Status = "open"        // 待处理，新上报的问题默认为此状态
	StatusInProgress Status = "in_progress" // 处理中
	StatusResolved   Status = "resolved"    // 已解决

	MaxTitleLength       = 100 // 标题最大字符数
	MaxDescriptionLength = 500 // 描述最大字符数

	InvalidPriorityMessage = "priority 只能是 low、medium、high"
	InvalidStatusMessage   = "status 只能是 open、in_progress、resolved"
)

// Position 问题标记在 Unity 场景中的世界坐标
type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}

// Issue 巡检问题
type Issue struct {
	ID          int64     `json:"id"`
	Title       string    `json:"title"`
	Description string    `json:"description"`
	Priority    Priority  `json:"priority"`
	Status      Status    `json:"status"`
	Position    Position  `json:"position"`
	CreatedAt   time.Time `json:"createdAt"`
	UpdatedAt   time.Time `json:"updatedAt"`
}

// CreateIssueRequest Unity 上报问题的请求体
type CreateIssueRequest struct {
	Title       string    `json:"title"`
	Description string    `json:"description"`
	Priority    Priority  `json:"priority"`
	Position    *Position `json:"position"`
}

// UpdateStatusRequest 修改处理状态的请求体
type UpdateStatusRequest struct {
	Status Status `json:"status"`
}

// IsValid 判断优先级是否为合法枚举值
func (priority Priority) IsValid() bool {
	return priority == PriorityLow || priority == PriorityMedium || priority == PriorityHigh
}

// IsValid 判断处理状态是否为合法枚举值
func (status Status) IsValid() bool {
	return status == StatusOpen || status == StatusInProgress || status == StatusResolved
}

// Validate 去除标题和描述首尾空格后校验上报参数，返回可直接展示给调用方的错误说明
func (request *CreateIssueRequest) Validate() error {
	request.Title = strings.TrimSpace(request.Title)
	request.Description = strings.TrimSpace(request.Description)

	if request.Title == "" {
		return errors.New("title 不能为空")
	}
	if utf8.RuneCountInString(request.Title) > MaxTitleLength {
		return fmt.Errorf("title 不能超过 %d 个字符", MaxTitleLength)
	}
	if utf8.RuneCountInString(request.Description) > MaxDescriptionLength {
		return fmt.Errorf("description 不能超过 %d 个字符", MaxDescriptionLength)
	}
	if !request.Priority.IsValid() {
		return errors.New(InvalidPriorityMessage)
	}
	if request.Position == nil {
		return errors.New("position 不能为空")
	}
	return nil
}
