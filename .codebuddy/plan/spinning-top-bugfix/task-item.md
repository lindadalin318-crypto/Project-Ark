# 实施计划 — SpinningTop Bug 修复

- [ ] 1. 修复事件系统崩溃（需求 1 & 8）
   - 在 `resolveEventChoice` 中将 `choice.resolve(G)` 改为 `choice.effect(G)`
   - 在 `showEventScreen` 中将 `ev.name` 改为 `ev.title`
   - _需求：1.2、8.2_

- [ ] 2. 修复 generateIntel 字段路径错误（需求 2）
   - 将 `generateIntel` 中所有 `enemy.decayMult`、`enemy.arDmg`、`enemy.baseRpm` 等顶层字段访问改为 `enemy.stats.decayMult`、`enemy.stats.arDmg`、`enemy.stats.baseRpm`
   - 验证精英/Boss 标识逻辑在修复后能正确触发
   - _需求：2.1、2.2、2.3_

- [ ] 3. 修复 getRewardPart 稀有度过滤（需求 3）
   - 将 `rarityPool` 中的 `'uncommon'` 替换为 PARTS_DB 中实际存在的稀有度值（`'common'`/`'rare'`/`'epic'`/`'legendary'`）
   - 确保 Boss 奖励池非空，回退逻辑仍保留
   - _需求：3.1、3.2、3.3_

- [ ] 4. 修复元素效果系统（需求 4 & 5）
   - 在 `applyElementEffect` 中修正 `arId` 赋值逻辑：从 `G.loadout.ar` 对象中读取 `id` 字段后再查 `PARTS_DB`
   - 将粒子渲染 patch 中的 `document.getElementById('battle-canvas')` 替换为全局变量 `_arenaCanvas` / `_arenaCtx`
   - _需求：4.1、4.2、5.1、5.2_

- [ ] 5. 修复 Boss 阶段公告挂载目标（需求 6）
   - 在 `showBattleAnnouncement` 中将挂载目标从 `#arena-wrap` / `#battle-screen` 改为 `#battle-canvas-wrap`
   - 验证公告在 2.2 秒后能正常淡出并移除
   - _需求：6.1、6.2_

- [ ] 6. 删除重复的 Phase 7 代码块（需求 7）
   - 定位文件末尾约 4127 行处的重复 Phase 7 代码块并删除
   - 确保 `updateStatusBar` 只被 patch 一次，无双重包装
   - _需求：7.1、7.2_

- [ ] 7. 端到端流程验证（需求 9）
   - 在浏览器中打开 `spinning-top.html`，按以下路径逐一验证：
     - 标题 → NEW GAME → 底座选择 → 地图
     - 地图 → 战斗节点 → 准备 → 发射 → 战斗 → 结算 → 返回地图
     - 地图 → 事件节点 → 事件界面 → 选择 → 返回地图（标题正确显示）
     - 地图 → 商店节点 → 商店 → 返回地图
     - Floor 4 Boss 击败 → Run End 通关界面
     - HP 归零 → Game Over → 返回标题
   - _需求：9.1、9.2、9.3、9.4、9.5、9.6_
