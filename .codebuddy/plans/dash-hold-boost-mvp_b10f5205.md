---
name: dash-hold-boost-mvp
overview: 将当前“按一次空格触发 Dash 后固定 0.2s Boost”的行为，改为“按住空格：Dash 起手后持续 Boost，松手结束”，并尽量只改输入、Dash、Boost 三处逻辑。
todos:
  - id: lock-minimal-scope
    content: 用 [skill:simple-brainstorm] 锁定最小改动边界与验收点
    status: completed
  - id: add-dash-hold-input
    content: 修改 `InputHandler.cs` 记录 Dash 按住与松开状态
    status: completed
    dependencies:
      - lock-minimal-scope
  - id: wire-dash-to-hold-boost
    content: 用 [skill:unity-developer] 调整 `ShipDash.cs` 与 `ShipBoost.cs` 的长按衔接
    status: completed
    dependencies:
      - add-dash-hold-input
  - id: playmode-verify-hold-flow
    content: 用 [mcp:unityMCP] 验证 Dash 后持续 Boost 与松手结束
    status: completed
    dependencies:
      - wire-dash-to-hold-boost
  - id: review-and-log
    content: 用 [skill:code-reviewer] 自查并追加实现日志
    status: completed
    dependencies:
      - playmode-verify-hold-flow
---

## User Requirements

- 将当前“按一次空格 → Dash → 固定时长 Boost”的行为，改为“**按住空格**：先 Dash，Dash 结束后进入持续 Boost，**松开空格立即结束 Boost**”。
- 只做**最小可玩实现**，不新增无谓系统、不做大范围重构、不扩展额外功能。
- 若玩家只是点按空格，或在 Dash 结束前已经松开空格，则**不进入持续 Boost**。
- 现有 Boost 相关视觉反馈应沿用当前链路：进入持续 Boost 时主拖尾、引擎增强等持续生效；松手退出时按现有退出链路收尾。

## Product Overview

- 空格键改为一个连续动作：按下先完成短 Dash，若仍保持按住，则自然衔接到持续推进的 Boost。
- 视觉上应表现为：Dash 起手后接上持续尾迹与推进感，直到玩家松手为止，而不是固定 0.2 秒后自动熄火。

## Core Features

- 空格长按触发：Dash 起手后自动衔接持续 Boost。
- 空格松开结束：退出 Boost 并走现有结束事件链。
- 提前松手保护：Dash 未结束前松手，不进入持续 Boost。
- 最小改动复用：沿用现有状态、事件、视觉链路，不新增多余配置与页面。

## Tech Stack Selection

- 引擎与语言：Unity 6000.3.7f1 + C#
- 输入层：现有 New Input System，复用 `Assets/Input/ShipActions.inputactions` 中已存在的 `Dash` action
- 异步层：复用项目现有 UniTask
- 状态与视觉链：复用 `ShipStateController`、`ShipView`、`ShipEngineVFX`、`BoostTrailView`

## Implementation Approach

### 方法概述

采用**最小改动链路**：继续使用现有“空格触发 Dash”的输入，不新增新的按键资产；只在输入层补充“Dash 是否仍被按住”的状态，在 Dash 结束时决定是否衔接 Boost，并让 Boost 由“固定时长结束”改为“持有期间持续、松手退出”。

### 关键技术决策

- **复用现有 `Dash` action，不新增 `Boost` 输入资产**  
当前空格已经绑定 `Dash`，且 `InputHandler` 已持有 `_dashAction`。最小方案应直接利用 `performed/canceled` 建立长按语义，避免改动 `ShipActions.inputactions` 或新增独立按键映射。
- **在 `InputHandler` 增加 Dash 持有状态，而不是新增输入系统层抽象**  
参考现有 `IsFireHeld` / `IsSecondaryFireHeld` 模式，增加 `IsDashHeld`（必要时可配套释放事件），最符合当前项目风格，改动面最小。
- **在 `ShipDash` 只改“Dash 结束后的 Boost 触发条件”**  
保留现有 Dash 冲量、i-frame、冷却、缓冲逻辑；仅将 `_boost?.ForceActivate()` 改为“只有 Dash 结束时仍按住才进入 Boost”。
- **在 `ShipBoost` 只改退出条件，不改 VFX 链**  
进入/退出 Boost 仍通过现有 `OnBoostStarted` / `OnBoostEnded` 驱动 `ShipView`、`ShipEngineVFX`、`BoostTrailView`，避免碰视觉脚本与 shader。
- **优先保留兼容退路**  
建议让 `ShipBoost.ForceActivate(...)` 能区分“Dash 长按衔接”与“其他可能的未来触发源”；Dash 长按走“松手退出”，非 Dash 触发可保留原有固定时长兜底，降低爆炸半径。

### 性能与可靠性

- 输入侧为 O(1) 状态更新；Boost 持续期间仅保留一个已有的异步等待链，不引入新管理器或额外场景查询。
- 复用 `ShipStateController.ToStateForce()`，避免新增状态枚举和重复状态分支。
- 必须在 `OnDisable` 中同步取消新增的 `canceled` 订阅并清空 Dash 持有状态，防止输入粘连。
- 不修改 `ScriptableObject` 资产运行时数据，避免 authored data 污染。

## Implementation Notes

- **不要改** `BoostTrailView.cs`、shader、场景对象与 Prefab 绑定；本需求是输入/状态语义调整，不是 VFX 重做。
- **不要新增** `Boost` action 资产或改空格绑定；当前 `Dash` action 已足够提供按下/松开语义。
- 若实现中保留 `BoostDuration` 兼容兜底，应把兼容逻辑留在 `ShipBoost.cs`，不要改 `ShipStatsSO` 资产内容。
- 保持 `DashCooldown`、`DashBufferWindow`、Dash 冲量与 i-frame 原行为不变，只收窄到“Dash 结束后的 handoff 条件”。

## Architecture Design

### 当前链路与改造后链路

- 输入：`InputHandler`
- Dash：`ShipDash`
- Boost：`ShipBoost`
- 状态：`ShipStateController`
- 视觉：`ShipView` / `ShipEngineVFX` / `BoostTrailView`

改造后的完成链：

1. 空格按下触发 `Dash`
2. `InputHandler` 记录 Dash 处于 held
3. `ShipDash` 完成 Dash 后检查 held 状态
4. 若仍 held，则进入 `ShipBoost`
5. `ShipBoost` 持续保持 Boost，直到 Dash 键释放
6. 释放后退出 Boost，并复用现有 `OnBoostEnded` 视觉收尾

### 模块关系

- `InputHandler` 只负责输入语义，不负责物理和状态切换
- `ShipDash` 只负责 Dash 执行与 Dash→Boost 衔接条件
- `ShipBoost` 只负责 Boost 状态生命周期
- `ShipStateController` 继续作为唯一状态切换入口
- VFX 订阅链不改，继续吃 `OnBoostStarted/OnBoostEnded`

## Directory Structure

## Directory Structure Summary

本次实现聚焦输入与移动脚本，避免动视觉资源与场景资产；仅调整空格长按语义、Dash→Boost 衔接和 Boost 退出条件，并补充实现日志。

/Users/dada/Documents/GitHub/Project-Ark/

- `Assets/Scripts/Ship/Input/InputHandler.cs`  [MODIFY] 输入适配层。补充 Dash 按住/松开状态读取，复用现有 `performed/canceled` 订阅模式；不新增复杂输入抽象，不改火力与交互逻辑。
- `Assets/Scripts/Ship/Movement/ShipDash.cs`  [MODIFY] Dash 执行层。保留 Dash 冷却、缓冲、冲量与 i-frame；仅调整 Dash 结束后的 Boost 触发条件为“仍在按住空格时才衔接 Boost”。
- `Assets/Scripts/Ship/Movement/ShipBoost.cs`  [MODIFY] Boost 生命周期层。将 Dash 衔接触发改为“按住期间持续、松手退出”；继续复用 `ShipStateController` 和 `OnBoostStarted/OnBoostEnded`，必要时保留非 Dash 来源的兼容兜底。
- `Assets/Input/ShipActions.inputactions`  [VERIFY] 现有输入资产。确认继续复用空格到 `Dash` 的绑定，不新增 `Boost` action，不做资产层改键。
- `Docs/ImplementationLog/ImplementationLog.md`  [MODIFY] 追加本轮“空格长按 Dash→持续 Boost→松手结束”的实现记录、目的与验证结果。

## Key Code Structures

- 优先采用小改动接口而不是新系统：
- `InputHandler` 增加 Dash held 状态读取
- `ShipDash` 在 Dash 结束时读取 held 状态决定是否调用 Boost
- `ShipBoost` 支持“持续到释放”为主、“固定时长”为兼容兜底

## Agent Extensions

### Skill

- **simple-brainstorm**
- Purpose: 在改动前收敛“最小实现”边界，避免把输入、状态、VFX 一起扩大修改。
- Expected outcome: 明确只改 `InputHandler`、`ShipDash`、`ShipBoost` 三处核心脚本。
- **unity-developer**
- Purpose: 评估 New Input System 的 `performed/canceled` 用法与 Unity 生命周期影响，确保长按语义实现稳定。
- Expected outcome: 形成符合 Unity 事件订阅习惯、无粘键副作用的实现方案。
- **code-reviewer**
- Purpose: 在完成后检查事件卫生、生命周期、回归风险与是否超出最小改动范围。
- Expected outcome: 确认没有引入无谓代码、没有破坏现有 Dash/VFX/状态链。

### MCP

- **unityMCP**
- Purpose: 进入 Play Mode 直接验证“按下 Dash、持续 Boost、松手退出”的运行时状态与视觉链路。
- Expected outcome: 得到可复现的编辑器内验证结果，确认 `BoostTrailView` 与 `OnBoostEnded` 行为正确。