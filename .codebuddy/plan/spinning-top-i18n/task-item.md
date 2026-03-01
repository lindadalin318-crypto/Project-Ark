# 实施计划：spinning-top.html 全面汉化

- [ ] 1. 汉化 HTML 静态结构文字
   - 替换标题界面按钮文字：NEW GAME→新游戏、CONTINUE→继续游戏、TUTORIAL→教程
   - 替换地图界面标题：NODE MAP→节点地图
   - 替换准备界面三栏标题：INTEL→情报、LOADOUT→配置、INVENTORY→背包
   - 替换商店界面标题、刷新按钮、返回按钮
   - 替换发射界面标签文字
   - 替换战斗界面左侧面板标签（MY TOP / ENEMY）和三个干预按钮
   - 替换结算界面统计标签和跳过按钮
   - 替换事件界面默认标题
   - 替换跑局结束界面统计标签和重新开始按钮
   - 替换状态栏标签（HP / GOLD / FLOOR / NODE）
   - _需求：1.1–1.10_

- [ ] 2. 汉化零件数据库（PARTS_DB）
   - 翻译全部 22 个零件的 `name` 字段（AR 攻击环 9 个、WD 重量盘 5 个、SG 旋转齿轮 6 个、BB 刀刃底座 8 个）
   - 翻译全部零件的 `desc` 描述字段
   - 在显示稀有度时将 common/rare/epic/legendary 映射为普通/稀有/史诗/传说（保持代码内部 key 不变）
   - _需求：2.1–2.5_

- [ ] 3. 汉化敌人数据库（ENEMIES_DB）
   - 翻译全部 12 个敌人的 `name` 字段（普通 9 个、精英 3 个、Boss 1 个）
   - 翻译全部敌人的 `desc` 描述字段
   - 翻译全部敌人的 `intelPool` 情报条目数组
   - _需求：3.1–3.4_

- [ ] 4. 汉化事件数据库（EVENTS_DB）
   - 翻译全部 5 个事件的 `title`、`desc` 字段
   - 翻译每个事件所有 `choices` 的 `text`、`hint` 字段
   - 翻译每个选项 `effect` 回调中 `showModal` 调用的结果消息字符串
   - _需求：4.1–4.3_

- [ ] 5. 汉化 JavaScript 动态 UI 字符串
   - 翻译底座选择弹窗标题和说明文字（showBaseSelectModal）
   - 翻译战斗胜负弹窗文字（RING OUT / BURST WIN / VICTORY / DEFEAT）
   - 翻译干预技能反馈文字（⚡ Dash! / 🛑 Brake! / ✨ Activate!）
   - 翻译 Canvas 上的结果文字（TIME OUT / DRAW 及各胜负大字）
   - 翻译商店购买成功弹窗文字
   - 翻译背包出售按钮文字（Sell XXG → 出售 XXG）
   - 翻译"继续游戏"无存档提示弹窗
   - 翻译 Boss 阶段公告文字（showBattleAnnouncement）
   - 翻译 Game Over 弹窗文字
   - 翻译发射界面步骤文字（STEP 1 — SET ANGLE / STEP 2 — SET POWER / LAUNCHING...）
   - 翻译准备界面空背包提示（No parts in inventory）和空零件槽占位（— empty —）
   - 翻译零件槽标签（Attack Ring / Weight Disk / Spin Gear / Blade Base）
   - _需求：5.1–5.14_

- [ ] 6. 汉化教程系统（TUTORIAL_STEPS）
   - 翻译全部 5 个教程步骤的 `title` 和 `body` 字段
   - 翻译教程弹窗按钮文字：Next →→下一步 →、Skip→跳过、Start Playing!→开始游戏！
   - 翻译教程弹窗标题格式：Tutorial (X/5) → 教程 (X/5)
   - _需求：6.1–6.3_

- [ ] 7. 汉化地图节点标签与 generateIntel 函数输出
   - 翻译地图节点类型标签：battle→战斗、elite→精英、shop→商店、event→事件、boss→首领
   - 翻译 `generateIntel` 函数内所有硬编码字符串（Archetype、Base RPM、ELITE、BOSS 等）
   - 翻译情报原型标签：Aggressor→进攻型、Survivor→生存型、Trapper→陷阱型
   - _需求：7.1–7.2、8.1–8.3_
