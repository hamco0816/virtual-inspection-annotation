// 接口端到端测试：用临时 SQLite 文件验证上报、查询、修改状态、参数校验、跨域和重启后数据保留。
package router

import (
	"database/sql"
	"encoding/json"
	"io"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"strconv"
	"strings"
	"testing"

	"github.com/gin-gonic/gin"

	"inspection-server/internal/model"
	"inspection-server/internal/store"
)

const validIssueBody = `{"title":"1 号机柜温度异常","description":"机柜右侧温度明显偏高","priority":"high","position":{"x":1.25,"y":0.8,"z":2.4}}`

// testResponse 测试中解析的统一响应体
type testResponse struct {
	Code    int             `json:"code"`
	Message string          `json:"message"`
	Data    json.RawMessage `json:"data"`
}

// openTestEngine 打开指定路径的数据库并创建路由，测试结束自动关闭数据库
func openTestEngine(t *testing.T, databasePath string) (*gin.Engine, *sql.DB) {
	t.Helper()
	database, err := store.Open(databasePath)
	if err != nil {
		t.Fatalf("打开数据库失败: %v", err)
	}
	t.Cleanup(func() { database.Close() })
	logger := slog.New(slog.NewTextHandler(io.Discard, nil))
	return New(database, []string{"*"}, logger), database
}

// newTestEngine 使用全新临时数据库创建路由
func newTestEngine(t *testing.T) *gin.Engine {
	engine, _ := openTestEngine(t, filepath.Join(t.TempDir(), "test.db"))
	return engine
}

// sendRequest 发送请求并解析统一响应体
func sendRequest(t *testing.T, engine *gin.Engine, method string, path string, body string) (int, testResponse) {
	t.Helper()
	request := httptest.NewRequest(method, path, strings.NewReader(body))
	request.Header.Set("Content-Type", "application/json")
	recorder := httptest.NewRecorder()
	engine.ServeHTTP(recorder, request)

	var parsed testResponse
	if recorder.Body.Len() > 0 {
		if err := json.Unmarshal(recorder.Body.Bytes(), &parsed); err != nil {
			t.Fatalf("响应不是合法 JSON: %v, body=%s", err, recorder.Body.String())
		}
	}
	return recorder.Code, parsed
}

// createIssue 上报一个合法问题并返回保存结果
func createIssue(t *testing.T, engine *gin.Engine) model.Issue {
	t.Helper()
	statusCode, parsed := sendRequest(t, engine, http.MethodPost, "/api/issues", validIssueBody)
	if statusCode != http.StatusOK {
		t.Fatalf("上报问题失败: status=%d message=%s", statusCode, parsed.Message)
	}
	var issue model.Issue
	if err := json.Unmarshal(parsed.Data, &issue); err != nil {
		t.Fatalf("解析问题数据失败: %v", err)
	}
	return issue
}

func TestHealthCheck(t *testing.T) {
	statusCode, parsed := sendRequest(t, newTestEngine(t), http.MethodGet, "/api/health", "")
	if statusCode != http.StatusOK || parsed.Code != http.StatusOK {
		t.Fatalf("期望 200，实际 status=%d code=%d", statusCode, parsed.Code)
	}
}

func TestCreateIssueGeneratesIDAndDefaultsToOpen(t *testing.T) {
	issue := createIssue(t, newTestEngine(t))
	if issue.ID < 1 {
		t.Errorf("期望生成正整数 ID，实际 %d", issue.ID)
	}
	if issue.Status != model.StatusOpen {
		t.Errorf("期望默认状态 open，实际 %s", issue.Status)
	}
	if issue.Position.X != 1.25 || issue.Position.Y != 0.8 || issue.Position.Z != 2.4 {
		t.Errorf("坐标保存不正确: %+v", issue.Position)
	}
	if issue.CreatedAt.IsZero() || issue.UpdatedAt.IsZero() {
		t.Error("期望后端生成 createdAt 和 updatedAt")
	}
}

func TestCreateIssueRejectsInvalidInput(t *testing.T) {
	engine := newTestEngine(t)
	testCases := map[string]string{
		"标题为空":   `{"title":"","priority":"high","position":{"x":0,"y":0,"z":0}}`,
		"标题只有空格": `{"title":"   ","priority":"high","position":{"x":0,"y":0,"z":0}}`,
		"优先级非法":  `{"title":"漏水","priority":"urgent","position":{"x":0,"y":0,"z":0}}`,
		"缺少坐标":   `{"title":"漏水","priority":"low"}`,
		"字段类型错误": `{"title":"漏水","priority":1,"position":{"x":0,"y":0,"z":0}}`,
		"不是JSON": `not json`,
	}
	for name, body := range testCases {
		t.Run(name, func(t *testing.T) {
			statusCode, parsed := sendRequest(t, engine, http.MethodPost, "/api/issues", body)
			if statusCode != http.StatusBadRequest || parsed.Code != http.StatusBadRequest || parsed.Message == "" {
				t.Errorf("期望 400 且带错误说明，实际 status=%d message=%q", statusCode, parsed.Message)
			}
		})
	}
}

func TestUpdateStatus(t *testing.T) {
	engine := newTestEngine(t)
	issue := createIssue(t, engine)
	issuePath := "/api/issues/" + strconv.FormatInt(issue.ID, 10) + "/status"

	statusCode, parsed := sendRequest(t, engine, http.MethodPatch, issuePath, `{"status":"in_progress"}`)
	if statusCode != http.StatusOK {
		t.Fatalf("修改状态失败: status=%d message=%s", statusCode, parsed.Message)
	}
	var updated model.Issue
	if err := json.Unmarshal(parsed.Data, &updated); err != nil || updated.Status != model.StatusInProgress {
		t.Fatalf("期望状态 in_progress，实际 %+v, err=%v", updated, err)
	}

	if statusCode, _ := sendRequest(t, engine, http.MethodPatch, issuePath, `{"status":"done"}`); statusCode != http.StatusBadRequest {
		t.Errorf("非法状态期望 400，实际 %d", statusCode)
	}
	if statusCode, _ := sendRequest(t, engine, http.MethodPatch, "/api/issues/9999/status", `{"status":"resolved"}`); statusCode != http.StatusNotFound {
		t.Errorf("不存在的问题期望 404，实际 %d", statusCode)
	}
	if statusCode, _ := sendRequest(t, engine, http.MethodPatch, "/api/issues/abc/status", `{"status":"resolved"}`); statusCode != http.StatusBadRequest {
		t.Errorf("非法 ID 期望 400，实际 %d", statusCode)
	}
}

func TestListIssuesPaginatesNewestFirst(t *testing.T) {
	engine := newTestEngine(t)
	for range 3 {
		createIssue(t, engine)
	}

	statusCode, parsed := sendRequest(t, engine, http.MethodGet, "/api/issues?pageNum=1&pageSize=2", "")
	if statusCode != http.StatusOK {
		t.Fatalf("查询列表失败: status=%d message=%s", statusCode, parsed.Message)
	}
	var page struct {
		Total int64         `json:"total"`
		List  []model.Issue `json:"list"`
	}
	if err := json.Unmarshal(parsed.Data, &page); err != nil {
		t.Fatalf("解析分页数据失败: %v", err)
	}
	if page.Total != 3 || len(page.List) != 2 {
		t.Fatalf("期望 total=3 且本页 2 条，实际 total=%d 本页 %d 条", page.Total, len(page.List))
	}
	if page.List[0].ID != 3 {
		t.Errorf("期望最新的问题排在第一条，实际第一条 ID=%d", page.List[0].ID)
	}

	if statusCode, _ := sendRequest(t, engine, http.MethodGet, "/api/issues?pageSize=1000", ""); statusCode != http.StatusBadRequest {
		t.Errorf("pageSize 超出上限期望 400，实际 %d", statusCode)
	}
}

func TestDataPersistsAfterReopen(t *testing.T) {
	databasePath := filepath.Join(t.TempDir(), "persist.db")
	engine, database := openTestEngine(t, databasePath)
	created := createIssue(t, engine)
	database.Close()

	reopenedEngine, _ := openTestEngine(t, databasePath)
	statusCode, parsed := sendRequest(t, reopenedEngine, http.MethodGet, "/api/issues/"+strconv.FormatInt(created.ID, 10), "")
	if statusCode != http.StatusOK {
		t.Fatalf("重新打开数据库后查不到问题: status=%d message=%s", statusCode, parsed.Message)
	}
}

func TestPreflightRequestAllowsCrossOrigin(t *testing.T) {
	request := httptest.NewRequest(http.MethodOptions, "/api/issues", nil)
	request.Header.Set("Origin", "http://example.test")
	recorder := httptest.NewRecorder()
	newTestEngine(t).ServeHTTP(recorder, request)

	if recorder.Code != http.StatusNoContent {
		t.Errorf("预检请求期望 204，实际 %d", recorder.Code)
	}
	if recorder.Header().Get("Access-Control-Allow-Origin") != "http://example.test" {
		t.Errorf("期望返回允许跨域的来源，实际 %q", recorder.Header().Get("Access-Control-Allow-Origin"))
	}
}
