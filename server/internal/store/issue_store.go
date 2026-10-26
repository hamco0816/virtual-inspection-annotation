// 巡检问题的数据读写：新增、分页查询、详情、修改状态。
package store

import (
	"context"
	"database/sql"
	"errors"
	"fmt"
	"strings"
	"time"

	"inspection-server/internal/model"
)

// ErrIssueNotFound 问题不存在或已被逻辑删除
var ErrIssueNotFound = errors.New("issue not found")

const issueColumns = "id, title, description, priority, status, position_x, position_y, position_z, create_time, update_time"

// ListFilter 问题列表查询条件
type ListFilter struct {
	Status   model.Status   // 按处理状态筛选，空字符串表示不筛选
	Priority model.Priority // 按优先级筛选，空字符串表示不筛选
	PageNum  int            // 页码，从 1 开始
	PageSize int            // 每页条数
}

// IssueStore 巡检问题存储
type IssueStore struct {
	database *sql.DB
}

// rowScanner 兼容 *sql.Row 和 *sql.Rows 的扫描接口
type rowScanner interface {
	Scan(destinations ...any) error
}

// NewIssueStore 创建巡检问题存储
func NewIssueStore(database *sql.DB) *IssueStore {
	return &IssueStore{database: database}
}

// Create 保存新上报的问题，状态固定为 open，返回带 ID 和时间的完整记录
func (issueStore *IssueStore) Create(ctx context.Context, request model.CreateIssueRequest) (model.Issue, error) {
	now := time.Now().Truncate(time.Second)
	issue := model.Issue{
		Title:       request.Title,
		Description: request.Description,
		Priority:    request.Priority,
		Status:      model.StatusOpen,
		Position:    *request.Position,
		CreatedAt:   now,
		UpdatedAt:   now,
	}

	result, err := issueStore.database.ExecContext(ctx,
		`INSERT INTO issue (title, description, priority, status, position_x, position_y, position_z, create_time, update_time)
		 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
		issue.Title, issue.Description, string(issue.Priority), string(issue.Status),
		issue.Position.X, issue.Position.Y, issue.Position.Z, issue.CreatedAt, issue.UpdatedAt)
	if err != nil {
		return model.Issue{}, fmt.Errorf("插入问题失败: %w", err)
	}

	issue.ID, err = result.LastInsertId()
	if err != nil {
		return model.Issue{}, fmt.Errorf("读取新问题 ID 失败: %w", err)
	}
	return issue, nil
}

// List 按条件分页查询未删除的问题，按创建时间倒序，返回当前页数据和总条数
func (issueStore *IssueStore) List(ctx context.Context, filter ListFilter) ([]model.Issue, int64, error) {
	whereClause, arguments := buildWhereClause(filter)

	var total int64
	countSQL := "SELECT COUNT(*) FROM issue" + whereClause
	if err := issueStore.database.QueryRowContext(ctx, countSQL, arguments...).Scan(&total); err != nil {
		return nil, 0, fmt.Errorf("统计问题数量失败: %w", err)
	}

	offset := (filter.PageNum - 1) * filter.PageSize
	pageSQL := "SELECT " + issueColumns + " FROM issue" + whereClause + " ORDER BY create_time DESC, id DESC LIMIT ? OFFSET ?"
	rows, err := issueStore.database.QueryContext(ctx, pageSQL, append(arguments, filter.PageSize, offset)...)
	if err != nil {
		return nil, 0, fmt.Errorf("查询问题列表失败: %w", err)
	}
	defer rows.Close()

	issues := make([]model.Issue, 0, filter.PageSize)
	for rows.Next() {
		issue, err := scanIssue(rows)
		if err != nil {
			return nil, 0, fmt.Errorf("读取问题记录失败: %w", err)
		}
		issues = append(issues, issue)
	}
	if err := rows.Err(); err != nil {
		return nil, 0, fmt.Errorf("遍历问题列表失败: %w", err)
	}
	return issues, total, nil
}

// GetByID 查询单个未删除的问题
func (issueStore *IssueStore) GetByID(ctx context.Context, issueID int64) (model.Issue, error) {
	row := issueStore.database.QueryRowContext(ctx,
		"SELECT "+issueColumns+" FROM issue WHERE id = ? AND deleted = 0", issueID)
	issue, err := scanIssue(row)
	if errors.Is(err, sql.ErrNoRows) {
		return model.Issue{}, ErrIssueNotFound
	}
	if err != nil {
		return model.Issue{}, fmt.Errorf("查询问题详情失败: %w", err)
	}
	return issue, nil
}

// UpdateStatus 修改问题处理状态并刷新更新时间，返回修改后的完整记录
func (issueStore *IssueStore) UpdateStatus(ctx context.Context, issueID int64, status model.Status) (model.Issue, error) {
	result, err := issueStore.database.ExecContext(ctx,
		"UPDATE issue SET status = ?, update_time = ? WHERE id = ? AND deleted = 0",
		string(status), time.Now().Truncate(time.Second), issueID)
	if err != nil {
		return model.Issue{}, fmt.Errorf("修改问题状态失败: %w", err)
	}

	affectedRows, err := result.RowsAffected()
	if err != nil {
		return model.Issue{}, fmt.Errorf("读取修改结果失败: %w", err)
	}
	if affectedRows == 0 {
		return model.Issue{}, ErrIssueNotFound
	}
	return issueStore.GetByID(ctx, issueID)
}

// buildWhereClause 根据筛选条件拼接 WHERE 子句，条件值全部走占位符参数
func buildWhereClause(filter ListFilter) (string, []any) {
	conditions := []string{"deleted = 0"}
	arguments := make([]any, 0)
	if filter.Status != "" {
		conditions = append(conditions, "status = ?")
		arguments = append(arguments, string(filter.Status))
	}
	if filter.Priority != "" {
		conditions = append(conditions, "priority = ?")
		arguments = append(arguments, string(filter.Priority))
	}
	return " WHERE " + strings.Join(conditions, " AND "), arguments
}

// scanIssue 把一行查询结果转换为问题对象
func scanIssue(scanner rowScanner) (model.Issue, error) {
	var issue model.Issue
	err := scanner.Scan(&issue.ID, &issue.Title, &issue.Description, &issue.Priority, &issue.Status,
		&issue.Position.X, &issue.Position.Y, &issue.Position.Z, &issue.CreatedAt, &issue.UpdatedAt)
	return issue, err
}
