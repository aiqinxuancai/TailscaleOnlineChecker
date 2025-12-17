# Tailscale 在线状态检查器

一个用于监控 Tailscale 设备在线状态的自动化工具，当设备离线超过指定时间时，会通过 PushDeer 发送推送通知。

## 功能特性

- 🔍 定期检查 Tailscale 网络中的设备在线状态
- 📱 设备离线超过 60 分钟时自动发送 PushDeer 推送通知
- 🎯 支持设备名称过滤，只监控特定设备
- ⏰ 可自定义检查间隔时间
- 🐳 支持 Docker 容器化部署

## 环境要求

- .NET 9.0 或更高版本（如果本地运行）
- Docker 和 Docker Compose（如果使用容器部署）
- Tailscale 账号和 OAuth 密钥
- PushDeer 账号和推送密钥

## 环境变量配置

在使用前，需要配置以下环境变量：

| 环境变量 | 说明 | 是否必需 | 默认值 |
|---------|------|---------|--------|
| `TAILSCALE_OAUTH_KEY` | Tailscale OAuth 密钥 | ✅ 必需 | - |
| `PUSHDEER_PUSHKEY` | PushDeer 推送密钥 | ✅ 必需 | - |
| `TAILSCALE_TAILNET` | Tailscale 网络名称 | ✅ 必需 | - |
| `DEVICE_NAME_FILTER` | 设备名称过滤关键字 | ✅ 必需 | - |
| `CHECK_INTERVAL_MINUTES` | 检查间隔（分钟） | ❌ 可选 | 30 |

### 获取 Tailscale OAuth 密钥

1. 访问 [Tailscale OAuth 设置页面](https://login.tailscale.com/admin/settings/oauth)
2. 创建一个新的 OAuth 客户端
3. 复制生成的密钥

### 获取 PushDeer 推送密钥

1. 下载并安装 PushDeer 应用
2. 在应用中生成推送密钥
3. 复制密钥用于配置

## Docker 部署

### 方式一：使用 GHCR 预构建镜像（推荐）

项目已配置 GitHub Actions 自动构建，每次推送代码到 main/master 分支或创建新标签时，会自动构建并推送镜像到 GitHub Container Registry (GHCR)。

#### 1. 创建 docker-compose.yml 文件

```yaml
version: '3.8'

services:
  tailscale-checker:
    image: ghcr.io/aiqinxuancai/tailscaleonlinechecker:latest
    container_name: tailscale-online-checker
    restart: unless-stopped

    environment:
      - TAILSCALE_OAUTH_KEY=${TAILSCALE_OAUTH_KEY}
      - PUSHDEER_PUSHKEY=${PUSHDEER_PUSHKEY}
      - TAILSCALE_TAILNET=${TAILSCALE_TAILNET}
      - DEVICE_NAME_FILTER=${DEVICE_NAME_FILTER}
      - CHECK_INTERVAL_MINUTES=${CHECK_INTERVAL_MINUTES:-30}
      - TZ=Asia/Shanghai

    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

#### 2. 创建环境变量文件

在同一目录创建 `.env` 文件：

```bash
# Tailscale 配置
TAILSCALE_OAUTH_KEY=your_oauth_key_here
TAILSCALE_TAILNET=your-org.ts.net

# PushDeer 配置
PUSHDEER_PUSHKEY=your_pushdeer_key_here

# 设备过滤
DEVICE_NAME_FILTER=your_device_keyword

# 检查间隔（可选，单位：分钟）
CHECK_INTERVAL_MINUTES=30
```

#### 3. 启动容器

```bash
# 拉取最新镜像并启动容器
docker-compose pull
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止容器
docker-compose down
```

#### 4. 使用特定版本的镜像

如果你想使用特定版本的镜像，可以在 docker-compose.yml 中指定标签：

```yaml
# 使用 latest 标签（最新版本）
image: ghcr.io/username/tailscaleonlinechecker:latest

# 使用特定版本标签
image: ghcr.io/username/tailscaleonlinechecker:v1.0.0

# 使用特定分支的构建
image: ghcr.io/username/tailscaleonlinechecker:main-abc1234
```

### 方式二：本地构建镜像

如果你需要修改代码或自定义构建，可以使用本地 Dockerfile 构建。

#### 1. 创建环境变量文件

在项目根目录创建 `.env` 文件：

```bash
# Tailscale 配置
TAILSCALE_OAUTH_KEY=your_oauth_key_here
TAILSCALE_TAILNET=your-org.ts.net

# PushDeer 配置
PUSHDEER_PUSHKEY=your_pushdeer_key_here

# 设备过滤
DEVICE_NAME_FILTER=your_device_keyword

# 检查间隔（可选，单位：分钟）
CHECK_INTERVAL_MINUTES=5
```

### 2. 启动容器

```bash
# 构建并启动容器
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止容器
docker-compose down
```

### 3. 更新配置

修改 `.env` 文件后，重启容器使配置生效：

```bash
docker-compose restart
```

## 本地运行

如果不使用 Docker，可以直接运行：

```bash
# 设置环境变量（Windows PowerShell）
$env:TAILSCALE_OAUTH_KEY="your_oauth_key"
$env:PUSHDEER_PUSHKEY="your_pushdeer_key"
$env:TAILSCALE_TAILNET="your-org.ts.net"
$env:DEVICE_NAME_FILTER="your_device_keyword"
$env:CHECK_INTERVAL_MINUTES="5"

# 运行程序
cd TailscaleOnlineChecker
dotnet run
```

## 工作原理

1. 程序启动后，按照设定的时间间隔（默认 5 分钟）循环执行检查
2. 使用 OAuth 密钥获取 Tailscale API 访问令牌
3. 获取 Tailscale 网络中的所有设备列表
4. 筛选出包含指定关键字的设备
5. 检查设备的最后在线时间
6. 如果设备离线超过 60 分钟，通过 PushDeer 发送推送通知
7. 等待指定的时间间隔后，重复上述过程

## 注意事项

1. **API 频率限制**：请合理设置检查间隔，避免触发 Tailscale API 的频率限制，建议>30分钟
2. **OAuth 权限**：确保 OAuth 密钥具有读取设备信息的权限

## 技术栈

- .NET 10.0
- Flurl.Http - HTTP 请求库
- Newtonsoft.Json - JSON 序列化库
- Docker - 容器化部署

## 许可证

本项目仅供学习和个人使用。
