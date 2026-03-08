# 需求文档：Glitch 飞船完整特效复刻

## 引言

本需求文档描述对 Galactic Glitch（GG）中 **Glitch 飞船**的完整一比一视觉特效复刻，移植到 Project Ark。

### 背景与现状差距

上一轮实现（`glitch-ship-replica`）仅完成了**静态视觉层框架**（5层 Sprite 结构 + ShipView 组件），以下特效模块**完全缺失**：

| 缺失模块 | GG 原版实现 | 当前 Ark 状态 |
|---------|-----------|-------------|
| Boost 专属尾迹粒子 | `PlayerViewFluxyTrailModule` + `PlayerViewLQTrailModule`，4个专用材质 | 仅提高引擎粒子 emission rate |
| Dodge 专用残影 GO | `Dodge_Sprite` SpriteRenderer（SortOrder -1，青绿色，专用贴图） | 无独立 GO，仅动态生成残影 |
| Boost 推进器动画 | `BoostState.anim`（触发 `SurroundingGravGunState` GO 激活） | 无 |
| Boost 尾迹 TrailRenderer | `AdditiveTrail.mat` 驱动的 TrailRenderer | 无 |
| 引擎粒子精确参数 | 独立 Boost 粒子系统，与常规引擎分离 | 单一粒子系统，参数未对齐 |

### 参考资产来源

- **贴图**：`D:\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\Texture2D\`
  - `player_test_fire.png`（751×722，PPU=707）— Dodge 残影专用贴图
  - `SHIP_PLAYER_DODGE_HALF.png`（135×104）— Dodge 半身剪影
  - `GrabGun_Base_9.png`（430×430，PPU=320）— 飞船主体
  - `GrabGun_Back_3.png`（186×96，PPU=320）— 推进器后层
- **动画**：`BoostState.anim` — Boost 状态动画（触发 `SurroundingGravGunState` 激活）
- **材质**：`AdditiveTrail.mat` — Additive 尾迹材质
- **代码结构**：`PlayerViewBoostModule`、`PlayerViewFluxyTrailModule`、`PlayerViewLQTrailModule`（来自 dump.cs）

---

## 需求

### 需求 1：Dodge_Sprite 专用残影 GameObject

**用户故事：** 作为玩家，我希望 Dash 时看到一个青绿色的飞船轮廓残影持续显示在原位，以便直观感受到无敌帧的存在和 Dash 的位移距离。

#### 验收标准

1. WHEN 飞船执行 Dash THEN 系统 SHALL 在 `ShipVisual` 下激活名为 `Dodge_Sprite` 的 SpriteRenderer（SortOrder = -1），使用 `player_test_fire.png` 作为 Sprite，颜色为 `rgba(0.28, 0.43, 0.43, 1.0)`（青绿色）
2. WHEN Dash 开始 THEN `Dodge_Sprite` SHALL 在 Dash 起始位置保持静止（不跟随飞船移动），Alpha 从 1.0 在 `AfterImageFadeDuration`（默认 0.15s）内线性衰减至 0
3. WHEN Dash 结束 THEN 系统 SHALL 重置 `Dodge_Sprite` 的 Alpha 为 0 并停用该 GO
4. IF `player_test_fire.png` 未导入 THEN 系统 SHALL fallback 使用 `Ship_Sprite_Solid` 的当前 Sprite
5. WHEN `Dodge_Sprite` 激活 THEN 其 Transform 位置 SHALL 与 Dash 起始帧的飞船世界坐标一致（不受父节点 Transform 影响，使用世界坐标解耦）

---

### 需求 2：Boost 专属尾迹粒子系统

**用户故事：** 作为玩家，我希望 Boost 时飞船尾部喷出明显的火焰尾迹粒子，以便感受到速度爆发感和推进器过载的视觉冲击。

#### 验收标准

1. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 激活名为 `BoostTrailParticles` 的独立 ParticleSystem（挂载在 `Ship_Sprite_Back` 子节点下，位置偏移 `(0, -0.15, 0)`）
2. WHEN Boost 激活时 `BoostTrailParticles` SHALL 使用以下参数：发射率 ≥ 80/s，粒子生命周期 0.3s，起始速度 3.0（反飞船朝向），起始大小 0.08～0.15（随机），颜色从 `rgba(0.28, 0.43, 0.43, 1.0)`（青绿色）渐变至透明
3. WHEN Boost 结束 THEN 系统 SHALL 停止 `BoostTrailParticles` 的新粒子发射（`Stop(false)`），已有粒子自然消亡
4. WHEN 飞船处于常规飞行（非 Boost）THEN `BoostTrailParticles` SHALL 停止发射（emission rate = 0）
5. `BoostTrailParticles` 的材质 SHALL 使用 Additive 混合（对应 GG 的 `AdditiveTrail.mat`），确保粒子叠加发光效果

---

### 需求 3：Boost 尾迹 TrailRenderer

**用户故事：** 作为玩家，我希望 Boost 时飞船身后留下一条连续的发光尾迹线，以便感受到高速移动的流体感。

#### 验收标准

1. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 激活挂载在 `Ship_Sprite_Back` 下的 `TrailRenderer`（GO 名：`BoostTrail`）
2. `BoostTrail` TrailRenderer SHALL 使用以下参数：时间宽度 0.25s，起始宽度 0.12，末端宽度 0.0，颜色从 `rgba(0.28, 0.43, 0.43, 0.8)` 渐变至透明，材质使用 Additive 混合
3. WHEN Boost 结束 THEN 系统 SHALL 将 `BoostTrail` 的 `emitting` 设为 false，已有尾迹在 `time` 时间内自然消退
4. WHEN 飞船处于常规飞行 THEN `BoostTrail.emitting` SHALL 为 false
5. IF Boost 持续时间超过 0.5s THEN 尾迹 SHALL 保持稳定宽度不抖动（使用 `minVertexDistance = 0.05`）

---

### 需求 4：Boost 推进器动画（Ship_Sprite_Back Animator）

**用户故事：** 作为玩家，我希望 Boost 时推进器后层贴图有明显的视觉变化（如缩放/亮度脉冲），以便感受到推进器过载的机械感。

#### 验收标准

1. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 触发 `Ship_Sprite_Back` 上 Animator 的 `BoostStart` trigger，播放推进器缩放脉冲动画（Scale Y 从 1.0 → 1.3 → 1.0，持续 0.15s，对应 GG `BoostState.anim` 的行为）
2. WHEN 飞船退出 Boost 状态 THEN 系统 SHALL 触发 `BoostEnd` trigger，推进器 Scale 在 0.1s 内恢复至 `(1.0, 1.0, 1.0)`
3. WHEN 飞船处于 Boost 持续阶段 THEN `Ship_Sprite_Back` SHALL 以 0.3s 周期循环轻微脉冲（Scale Y 1.0 ↔ 1.1）
4. IF Animator 未配置 THEN 系统 SHALL 使用 PrimeTween 的 `Tween.Scale()` 作为 fallback 实现相同效果，不依赖 Animator 组件

---

### 需求 5：引擎粒子系统精确参数对齐

**用户故事：** 作为玩家，我希望常规飞行时引擎粒子的大小、颜色、速度与 GG 原版一致，以便获得相同的飞行质感。

#### 验收标准

1. WHEN 飞船以最大速度飞行 THEN 引擎粒子 SHALL 使用以下参数：发射率 40/s，粒子生命周期 0.2s，起始速度 2.0（反飞船朝向），起始大小 0.04～0.08（随机），颜色 `rgba(0.28, 0.43, 0.43, 1.0)` 渐变至透明
2. WHEN 飞船静止 THEN 引擎粒子发射率 SHALL 降至 5/s（怠速状态，保持引擎存在感）
3. WHEN 飞船执行 Dash THEN 引擎粒子 SHALL 在 Dash 持续期间发射率提升至 120/s，粒子大小乘以 1.5
4. 引擎粒子 GO（`EngineExhaust`）SHALL 位于 `Ship_Sprite_Back` 的局部坐标 `(0, -0.15, 0)`，朝向飞船尾部（局部 -Y 方向）
5. WHEN 飞船旋转 THEN 引擎粒子发射方向 SHALL 始终跟随飞船朝向（使用 `ParticleSystem.MainModule.simulationSpace = Local`）

---

### 需求 6：ShipView 扩展——统一驱动所有特效模块

**用户故事：** 作为开发者，我希望所有飞船特效由 `ShipView` 统一管理，以便保持架构解耦，避免特效逻辑散落在多个组件中。

#### 验收标准

1. WHEN `ShipView` 初始化 THEN 系统 SHALL 自动获取 `Dodge_Sprite`、`BoostTrailParticles`、`BoostTrail`、`Ship_Sprite_Back` Animator 的引用（通过 `[SerializeField]` 或 `GetComponentInChildren`）
2. `ShipView` SHALL 在 `OnEnable` 中订阅 `ShipBoost.OnBoostStarted/OnBoostEnded` 和 `ShipDash.OnDashStarted/OnDashEnded`，在 `OnDisable` 中取消订阅
3. WHEN `ShipView.OnDisable` 被调用 THEN 系统 SHALL 立即停止所有特效（TrailRenderer.Clear、ParticleSystem.Stop、Dodge_Sprite Alpha=0）
4. 所有特效参数（颜色、时长、大小范围）SHALL 通过 `ShipJuiceSettingsSO` 暴露，不得 hardcode
5. WHEN 飞船被对象池回收 THEN `ShipView.ResetVFX()` SHALL 被调用，重置所有特效至初始状态

---

### 需求 7：ShipPrefabRebuilder 扩展——自动化 Prefab 重建

**用户故事：** 作为开发者，我希望通过一键 Editor 工具完成所有新增 GO 节点的创建和连线，以便避免手动编辑 Prefab 序列化文件的风险。

#### 验收标准

1. WHEN 运行 `ProjectArk > Ship > Rebuild Ship Prefab Sprite Layers` THEN 系统 SHALL 在 `ShipVisual` 下创建 `Dodge_Sprite` GO（SpriteRenderer，SortOrder=-1，初始 Alpha=0，inactive）
2. WHEN 运行重建工具 THEN 系统 SHALL 在 `Ship_Sprite_Back` 下创建 `BoostTrailParticles`（ParticleSystem）和 `BoostTrail`（TrailRenderer）GO
3. WHEN 运行重建工具 THEN 系统 SHALL 自动将 `player_test_fire.png` 赋值给 `Dodge_Sprite.sprite`（若已导入）
4. WHEN 运行重建工具 THEN 系统 SHALL 将所有新增组件引用连线到 `ShipView` 的对应 `[SerializeField]` 字段
5. 重建工具 SHALL 幂等（多次运行结果相同，不产生重复节点）

---

## 非功能性需求

- **性能**：Boost 尾迹粒子数量上限 ≤ 200 个，TrailRenderer 顶点数 ≤ 50，不影响 60fps 目标
- **架构**：所有特效逻辑封装在 `ShipView` 中，`ShipBoost`/`ShipDash` 不包含任何渲染代码
- **可配置性**：所有视觉参数通过 `ShipJuiceSettingsSO` 暴露，支持运行时调整
- **兼容性**：与现有 `DashAfterImageSpawner`（动态残影池）并存，`Dodge_Sprite` 是静态残影 GO，两者互补
