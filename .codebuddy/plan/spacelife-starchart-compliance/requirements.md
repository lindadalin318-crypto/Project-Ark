# 需求文档：SpaceLife 与 StarChart 模块 CLAUDE.md 规范合规性审查

## 引言

本文档是对 `ProjectArk.SpaceLife` 和 `ProjectArk.Combat`（StarChart 子模块）两个模块进行 CLAUDE.md 规范合规性审查后的完整发现。目标是确保这两个模块的代码**完整**符合项目的架构原则、代码规范、异步纪律、事件卫生、服务定位等所有要求。

审查范围：
- **SpaceLife 模块**（20 个 .cs 文件）：SpaceLifeManager, SpaceLifeInputHandler, PlayerController2D, SpaceLifeRoom, SpaceLifeRoomManager, SpaceLifeDoor, NPCController, DialogueUI, Interactable, PlayerInteraction, RelationshipManager, GiftInventory, GiftUI, MinimapUI, NPCInteractionUI, TransitionUI, SpaceLifeQuickSetup, DialogueData, NPCDataSO, ItemSO, 以及 Editor 目录
- **StarChart 模块**（18 个 .cs 文件）：StarChartController, WeaponTrack, SnapshotBuilder, SlotLayer, StarChartItemSO, StarCoreSO, PrismSO, LightSailSO, SatelliteSO, StarChartEnums, FiringSnapshot, StarChartContext, ProjectileParams, StatModifier, FirePoint, IStarChartItemResolver, LightSailBehavior, LightSailRunner, SatelliteRunner, SatelliteBehavior, SpeedDamageSail

---

## 需求

### 需求 1：修复 SpaceLife 模块的 `FindObjectsByType` / `FindFirstObjectByType` 禁令违规

**用户故事：** 作为首席架构师，我希望消除所有 `FindObjectsByType` / `FindFirstObjectByType` 的运行时调用，以便遵循 ServiceLocator 架构原则（CLAUDE.md 第7条：禁止 FindAnyObjectByType / FindObjectOfType 运行时查找）。

#### 违规清单：
| 文件 | 行 | 违规代码 | 严重程度 |
|------|------|----------|----------|
| SpaceLifeManager.cs | Start() | `FindFirstObjectByType<SpaceLifeInputHandler>()` | ⚠ WARNING — fallback 逻辑，但违反规范 |
| SpaceLifeManager.cs | Start() | `FindFirstObjectByType<InputHandler>()` | ⚠ WARNING — fallback 逻辑 |
| SpaceLifeRoomManager.cs | FindAllRooms() | `FindObjectsByType<SpaceLifeRoom>(FindObjectsSortMode.None)` | 🔴 ERROR — 运行时热路径 |
| PlayerInteraction.cs | FindNearestInteractable() | `FindObjectsByType<Interactable>(FindObjectsSortMode.None)` | 🔴 CRITICAL — 每帧 Update 调用！O(n) 遍历 |

#### 验收标准
1. WHEN SpaceLifeManager.Start() 执行 THEN 系统 SHALL 仅通过 ServiceLocator 获取 SpaceLifeInputHandler 和 InputHandler，不调用 FindFirstObjectByType
2. WHEN SpaceLifeRoomManager 需要收集房间列表 THEN 系统 SHALL 通过序列化列表或 ServiceLocator 注册模式获取房间引用，不调用 FindObjectsByType
3. WHEN PlayerInteraction.Update() 每帧执行 THEN 系统 SHALL 通过缓存的 Interactable 列表或触发器检测获取附近可交互物，不调用 FindObjectsByType（每帧性能杀手）

---

### 需求 2：修复 SpaceLife 模块的 SO 数据类 public 字段违规

**用户故事：** 作为首席程序员，我希望 SpaceLife 的 ScriptableObject 数据类使用 `[SerializeField] private` + 公共属性暴露字段，以便遵循代码规范（CLAUDE.md：使用 `[SerializeField]` 暴露私有字段，不用 public）。

#### 违规清单：
| 文件 | 违规字段 |
|------|----------|
| NPCDataSO.cs | `public string npcName`, `public Sprite avatar`, `public NPCRole role`, `public int startingRelationship`, `public List<DialogueLine> defaultDialogues/friendlyDialogues/bestFriendDialogues`, `public List<ItemSO> likedGifts/dislikedGifts` |
| ItemSO.cs | `public string itemName`, `public string description`, `public Sprite icon`, `public int baseGiftValue` |
| DialogueData.cs | `public string speakerName`, `public string text`, `public Sprite speakerAvatar`, `public List<DialogueOption> options`, `public string optionText`, `public DialogueLine nextLine`, `public int relationshipChange` |

#### 验收标准
1. WHEN NPCDataSO、ItemSO、DialogueData 中的字段被定义 THEN 系统 SHALL 使用 `[SerializeField] private` 声明并提供只读属性访问器
2. IF 外部代码需要读取 SO 字段 THEN 系统 SHALL 通过公共属性（PascalCase）访问，不直接访问字段

---

### 需求 3：修复 SpaceLife 模块的命名规范违规

**用户故事：** 作为首席架构师，我希望所有字段命名遵循 `_camelCase` 前缀规范，以便代码风格统一。

#### 违规清单：
| 文件 | 违规 | 正确写法 |
|------|------|----------|
| SpaceLifeRoom.cs | 枚举 `SpaceLifeRoomType` 与类定义同文件 | 应拆分到独立文件 `SpaceLifeRoomType.cs` |
| RelationshipManager.cs | 枚举 `RelationshipLevel` 与类定义同文件 | 应拆分到独立文件 `RelationshipLevel.cs` |
| NPCDataSO.cs | 枚举 `NPCRole` 与类定义同文件 | 应拆分到独立文件 `NPCRole.cs` |
| DialogueData.cs | 两个类 `DialogueLine` 和 `DialogueOption` 同文件 | 可保留（小型辅助类例外） |

#### 验收标准
1. WHEN 一个枚举类型不是类的内部类型 THEN 系统 SHALL 将其放在独立文件中（文件名=枚举名.cs）
2. IF 两个类属于紧密关联的小型辅助类 THEN 系统 SHALL 允许同文件定义（如 DialogueLine + DialogueOption）

---

### 需求 4：修复 SpaceLife 模块的事件卫生违规

**用户故事：** 作为首席架构师，我希望所有事件订阅在 OnDisable/OnDestroy 中正确取消，以便避免僵尸引用（CLAUDE.md 第5条：事件卫生）。

#### 违规清单：
| 文件 | 问题描述 |
|------|----------|
| SpaceLifeDoor.cs | `OnInteract.AddListener(OnInteract)` 在 `SetupInteractable()` 中注册，但 `OnDestroy` 中使用 `RemoveListener` — ✅ 合规 |
| MinimapUI.cs | 订阅 `_roomManager.OnRoomChanged` 和 `_spaceLifeManager.OnEnterSpaceLife/OnExitSpaceLife`，`OnDestroy` 中正确取消 — ✅ 合规 |
| SpaceLifeManager.cs | `OnEnterSpaceLife` / `OnExitSpaceLife` 事件在 `OnDestroy` 中未清空 | 🔴 应在 OnDestroy 中 `OnEnterSpaceLife = null; OnExitSpaceLife = null;` |
| GiftInventory.cs | `OnInventoryChanged` 事件在 `OnDestroy` 中未清空 | ⚠ 轻微风险 |
| RelationshipManager.cs | `OnRelationshipChanged` 事件在 `OnDestroy` 中未清空 | ⚠ 轻微风险 |
| DialogueUI.cs | `OnDialogueEnd` 事件在 `OnDestroy` 中未清空 | ⚠ 轻微风险 |
| GiftUI.cs | `OnGiftGiven` 事件在 `OnDestroy` 中未清空 | ⚠ 轻微风险 |

#### 验收标准
1. WHEN 一个 MonoBehaviour 声明了 C# event 字段 THEN 系统 SHALL 在 `OnDestroy` 中将该事件置为 `null`
2. WHEN SpaceLifeManager 被销毁 THEN 系统 SHALL 清空 `OnEnterSpaceLife = null; OnExitSpaceLife = null;`

---

### 需求 5：修复 SpaceLife 模块中 TransitionUI 的补间实现违规

**用户故事：** 作为首席架构师，我希望补间动画使用 PrimeTween 而非手写 Lerp，以便遵循异步纪律（CLAUDE.md：补间动画使用 PrimeTween，不在 Update 中手写 Lerp）。

#### 违规清单：
| 文件 | 方法 | 违规 |
|------|------|------|
| TransitionUI.cs | `FadeInAsync` | 手写 `while(elapsed < duration) { Mathf.Lerp; await UniTask.Yield; }` |
| TransitionUI.cs | `FadeOutAsync` | 同上 |
| SpaceLifeRoomManager.cs | `SmoothMoveCameraAsync` | 手写 `while(distance > 0.01f) { Vector3.Lerp; await UniTask.Yield; }` |

#### 验收标准
1. WHEN TransitionUI 需要淡入淡出效果 THEN 系统 SHALL 使用 PrimeTween（如 `Tween.Alpha` 或 `Tween.Custom`）实现
2. WHEN SpaceLifeRoomManager 需要相机平滑移动 THEN 系统 SHALL 使用 PrimeTween（如 `Tween.Position`）替代手写 Lerp 循环

---

### 需求 6：修复 SpaceLife 模块的 Editor asmdef 缺失 rootNamespace

**用户故事：** 作为首席架构师，我希望所有 asmdef 都正确设置 `rootNamespace`，以便 IDE 自动推断命名空间。

#### 违规清单：
| 文件 | 问题 |
|------|------|
| ProjectArk.SpaceLife.Editor.asmdef | 缺少 `"rootNamespace": "ProjectArk.SpaceLife.Editor"` |

#### 验收标准
1. WHEN ProjectArk.SpaceLife.Editor.asmdef 被定义 THEN 系统 SHALL 包含 `"rootNamespace": "ProjectArk.SpaceLife.Editor"` 字段

---

### 需求 7：修复 StarChart 模块的 `Awake` 中动态添加 `AudioSource` 组件

**用户故事：** 作为首席程序员，我希望 StarChartController 的 AudioSource 依赖通过 `[RequireComponent]` 或 `[SerializeField]` 管理，以便避免运行时动态添加组件。

#### 违规清单：
| 文件 | 方法 | 违规 |
|------|------|------|
| StarChartController.cs | Awake() | `_audioSource = gameObject.AddComponent<AudioSource>()` — 运行时动态添加组件 |

#### 验收标准
1. WHEN StarChartController 需要 AudioSource THEN 系统 SHALL 通过 `[RequireComponent(typeof(AudioSource))]` 声明依赖并在 Awake 中 `GetComponent<AudioSource>()`

---

### 需求 8：修复 Interactable 指示器的运行时 Instantiate/Destroy 问题

**用户故事：** 作为首席架构师，我希望 Interactable 的交互指示器不在运行时频繁 Instantiate/Destroy，以便避免 GC 压力和违反性能原则。

#### 违规清单：
| 文件 | 方法 | 违规 |
|------|------|------|
| Interactable.cs | CreateIndicator() / DestroyIndicator() | 每次进出范围都 `new GameObject` + `Destroy`，且指示器没有 Sprite（SpriteRenderer 但无 Sprite 赋值） |

#### 验收标准
1. WHEN 玩家接近可交互物 THEN 系统 SHALL 通过预创建的指示器 GameObject 的 SetActive 切换显示/隐藏，不频繁 Instantiate/Destroy
2. WHEN 指示器被创建 THEN 系统 SHALL 赋值一个有效的 Sprite 或使用程序化方形 Sprite（参考已有的 `CreateSquareSprite`），避免 SpriteRenderer 不可见陷阱

---

### 需求 9：确认 StarChart 模块合规项（无需修改）

**用户故事：** 作为首席架构师，我希望记录 StarChart 模块的合规性确认，以便有完整的审查记录。

#### StarChart 模块合规确认清单：
| 规范项 | 状态 | 备注 |
|------|------|------|
| 命名规范 | ✅ 合规 | PascalCase 类/方法/属性，_camelCase 私有字段，英文 XML doc |
| [SerializeField] private | ✅ 合规 | 所有 SO（StarChartItemSO, StarCoreSO, PrismSO 等）均使用 [SerializeField] private |
| 文件组织（一文件一类） | ✅ 合规 | 枚举类 StarChartEnums.cs 含多个枚举但合理聚合 |
| 数据驱动 | ✅ 合规 | 所有数值在 SO 中，无 hardcode |
| 对象池 | ✅ 合规 | 所有投射物/VFX 通过 PoolManager 池化，战斗中无 Instantiate/Destroy |
| ServiceLocator | ✅ 合规 | Awake 注册 / OnDestroy 注销 |
| 事件卫生 | ✅ 合规 | OnDestroy 中 Dispose 所有 runner，但 `OnTrackFired`/`OnWeaponFired`/`OnLightSailChanged`/`OnSatellitesChanged` 事件未在 OnDestroy 中置 null |
| [RequireComponent] | ⚠ 部分合规 | InputHandler + ShipAiming + ShipMotor 已声明，但 AudioSource 动态添加 |
| 异步纪律 | ✅ 合规 | 未使用 Coroutine，纯 C# 类管理生命周期 |
| 依赖反转 | ✅ 合规 | `IStarChartItemResolver` 在 Combat 层定义，高层实现 |

#### StarChart 事件卫生补充：
| 文件 | 事件 | 问题 |
|------|------|------|
| StarChartController.cs | `OnTrackFired`, `OnLightSailChanged`, `OnSatellitesChanged` | OnDestroy 中未置 null |
| StarChartController.cs | `OnWeaponFired`（static event） | 静态事件无法置 null，但订阅者应在自己的 OnDisable 中取消 — ⚠ 需验证 |
| WeaponTrack.cs | `OnLoadoutChanged` | 非 MonoBehaviour，无 OnDestroy 生命周期，由 StarChartController 管理 — ✅ 可接受 |

#### 验收标准
1. WHEN StarChartController 被销毁 THEN 系统 SHALL 在 OnDestroy 中将 `OnTrackFired = null`, `OnLightSailChanged = null`, `OnSatellitesChanged = null`
2. WHEN 静态事件 `OnWeaponFired` 被订阅 THEN 订阅者 SHALL 在自身 OnDisable/OnDestroy 中取消订阅

---

## 附录 A：审查不涉及的已知问题（不在本次修复范围）

1. **SpaceLifeSetupWindow.cs**（Editor 工具 1204 行）— Editor-only 代码，不受运行时规范约束
2. **SpaceLifeMenuItems.cs**（Editor 工具）— Editor-only 代码，`FindObjectsByType` 在 Editor 中可接受
3. **DialogueUI / GiftUI 中的 `Instantiate(_optionButtonPrefab)`** — UI 按钮的动态创建属于低频操作，非战斗热路径，可暂时接受
4. **StarChart 的 `InstantiateModifiers()` 中 `AddComponent` + `JsonUtility`** — 这是运行时深拷贝的已知设计决策，用于解决 SO Prefab 共享实例陷阱（CLAUDE.md 已记录）
