# 验收清单：Level Phase 3-5 + 飞船手感增强系统

**版本：** v1.0  
**涵盖范围：**
- Level Phase 3 (L10-L12)：EncounterSystem、ArenaController、Hazard System
- Level Phase 4 (L13-L15)：Minimap/Map System、Save Integration、Sheba Scaffolder
- Level Phase 5 (L16-L19)：Multi-Floor、Enhanced Layer Transition、Narrative Fall Placeholder
- Ship Feel Enhancement：曲线移动、Dash、受击反馈、视觉 Juice

---

## 总清单（Quick Checklist）

在每项前打勾表示通过。先做**编译 & 资产检查**（无需进入 Play Mode），再做**功能验证**（Play Mode）。

### A. 编译 & 资产检查（不进入 Play Mode）

- [ ] **A1** Unity Console 无红色编译错误
- [ ] **A2** `ShipActions.inputactions` 中 Ship map 可见 `ToggleMap` action（M 键 / Gamepad Select）
- [ ] **A3** `ProjectArk.Level.asmdef` 引用列表含 `Unity.TextMeshPro` 和 `Unity.InputSystem`
- [ ] **A4** `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef` 存在且 `includePlatforms = ["Editor"]`
- [ ] **A5** 菜单 `ProjectArk > Scaffold Sheba Level` 可见
- [ ] **A6** 菜单 `ProjectArk > Ship > Create Ship Feel Assets (All)` 可见
- [ ] **A7** `Assets/_Data/Ship/DefaultShipStats.asset` 存在，含 Dash / HitFeedback / Curves 字段
- [ ] **A8** `Assets/_Data/Ship/DefaultShipJuiceSettings.asset` 存在（若不存在，执行 A6 创建）

### B. Level Phase 3 — 战斗房间逻辑

- [ ] **B1** EncounterSO 资产可通过 `Create > ProjectArk > Level > Encounter` 创建
- [ ] **B2** Arena/Boss 房间 RoomSO 引用了 EncounterSO 且 WaveCount > 0
- [ ] **B3** Arena/Boss 房间 GameObject 上有 `ArenaController` 组件
- [ ] **B4** Room 子物体中有 `EnemySpawner`，SpawnPoints 至少 1 个
- [ ] **B5** 进入 Arena 房 → 门锁定 → 延迟后刷怪 → 全清后解锁
- [ ] **B6** 已清除的 Arena 再次进入不再触发遭遇
- [ ] **B7** DamageZone 按 `_tickInterval` 持续扣血
- [ ] **B8** ContactHazard 首次接触伤害，冷却期内不再伤害
- [ ] **B9** TimedHazard 周期开关 Collider，激活时伤害

### C. Level Phase 4 — 地图 & 存档集成

- [ ] **C1** MinimapManager 场景中存在并注册到 ServiceLocator
- [ ] **C2** MapPanel Prefab 创建完毕，含 ScrollRect + Content + 楼层 Tab 容器
- [ ] **C3** MapRoomWidget Prefab 创建完毕（含 Background/FogOverlay/CurrentHighlight Image）
- [ ] **C4** MapConnectionLine Prefab 创建完毕（含 Image）
- [ ] **C5** MinimapHUD 角落叠加层配置完毕
- [ ] **C6** M 键可打开/关闭全屏地图
- [ ] **C7** 进入新房间后地图标记为已访问
- [ ] **C8** SaveBridge 场景中存在，存档后 VisitedRoomIDs 正确持久化
- [ ] **C9** 死亡重生后已访问房间不丢失

### D. Level Phase 5 — 多楼层 & 增强层间转场

- [ ] **D1** 楼层不同的房间在地图上分 Tab 显示
- [ ] **D2** 通过 IsLayerTransition 的门时有粒子/SFX/相机缩放
- [ ] **D3** 目标房间有 _ambientMusic 时 BGM 交叉淡入
- [ ] **D4** NarrativeFallTrigger 场景中可放置，触发后传送到目标房间

### E. Level Phase 5 — Sheba 关卡脚手架

- [ ] **E1** 执行 `ProjectArk > Scaffold Sheba Level` 成功
- [ ] **E2** 场景中生成 12 个 Room 含 BoxCollider2D + PolygonCollider2D + Room 组件
- [ ] **E3** `Assets/_Data/Level/Rooms/Sheba/` 下生成 12 个 RoomSO
- [ ] **E4** `Assets/_Data/Level/Encounters/Sheba/` 下生成 4-5 个 EncounterSO
- [ ] **E5** 每个 Room 之间有双向 Door 连接
- [ ] **E6** Console 输出 10 步配置清单

### F. 飞船手感增强系统

- [ ] **F1** ShipDash 组件已挂到飞船 Prefab
- [ ] **F2** ShipVisualJuice 组件已配置 `_visualChild`（视觉子物体 Transform）
- [ ] **F3** ShipEngineVFX 组件已配置 `_engineParticles`（引擎粒子 ParticleSystem）
- [ ] **F4** DashAfterImageSpawner 已配置 `_afterImagePrefab` 和 `_shipSpriteRenderer`
- [ ] **F5** CinemachineImpulseSource 已挂到相机并注册到 HitFeedbackService
- [ ] **F6** 起步有加速推力感，松手有惯性滑行
- [ ] **F7** 急转弯（>90°）明显减速
- [ ] **F8** Space 键 Dash 冲刺，冷却约 0.3s
- [ ] **F9** Dash 期间无敌，结束后有惯性延续
- [ ] **F10** 受伤时 HitStop（短暂顿帧）+ 屏幕震动 + 无敌帧闪烁
- [ ] **F11** 横移时飞船视觉倾斜
- [ ] **F12** 引擎粒子随速度变化
- [ ] **F13** Dash 时出现半透明残影

---

## 详细配置与验证步骤

---

### 第一步：编译检查

1. 打开 Unity 项目，等待编译完成
2. 查看 Console → 确认 **零红色错误**
3. 如有 `CS0246` / `CS0117` / `CS4014` 等错误，记录并报告

---

### 第二步：运行 Ship Feel Assets 创建工具

> 此步骤确保飞船手感相关的 SO 资产存在

1. 菜单 → `ProjectArk > Ship > Create Ship Feel Assets (All)`
2. 确认 Console 输出创建成功日志
3. 确认以下资产存在：
   - `Assets/_Data/Ship/DefaultShipStats.asset` — 展开 Inspector，确认包含：
     - **Movement — Curves & Feel** 区：`_accelerationCurve`、`_decelerationCurve`、`_sharpTurnAngleThreshold`(90)、`_sharpTurnSpeedPenalty`(0.7)、`_initialBoostMultiplier`(1.5)
     - **Dash** 区：`_dashSpeed`(30)、`_dashDuration`(0.15)、`_dashCooldown`(0.3)、`_dashBufferWindow`(0.15)、`_dashExitSpeedRatio`(0.5)、`_dashIFrames`(true)
     - **Hit Feedback** 区：`_hitStopDuration`(0.05)、`_iFrameDuration`(1)、`_iFrameBlinkInterval`(0.1)、`_screenShakeBaseIntensity`(0.3)
   - `Assets/_Data/Ship/DefaultShipJuiceSettings.asset` — 展开 Inspector，确认包含：
     - `_moveTiltMaxAngle`(15)、`_squashStretchIntensity`(0.15)
     - `_dashAfterImageCount`(3)、`_afterImageFadeDuration`(0.15)、`_afterImageAlpha`(0.4)
     - `_engineBaseEmissionRate`(20)、`_engineDashEmissionMultiplier`(3)

---

### 第三步：配置飞船 Prefab

> 前置条件：飞船 Prefab 已有 InputHandler、ShipMotor、ShipAiming、ShipHealth

1. 打开飞船 Prefab（如 `Assets/_Prefabs/Ship/Ship.prefab`）

2. **创建视觉子物体**（如果 SpriteRenderer 仍在根 GameObject 上）：
   - 在根下新建子 GameObject `ShipVisual`
   - 将根上的 SpriteRenderer 移到 `ShipVisual`
   - 确认 ShipAiming 等脚本不直接引用根的 SpriteRenderer（如有引用，更新为子物体）

3. **添加组件并绑定引用**：

   | 组件 | 所在 GameObject | 需配置的字段 |
   |------|----------------|-------------|
   | `ShipDash` | 根（与 ShipMotor 同 GO） | `_stats` → DefaultShipStats.asset |
   | `ShipVisualJuice` | 根 | `_visualChild` → ShipVisual 子物体的 Transform；`_juiceSettings` → DefaultShipJuiceSettings.asset |
   | `ShipEngineVFX` | 根 | `_engineParticles` → 步骤 4 创建的粒子；`_juiceSettings` → DefaultShipJuiceSettings.asset |
   | `DashAfterImageSpawner` | 根 | `_afterImagePrefab` → 步骤 5 创建的 Prefab；`_shipSpriteRenderer` → ShipVisual 的 SpriteRenderer；`_juiceSettings` → DefaultShipJuiceSettings.asset；`_stats` → DefaultShipStats.asset |

4. **创建引擎粒子**：
   - 在飞船根下新建子 GameObject `EngineExhaust`
   - 添加 `ParticleSystem`
   - 推荐设置：Shape = Cone, Start Size = 0.1-0.3, Start Lifetime = 0.3, Emission Rate = 20
   - 将此 ParticleSystem 赋给 `ShipEngineVFX._engineParticles`

5. **创建 Dash 残影 Prefab**：
   - 新建 Prefab（如 `Assets/_Prefabs/Ship/DashAfterImage.prefab`）
   - 添加 `SpriteRenderer`（Sprite 留空，运行时从飞船拷贝）
   - 添加 `DashAfterImage` 组件
   - 将此 Prefab 赋给 `DashAfterImageSpawner._afterImagePrefab`

6. **配置 ScreenShake**：
   - 在 CinemachineCamera（或 Virtual Camera）上添加 `CinemachineImpulseSource`
   - 在场景初始化脚本中调用 `HitFeedbackService.RegisterImpulseSource(source)`
   - 或创建一个简单的启动脚本将其注册（挂到相机 GameObject 上）：
     ```csharp
     void Start() {
         var source = GetComponent<CinemachineImpulseSource>();
         HitFeedbackService.RegisterImpulseSource(source);
     }
     ```

---

### 第四步：配置关卡场景（Phase 3 验证准备）

> 如果已有测试场景，跳到对应项。如果需要从 Sheba Scaffolder 开始，先做第五步。

#### 4A. EncounterSO 配置

1. 打开 `Create > ProjectArk > Level > Encounter` 创建测试 EncounterSO
2. 配置 2 波：
   - Wave 0：DelayBeforeWave = 0, Entries = [敌人 Prefab × 2]
   - Wave 1：DelayBeforeWave = 1.5, Entries = [敌人 Prefab × 3]
3. **必须**为每个 Entry 的 `EnemyPrefab` 指定有效的敌人 Prefab（含 EnemyEntity + EnemyBrain）

#### 4B. Arena 房间配置

1. 选择一个 Arena 类型的 Room GameObject
2. 确认其 RoomSO 的 `_type = Arena`，`_encounter` 引用了上面的 EncounterSO
3. 确认 Room 子物体中有 `EnemySpawner`，`_spawnPoints` 已配置（≥1 个）
4. 确认 Room GameObject 上有 `ArenaController` 组件
5. ArenaController 配置：
   - `_preEncounterDelay` = 1.5（锁门到刷怪的延迟）
   - `_postClearDelay` = 1.0（全清到解锁的延迟）
   - `_alarmSFX` / `_victorySFX` — 可选，先留空
   - `_rewardPrefab` — 可选

#### 4C. Hazard 配置（测试用）

1. 在测试场景中放置 3 个 Hazard：
   - **DamageZone**：添加 BoxCollider2D (trigger) + DamageZone 组件
     - `_damage` = 5, `_tickInterval` = 0.5, `_targetLayer` = Player 层
   - **ContactHazard**：添加 BoxCollider2D (trigger) + ContactHazard 组件
     - `_damage` = 10, `_hitCooldown` = 1.0, `_targetLayer` = Player 层
   - **TimedHazard**：添加 BoxCollider2D (trigger) + TimedHazard 组件
     - `_damage` = 8, `_activeDuration` = 2.0, `_inactiveDuration` = 3.0
     - 可选：挂 SpriteRenderer 赋给 `_visual` 查看开关状态

2. **Physics2D 碰撞矩阵**：确认 Hazard 所在层与 Player 层能互相触发 Trigger（在 `Edit > Project Settings > Physics 2D` 中配置）

---

### 第五步：执行 Sheba Level Scaffolder

1. 菜单 → `ProjectArk > Scaffold Sheba Level`
2. 确认弹窗提示 → 点击 "Scaffold"
3. 等待完成，检查：
   - **Hierarchy**：出现 `--- Sheba Level ---` 父物体，下挂 12 个 `Room_sheba_*`
   - **Project**：
     - `Assets/_Data/Level/Rooms/Sheba/` 下有 12 个 `.asset`
     - `Assets/_Data/Level/Encounters/Sheba/` 下有 4-5 个 `.asset`
   - **Console**：输出 10 步配置清单

4. **Scaffolder 后续手动配置**（按 Console 清单）：
   - [ ] 为所有 Room 和 Door 的 `_playerLayer` 字段设置 Player 层
   - [ ] 为每个 EncounterSO 分配敌人 Prefab
   - [ ] 在 Safe 房间放置 Checkpoint
   - [ ] 在 `sheba_entrance` 放置玩家出生点
   - [ ] 在 `sheba_key_chamber` 放置钥匙拾取物
   - [ ] 根据需要调整 BoxCollider2D / PolygonCollider2D 尺寸

---

### 第六步：配置 Level Phase 4-5 UI 组件

#### 6A. 创建 MapRoomWidget Prefab

1. 新建 GameObject `MapRoomWidget`，添加 `MapRoomWidget` 脚本
2. 添加子物体层级：
   ```
   MapRoomWidget (RectTransform)
   ├── Background (Image) ← 赋给 _background
   ├── IconOverlay (Image) ← 赋给 _iconOverlay (默认 disabled)
   ├── CurrentHighlight (Image) ← 赋给 _currentHighlight (边框效果)
   ├── FogOverlay (Image) ← 赋给 _fogOverlay (半透明黑色)
   └── Label (TMP_Text) ← 赋给 _labelText
   ```
3. 保存为 Prefab（如 `Assets/_Prefabs/UI/Map/MapRoomWidget.prefab`）

#### 6B. 创建 MapConnectionLine Prefab

1. 新建 GameObject `MapConnectionLine`，添加 `Image` + `MapConnectionLine` 脚本
2. Image 使用纯白色 1x1 sprite，Raycast Target = false
3. 保存为 Prefab（如 `Assets/_Prefabs/UI/Map/MapConnectionLine.prefab`）

#### 6C. 创建 Floor Tab Button Prefab

1. 新建 UI Button（`UI > Button - TextMeshPro`）
2. 调整大小适合 Tab 样式（如 80×30）
3. 保存为 Prefab（如 `Assets/_Prefabs/UI/Map/FloorTabButton.prefab`）

#### 6D. 配置 MapPanel

1. 在持久化 Canvas 下创建：
   ```
   MapPanel (MapPanel 脚本)
   ├── ScrollRect (ScrollRect 组件)
   │   ├── Viewport (Mask + Image)
   │   │   └── MapContent (RectTransform) ← _mapContent
   │   └── PlayerIcon (Image, 小三角/圆点) ← _playerIcon
   └── FloorTabContainer (HorizontalLayoutGroup) ← _floorTabContainer
   ```
2. 赋值：
   - `_inputActions` → ShipActions.inputactions
   - `_scrollRect` → ScrollRect 组件
   - `_mapContent` → MapContent 的 RectTransform
   - `_floorTabContainer` → FloorTabContainer 的 RectTransform
   - `_roomWidgetPrefab` → 步骤 6A 的 Prefab
   - `_connectionLinePrefab` → 步骤 6B 的 Prefab
   - `_floorTabPrefab` → 步骤 6C 的 Prefab
   - `_playerIcon` → PlayerIcon 的 RectTransform
3. 默认隐藏（脚本 Awake 会自动 SetActive(false)）

#### 6E. 配置 MinimapHUD

1. 在持久化 Canvas 下创建角落 UI：
   ```
   MinimapHUD (MinimapHUD 脚本, 锚点右下角)
   ├── Background (Image, 半透明黑色)
   ├── Content (RectTransform) ← _content
   └── FloorLabel (TMP_Text) ← _floorLabel
   ```
2. 赋值：
   - `_content` → Content 的 RectTransform
   - `_background` → Background 的 Image
   - `_miniRoomWidgetPrefab` → MapRoomWidget Prefab（或单独的小版本）
   - `_miniConnectionLinePrefab` → MapConnectionLine Prefab
   - `_worldToMinimapScale` = 2, `_visibleRadius` = 30

#### 6F. 场景管理器配置

在场景中确保以下管理器 GameObject 存在并挂了对应组件：

| 组件 | 必需 | 说明 |
|------|------|------|
| `MinimapManager` | Phase 4 | `_saveSlot` = 0 |
| `SaveBridge` | Phase 4 | `_saveSlot` = 0 |
| `RoomManager` | 已有 | 无需额外配置 |
| `GameFlowManager` | 已有 | 现在 Start() 会自动调用 SaveBridge.LoadAll() |
| `CheckpointManager` | 已有 | 已改为通过 SaveBridge 存档 |
| `WorldProgressManager` | 已有 | 已改为通过 SaveBridge 存档 |

---

### 第七步：Play Mode 功能验证

#### 7A. 飞船手感（优先级最高，可独立验证）

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| F6 | 加速曲线 | WASD 起步 | 有推力加速感，非瞬间满速 |
| F6 | 减速惯性 | 松开方向键 | 有明显滑行后减速 |
| F7 | 急转弯 | 高速移动中反向 | 速度明显降低 |
| F8 | Dash | 按 Space | 短距离冲刺，~0.3s 后可再次 Dash |
| F9 | Dash 无敌 | Dash 穿过伤害区 | 不受伤害 |
| F9 | Dash 动量 | Dash 结束后 | 有一定惯性延续 |
| F8 | 输入缓冲 | 冷却期内按 Space | 冷却结束后自动执行 Dash |
| F10 | HitStop | 被敌人攻击 | 短暂顿帧 |
| F10 | 屏幕震动 | 被攻击 | 相机抖动（需配置 CinemachineImpulseSource） |
| F10 | 无敌帧 | 被攻击后 ~1s 内再次接触敌人 | 不受伤害，精灵闪烁 |
| F11 | 移动倾斜 | 横向移动 | 视觉子物体有微小倾斜 |
| F12 | 引擎粒子 | 移动 / Dash | 尾焰粒子，Dash 时更强 |
| F13 | Dash 残影 | Dash | 身后出现 3 个半透明残影并淡出 |

#### 7B. Level Phase 3 — 战斗房间

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| B5 | Arena 遭遇流程 | 进入 Arena 房 | 门锁定 → (1.5s) → 敌人生成 → 全清 → (1s) → 门解锁 |
| B5 | 波次推进 | 杀完第 1 波 | Console: `Spawning wave 2/X`，延迟后第 2 波生成 |
| B6 | 已清除跳过 | 再次进入已清除 Arena | 不触发遭遇，门保持 Open |
| B7 | DamageZone | 站在酸液区 | 每 0.5s 扣 5 HP |
| B8 | ContactHazard | 接触激光栅栏 | 扣 10 HP，1s 内再碰不扣 |
| B9 | TimedHazard | 等待周期切换 | 激活 2s → 关闭 3s → 激活，激活时碰到扣血 |

#### 7C. Level Phase 4 — 地图系统

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| C6 | 打开全屏地图 | 按 M 键 | MapPanel 全屏显示 |
| C6 | 关闭全屏地图 | 再按 M 键 | MapPanel 关闭 |
| C7 | 房间访问标记 | 进入新房间 | 地图上该房间从 "迷雾" 变为可见 |
| — | MinimapHUD 刷新 | 进入新房间 | 角落小地图以当前房间为中心刷新 |
| — | 滚轮缩放 | 全屏地图中滚轮 | 地图内容缩放 |
| C8 | 存档持久化 | 激活存档点后退出再进入 | 已访问房间仍然显示 |

#### 7D. Level Phase 5 — 多楼层 & 层间转场

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| D1 | 楼层 Tab | 访问不同楼层房间后按 M | 地图顶部出现楼层 Tab（F0 / F-1 等） |
| D2 | 层间转场增强 | 通过 IsLayerTransition 门 | 更长淡入淡出 + 粒子 + SFX + 相机缩放（需配置字段） |
| D3 | BGM 交叉淡入 | 目标房间 RoomSO 有 _ambientMusic | 音乐平滑过渡（需配置 AudioClip） |

---

### 第八步：层间转场增强配置（可选，Phase 5 细化）

> 以下字段在 `DoorTransitionController` Inspector 上配置。均为可选，未配置时只使用基础淡入淡出。

| 字段 | 推荐值 | 说明 |
|------|--------|------|
| `_layerTransitionParticles` | 子 ParticleSystem | 下降/上升粒子，创建子物体挂 ParticleSystem |
| `_layerTransitionSFX` | AudioClip | 转场音效（rumble / whoosh） |
| `_bgmCrossfadeDuration` | 1.5 | BGM 交叉淡入时长 |
| `_layerZoomOutAmount` | 2 | 相机 zoom-out 幅度 |
| `_layerZoomDuration` | 0.3 | zoom 动画时长 |

配合在 `RoomSO` 上为不同楼层房间设置 `_ambientMusic`（AudioClip），实现楼层间 BGM 切换。

---

### 第九步：NarrativeFallTrigger 验证（占位符）

> 当前为占位符实现，仅验证脚本可放置和基本传送功能。

1. 在测试场景中创建空 GameObject，添加 BoxCollider2D (trigger) + `NarrativeFallTrigger`
2. 配置：
   - `_targetRoom` → 目标楼层的 Room
   - `_landingPoint` → 目标 Room 下的一个 Transform
   - `_playerLayer` → Player 层
3. Play Mode：飞船进入触发区 → Console 输出 `PLACEHOLDER — Fall sequence triggered`
4. 飞船被传送到目标房间（无动画，纯传送 — 这是正确的占位行为）

---

## 常见问题排查

| 现象 | 可能原因 | 解决方案 |
|------|---------|---------|
| 编译错误 `CS0246: TextMeshPro` | Level.asmdef 缺少 TMP 引用 | 确认 `ProjectArk.Level.asmdef` 有 `Unity.TextMeshPro` |
| 编译错误 `InputAction` | Level.asmdef 缺少 InputSystem 引用 | 确认有 `Unity.InputSystem` |
| M 键无反应 | MapPanel 未配置 `_inputActions` | 赋值 ShipActions.inputactions |
| MapPanel 打开后空白 | 未配置 Prefab 引用 | 检查 `_roomWidgetPrefab` / `_connectionLinePrefab` |
| 地图无房间 | MinimapManager 未添加到场景 | 确认有 MinimapManager 组件 |
| Dash 无反应 | ShipDash 未挂到飞船 | 添加 ShipDash 并赋值 `_stats` |
| 无屏幕震动 | CinemachineImpulseSource 未注册 | 见第三步第 6 项 |
| Arena 不锁门 | RoomSO type 不是 Arena/Boss | 检查 RoomSO `_type` |
| Arena 不刷怪 | EncounterSO 的 Entry.EnemyPrefab 为空 | 赋值有效的敌人 Prefab |
| 存档无效 | SaveBridge 未在场景中 | 添加 SaveBridge 到管理器 GO |
| Scaffolder 菜单不可见 | Editor asmdef 配置问题 | 确认 `ProjectArk.Level.Editor.asmdef` 引用正确 |
