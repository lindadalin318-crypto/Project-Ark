# SpinningTop Roguelite — 实现完整性补齐需求文档

## 引言

本文档基于对 `SpinningTopGDD.md`（v0.1）与当前 `spinning-top.html`（3755行，Phase 1-7 已完成）的系统性对比分析，梳理出**已实现**、**部分实现**和**完全缺失**的功能，并将缺口转化为可执行的补齐需求。

### 当前实现状态总览

| 模块 | 状态 | 说明 |
|------|------|------|
| 基础 UI 框架 / 屏幕切换 | ✅ 完整 | 9 个屏幕，状态机正常 |
| 零件数据库（PARTS_DB） | ✅ 完整 | 15+ 种零件，4 槽位 |
| 敌人数据库（ENEMIES_DB） | ✅ 完整 | 9 基础 + 3 精英 |
| 随机事件（EVENTS_DB） | ✅ 完整 | 5 种事件 |
| 爬塔地图生成 | ✅ 完整 | 4 层节点地图，Canvas 渲染 |
| 准备回合 UI | ✅ 完整 | 情报/装配/库存/商店 |
| 商店系统 | ✅ 完整 | 购买/刷新/HP 恢复 |
| 竞技场物理引擎 | ✅ 完整 | 圆形碗壁、摩擦、碰撞 |
| 发射系统 | ⚠️ 部分 | 角度盘已实现，但与 DOM 脱节；力道条逻辑在 JS 中但 DOM 使用旧结构 |
| RPM 战斗机制 | ⚠️ 部分 | 基础 RPM 衰减/碰撞伤害已实现，但 RPM 阶段效果（危险/濒死弹飞加成）未接入 |
| 玩家干预系统 | ⚠️ 部分 | Thrust/Brake/Activate 逻辑存在，但冷却 tick 注入有 bug（函数重定义冲突） |
| 敌人 AI | ⚠️ 部分 | 3 种 archetype 基础 AI 已实现，但 archetype 字段未在 ENEMIES_DB 中正确赋值 |
| 底座选择（开局） | ⚠️ 部分 | `showBaseSelection` 引用了错误的 part ID（`sharp_tip` 而非 `bb_sharp`） |
| 胜负结算 | ⚠️ 部分 | 基础胜负判定已实现，但场外胜/爆裂胜奖励加成未实现 |
| 发射屏幕 DOM | ❌ 缺失 | `screen-launch` 的 DOM 结构与 Phase 5 JS 逻辑不匹配（ID 不一致） |
| RPM 阶段视觉效果 | ❌ 缺失 | 陀螺晃动随 RPM 下降加剧的视觉表现未完整实现 |
| Boss 三阶段机制 | ❌ 缺失 | GDD 定义了 Boss 三阶段行为切换，当前 Boss 与普通敌人无区别 |
| 战斗时间上限 | ❌ 缺失 | GDD 规定 3 分钟超时判平局，当前无超时机制 |
| 胜利类型判定 | ❌ 缺失 | 场外胜（Ring Out）和爆裂胜（Burst）的判定逻辑缺失 |
| 元素效果系统 | ❌ 缺失 | 元素 AR（火/冰/电/毒/磁）的碰撞后持续效果未实现 |
| 旋转方向克制 | ❌ 缺失 | 左旋/右旋碰撞时的差异化伤害计算未实现 |
| 配重盘物理差异 | ❌ 缺失 | 重型/轻型盘的质量差异对碰撞弹飞距离的影响未实现 |

---

## 需求

### 需求 1：修复发射屏幕 DOM 与 JS 逻辑的对接

**用户故事：** 作为玩家，我希望点击"Fight!"后能进入正常的发射界面（角度盘 + 力道条），以便完成发射操作进入战斗。

#### 验收标准

1. WHEN 玩家点击 `btn-start-battle` THEN 系统 SHALL 显示 `screen-LAUNCH` 屏幕，并启动角度盘旋转动画
2. WHEN `screen-LAUNCH` 显示时 THEN 系统 SHALL 正确初始化 `launch-canvas`（320×320）并开始 `launchAnimLoop`
3. WHEN 玩家点击角度盘 THEN 系统 SHALL 锁定当前角度并切换到力道条步骤
4. WHEN 玩家点击力道条 THEN 系统 SHALL 锁定力道值并在 600ms 后调用 `executeLaunch()`
5. IF `screen-LAUNCH` 的 DOM ID 与 `showScreen('LAUNCH')` 调用不匹配 THEN 系统 SHALL 正确映射（`screen-launch` → `LAUNCH`）

---

### 需求 2：修复底座选择的 Part ID 引用错误

**用户故事：** 作为玩家，我希望游戏开始时能正确显示三种底座选项（尖锐/平面/橡胶），以便做出有意义的开局策略选择。

#### 验收标准

1. WHEN `showBaseSelection()` 被调用 THEN 系统 SHALL 正确引用 `bb_sharp`、`bb_flat`、`bb_rubber`（而非 `sharp_tip`、`flat_tip`、`rubber_tip`）
2. WHEN 玩家选择底座 THEN 系统 SHALL 将对应 BB 零件装入 `G.loadout.bb`，并生成地图跳转到 MAP 屏幕
3. WHEN 底座选择 Modal 显示时 THEN 系统 SHALL 展示每种底座的名称、描述和衰减倍率

---

### 需求 3：修复敌人 AI archetype 字段赋值

**用户故事：** 作为玩家，我希望不同类型的敌人表现出不同的行为模式（冲锋/绕圈/陷阱），以便战斗有策略深度。

#### 验收标准

1. WHEN `ENEMIES_DB` 中的攻击型敌人（rusher/cyclone/buster）被使用 THEN 系统 SHALL 其 `archetype` 字段为 `'Aggressor'`
2. WHEN `ENEMIES_DB` 中的防御型敌人（fortress/mirror_shield/magnet_core）被使用 THEN 系统 SHALL 其 `archetype` 字段为 `'Survivor'`
3. WHEN `ENEMIES_DB` 中的持久型敌人（endurer/miasma/leech）被使用 THEN 系统 SHALL 其 `archetype` 字段为 `'Trapper'`
4. WHEN `applyEnemyAI()` 读取 `archetype` 时 THEN 系统 SHALL 正确匹配到对应的 AI 行为分支

---

### 需求 4：实现 RPM 阶段效果（危险/濒死弹飞加成）

**用户故事：** 作为玩家，我希望低 RPM 的陀螺在被撞时更容易飞出场外，以便战斗有紧张的节奏感。

#### 验收标准

1. WHEN 陀螺 RPM 在 25%-50% 时 THEN 系统 SHALL 碰撞后弹飞距离乘以 1.2 倍
2. WHEN 陀螺 RPM 在 0%-25% 时 THEN 系统 SHALL 碰撞后弹飞距离乘以 1.5 倍，且爆裂概率 +30%
3. WHEN RPM 低于 50% 时 THEN 系统 SHALL 陀螺的 `wobble` 振幅随 RPM 下降线性增大（已有基础，需校准数值）
4. WHEN RPM 低于 25% 时 THEN 系统 SHALL 陀螺旋转音效（视觉上用颜色变化模拟）变为警告色（红色）

---

### 需求 5：实现战斗时间上限（3 分钟超时机制）

**用户故事：** 作为玩家，我希望战斗有时间上限，以便持久型 Build 不会导致无限拖延。

#### 验收标准

1. WHEN 战斗开始时 THEN 系统 SHALL 启动 180 秒倒计时，并在战斗 HUD 中显示剩余时间
2. WHEN 倒计时归零时 THEN 系统 SHALL 判定双方平局，各损失 1 HP
3. WHEN 倒计时剩余 30 秒时 THEN 系统 SHALL 倒计时显示变为红色警告色
4. WHEN 平局结算时 THEN 系统 SHALL 显示"TIME OUT — DRAW"结算 Modal，并返回地图

---

### 需求 6：实现场外胜（Ring Out）和爆裂胜（Burst）判定

**用户故事：** 作为玩家，我希望通过将敌人撞出场外或撞散架来获得额外奖励，以便进攻型 Build 有更高的收益上限。

#### 验收标准

1. WHEN 敌方陀螺被撞出竞技场边界（超出 `ARENA.radius`）时 THEN 系统 SHALL 判定场外胜，金币奖励 +20%
2. WHEN 敌方陀螺 RPM 在濒死状态（<25%）被碰撞时 THEN 系统 SHALL 有 30% 概率触发爆裂胜，金币奖励 +50% 且额外给予一个零件奖励
3. WHEN 场外胜/爆裂胜发生时 THEN 系统 SHALL 在竞技场 Canvas 上显示对应的特效文字（"RING OUT!" / "BURST!"）
4. WHEN 结算 Modal 显示时 THEN 系统 SHALL 标注胜利类型（存活胜/场外胜/爆裂胜）

---

### 需求 7：实现 Boss 三阶段机制

**用户故事：** 作为玩家，我希望 Boss 战斗有三个阶段的行为切换，以便 Boss 战有独特的挑战感和叙事感。

#### 验收标准

1. WHEN Boss 的 RPM 在 60%-100% 时 THEN 系统 SHALL Boss 使用 `Aggressor` AI（高速冲撞）
2. WHEN Boss 的 RPM 降至 30%-60% 时 THEN 系统 SHALL Boss 切换为 `Survivor` AI（绕圈防御），并在 Canvas 上显示阶段切换提示
3. WHEN Boss 的 RPM 降至 0%-30% 时 THEN 系统 SHALL Boss 切换为 `Trapper` AI（边缘弹射），并在 Canvas 上显示阶段切换提示
4. WHEN Boss 节点被进入时 THEN 系统 SHALL Boss 的初始 RPM 比普通敌人高 50%（体现 Boss 强度）
5. WHEN Boss 被击败时 THEN 系统 SHALL 显示专属的 Boss 击败动画和结算界面

---

### 需求 8：实现元素攻击环碰撞后持续效果

**用户故事：** 作为玩家，我希望装备元素攻击环后，碰撞能产生持续的元素效果，以便 Build 有更丰富的策略维度。

#### 验收标准

1. WHEN 装备火焰环（`ar_flame`）的陀螺发生碰撞 THEN 系统 SHALL 在碰撞点创建燃烧区域，对进入区域的敌方陀螺每秒造成 RPM 伤害
2. WHEN 装备冰霜环（`ar_frost`）的陀螺发生碰撞 THEN 系统 SHALL 使敌方陀螺移速降低 30%，持续 3 秒
3. WHEN 装备毒素环（`ar_venom`）的陀螺发生碰撞 THEN 系统 SHALL 给敌方陀螺施加毒素层，每层每秒造成额外 RPM 衰减
4. WHEN 元素效果激活时 THEN 系统 SHALL 在 Canvas 上显示对应颜色的粒子效果（火=橙红，冰=青蓝，毒=绿紫）
5. IF 玩家装备了共振核心（`sg_resonance`）且 AR 有元素效果 THEN 系统 SHALL 元素效果持续时间延长 50%

---

### 需求 9：实现旋转方向克制系统

**用户故事：** 作为玩家，我希望选择与敌人相反的旋转方向时能获得碰撞优势，以便情报侦察有实际的策略价值。

#### 验收标准

1. WHEN 玩家陀螺（左旋）与敌方陀螺（右旋）发生碰撞 THEN 系统 SHALL 碰撞伤害乘以 1.3 倍（反向碰撞剧烈弹飞）
2. WHEN 玩家陀螺与敌方陀螺旋转方向相同时发生碰撞 THEN 系统 SHALL 碰撞伤害乘以 0.8 倍（同向摩擦减速）
3. WHEN 情报侦察显示敌方旋转方向时 THEN 系统 SHALL 在准备回合的情报面板中高亮显示旋转方向信息
4. WHEN 玩家在准备回合选择 SG 零件时 THEN 系统 SHALL 显示当前选择的旋转方向（L/R）

---

### 需求 10：修复玩家干预系统的函数重定义冲突

**用户故事：** 作为玩家，我希望 Q/W/E 干预按钮在战斗中能正常工作，以便在关键时刻做出干预决策。

#### 验收标准

1. WHEN 战斗开始时 THEN 系统 SHALL `initIntervention()` 被正确调用，三个按钮显示正确的充能数量
2. WHEN 玩家按 Q 键或点击 Thrust 按钮 THEN 系统 SHALL 玩家陀螺向敌方方向施加冲力，充能数 -1
3. WHEN 玩家按 W 键或点击 Brake 按钮 THEN 系统 SHALL 玩家陀螺速度降至 20%，恢复 150 RPM，充能数 -1
4. WHEN 干预冷却中 THEN 系统 SHALL 按钮显示剩余冷却秒数，且不可点击
5. WHEN `battleLoop` 每帧执行时 THEN 系统 SHALL `tickInterventionCooldowns()` 被正确调用（无函数重定义冲突）

---

### 需求 11：完善战斗 HUD（计时器 + 胜利类型显示）

**用户故事：** 作为玩家，我希望战斗界面能清晰显示剩余时间和当前战况，以便做出及时的干预决策。

#### 验收标准

1. WHEN 战斗进行中 THEN 系统 SHALL 在战斗 HUD 中显示倒计时（MM:SS 格式）
2. WHEN 倒计时剩余 ≤30 秒 THEN 系统 SHALL 计时器颜色变为红色并闪烁
3. WHEN 战斗结束时 THEN 系统 SHALL 在 Canvas 上叠加显示胜利类型文字（VICTORY / RING OUT / BURST / DEFEAT / TIME OUT）
4. WHEN 战斗 HUD 显示时 THEN 系统 SHALL 同时显示玩家和敌方的 RPM 数值及百分比条

