# 实施计划：Glitch 飞船完整复刻

> **前置说明**：经代码库分析，手感物理参数（需求 5）已完全对齐 GG 数值，`ShipDash`/`ShipBoost`/`DashAfterImageSpawner`/`ShipEngineVFX` 均已存在。本计划聚焦于**美术层重建**（多层 Sprite 结构 + `ShipView` 封装）和**视觉反馈补全**（Boost 发光 + Dash 残影颜色对齐）。

---

- [ ] 1. 导入 GG 参考美术资产并配置 Sprite 导入设置
   - 从 `D:\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\` 复制以下文件到 `Assets/_Art/Ship/Glitch/Reference/`：
     - `GrabGun_Base_9.png`（430×430，飞船主体 + 发光层共用）
     - `GrabGun_Base_8.png`（430×430，高光层）
     - `GrabGun_Back_3.png`（186×96，推进器后层）
   - 在 Unity Editor 中为三张贴图设置：PPU = 320，Pivot = Center，Filter Mode = Point，Sprite Mode = Single
   - 在 `Assets/_Art/Ship/Glitch/Reference/` 创建 `README.md`，注明版权说明
   - _需求：2.1、2.2、2.4_

- [ ] 2. 创建飞船多层 Sprite 材质
   - 在 `Assets/_Art/Ship/Glitch/` 创建 `ShipGlowMaterial.mat`（基于 URP 2D Sprite-Lit-Default，Blend Mode = Additive）
   - 该材质供 `Ship_Sprite_Liquid` 发光层使用，与 `Ship_Sprite_Solid` 叠加产生发光效果
   - _需求：1.4_

- [ ] 3. 新建 `ShipView.cs` 组件
   - 路径：`Assets/Scripts/Ship/VFX/ShipView.cs`，namespace `ProjectArk.Ship`
   - `[SerializeField]` 字段引用 5 个 SpriteRenderer 子节点（Back/Liquid/HL/Solid/Core）
   - `Awake()` 中 `GetComponent<ShipBoost>()` 和 `GetComponent<ShipDash>()` 获取引用
   - `OnEnable()` 订阅 `ShipBoost.OnBoostStarted`、`ShipBoost.OnBoostEnded`、`ShipDash.OnDashStarted`、`ShipDash.OnDashEnded`
   - `OnDisable()` 取消所有订阅
   - Boost 响应：PrimeTween 在 0.1s 内将 `_liquidRenderer.color` 亮度提升至 1.5 倍；结束时 0.3s 恢复
   - Dash 响应：调用 `_dashAfterImageSpawner` 生成青绿色残影（颜色 `rgba(0.28, 0.43, 0.43, 0.8)`）；无敌帧期间驱动 `_solidRenderer` 以 0.05s 间隔闪烁（Alpha 1.0 ↔ 0.3）；`OnDashEnded` 时立即恢复 Alpha = 1.0
   - _需求：3.1、3.2、4.1、4.2、4.3、4.4、6.1、6.2、6.3、6.4、6.5_

- [ ] 4. 重建飞船 Prefab 的多层 Sprite 子节点结构
   - 打开 `Assets/_Prefabs/Ship/Ship.prefab`，在现有 `ShipVisual` 子节点下扩展为 5 层结构：
     - `Ship_Sprite_Back`（SortOrder -3）→ 引用 `GrabGun_Back_3.png`，默认材质
     - `Ship_Sprite_Liquid`（SortOrder -2）→ 引用 `GrabGun_Base_9.png`，使用 `ShipGlowMaterial`
     - `Ship_Sprite_HL`（SortOrder -1）→ 引用 `GrabGun_Base_8.png`，默认材质，Alpha = 0.5
     - `Ship_Sprite_Solid`（SortOrder 0）→ 引用 `GrabGun_Base_9.png`，默认材质
     - `Ship_Sprite_Core`（SortOrder 1）→ 暂时留空 Sprite（占位节点）
   - 在飞船根 GameObject 上添加 `ShipView` 组件，将 5 个 SpriteRenderer 拖入对应字段
   - _需求：1.1、1.2、1.3、2.3_

- [ ] 5. 扩展 `ShipBuilder.cs` Editor 工具以支持多层 Sprite 自动化构建
   - 在 `ShipBuilder.cs` 的 `EnsureChildNodes()` 方法中新增多层 Sprite 子节点创建逻辑（5 个节点，SortOrder 按规格设置）
   - 在 `WireReferences()` 中新增 `ShipView` 的 5 个 SpriteRenderer 字段自动连线
   - 在 `AddScriptComponents()` 中新增 `GetOrAddComponent<ShipView>(shipGo)`
   - 确保 `ShipBuilder` 菜单项可一键重建完整飞船 Prefab（包含新层级）
   - _需求：1.1、1.2、6.1_

- [ ] 6. 验证 Boost 尾迹粒子与 `ShipView` 的联动
   - 确认现有 `ShipEngineVFX` 的 Boost 尾迹粒子在 `ShipBoost.OnBoostStarted` 时正确激活、`OnBoostEnded` 后 ≤ 0.5s 自然消散
   - 若 `ShipEngineVFX` 未订阅 `ShipBoost` 事件，补充订阅逻辑（或在 `ShipView` 中统一驱动 `ShipEngineVFX`）
   - Play Mode 验证：按 Boost 键，引擎粒子爆发 + `Ship_Sprite_Liquid` 发光增强同步触发
   - _需求：3.3、3.4、3.5_

- [ ] 7. Play Mode 全流程验证并更新 ImplementationLog
   - 验证 5 层 Sprite 正确渲染，无 Z-fighting，SortOrder 层级正确
   - 验证 Boost：发光层亮度变化 + 尾迹粒子 + 冷却期间无重复触发
   - 验证 Dash：青绿色残影生成 + 0.2s 淡出 + 无敌帧闪烁 + 结束后 Alpha 恢复
   - 验证 `ShipView.OnDisable()` 取消订阅（切换场景后无报错）
   - 追加 `Docs/ImplementationLog/ImplementationLog.md`
   - _需求：1.1~1.4、3.1~3.5、4.1~4.4、6.4_
