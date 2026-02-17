# 实施计划 — SpaceLife 模块 Bug 修复与架构清理

> 基于 `requirements.md` 中的 6 条需求，拆分为 8 个编码任务。
> 任务按依赖顺序排列：先修复阻塞性 Bug（B1/B2），再解耦架构（B3/B5/B6），最后增强健壮性和工具。

---

- [ ] 1. 确认 Ship Prefab 生命周期并修复场景缺失问题
  - 检查 `SampleScene.unity` 中是否存在 Ship Prefab 实例；如果不存在，通过编辑场景文件或提供手动步骤将 `Assets/_Prefabs/Ship/Ship.prefab` 添加到场景中
  - 确认 Ship 的生命周期方式（静态放置 vs GameManager 动态 Spawn），如为动态 Spawn 则在 GameManager 或 SpaceLifeManager 中添加运行时自动实例化+防重复逻辑
  - 验证修复后进入 Play Mode 时 Console 不出现 `[ServiceLocator] Get: InputHandler = NOT FOUND` 错误
  - _需求：1.1, 1.2, 1.3, 1.4_

- [ ] 2. 修复 SpaceLifeManager 序列化引用缺失
  - 在 `SpaceLifeManager.Start()` 中为 `_mainCamera`、`_shipRoot`、`_spaceLifeInputHandler` 添加运行时自动获取逻辑（fallback）：
    - `_mainCamera` → `Camera.main`
    - `_shipRoot` → `ServiceLocator.Get<InputHandler>()?.gameObject`
    - `_spaceLifeInputHandler` → `FindFirstObjectByType<SpaceLifeInputHandler>()`
  - 为 `_spaceLifePlayerPrefab` 字段添加 null 检查守卫：在 `EnterSpaceLife()` 入口处（而非仅在 `SpawnPlayer()` 中）检查并阻止切换
  - 在每个 fallback 获取成功时打印 `[SpaceLifeManager] WARNING: xxx auto-found via fallback. Please assign in Inspector.`
  - 提供用户手动步骤：在 Unity Inspector 中将 `Player2D_Prefab.prefab` 拖入 `_spaceLifePlayerPrefab` 字段
  - _需求：2.1, 2.2, 2.3_

- [ ] 3. 在 `ShipActions.inputactions` 中新增 SpaceLife ActionMap
  - 在 `ShipActions.inputactions` JSON 中添加新的 `SpaceLife` ActionMap，包含以下 Action：
    - `Move`（Value/Vector2）：WASD 4方向 Composite + 方向键 Composite + Gamepad 左摇杆绑定，**与 Ship Map 的 Move 绑定完全一致**
    - `Interact`（Button）：E 键 + Gamepad Y 键
    - `ToggleSpaceLife`（Button）：Tab 键 + Gamepad Back 键（两个 Map 中都存在，确保双向可切换）
  - **不在** SpaceLife Map 中包含 Jump Action（SpaceLife 不需要跳跃）
  - **不在** SpaceLife Map 中包含 Fire/Aim/Dash 等 Ship 专用 Action
  - _需求：3.1, 3.2, 3.3, 3.4, 4.3_

- [ ] 4. 从 Ship ActionMap 中移除 `SpaceLifeJump` Action
  - 在 `ShipActions.inputactions` JSON 中删除 `SpaceLifeJump` Action 定义（id: `a1b2c3d4-2000-4000-8000-000000000011`）
  - 删除 `SpaceLifeJump` 的 4 条绑定（W 键、上方向键、Space、Gamepad South）
  - 搜索整个代码库确认没有任何代码引用 `SpaceLifeJump` 字符串（如有则清理）
  - _需求：4.1, 4.2_

- [ ] 5. 重构 `SpaceLifeInputHandler.cs` 使用 SpaceLife ActionMap
  - 将 `Awake()` 中的 `_inputActions.FindActionMap("Ship")` 改为 `_inputActions.FindActionMap("SpaceLife")`
  - 从新的 SpaceLife Map 中获取 `_toggleSpaceLifeAction`
  - `OnEnable()` 中 Enable `SpaceLife` Map（而非 `Ship` Map），`OnDisable()` 中 Disable `SpaceLife` Map（而非仅 Disable 单个 Action）
  - 移除所有 `SpaceLifeJump` 相关代码（字段、事件订阅、回调方法——当前代码中虽未直接使用 Jump，但需确认清理干净）
  - _需求：3.1, 3.2, 3.3, 3.4, 4.2_

- [ ] 6. 重构 `PlayerController2D.cs` 使用 SpaceLife ActionMap
  - 将 `Awake()` 中的 `_inputActions.FindActionMap("Ship")` 改为 `_inputActions.FindActionMap("SpaceLife")`
  - `OnEnable()` 中 Enable `SpaceLife` Map（而非 `Ship` Map），`OnDisable()` 中 Disable `SpaceLife` Map
  - 验证 4 方向移动逻辑保持不变（`ReadMovementInput()` 和 `ApplyMovement()` 无需修改，仅切换数据来源 Map）
  - _需求：3.1, 3.2, 4.4_

- [ ] 7. 增强 SpaceLifeManager 防御性日志和健壮性
  - 在 `Start()` 中每个 ServiceLocator 获取后添加具体的错误日志（包含组件名+修复建议），替换现有简单的 `LogError`
  - 在 `ToggleSpaceLife()` 入口处添加前置条件检查（`_spaceLifePlayerPrefab != null`、`_spaceLifeCamera != null`、`_spaceLifeSceneRoot != null`），不满足时打印具体原因并提前返回
  - 在 `_shipInputHandler` 为 null 时添加 `FindFirstObjectByType<InputHandler>()` fallback + Warning 日志
  - _需求：5.1, 5.2, 5.3, 5.4_

- [ ] 8. 增强 SpaceLife Setup Wizard — 添加场景健康检查面板
  - 在 `SpaceLifeSetupWindow.cs` 中添加一个"Scene Health Check"区域，列出所有关键组件的 ✅/❌ 状态：
    - Ship Prefab 实例（`FindFirstObjectByType<InputHandler>()`）
    - SpaceLifeManager 及其各序列化引用（通过 SerializedObject 检查每个字段）
    - SpaceLifeInputHandler 存在性
    - SpaceLifeCamera 存在性
    - SpaceLifeSceneRoot 存在性
    - Player2D Prefab 资产是否存在（`AssetDatabase.FindAssets`）
  - 添加 **"添加 Ship 到场景"** 按钮：从 `Assets/_Prefabs/Ship/Ship.prefab` 实例化到场景
  - 添加 **"Auto-Wire References"** 按钮：自动查找并填充 SpaceLifeManager 可推导的序列化引用（`_mainCamera` → `Camera.main`、`_spaceLifeInputHandler` → `FindFirstObjectByType`、`_shipRoot` → Ship 实例等）
  - _需求：6.1, 6.2, 6.3_
