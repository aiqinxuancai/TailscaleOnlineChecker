# 使用 .NET 10.0 SDK 作为构建镜像
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制项目文件
COPY TailscaleOnlineChecker/TailscaleOnlineChecker.csproj TailscaleOnlineChecker/
# 还原 NuGet 包依赖
RUN dotnet restore "TailscaleOnlineChecker/TailscaleOnlineChecker.csproj"

# 复制所有源代码
COPY TailscaleOnlineChecker/ TailscaleOnlineChecker/
WORKDIR /src/TailscaleOnlineChecker

# 构建并发布应用程序
# 使用 Release 配置进行优化构建
RUN dotnet publish "TailscaleOnlineChecker.csproj" -c Release -o /app/publish

# 使用 .NET 10.0 运行时作为最终镜像（更小的镜像体积）
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# 复制构建产物
COPY --from=build /app/publish .

# 设置时区为上海（可选，根据需要调整）
ENV TZ=Asia/Shanghai

# 启动应用程序
ENTRYPOINT ["dotnet", "TailscaleOnlineChecker.dll"]
