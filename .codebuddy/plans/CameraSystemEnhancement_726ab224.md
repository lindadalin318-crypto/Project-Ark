---
name: CameraSystemEnhancement
overview: 逐条实现 6 项相机系统改进（P0-P5），将 Ark 相机系统提升至 Minishoot/Silksong 水平
todos:
  - id: p0-fix-findany
    content: "[P0] 修复 CameraTrigger 中 FindAnyObjectByType 违规 - 改用 SerializeField 引用 CinemachineCamera"
    status: completed
  - id: p1-create-director
    content: "[P1] 创建 CameraDirector 模式系统 - 实现 FOLLOWING/LOCKED/PANNING/FROZEN/FADEOUT/FADEIN 6 种模式"
    status: completed
    dependencies:
      - p0-fix-findany
  - id: p1-migrate-trigger
    content: "[P1] 迁移 CameraTrigger 到 CameraDirector - 修改触发器使用新director API"
    status: completed
    dependencies:
      - p1-create-director
  - id: p2-pan-target
    content: "[P2] 实现 Pan-to-Target 镜头飞向目标 - 在 CameraDirector 添加平移方法"
    status: completed
    dependencies:
      - p1-create-director
  - id: p3-trigger-enhance
    content: "[P3] 增强 CameraTrigger - 添加 priority/音效/清场子弹支持"
    status: completed
    dependencies:
      - p1-migrate-trigger
  - id: p4-trigger-arbitration
    content: "[P4] 实现 Trigger 重叠仲裁 - CameraDirector 维护 ActiveTriggerStack 按优先级处理"
    status: completed
    dependencies:
      - p3-trigger-enhance
  - id: p5-fade-service
    content: "[P5] 抽取通用 Fade 服务 - 从 DoorTransitionController 抽取 FadeService 供全局调用"
    status: completed
  - id: p5-migrate-door
    content: "[P5] 迁移 DoorTransitionController - 改用 FadeService 实现淡入淡出"
    status: completed
    dependencies:
      - p5-fade-service
---

## 用户需求

参考 Minishoot/Silksong/TUNIC 的相机实现，逐条实现 6 项相机系统改进。

## 核心功能

- **P0**: 修复 FindAnyObjectByType 违规模式（违反架构原则）
- **P1**: CameraDirector 模式系统 - 基础设施，后续所有功能依赖
- **P2**: Pan-to-Target 镜头飞向目标（解锁/开门镜头演出）
- **P3**: CameraTrigger 增强（priority + 入场音效 + 清场子弹）
- **P4**: Trigger 重叠仲裁（priority stack）
- **P5**: 通用 Fade 服务（从 DoorTransitionController 抽取）

## 验收标准

- P0: CameraTrigger 不再使用 FindAnyObjectByType，改用 ServiceLocator 或 SerializeField
- P1: CameraDirector 管理 FOLLOWING/LOCKED/PANNING/FROZEN/FADEOUT/FADEIN 模式切换
- P2: 支持镜头从当前位置平滑飞向目标位置，完成后返回或停留在目标
- P3: CameraTrigger 支持 priority 字段、入场音效播放、子弹清场
- P4: 多个重叠 CameraTrigger 时按 priority 高低仲裁
- P5: 任意系统可调用 FadeService.FadeInAsync/FadeOutAsync

## 技术栈

- Unity 6000.3.7f1 + URP 2D
- Cinemachine (Unity.Cinemachine)
- PrimeTween (补间动画)
- UniTask (异步编程)
- ServiceLocator (依赖解析)

## 实现方案

### P0: 修复 FindAnyObjectByType

将 `CameraTrigger.Start()` 中的 `FindAnyObjectByType<CinemachineCamera>()` 改为：

- 方案 A: 使用 `ServiceLocator.Get<CinemachineCamera>()` - 需要先注册
- 方案 B: 使用 `[SerializeField] private CinemachineCamera _vcam;` - 推荐，更显式

### P1: CameraDirector 模式系统

新建 `CameraDirector.cs`，管理 6 种相机模式：

- `FOLLOWING`: 正常跟随玩家（默认）
- `LOCKED`: 锁定在指定位置不动
- `PANNING`: 从 A 平移到 B
- `FROZEN`: 冻结当前画面
- `FADEOUT`: 淡出到黑
- `FADEIN`: 淡入

CameraDirector 作为单例注册到 ServiceLocator，其他组件通过它控制相机。

### P2: Pan-to-Target

在 CameraDirector 中新增 `PanToPositionAsync(Vector3 target, float duration, CancellationToken)` 方法：

- 记录当前相机位置 → Tween 移动到目标 → 可选返回
- 支持叙事场景："镜头飞向解锁的门"

### P3: CameraTrigger 增强

扩展 `CameraTrigger.cs`:

- 新增 `_priority` 字段 (int, 默认 0)
- 新增 `_entrySFX` 字段 (AudioClip)
- 新增 `_clearProjectilesOnEntry` 字段 (bool)
- 修改进入/退出逻辑，集成到 CameraDirector

### P4: Trigger 重叠仲裁

在 CameraDirector 中维护 `ActiveTriggerStack`:

- 新 trigger 进入时：如果 priority >= 栈顶，则压栈并应用；否则忽略
- trigger 退出时：如果在栈顶则弹出并恢复上一个；忽略不在栈顶的
- 需要 `CameraTrigger` 实现 `IComparable<CameraTrigger>` 或提供优先级比较方法

### P5: 通用 Fade 服务

从 DoorTransitionController 抽取 Fade 逻辑：

- 新建 `FadeService.cs`，注册到 ServiceLocator
- 提供 `FadeOutAsync(float duration, CancellationToken)` 和 `FadeInAsync(float duration, CancellationToken)`
- DoorTransitionController 改为调用 FadeService，实现复用