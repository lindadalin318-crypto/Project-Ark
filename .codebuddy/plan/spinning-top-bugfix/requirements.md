# SpinningTop Roguelite — Bug 修复与功能完善需求文档

## 引言

`spinning-top.html` 是一个基于 GDD（SpinningTopGDD.md）实现的单文件 HTML 游戏原型。
经过代码审查，发现以下 **关键 Bug** 和 **功能缺失**，需要修复以确保完整游戏流程可以无错误跑通。

---

## 需求

### 需求 1：修复 Event 屏幕崩溃 Bug

**用户故事：** 作为玩家，我希望点击神秘事件节点后能正常进入事件界面并做出选择，以便游戏流程不中断。

#### 验收标准

1. WHEN 玩家点击 Event 节点 THEN 系统 SHALL 正确调用 `showEventScreen(ev)` 并显示事件内容。
2. WHEN 玩家点击事件选项 THEN 系统 SHALL 调用 `choice.effect(G)` 而非不存在的 `choice.resolve(G)`，并显示结果弹窗。
3. WHEN 事件结果弹窗关闭 THEN 系统 SHALL 标记节点已清除并返回地图。

> **根因**：`resolveEventChoice` 调用了 `choice.resolve(G)`，但 EVENTS_DB 中每个 choice 的回调字段名为 `effect`，不存在 `resolve`，导致运行时 TypeError。

---

### 需求 2：修复 generateIntel 使用错误字段

**用户故事：** 作为玩家，我希望准备回合能看到有意义的敌人情报，以便做出策略决策。

#### 验收标准

1. WHEN 进入准备回合 THEN 系统 SHALL 从 `enemy.intelPool` 随机抽取 2-4 条情报展示。
2. WHEN 敌人没有 `intelPool` 时 THEN 系统 SHALL 回退到基于 `enemy.stats` 字段生成情报。
3. IF 敌人是精英或 Boss THEN 系统 SHALL 在情报中标注 ELITE / BOSS 标识。

> **根因**：`generateIntel` 函数读取了 `enemy.decayMult`、`enemy.arDmg`、`enemy.baseRpm` 等字段，但 ENEMIES_DB 中这些数据存放在 `enemy.stats` 子对象内（如 `enemy.stats.decayMult`），导致所有条件判断失效，情报内容不准确。

---

### 需求 3：修复 getRewardPart 稀有度过滤失效

**用户故事：** 作为玩家，我希望战斗奖励的零件稀有度符合当前楼层，以便游戏进程有合理的成长曲线。

#### 验收标准

1. WHEN 普通战斗胜利 THEN 系统 SHALL 从 `common` 稀有度零件池中随机奖励。
2. WHEN Boss 战斗胜利 THEN 系统 SHALL 从 `rare` 或更高稀有度零件池中奖励额外零件。
3. WHEN 零件池为空 THEN 系统 SHALL 回退到全部零件池随机选取。

> **根因**：`getRewardPart` 中 `rarityPool` 包含 `'uncommon'`，但 PARTS_DB 中没有任何零件的 `rarity` 为 `'uncommon'`（只有 `common/rare/epic/legendary`），导致 Boss 奖励池始终为空，回退到全部零件。

---

### 需求 4：修复元素粒子特效渲染目标错误

**用户故事：** 作为玩家，我希望元素攻击环（火焰/冰霜/毒素等）在碰撞后能看到对应的粒子特效，以便获得视觉反馈。

#### 验收标准

1. WHEN 玩家装备元素攻击环并发生碰撞 THEN 系统 SHALL 在竞技场 Canvas 上渲染元素粒子。
2. WHEN 渲染粒子时 THEN 系统 SHALL 使用 `_arenaCanvas` 和 `_arenaCtx` 而非不存在的 `#battle-canvas`。

> **根因**：`renderArena` 的 patch 中调用 `document.getElementById('battle-canvas')`，但 HTML 中竞技场 Canvas 的 id 为 `arena-canvas`，且已有全局变量 `_arenaCanvas`/`_arenaCtx`，导致粒子特效从不渲染。

---

### 需求 5：修复 applyElementEffect 读取 loadout 方式错误

**用户故事：** 作为玩家，我希望装备的元素攻击环能在碰撞时正确触发元素效果。

#### 验收标准

1. WHEN 玩家装备元素攻击环并发生碰撞 THEN 系统 SHALL 正确读取 `G.loadout.ar` 对象并检查其 `element` 字段。
2. WHEN `G.loadout.ar` 为 null 时 THEN 系统 SHALL 跳过元素效果处理。

> **根因**：`applyElementEffect` 中 `const arId = G.loadout && G.loadout.ar` 将整个 part 对象赋给 `arId`，然后又用 `PARTS_DB[arId]` 查找（以对象为 key），导致 `arPart` 始终为 `undefined`，元素效果从不触发。

---

### 需求 6：修复 showBattleAnnouncement 挂载目标不存在

**用户故事：** 作为玩家，我希望 Boss 战斗阶段切换时能看到阶段公告，以便了解 Boss 当前状态。

#### 验收标准

1. WHEN Boss 进入第二或第三阶段 THEN 系统 SHALL 在竞技场上方显示阶段公告文字。
2. WHEN 公告显示 2.2 秒后 THEN 系统 SHALL 自动淡出并移除公告元素。

> **根因**：`showBattleAnnouncement` 尝试挂载到 `#arena-wrap` 或 `#battle-screen`，但 HTML 中对应元素 id 为 `battle-canvas-wrap`，导致公告无法显示。

---

### 需求 7：修复 Phase 7 重复代码导致 updateStatusBar 被 patch 两次

**用户故事：** 作为开发者，我希望代码中不存在重复的函数 patch，以便避免潜在的无限递归或逻辑错误。

#### 验收标准

1. WHEN 页面加载完成 THEN `updateStatusBar` 函数 SHALL 只被 patch 一次（在 Phase 7 末尾）。
2. WHEN `updateStatusBar` 被调用 THEN 系统 SHALL 正确更新 HP、金币、楼层和楼层指示器，不发生递归。

> **根因**：Phase 7 中 `updateStatusBar` 的 patch 代码出现了两次（约 4127 行处有重复的 Phase 7 代码块），导致 `updateStatusBar` 被双重包装。

---

### 需求 8：修复 showEventScreen 调用 ev.name 而非 ev.title

**用户故事：** 作为玩家，我希望事件界面能正确显示事件标题，以便了解当前事件内容。

#### 验收标准

1. WHEN 进入事件节点 THEN 系统 SHALL 在事件界面显示正确的事件标题文字。
2. WHEN 事件标题元素存在 THEN 系统 SHALL 使用 `ev.title` 字段填充，而非 `ev.name`（EVENTS_DB 中不存在 `name` 字段）。

> **根因**：`showEventScreen` 调用 `document.getElementById('event-title').textContent = ev.name`，但 EVENTS_DB 中事件对象的标题字段为 `title`，不存在 `name`，导致事件标题显示为 `undefined`。

---

### 需求 9：确保完整游戏流程端到端可跑通

**用户故事：** 作为玩家，我希望能从标题界面开始，完整经历选底座→地图→准备→发射→战斗→结算→地图循环，直到通关或失败，以便体验完整的游戏内容。

#### 验收标准

1. WHEN 点击 NEW GAME THEN 系统 SHALL 显示底座选择弹窗，选择后进入地图。
2. WHEN 点击地图上的战斗节点 THEN 系统 SHALL 依次经过准备→发射→战斗→结算流程。
3. WHEN 点击地图上的商店节点 THEN 系统 SHALL 进入商店，返回后标记节点已清除。
4. WHEN 点击地图上的事件节点 THEN 系统 SHALL 显示事件内容，选择后标记节点已清除并返回地图。
5. WHEN 击败 Floor 4 Boss THEN 系统 SHALL 显示结算界面，之后进入 Run End 通关界面。
6. WHEN HP 归零 THEN 系统 SHALL 显示 Game Over 弹窗，点击后返回标题界面。
