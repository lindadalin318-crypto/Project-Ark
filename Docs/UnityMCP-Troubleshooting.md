# Unity MCP 连接故障排查记录

> 日期：2026-03-04  
> 环境：Windows 11, .NET 10.0.102 (SDK), .NET 9.0.13 (Runtime), Unity 6000.3.7f1  
> 插件版本：`com.ivanmurzak.unity.mcp` 0.51.4

---

## 问题描述

Cursor IDE 无法连接 Unity MCP，出现以下两类错误交替出现：

**Unity Console 错误：**
```
SocketException: 由于目标计算机积极拒绝，无法连接。
HubConnection Failed to start connection. Error getting negotiation response from 'http://localhost:50980/hub/mcp-server'.
```

**Cursor MCP 日志错误：**
```
SSE error: TypeError: fetch failed: connect ECONNREFUSED 127.0.0.1:50980
Unexpected token 'G', "      GetMcpClie"... is not valid JSON
```

---

## 架构说明（`com.ivanmurzak.unity.mcp` 0.51.x）

该插件采用三方通信架构：

```
Cursor (MCP Client)
    ↕ stdio (JSON-RPC)
unity-mcp-server.exe (MCP Server / 中继)
    ↕ SignalR / HTTP (port 50980)
Unity Editor Plugin (McpManagerClientHub)
```

- **Cursor** 通过 `stdio` 启动并与 `unity-mcp-server.exe` 通信
- **`unity-mcp-server.exe`** 在本地 50980 端口监听，作为中继服务器
- **Unity Editor 插件**通过 SignalR 连接到中继服务器的 `/hub/mcp-server`

---

## 根本原因分析

### Bug 1：`mcp.json` 配置错误

初始配置使用了 HTTP 模式，但 Cursor 需要 stdio 模式来启动服务器进程：

```json
// ❌ 错误（HTTP 模式，要求服务器已在运行）
{
  "mcpServers": {
    "unityMCP": { "url": "http://localhost:8080/mcp" },
    "ai-game-developer": { "type": "http", "url": "http://localhost:50980" }
  }
}
```

### Bug 2：预编译 exe 在本机环境崩溃（核心 Bug）

Unity 插件下载的 `Library/mcp-server/win-x64/unity-mcp-server.exe` 无法启动：

```
Unhandled exception. System.IO.FileLoadException:
File name: 'Microsoft.AspNetCore, Version=9.0.0.0'
---> System.ArgumentException: Value does not fall within the expected range.
    at NLog.LogManager.GetCurrentClassLogger()
    at com.IvanMurzak.Unity.MCP.Server.Program.Main(String[] args)
```

**诊断过程：**

1. 确认 .NET Runtime 已正确安装（9.0.13, 10.x 均存在）
2. 修改 `NLog.config`（移除 `callsite` 布局）→ 无效
3. 删除 `aspnetcorev2_inprocess.dll` → 无效
4. 删除 `web.config` → 无效
5. 设置各类 `DOTNET_*` 环境变量 → 无效
6. 检查 exe 内嵌的 runtimeconfig：发现是 **self-contained 单文件发布**，内嵌完整 .NET 9.0.13
7. **最终定位**：`NLog.LogManager.GetCurrentClassLogger()` 在 .NET 9 self-contained 单文件发布中，`StackFrame.GetMethod()` 返回 null，NLog 没有做 null 检查，导致 `ArgumentException` 被包装成 `FileLoadException`

这是 **NLog + .NET 9 单文件 self-contained 发布**的已知兼容性 Bug，与系统 .NET 版本无关，无法通过环境变量绕过。

### Bug 3：`client-transport` 参数传错

使用 dotnet tool 版本后，将 `client-transport=streamableHttp` 传入，导致服务器启动 HTTP 模式，把运行日志输出到 stdout，Cursor 误将日志解析为 JSON-RPC 消息：

```
Unexpected token 'G', "      GetMcpClie"... is not valid JSON
```

---

## 解决方案

### 关键发现

系统中已安装了 `com.IvanMurzak.Unity.MCP.Server` 的 **dotnet global tool** 版本（0.51.3）：

```
C:\Users\seanyyao\.dotnet\tools\unity-mcp-server.exe
```

该版本是 **framework-dependent** 发布，使用系统 .NET 9 Runtime，**不触发 NLog self-contained 兼容性 Bug，可以正常运行**。

### 最终 `mcp.json` 配置

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "C:/Users/seanyyao/.dotnet/tools/unity-mcp-server.exe",
      "args": ["port=50980", "client-transport=stdio"],
      "type": "stdio"
    }
  }
}
```

**关键参数说明：**
- `command`：指向 dotnet global tool 版本（而非 `Library/mcp-server/win-x64/` 下的预编译版）
- `args: ["port=50980"]`：指定插件连接端口，与 Unity 插件默认端口一致
- `args: ["client-transport=stdio"]`：告知服务器通过 stdio 与 Cursor 通信，服务器日志不写入 stdout，避免 JSON 解析错误
- `type: "stdio"`：告知 Cursor 以 stdio 模式启动此进程

---

## 操作步骤（可复现）

1. **确认 dotnet global tool 已安装：**
   ```powershell
   dotnet tool list -g | findstr "unity-mcp"
   ```
   应看到 `com.ivanmurzak.unity.mcp.server`。若未安装：
   ```powershell
   dotnet tool install -g com.IvanMurzak.Unity.MCP.Server --version 0.51.3
   ```

2. **修改 `.cursor/mcp.json`**（如上所示）

3. **重启 Cursor**

4. **确保 Unity Editor 已打开**，Unity 插件的 `McpServerManager` 会自动连接到 50980 端口

5. **验证连接**：Unity Console 中应出现连接成功日志，Cursor 的 MCP 面板中 `ai-game-developer` 变为绿色

---

## 注意事项

### Unity 插件会覆盖 `mcp.json`

`McpServerManager` 在检测到配置不存在或版本不匹配时，会**自动重写 `mcp.json`**，把 `command` 改回 `Library/mcp-server/win-x64/unity-mcp-server.exe`（有 Bug 的版本）。

**如果 MCP 连接又断了，检查 `mcp.json` 是否被覆盖，重新改回 dotnet tool 路径。**

### 预编译 exe 的修复条件

`Library/mcp-server/win-x64/unity-mcp-server.exe` 在以下条件下会崩溃：
- 机器上安装了 .NET 10.x SDK（`dotnet --version` 返回 10.x）
- NLog 初始化时 `StackFrame.GetMethod()` 返回 null

作者修复此 Bug 后（更新 NLog 版本），预编译 exe 应恢复正常，届时可改回使用 `Library/` 下的版本。

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `.cursor/mcp.json` | MCP 服务器配置，**最终正确版本见上** |
| `Library/mcp-server/win-x64/unity-mcp-server.exe` | 预编译版，当前有 Bug，**不要使用** |
| `Library/mcp-server/win-x64/aspnetcorev2_inprocess.dll.bak` | 已重命名备份（排查过程中的遗留，无影响） |
| `Library/mcp-server/win-x64/NLog.config.bak` | 已重命名备份（排查过程中的遗留，无影响） |
| `C:\Users\seanyyao\.dotnet\tools\unity-mcp-server.exe` | **实际使用的可运行版本** |
