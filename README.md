# 虚拟巡检问题标注

Unity 研发实操题（无 AR 设备备选版）。在 Unity 虚拟巡检现场里用射线点击设备或场景表面放置问题标记并上报，Go 后端保存，React 管理端查看问题并修改处理状态。

```
Unity 进入虚拟现场 → 点击设备或场景表面放置标记 → 填写并上报 → Go 后端保存 → React 查看并修改状态
```

## 演示视频

视频放在 GitHub Release 附件中：[v1.0 演示视频](https://github.com/hamco0816/virtual-inspection-annotation/releases/tag/v1.0)

| 视频 | 内容 |
|---|---|
| [demo-unity-report.mp4](https://github.com/hamco0816/virtual-inspection-annotation/releases/download/v1.0/demo-unity-report.mp4) | Unity 中移动观察虚拟现场、点击设备放置标记、填写并提交问题，React 刷新后看到该问题 |

## 目录结构

| 目录 | 说明 |
|---|---|
| `server/` | Go 后端（Gin + SQLite） |
| `admin-web/` | React 管理端（React + TypeScript + Vite） |
| `unity-client/` | Unity 客户端（虚拟巡检现场） |
| `AI_LOG.md` | AI 使用记录 |

## 使用的版本

| 组件 | 版本 |
|---|---|
| Unity | 6000.3.27f1 LTS（内置渲染管线，uGUI） |
| Go | 1.25.5，Gin 1.12.0，modernc.org/sqlite 1.59.0（纯 Go 实现，无需 CGO / gcc） |
| Node.js | 22.14.0 |
| React 管理端 | React 19.3，TypeScript 7.0，Vite 8.3 |

## 启动方式

三个部分需要按顺序启动：先启动后端，再启动管理端和 Unity 客户端。

### 1. Go 后端

```bash
cd server
go run .
```

默认监听 `:8080`（本机所有网卡），数据保存在 `server/data/inspection.db`，重启服务数据仍在。可用环境变量调整：

| 环境变量 | 默认值 | 说明 |
|---|---|---|
| `SERVER_ADDR` | `:8080` | 监听地址 |
| `DB_PATH` | `data/inspection.db` | SQLite 数据库文件路径 |
| `CORS_ALLOWED_ORIGINS` | `*` | 允许跨域访问的前端来源，多个用逗号分隔 |
| `LOG_LEVEL` | `info` | 日志级别，设为 `debug` 时输出每个请求的访问日志 |

运行测试：`cd server && go test ./...`

### 2. React 管理端

```bash
cd admin-web
npm install
npm run dev
```

浏览器打开 http://localhost:5173 。开发服务器会把 `/api` 请求转发到 Go 后端，后端不在本机默认地址时用环境变量指定：

```bash
BACKEND_URL=http://192.168.1.20:8080 npm run dev
```

打包部署时（`npm run build`）可用 `VITE_API_BASE_URL` 指定后端地址，后端已开启跨域。

### 3. Unity 客户端

1. 用 Unity Hub 打开 `unity-client/` 目录，编辑器版本 6000.3.27f1。
2. 打开场景 `Assets/Scenes/InspectionSite.unity`，点击 Play。
   - 如果场景不存在或想恢复初始状态：菜单 **Inspection → 重新生成巡检场景**。
3. 打包 Windows 桌面版：菜单 **Inspection → 打包 Windows 版**，生成 `unity-client/Builds/Windows/InspectionClient.exe`。

后端地址配置在 `unity-client/Assets/StreamingAssets/client-config.json`：

```json
{
  "apiBaseUrl": "http://127.0.0.1:8080",
  "requestTimeoutSeconds": 8
}
```

打包后修改 `InspectionClient_Data/StreamingAssets/client-config.json` 即可，不用重新打包。

**操作方式**

| 操作 | 按键 |
|---|---|
| 移动 / 升降 / 加速 | W A S D / Q E / 按住 Shift |
| 转动视角 | 按住鼠标右键拖动 |
| 放置问题标记 | 鼠标左键点击设备、墙面或地面 |
| 查看已有问题 | 鼠标左键点击已保存的标记 |
| 从后端刷新标记 | R |
| 取消当前填写 | Esc 或表单中的「取消」 |

## 接口设计

统一响应格式：`{ "code": 200, "message": "success", "data": {} }`，HTTP 状态码与 `code` 一致（200 成功、400 参数错误、404 不存在、500 服务器错误）。

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/health` | 服务运行状态检查 |
| POST | `/api/issues` | 上报问题（Unity 调用） |
| GET | `/api/issues?pageNum=1&pageSize=20&status=open&priority=high` | 分页查询，按上报时间倒序；`status`、`priority` 可选；`pageSize` 最大 100 |
| GET | `/api/issues/:id` | 问题详情 |
| PATCH | `/api/issues/:id/status` | 修改处理状态，请求体 `{ "status": "in_progress" }` |

上报请求体：

```json
{
  "title": "1 号机柜温度异常",
  "description": "机柜右侧温度明显偏高",
  "priority": "high",
  "position": { "x": 1.25, "y": 0.8, "z": 2.4 }
}
```

返回的问题记录（三端字段名一致）：

```json
{
  "id": 1,
  "title": "1 号机柜温度异常",
  "description": "机柜右侧温度明显偏高",
  "priority": "high",
  "status": "open",
  "position": { "x": 1.25, "y": 0.8, "z": 2.4 },
  "createdAt": "2026-10-26T14:35:16+08:00",
  "updatedAt": "2026-10-26T14:35:16+08:00"
}
```

参数校验：`title` 去除首尾空格后不能为空且不超过 100 字；`description` 不超过 500 字；`priority` 只能是 `low`、`medium`、`high`；`status` 只能是 `open`、`in_progress`、`resolved`；`position` 必填。校验失败返回 400 和中文说明，例如 `{"code":400,"message":"title 不能为空","data":null}`。新问题由后端生成自增 ID，默认状态 `open`。

## 完成情况

### 已完成

- Unity：基础几何体搭建的虚拟机房（地面、四面墙、4 台机柜、精密空调、配电柜、消防栓箱、两条管线）；WASD 移动、右键转视角；射线检测在实际命中点放置标记；上报表单（标题必填、描述、优先级、坐标）；提交成功 / 失败提示；点击按钮或输入框不会误放标记；一次点击最多一个待提交标记，取消后移除；提交中禁用按钮防止重复提交。
- Go：运行状态检查、上报、分页列表、详情、修改状态；参数校验；SQLite 持久化，重启后数据仍在；跨域；统一响应格式；接口自动化测试。
- React：问题列表（标题、描述、优先级、状态、坐标、时间）；直接修改状态；后端不可用时显示错误提示和重新加载按钮，不白屏；空状态；分页。

### 加分项

- Unity 启动时从后端加载问题，在场景中恢复对应标记；按 R 重新同步（React 修改的状态会反映到标记颜色）。
- 标记按优先级着色（高红、中黄、低蓝），已解决变灰；颜色与 React 管理端一致。
- 点击已保存的标记查看问题详情。
- 鼠标指向时显示设备或表面名称，表单中显示命中的对象。
- React 按状态、优先级筛选；页头实时显示后端连接状态。
- Go 接口端到端测试（`go test ./...`）。

### 未完成

- 未实现登录和权限管理。
- 未实现 WebSocket 实时推送，Unity 需按 R、React 需点「刷新列表」获取最新数据。
- 未使用 AR Foundation XR Simulation 演示向 AR 版迁移。

### 已知问题

- 后端只保存坐标不保存表面朝向，重启后恢复的标记统一竖直放置；墙面上的标记恢复后立杆朝上而不是垂直于墙面。
- Unity 界面中文依赖系统字体（Windows 微软雅黑），在没有中文字体的系统上会显示方块。
- React 中修改状态后，如果当前按状态筛选，该条记录在刷新前仍留在当前列表中。

## AR 基础理解

**1. 把虚拟场景标注改成真实 AR，平面检测和点击放置通常需要哪些 AR Foundation 能力？**

- `ARSession`：管理 AR 会话的生命周期和设备跟踪。
- `XR Origin`（含 AR Camera）：把设备追踪到的真实空间映射为 Unity 坐标，相机随手机移动。
- `ARPlaneManager`：检测真实环境中的平面，`requestedDetectionMode` 设为水平面（Horizontal），生成可视化的平面对象。
- `ARRaycastManager`：从屏幕点击点向检测到的平面做射线检测（`TrackableType.PlaneWithinPolygon`），得到命中的位姿。它打的是追踪到的平面，不是 Unity 物理碰撞体，这是和本题 `Physics.Raycast` 的主要区别。
- `ARAnchorManager`：在命中位置创建锚点（`AttachAnchor(plane, pose)`，AR Foundation 6 起推荐 `TryAddAnchorAsync(pose)`），把标记挂在锚点下，跟踪修正时标记仍贴合真实位置。

**2. 为什么只保存 x/y/z 通常不能保证下次启动仍对应同一个真实位置？**

AR 的世界坐标原点和朝向由每次会话启动时设备所在的位置决定，坐标只在本次会话内有效。下次启动时手机位置、朝向不同，原点就不同，同样的 x/y/z 会落在另一个物理位置；会话中跟踪还会漂移和重新定位。要跨会话还原，需要持久化的空间锚点（ARCore Cloud Anchors、ARKit ARWorldMap 等），或者以现场固定的图片 / 二维码标识作为参考点，保存相对它的坐标，下次识别到该标识后再换算回来。

**3. AR 初始化失败、设备不支持或 Tracking 状态异常时，应用应该如何提示用户？**

- 启动时用 `ARSession.CheckAvailability()` 检查 `ARSession.state`：`Unsupported` 提示"当前设备不支持 AR"，并提供不依赖 AR 的模式（如本题的 3D 场景）；`NeedsInstall` 提示安装或更新 Google Play Services for AR（可调用 `ARSession.Install()`）；相机权限被拒绝时引导用户去系统设置开启。
- 运行中监听 `ARSession.stateChanged` 和 `ARSession.notTrackingReason`，按原因给出具体提示：`Initializing` 正在初始化，`ExcessiveMotion` 移动太快，`InsufficientLight` 光线不足，`InsufficientFeatures` 对准有纹理的表面，`Relocalizing` 正在重新定位。
- 还没检测到平面时提示"缓慢移动手机扫描地面"；跟踪异常期间禁止放置标记和上报，恢复后自动解除。
