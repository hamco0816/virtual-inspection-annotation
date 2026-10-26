// Package store 负责 SQLite 数据库连接、表结构初始化和巡检问题的读写。
package store

import (
	"database/sql"
	"fmt"
	"os"
	"path/filepath"

	_ "modernc.org/sqlite"
)

const (
	driverName = "sqlite"
	// 连接参数：写锁等待 5 秒；时间按 SQLite 标准格式存储
	connectionOptions           = "?_pragma=busy_timeout(5000)&_time_format=sqlite"
	databaseDirectoryPermission = 0o755

	// issue 表：巡检问题。id 为 64 位自增主键，deleted 为逻辑删除标记（0 未删，1 已删）
	schemaSQL = `
CREATE TABLE IF NOT EXISTS issue (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    title       VARCHAR(100) NOT NULL,
    description VARCHAR(500) NOT NULL DEFAULT '',
    priority    VARCHAR(16)  NOT NULL,
    status      VARCHAR(16)  NOT NULL DEFAULT 'open',
    position_x  REAL         NOT NULL,
    position_y  REAL         NOT NULL,
    position_z  REAL         NOT NULL,
    create_time DATETIME     NOT NULL,
    update_time DATETIME     NOT NULL,
    deleted     TINYINT      NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_issue_status ON issue (status);
CREATE INDEX IF NOT EXISTS idx_issue_create_time ON issue (create_time);`
)

// Open 打开 SQLite 数据库文件（不存在则创建）并初始化表结构
func Open(databasePath string) (*sql.DB, error) {
	if err := os.MkdirAll(filepath.Dir(databasePath), databaseDirectoryPermission); err != nil {
		return nil, fmt.Errorf("创建数据库目录失败: %w", err)
	}

	database, err := sql.Open(driverName, databasePath+connectionOptions)
	if err != nil {
		return nil, fmt.Errorf("打开数据库失败: %w", err)
	}
	// SQLite 同一时刻只允许一个写入者，限制为单连接避免并发写入时报 database is locked
	database.SetMaxOpenConns(1)

	if err := database.Ping(); err != nil {
		database.Close()
		return nil, fmt.Errorf("连接数据库失败: %w", err)
	}
	if _, err := database.Exec(schemaSQL); err != nil {
		database.Close()
		return nil, fmt.Errorf("初始化表结构失败: %w", err)
	}
	return database, nil
}
