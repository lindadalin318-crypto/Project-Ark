# 实施计划：SpaceLife 与 StarChart 模块 CLAUDE.md 规范合规性修复

- [ ] 1. 消除 SpaceLife 模块所有 `FindObjectsByType` / `FindFirstObjectByType` 运行时调用
  - [ ] 1.1 重构 `PlayerInteraction.cs`：将 `FindNearestInteractable()` 中每帧 `FindObjectsByType<Interactable>` 替换为触发器检测模式（OnTriggerEnter2D/Exit2D 维护 `_nearbyInteractables` 列表），添加 Collider2D trigger 用于范围检测
    - _需求：1.3_
  - [ ] 1.2 重构 `SpaceLifeRoomManager.cs`：将 `FindAllRooms()` 中的 `FindObjectsByType<SpaceLifeRoom>` 替换为 `[SerializeField] private List<SpaceLifeRoom> _rooms` 序列化列表，同时提供 `RegisterRoom` / `UnregisterRoom` 公共方法供 SpaceLifeRoom 自注册
    - _需求：1.2_
  - [ ] 1.3 重构 `SpaceLifeManager.cs`：移除 Start() 中的两处 `FindFirstObjectByType` fallback，改为仅通过 `ServiceLocator.Get<>()` 获取 SpaceLifeInputHandler 和 InputHandler，如获取失败输出 `Debug.LogError` 提示
    - _需求：1.1_

- [ ] 2. 修复 SpaceLife SO 数据类 public 字段为 `[SerializeField] private` + 属性访问器
  - [ ] 2.1 重构 `NPCDataSO.cs`：将所有 public 字段改为 `[SerializeField] private _camelCase` + PascalCase 只读属性，更新所有引用处
    - _需求：2.1, 2.2_
  - [ ] 2.2 重构 `ItemSO.cs`：同上模式，改写 4 个 public 字段
    - _需求：2.1, 2.2_
  - [ ] 2.3 重构 `DialogueData.cs`（DialogueLine + DialogueOption）：将所有 public 字段改为 `[SerializeField] private` + 属性，更新 DialogueUI 等引用处
    - _需求：2.1, 2.2_

- [ ] 3. 修复命名规范：枚举类型拆分到独立文件
  - [ ] 3.1 将 `SpaceLifeRoomType` 枚举从 `SpaceLifeRoom.cs` 中提取到新文件 `SpaceLifeRoomType.cs`
    - _需求：3.1_
  - [ ] 3.2 将 `RelationshipLevel` 枚举从 `RelationshipManager.cs` 中提取到新文件 `RelationshipLevel.cs`
    - _需求：3.1_
  - [ ] 3.3 将 `NPCRole` 枚举从 `NPCDataSO.cs` 中提取到新文件 `NPCRole.cs`（放在 Data 目录下）
    - _需求：3.1_

- [ ] 4. 修复 SpaceLife 与 StarChart 模块的事件卫生问题
  - [ ] 4.1 在 `SpaceLifeManager.cs` 的 OnDestroy 中添加 `OnEnterSpaceLife = null; OnExitSpaceLife = null;`
    - _需求：4.1, 4.2_
  - [ ] 4.2 在 `GiftInventory.cs`、`RelationshipManager.cs`、`DialogueUI.cs`、`GiftUI.cs` 的 OnDestroy 中将各自声明的事件置 null（如无 OnDestroy 则新增）
    - _需求：4.1_
  - [ ] 4.3 在 `StarChartController.cs` 的 OnDestroy 中添加 `OnTrackFired = null; OnLightSailChanged = null; OnSatellitesChanged = null;`
    - _需求：9.1_

- [ ] 5. 将手写 Lerp 替换为 PrimeTween
  - [ ] 5.1 重构 `TransitionUI.cs`：将 `FadeInAsync` / `FadeOutAsync` 中的 while-Lerp 循环替换为 `Tween.Alpha(canvasGroup, ...)` + `.ToUniTask()` 调用
    - _需求：5.1_
  - [ ] 5.2 重构 `SpaceLifeRoomManager.cs`：将 `SmoothMoveCameraAsync` 中的 while-Lerp 替换为 `Tween.Position(cameraTransform, ...)` + `.ToUniTask()` 调用
    - _需求：5.2_

- [ ] 6. 修复 Editor asmdef rootNamespace 缺失
  - 在 `ProjectArk.SpaceLife.Editor.asmdef` 中添加 `"rootNamespace": "ProjectArk.SpaceLife.Editor"` 字段
  - _需求：6.1_

- [ ] 7. 修复 StarChartController 动态 AddComponent<AudioSource>
  - 在 `StarChartController.cs` 类上添加 `[RequireComponent(typeof(AudioSource))]`，将 Awake 中 `AddComponent<AudioSource>()` 改为 `GetComponent<AudioSource>()`
  - _需求：7.1_

- [ ] 8. 重构 Interactable 指示器为预创建 + SetActive 模式
  - 在 `Interactable.cs` 的 `Awake` 或 `Start` 中预创建指示器 GameObject（赋值程序化方形 Sprite），默认 `SetActive(false)`；`ShowIndicator` 改为 `SetActive(true)`，`HideIndicator` 改为 `SetActive(false)`；移除 `CreateIndicator` / `DestroyIndicator` 中的 `new GameObject` / `Destroy` 调用
  - _需求：8.1, 8.2_

- [ ] 9. 编译验证 + 记录实现日志
  - 执行 `dotnet build` 确认零编译错误
  - 追加实现日志到 `Docs/ImplementationLog/ImplementationLog.md`
  - _需求：全部_
