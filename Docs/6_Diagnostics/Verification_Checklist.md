# 关卡模块现役验收清单（Level Verification Checklist）

**版本：** v3.0  
**对齐依据：** `Docs/5_ImplementationLog/ImplementationLog_2026-04.md` 中最新的 Level 记录（截至 `2026-04-01 23:06`）  

**适用范围：** `Level` 模块当前现役工作流、场景落地、运行时联动与人工试玩验收

---

## 1. 本清单的定位

这份清单只负责回答一件事：

**按照当前现役工具链和代码事实，Level 模块现在应该怎么验。**

它不再沿用以下**已退役**的一次性工具口径：

- `ProjectArk > Scaffold Sheba Level`
- `ProjectArk > Level > Phase 6: Create World Clock Assets`
- `ProjectArk > Level > Phase 6: Build Scene Managers`
- `ProjectArk > Level > Phase 6: Setup All`
- `ShebaSliceBuilder`
- `Phase6AssetCreator`

上述入口都已在后续 `ImplementationLog` 中被收口、替代或删除，**不再作为现役验收项**。

---

## 2. 当前现役验收原则

### 2.1 统一流程

当前 Level 模块的统一验收流程是：

```text
结构搭建 / 场景落地
→ 语义标注
→ Overlay 检查
→ Validate
→ Quick Play
→ 完整 Play Mode 验收
```

### 2.2 核心边界

- **`Validate`**：验证结构、引用、挂点、配置缺失等编辑期问题
- **`Quick Play`**：结构 smoke test，只证明“能跑起来”
- **完整 Play Mode 验收**：验证真实游玩闭环和跨系统联动

### 2.3 重要提醒

- **不要**再把“旧菜单还在不在”当成验收结论
- **不要**只跑 `Quick Play` 就判定切片完成
- **不要**把当前命令行 `dotnet test` 当成唯一可信验收来源（`ImplementationLog` 已记录其输出链仍不稳定）
- `GateID` 重复等部分检查目前仍需要**人工自查**，不能完全依赖 `LevelValidator`

---

## 3. 现役 Quick Checklist

先做 **A-C（编辑期）**，再做 **D-F（运行期）**。

### A. 编译与现役入口检查

- [ ] **A1** Unity Console 无红色编译错误
- [ ] **A2** `dotnet build Project-Ark.slnx` 可通过，且无 C# error
- [ ] **A3** Unity 菜单中可见 `ProjectArk > Level > Authority > Level Architect`
- [ ] **A4** `Level Architect` 窗口包含 `Build / Quick Edit / Validate` 三个工作面

- [ ] **A5** 当前搭建入口明确：
  - 唯一现役入口：`Level Architect / Build Tab → 白盒搭建`
- [ ] **A6** 团队成员不再把 `Scaffold Sheba Level`、`Phase 6: Setup All` 等旧菜单当作现役入口

### B. 场景结构与 authority 检查

- [ ] **B1** 房间采用当前标准根节点：`Navigation / Elements / Encounters / Hazards / Decoration / Triggers / CameraConfiner`
- [ ] **B2** `Navigation` 下存在 `Doors`、`SpawnPoints` 子层级
- [ ] **B3** `Encounters` 下存在 `SpawnPoints`（如该房间需要战斗生成点）
- [ ] **B4** Door 的 `_targetRoom` 与 `_targetSpawnPoint` 已完整连好
- [ ] **B5** 房间使用 `RoomNodeType`，门使用 `ConnectionType` / `TransitionCeremony`，不再依赖 legacy 迁移字段
- [ ] **B6** 当前切片所需管理器已在场景中存在并接线完成（按需要勾选）：`RoomManager`、`DoorTransitionController`、`SaveBridge`、`MinimapManager`、`WorldClock`、`WorldPhaseManager`、`AmbienceController`、`WorldProgressManager`

### C. `LevelValidator` 编辑期校验

- [ ] **C1** 在 `Validate` Tab 跑过一次完整 `Validate All`
- [ ] **C2** 没有阻塞主链的 Error（Door 断链、关键 SO 缺失、交互主引用缺失等）
- [ ] **C3** `Lock` / `Checkpoint` / `BiomeTrigger` / `ScheduledBehaviour` 的关键字段缺失问题已处理
- [ ] **C4** 交互与触发类对象的 Trigger / `_playerLayer` 问题已处理
- [ ] **C5** `ActivationGroup` 校验通过：
  - 位于合法房间层级下
  - 成员不为空
  - `_members` 无空引用
  - 不跨房间错误引用成员
- [ ] **C6** 挂点契约通过：
  - `Lock` / `Checkpoint` / `DestroyableObject` → `Elements`
  - `OpenEncounterTrigger` → `Encounters`
  - `BiomeTrigger` / `HiddenAreaMask` / `ScheduledBehaviour` / `WorldEventTrigger` → `Triggers`
  - `EnvironmentHazard` → `Hazards`

### D. `Quick Play` 结构冒烟验证

- [ ] **D1** 至少跑过一次 `Quick Play`
- [ ] **D2** 入口房间可正常进入
- [ ] **D3** 至少一条主路线 Door 可正常切换房间
- [ ] **D4** 房间间往返不会因 Door 接线问题卡死或传错点位
- [ ] **D5** `Quick Play` 过程中未出现关键缺引用 / 管理器缺失 / 结构断链错误

### E. 完整 Play Mode 运行时验收

- [ ] **E1** 玩家能完整走出“进入 → 施压 → 证明 → 回报 / 回路 → 收束”主链
- [ ] **E2** 至少一场真实遭遇可完整触发、刷怪、清场、收尾
- [ ] **E3** 至少一个 Hazard 行为符合预期（持续伤害 / 接触伤害 / 定时开关）
- [ ] **E4** 至少一次门过渡或层间过渡可完整播放并回到正确目标房间
- [ ] **E5** 地图访问记录可更新，且不会在死亡 / 读档后丢失
- [ ] **E6** `Checkpoint / Save / Respawn` 形成完整闭环
- [ ] **E7** 若切片使用世界进度：对应门解锁、事件推进、永久变化链路正常
- [ ] **E8** 若切片使用世界阶段：时钟、阶段切换、定时门、动态物体、氛围变化、存档恢复至少各验证一次

### F. 当前必须人工自查的项目

- [ ] **F1** 同一 `Room` 内 `GateID` 无重复
- [ ] **F2** `RoomNodeType` 标注和房间实际职责一致，不是“能跑但语义错位”
- [ ] **F3** `ConnectionType` 标注与设计意图一致（主线 / 回返 / 挑战 / 能力门等）
- [ ] **F4** `TransitionCeremony` 与该门的仪式感预期一致，不存在沿用旧值的误判
- [ ] **F5** `ScheduledBehaviour` / `WorldEventTrigger` / `ActivationGroup` 的 target 指向真实需要被控制的对象，而不是历史残留引用

---

## 4. 详细验收步骤

## 4.1 第一步：编译与入口确认

1. 打开 Unity，等待编译完成
2. 确认 Console 没有红色错误
3. 在终端运行：`dotnet build Project-Ark.slnx`
4. 打开 Unity 菜单：`ProjectArk > Level > Authority > Level Architect`
5. 确认窗口中有 `Build / Quick Edit / Validate` 三个工作面

### 通过标准

- 代码可编译
- 现役入口存在
- 不再依赖旧的一次性菜单


---

## 4.2 第二步：确认当前切片走的是现役主链

1. 打开 `Level Architect` 的 `Build` Tab
2. 首次使用时点击 `Create Built-in Presets`
3. 使用 `Blockout / Connect / Select` 搭建和精修房间
4. 如需继续补语义或 starter objects，进入 `Quick Edit`

**通过标准：**
- 结构来源清楚
- 当前切片是通过 `Level Architect` 现役入口落地，而不是历史工具遗留

---

## 4.3 第三步：结构与语义检查

在 Scene 中逐项确认：

1. 房间根节点结构符合标准语法
2. `Door` 的 `_targetRoom` / `_targetSpawnPoint` 已连通
3. `RoomNodeType`、`ConnectionType`、`TransitionCeremony` 已标注
4. 本切片所需管理器已经存在并绑定
5. 如果用了互动件/触发器/危险物，它们位于正确根节点下

**通过标准：**
- 结构与语义都能读得懂
- 不是“只要能跑就算过”

---

## 4.4 第四步：跑 `Validate`

1. 打开 `Validate` Tab
2. 执行完整 `Validate All`
3. 优先处理所有 Error
4. 再评估 Warning：
   - 是否是会导致行为静默失效的高价值警告
   - 是否只是命名/组织可读性问题
5. 若可安全 Auto-Fix，则执行有限 Auto-Fix
6. 对 `ActivationGroup`、`ScheduledBehaviour`、`BiomeTrigger`、`Lock`、`Checkpoint` 做重点复核

**通过标准：**
- 没有阻塞主链的 Error
- 关键交互件与触发器已过当前 Validator 护栏

---

## 4.5 第五步：跑 `Quick Play`

1. 在 `Build` Tab 运行 `Quick Play`
2. 检查：
   - 能否正常进入 Play Mode
   - 入口房是否正常加载
   - 至少一条 Door 链路可切换
   - 管理器是否缺失
3. 记录任何“刚进场就爆错”或“切一次门就断链”的问题

**通过标准：**
- 结构能跑起来
- Door 传送主链不断

**注意：**
`Quick Play = 结构 smoke test`，**不等于完整验收完成**。

---

## 4.6 第六步：完整 Play Mode 验收

至少人工完整走一遍当前切片。

### A. 战斗 / Hazard

- 至少打一场真实遭遇
- 验证 Arena 锁门 → 刷怪 → 清场 → 解锁
- 验证至少一个 Hazard 行为

### B. 地图 / 存档 / 重生

- 进入新房间后访问记录更新
- 存档后退出再进，访问数据仍在
- 死亡 / 重生链路恢复正确

### C. 多楼层 / 过渡

- 至少一次门过渡或层间过渡成立
- 若有楼层切换，小地图 / 场景逻辑无错层

### D. 世界阶段 / 动态世界（如该切片使用）

- `WorldClock` 时间推进正常
- `WorldPhaseManager` 阶段切换正常
- `ScheduledBehaviour` 目标启停正确
- 定时门或阶段门启闭正确
- `WorldEventTrigger` 永久变化触发正确
- `AmbienceController` 的氛围变化真实可见/可听
- 存档恢复后，时间与阶段状态正确

### E. 触发器 / 导演层

- `BiomeTrigger`、`HiddenAreaMask`、`ActivationGroup`、其他导演层触发器不出现“能摆但不工作”的静默问题

**通过标准：**
- 当前切片已形成真实玩家闭环
- 不是只有编辑器结构正确，而是运行时体验链路也正确

---

## 5. 当前推荐的验收结论模板

当你完成验收后，建议按下面格式记录结果：

```text
切片名称：
搭建入口：路径 A / 路径 B
Validate：通过 / 未通过（列出主要问题）
Quick Play：通过 / 未通过
完整 Play Mode：通过 / 未通过
人工自查：GateID / NodeType / ConnectionType / Ceremony 已复核 / 待复核
结论：可继续内容迭代 / 需先修结构 / 需补运行时联动
```

---

## 6. 不再使用的旧验收口径

以下项目已**不建议继续作为当前验收标准**：

- 检查 `Scaffold Sheba Level` 菜单是否存在
- 检查 `Phase 6: Setup All` 菜单是否存在
- 检查 `Phase 6: Create World Clock Assets` / `Build Scene Managers` 是否存在
- 把 `ShebaSliceBuilder` 当成当前官方切片构建入口
- 把“工具存在”当成“场景已经正确落地”的证明

**原因：**
最新 `ImplementationLog` 已明确这些入口要么是**一次性工具**，要么已经被**`LevelArchitectWindow` / `LevelValidator` / 当前场景真相源**替代。

---

## 7. 最终判定标准

一个 `Level` 切片，只有同时满足下面 4 条，才建议视为本轮通过：

1. **结构成立**：标准层级与 Door 接线正确
2. **护栏通过**：`Validate` 无阻塞型 Error
3. **基础能跑**：`Quick Play` 可正常打通主链
4. **真实可玩**：完整 Play Mode 下战斗 / 过渡 / 存档 / 世界系统联动成立

如果只满足前 3 条，这个切片最多算：

**“工具和结构已到位，仍待场景试玩验收。”**
