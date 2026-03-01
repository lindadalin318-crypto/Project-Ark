# 需求文档：spinning-top.html 完整性修复

## 引言

通过对 `SideProject/spinning-top.html` 的全面代码审查，发现了 9 处 Bug，涵盖游戏流程阻断、UI 文字残留英文两大类。其中最严重的是 Phase 8 中 `showBaseSelection` 的 patch 逻辑错误，导致点击"新游戏"后弹窗被立即覆盖，表现为"页面无反应"。本文档定义所有需要修复的问题及验收标准。

---

## 需求

### 需求 1：修复新游戏流程阻断（最高优先级）

**用户故事：** 作为玩家，我希望点击"新游戏"后能正常看到底座选择弹窗（或教程），以便开始游戏。

#### 验收标准

1. WHEN 玩家点击"新游戏"按钮 THEN 系统 SHALL 显示底座选择弹窗（非首次）或教程弹窗（首次）。
2. WHEN 教程弹窗显示时，玩家点击"下一步 →" THEN 系统 SHALL 仅切换到下一步教程，不得同时弹出底座选择弹窗。
3. WHEN 教程最后一步玩家点击"开始游戏！"或任意步骤点击"跳过" THEN 系统 SHALL 关闭教程并显示底座选择弹窗。
4. IF `localStorage` 中已有 `st_tutorial_done` THEN 系统 SHALL 直接显示底座选择弹窗，跳过教程。

**根本原因：** Phase 8 中 `showBaseSelection` 的 patch 通过 monkey-patch `hideModal` 来实现"教程结束后显示底座选择"，但 `showModal` 的每个按钮 `onclick` 都会先调用 `hideModal()`，导致教程"下一步"按钮也触发了底座选择弹窗，两个弹窗互相覆盖。

**修复方案：** 移除对 `hideModal` 的 monkey-patch，改为在教程最后一步的"开始游戏！"和"跳过"按钮的 `onClick` 回调中直接调用 `_origShowBaseSelection()`。

---

### 需求 2：汉化干预技能 HUD 按钮文字

**用户故事：** 作为玩家，我希望战斗界面的干预技能按钮显示中文，以便理解操作。

#### 验收标准

1. WHEN 战斗开始后 `updateInterventionHUD` 被调用 THEN 系统 SHALL 将冲刺按钮文字显示为 `⚡ 冲刺 (N)` 格式（N 为剩余次数）。
2. WHEN 冷却中 THEN 系统 SHALL 在按钮文字后追加 `Xs` 冷却时间（中文格式）。
3. WHEN 制动按钮更新 THEN 系统 SHALL 显示 `🛑 制动 (N)` 格式。
4. WHEN 激活按钮更新 THEN 系统 SHALL 显示 `✨ 激活` 格式。

---

### 需求 3：汉化商店 HP 恢复物品

**用户故事：** 作为玩家，我希望商店中的 HP 恢复物品显示中文名称和描述。

#### 验收标准

1. WHEN 商店渲染时 THEN 系统 SHALL 将 HP Restore 物品的 `name` 显示为 `HP 恢复`。
2. WHEN 商店渲染时 THEN 系统 SHALL 将 HP Restore 物品的 `desc` 显示为 `恢复 1 点生命。上限为 3。`。

---

### 需求 4：汉化 Boss 阶段公告文字

**用户故事：** 作为玩家，我希望 Boss 战的阶段公告显示中文，以便理解战斗进程。

#### 验收标准

1. WHEN Boss 进入第 2 阶段 THEN 系统 SHALL 显示 `⚡ 混沌冠军 — 第二阶段` 公告。
2. WHEN Boss 进入第 3 阶段 THEN 系统 SHALL 显示 `⚡ 混沌冠军 — 第三阶段` 公告。

---

### 需求 5：汉化 Canvas 上的 TIME OUT 和 DRAW 文字

**用户故事：** 作为玩家，我希望战斗超时时 Canvas 上显示的文字是中文。

#### 验收标准

1. WHEN 战斗超时 THEN 系统 SHALL 在 Canvas 上绘制 `时间到` 文字（替代 `TIME OUT`）。
2. WHEN 战斗超时 THEN 系统 SHALL 在 Canvas 上绘制 `平局 — 双方陀螺均存活` 文字（替代 `DRAW — Both tops still spinning`）。

---

### 需求 6：汉化零件激活效果和碰撞反馈文字

**用户故事：** 作为玩家，我希望激活零件特殊效果时的反馈文字是中文。

#### 验收标准

1. WHEN 激活三翼攻击环 THEN 系统 SHALL 显示 `下次碰撞造成 +400 RPM 伤害！`。
2. WHEN 激活橡胶底座 THEN 系统 SHALL 显示 `下次碰撞将被吸收！`。
3. WHEN 激活陀螺旋转齿轮 THEN 系统 SHALL 显示 `+300 RPM 已恢复！`。
4. WHEN 激活重型重量盘 THEN 系统 SHALL 显示 `敌方被击飞至边界！`。
5. WHEN 爆裂伤害触发 THEN 系统 SHALL 显示 `💥 爆裂命中！-N RPM`（中文格式）。
6. WHEN 吸收命中触发 THEN 系统 SHALL 显示 `🛡️ 命中已吸收！+200 RPM`（中文格式）。

---

### 需求 7：汉化"继续游戏"无存档提示

**用户故事：** 作为玩家，我希望点击"继续游戏"时无存档的提示弹窗显示中文。

#### 验收标准

1. WHEN 玩家点击"继续游戏"且无存档时 THEN 系统 SHALL 显示标题为 `无存档` 的弹窗。
2. WHEN 弹窗显示时 THEN 系统 SHALL 显示内容 `没有进行中的游戏，请开始新游戏！`。

---

### 需求 8：修复 Settlement Screen 和 Run-End Screen 初始文字

**用户故事：** 作为玩家，我希望结算界面和跑局结束界面的初始占位文字也是中文。

#### 验收标准

1. WHEN 页面加载时 THEN `settlement-title` 的初始文字 SHALL 为空或中文（不显示英文 `VICTORY`）。
2. WHEN 页面加载时 THEN `runend-title` 的初始文字 SHALL 为空或中文（不显示英文 `VICTORY`）。
