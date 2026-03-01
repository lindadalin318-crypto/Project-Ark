# 实施计划：spinning-top.html 完整性修复

- [ ] 1. 修复新游戏流程阻断（Phase 8 `showBaseSelection` patch 逻辑）
   - 定位 Phase 8 中 `showBaseSelection` 的 monkey-patch 代码块
   - 移除对 `hideModal` 的 monkey-patch
   - 改为在 `showTutorial` 函数内，将"跳过"按钮和最后一步"开始游戏！"按钮的 `onClick` 回调中直接调用 `_origShowBaseSelection()`
   - 验证：非首次游戏点击"新游戏"直接显示底座选择弹窗；首次游戏教程"下一步"不触发底座选择弹窗
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 2. 汉化干预技能 HUD 按钮文字
   - 定位 `updateInterventionHUD` 函数
   - 将 `Thrust` 替换为 `冲刺`，`Brake` 替换为 `制动`，`Activate` 替换为 `激活`
   - 保持 emoji 前缀和 `(N)` 次数格式不变
   - _需求：2.1、2.2、2.3、2.4_

- [ ] 3. 汉化商店 HP 恢复物品及"继续游戏"无存档提示
   - 定位 `generateShopItems` 函数中的 HP Restore 物品对象
   - 将 `name: 'HP Restore'` 改为 `name: 'HP 恢复'`，`desc` 改为 `恢复 1 点生命。上限为 3。`
   - 定位 `btn-continue` 的 click 事件处理，将 `'No Save'` 改为 `'无存档'`，将 `'No active run found. Start a new game!'` 改为 `'没有进行中的游戏，请开始新游戏！'`
   - _需求：3.1、3.2、7.1、7.2_

- [ ] 4. 汉化 Boss 阶段公告和 Canvas 超时文字
   - 定位 `showBattleAnnouncement` 函数中的 Boss 阶段公告字符串
   - 将 `PHASE 2` 替换为 `第二阶段`，`PHASE 3` 替换为 `第三阶段`，`CHAOS CHAMPION` 替换为 `混沌冠军`
   - 定位 `endBattleTimeout` 函数中的 Canvas `fillText` 调用
   - 将 `'TIME OUT'` 替换为 `'时间到'`，将 `'DRAW — Both tops still spinning'` 替换为 `'平局 — 双方陀螺均存活'`
   - _需求：4.1、4.2、5.1、5.2_

- [ ] 5. 汉化零件激活效果和碰撞反馈文字
   - 定位 `patchPartActivations` 函数中各零件的激活返回字符串
   - 将 `'Next hit deals +400 RPM damage!'` 替换为 `'下次碰撞造成 +400 RPM 伤害！'`
   - 将 `'Next collision absorbed!'` 替换为 `'下次碰撞将被吸收！'`
   - 将 `'+300 RPM restored!'`（或类似）替换为 `'+300 RPM 已恢复！'`
   - 将 `'Enemy slammed to wall!'` 替换为 `'敌方被击飞至边界！'`
   - 定位 `checkTopCollision` patch 中的反馈文字，将 `'💥 BURST HIT!'` 替换为 `'💥 爆裂命中！'`，将 `'🛑 HIT ABSORBED!'` 替换为 `'🛡️ 命中已吸收！'`
   - _需求：6.1、6.2、6.3、6.4、6.5、6.6_

- [ ] 6. 修复 Settlement Screen 和 Run-End Screen 初始占位文字
   - 定位 HTML 中 `id="settlement-title"` 和 `id="runend-title"` 元素
   - 将初始文字内容从英文 `VICTORY` 改为空字符串或对应中文占位
   - _需求：8.1、8.2_
