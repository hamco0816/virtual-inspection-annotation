// 巡检问题服务入口：加载配置、初始化数据库、启动 HTTP 服务，收到退出信号后优雅关闭。
package main

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"inspection-server/internal/config"
	"inspection-server/internal/router"
	"inspection-server/internal/store"
)

const (
	shutdownTimeout   = 5 * time.Second
	readHeaderTimeout = 10 * time.Second
)

// main 启动服务，启动失败时记录错误并以非 0 状态退出
func main() {
	if err := run(); err != nil {
		slog.Error("服务运行失败", "error", err)
		os.Exit(1)
	}
}

// run 初始化依赖并运行 HTTP 服务，直到收到 Ctrl+C 或终止信号
func run() error {
	serviceConfig, err := config.Load()
	if err != nil {
		return err
	}
	logger := slog.New(slog.NewTextHandler(os.Stdout, &slog.HandlerOptions{Level: serviceConfig.LogLevel}))
	slog.SetDefault(logger)

	database, err := store.Open(serviceConfig.DatabasePath)
	if err != nil {
		return err
	}
	defer database.Close()

	server := &http.Server{
		Addr:              serviceConfig.ListenAddress,
		Handler:           router.New(database, serviceConfig.AllowedOrigins, logger),
		ReadHeaderTimeout: readHeaderTimeout,
	}
	serverErrors := make(chan error, 1)
	go func() {
		logger.Info("服务已启动", "address", serviceConfig.ListenAddress, "database", serviceConfig.DatabasePath)
		serverErrors <- server.ListenAndServe()
	}()

	signalContext, stopListening := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stopListening()

	select {
	case err := <-serverErrors:
		if errors.Is(err, http.ErrServerClosed) {
			return nil
		}
		return fmt.Errorf("HTTP 服务异常退出: %w", err)
	case <-signalContext.Done():
	}

	logger.Info("收到退出信号，正在关闭服务")
	shutdownContext, cancel := context.WithTimeout(context.Background(), shutdownTimeout)
	defer cancel()
	return server.Shutdown(shutdownContext)
}
