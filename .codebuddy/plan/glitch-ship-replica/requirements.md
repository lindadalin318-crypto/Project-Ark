# 需求文档：Glitch 飞船完整复刻

## 引言

本功能目标是在 Project Ark 中完整复刻 Galactic Glitch（GG）游戏中的 **Glitch 飞船**，包含两个维度：

**手感维度（Gameplay Feel）**：GG 飞船的物理模型已在 Project Ark 中完成了代码层面的对齐（角加速度旋转 + 前向推力 + Boost 状态切换 + Dash 冲量）。本次需求聚焦于**验证并微调**现有参数，确保实际游玩手感与 GG 原版一致。

**美术维度（Visual）**：Project Ark 目前飞船为程序化占位（无 Sprite），需要完整实现 GG Glitch 飞船的多层 SpriteRenderer 视觉结构，包括：主体层、高光层、发光层、推进器层，以及 Boost/Dodge 状态的视觉反馈特效。

**参考数据来源**：
- `D:\ReferenceAssets\GalacticGlitch\GalacticGlitch_ArtAssets_Analysis.md`（飞船美术规格）
- `D:\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\`（原始资产）
- `D:\ReferenceAssets\GalacticGlitch\GalacticGlitch_dump\dump.cs`（GGSteering 物理参数）
- Project Ark `DefaultShipStats.asset`（当前已对齐的参数）

---

## 需求

### 需求 1：飞船多层 Sprite 视觉结构

**用户故事：** 作为玩家，我希望看到有层次感的飞船外观（主体 + 高光 + 发光 + 推进器），以便飞船在视觉上有质感和立体感，而不是单张平面图。

#### 验收标准

1. WHEN 游戏场景加载 THEN 飞船 Prefab SHALL 包含以下 5 个 SpriteRenderer 子节点，按 SortOrder 排列：
   - `Ship_Sprite_Back`（SortOrder -3，推进器/后层）
   - `Ship_Sprite_Liquid`（SortOrder -2，发光/能量层）
   - `Ship_Sprite_HL`（SortOrder -1，高光层）
   - `Ship_Sprite_Solid`（SortOrder 0，主体实体层）
   - `Ship_Sprite_Core`（SortOrder 1，核心/驾驶舱）
2. WHEN 飞船 Prefab 被实例化 THEN 所有 SpriteRenderer 节点 SHALL 使用对应的占位 Sprite（可以是程序化生成的简单形状，但层级结构必须正确）
3. IF 美术资产（PNG 文件）存在于 `Assets/_Art/Ship/Glitch/` THEN 系统 SHALL 自动引用对应贴图，无需修改代码
4. WHEN 飞船渲染 THEN `Ship_Sprite_Liquid` 层 SHALL 使用 Additive 或自定义发光材质，与 `Ship_Sprite_Solid` 层视觉上形成叠加发光效果

---

### 需求 2：飞船美术资产导入与配置

**用户故事：** 作为开发者，我希望将 GG 原版 Glitch 飞船的贴图正确导入 Project Ark 并配置好 Sprite 参数，以便作为视觉参考和临时占位使用（仅用于学习研究，最终替换为自制资产）。

#### 验收标准

1. WHEN 美术资产导入 THEN 以下文件 SHALL 被复制到 `Assets/_Art/Ship/Glitch/Reference/`（标注为 Reference，不用于最终发布）：
   - `GrabGun_Base_9.png`（430×430，飞船主体）
   - `GrabGun_Base_8.png`（430×430，高光层）
   - `GrabGun_Back_3.png`（186×96，推进器后层）
2. WHEN Sprite 导入设置配置 THEN 所有飞船贴图 SHALL 设置 PPU = 320，Pivot = Center，Filter Mode = Point（保持像素清晰）
3. WHEN 飞船 Prefab 引用美术资产 THEN `Ship_Sprite_Solid` 和 `Ship_Sprite_Liquid` SHALL 引用同一张 `GrabGun_Base_9.png`（通过不同材质呈现不同效果）
4. IF 美术资产为版权内容 THEN 资产文件夹 SHALL 包含 `README.md` 说明其为逆向工程参考资产，仅用于学习研究，不得用于商业发布

---

### 需求 3：Boost 状态视觉反馈

**用户故事：** 作为玩家，我希望在触发 Boost 时看到明显的视觉变化（引擎发光增强 + 尾迹特效），以便获得清晰的操作反馈，感受到速度感。

#### 验收标准

1. WHEN `ShipBoost.OnBoostStarted` 事件触发 THEN `Ship_Sprite_Liquid` 层 SHALL 在 0.1s 内通过 PrimeTween 将颜色亮度提升至 1.5 倍（模拟引擎过载发光）
2. WHEN `ShipBoost.OnBoostEnded` 事件触发 THEN `Ship_Sprite_Liquid` 层 SHALL 在 0.3s 内通过 PrimeTween 恢复正常亮度
3. WHEN Boost 激活期间 THEN 飞船尾部 SHALL 显示一个 Particle System 尾迹效果（至少包含：粒子颜色与飞船主色调一致，粒子方向与飞行方向相反，持续时间与 Boost 时长同步）
4. WHEN Boost 结束 THEN 尾迹粒子 SHALL 自然消散（不立即消失），消散时间 ≤ 0.5s
5. IF Boost 处于冷却中 THEN 不应有 Boost 视觉效果触发

---

### 需求 4：Dash 状态视觉反馈

**用户故事：** 作为玩家，我希望在触发 Dash 时看到残影效果，以便感受到瞬间位移的速度感和无敌帧的视觉提示。

#### 验收标准

1. WHEN `ShipDash` 触发 THEN 在 Dash 起始位置 SHALL 生成一个飞船轮廓残影（Ghost），颜色为青绿色 `rgba(0.28, 0.43, 0.43, 0.8)`，对应 GG 的 `Dodge_Sprite` 效果
2. WHEN 残影生成后 THEN 残影 SHALL 在 0.2s 内通过 PrimeTween Alpha 从 0.8 淡出至 0，然后自动销毁（或回收到对象池）
3. WHEN Dash 无敌帧激活期间 THEN 飞船主体 `Ship_Sprite_Solid` SHALL 以 0.05s 间隔闪烁（Alpha 在 1.0 和 0.3 之间交替），提示无敌状态
4. WHEN 无敌帧结束 THEN 飞船主体 SHALL 立即恢复正常 Alpha = 1.0，停止闪烁

---

### 需求 5：手感参数验证与微调

**用户故事：** 作为玩家，我希望 Project Ark 飞船的操控手感与 GG Glitch 飞船一致（旋转有重量感、前向推力、Boost 加速感），以便获得相同的操控体验。

#### 验收标准

1. WHEN 玩家按住前进键 THEN 飞船 SHALL 沿船头方向加速，加速度对应 `ShipStatsSO._forwardAcceleration`（当前值 20 units/s²，对应 GG mass=1 适配值）
2. WHEN 玩家松开所有输入 THEN 飞船 SHALL 通过 `linearDrag = 3` 自然减速滑行，不应有突然停止感
3. WHEN 玩家快速转向 THEN 旋转 SHALL 有明显惯性（角加速度模型），不应瞬间到位，对应 `_angularAcceleration = 800 deg/s²`
4. WHEN Boost 激活 THEN `linearDrag` SHALL 临时降低至 2.5，`maxSpeed` 提升至 9，持续 `_boostDuration = 0.2s`，与 GG IsBoostState 参数一致
5. WHEN Dash 触发 THEN 飞船 SHALL 沿当前输入方向（或船头方向）获得 `_dashImpulse = 12` 的一次性冲量，无敌帧持续 `_dashIFrameDuration = 0.15s`
6. IF 当前 `DefaultShipStats.asset` 参数与上述验收标准不符 THEN SHALL 更新 SO 数值至对齐值

---

### 需求 6：ShipView 组件封装

**用户故事：** 作为开发者，我希望飞船的所有视觉逻辑（层级管理、状态切换、特效触发）封装在一个独立的 `ShipView` 组件中，以便视觉层与物理层（ShipMotor/ShipAiming）完全解耦。

#### 验收标准

1. WHEN `ShipView` 组件初始化 THEN 它 SHALL 通过 `[SerializeField]` 引用所有 SpriteRenderer 子节点，不使用 `GetComponentInChildren` 运行时查找
2. WHEN `ShipBoost.OnBoostStarted/OnBoostEnded` 事件触发 THEN `ShipView` SHALL 响应并驱动对应视觉变化，`ShipBoost` 本身不包含任何视觉代码
3. WHEN `ShipDash` 触发 THEN `ShipView` SHALL 响应并生成残影，`ShipDash` 本身不包含任何视觉代码
4. WHEN `ShipView` 组件被禁用（OnDisable） THEN 它 SHALL 取消所有事件订阅，防止内存泄漏
5. IF `ShipView` 需要访问 `ShipBoost` 或 `ShipDash` THEN 它 SHALL 通过 `GetComponent<T>()` 在 `Awake` 中获取引用，不使用 `ServiceLocator` 或 `FindObjectOfType`
