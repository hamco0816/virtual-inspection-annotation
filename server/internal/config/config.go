// Package config 从环境变量读取服务运行配置。
package config

import (
	"fmt"
	"log/slog"
	"os"
	"strings"
)

const (
	defaultListenAddress  = ":8080"
	defaultDatabasePath   = "data/inspection.db"
	defaultAllowedOrigins = "*"
	defaultLogLevel       = "info"
)

// Config 服务运行配置
type Config struct {
	ListenAddress  string     // 服务监听地址，":8080" 表示监听本机所有网卡的 8080 端口
	DatabasePath   string     // SQLite 数据库文件路径
	AllowedOrigins []string   // 允许跨域访问的前端来源，"*" 表示全部允许
	LogLevel       slog.Level // 日志级别，debug 时会输出每个 HTTP 请求的访问日志
}

// Load 读取环境变量配置，未设置的项使用默认值
func Load() (Config, error) {
	var logLevel slog.Level
	rawLogLevel := readEnv("LOG_LEVEL", defaultLogLevel)
	if err := logLevel.UnmarshalText([]byte(rawLogLevel)); err != nil {
		return Config{}, fmt.Errorf("LOG_LEVEL 取值无效 %q: %w", rawLogLevel, err)
	}

	return Config{
		ListenAddress:  readEnv("SERVER_ADDR", defaultListenAddress),
		DatabasePath:   readEnv("DB_PATH", defaultDatabasePath),
		AllowedOrigins: splitOrigins(readEnv("CORS_ALLOWED_ORIGINS", defaultAllowedOrigins)),
		LogLevel:       logLevel,
	}, nil
}

// readEnv 读取环境变量，为空时返回默认值
func readEnv(key string, defaultValue string) string {
	value := strings.TrimSpace(os.Getenv(key))
	if value == "" {
		return defaultValue
	}
	return value
}

// splitOrigins 把逗号分隔的来源列表拆分成数组
func splitOrigins(rawOrigins string) []string {
	origins := make([]string, 0)
	for _, origin := range strings.Split(rawOrigins, ",") {
		trimmedOrigin := strings.TrimSpace(origin)
		if trimmedOrigin != "" {
			origins = append(origins, trimmedOrigin)
		}
	}
	return origins
}
