# ShipVFX-PhaseA

<!-- markdownlint-disable MD024 -->

## 文档定位

本文件是 `Ship / VFX` 的 `Phase A` **已完成专项归档**。

它负责保留：

- 本轮治理目标
- 范围边界
- 完成标准
- 完成状态
- 工作拆分与执行结论
- 完成结论与后续切片入口
- 关联文档

它不替代以下真相源：

- `Implement_rules.md`
- `ShipVFX_CanonicalSpec.md`
- `ShipVFX_AssetRegistry.md`
- `ShipVFX_MigrationPlan.md`

一句话原则：

> 本文件回答“`Ship / VFX` 的 `Phase A` 做完了什么、为什么算完成、后续从哪里继续”。

---

## 当前目标

> **把 `Ship / VFX` 从“多入口可写、靠经验排查”的状态，收口到“权威清晰、工具分层、能自动抓错”的状态。**

本轮优先解决：

- prefab / scene / runtime / debug 多入口并行写入
- builder 越权写回
- runtime fallback 维持双轨主链
- debug 工具默认参与正式链路
- scene override 漂移与 silent no-op
- legacy / migration residue 长期滞留

---

## 范围

### In Scope

- `Assets/Scripts/Ship/Editor/`
- `Assets/Scripts/Ship/VFX/`
- `Assets/Scripts/Ship/Movement/ShipBoost.cs`
- `Assets/_Prefabs/Ship/Ship.prefab`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Assets/Scenes/SampleScene.unity` 中的 scene-only Bloom 链路
- `ShipVFX` 相关文档与工具职责口径

### Out of Scope

- 大规模视觉重做
- 大范围物理 rename / 资源迁移
- 脱离当前读感目标的整包重做
- 批量清理所有 dormant 资源
- 直接推进 backlog 中的视觉验收条目

---

## 完成标准

1. **唯一权威**：每类引用只有一个权威来源
2. **无双轨主链**：不再保留不必要的 runtime fallback
3. **debug 不接管主链**：debug 工具默认只观察，不持续覆盖正式运行态
4. **override 白名单化**：明确哪些 scene override 可保留，哪些必须清理
5. **无静默失败**：关键依赖缺失时会报错，或能被 validator / audit 抓到
6. **Clean Exit**：不以“legacy 继续挂着备用”的形态收尾

---

## 完成状态

- **状态**：已完成（`Gate G` 已通过，2026-03-17）
- **已完成内容**：A0 冻结治理边界、A1 工具权限审计、A2 菜单与职责收口、A3 Validator / Audit MVP、A4 删除双轨主链与冗余旧路径、A5 Scene Override 收口
- **完成判定**：治理目标已达到，`Ship / VFX` 可从 `Phase A` 切换到后续体验重构主线
- **后续入口**：如正式启动体验重构，应新建 `ShipVFX-PhaseB`，而不是继续把新工作堆回本文件

---

## 6. 工作拆分总览

| 步骤 | 名称 | 目标 | 产出 | 通过标准 |
| --- | --- | --- | --- | --- |
| A0 | 冻结治理边界 | 锁定本轮治理范围，避免任务扩散 | 范围说明与工作约束 | 团队对“这轮先治理、不先美化”无歧义 |
| A1 | 工具权限审计 | 把 authority matrix 变成代码现状审计表 | 审计表 + 归类结果 | 每个工具都能回答“现在写什么、以后该归谁” |
| A2 | 菜单与职责收口 | 让 authority / bootstrap / debug-only / legacy 的身份可见 | 注释、菜单、命名整改清单 | 不再存在多个工具看起来都能写同一件事 |
| A3 | Validator / Audit MVP | 把“无静默失败”落成自动检查 | 可执行审计入口 | 关键引用缺失时能被工具抓到 |
| A4 | 删除双轨主链与冗余旧路径 | 删掉 runtime fallback、隐式写回、debug takeover 与已失去职责的旧入口 | 清理清单 + 代码整改 | debug 关闭后主链仍独立成立，且冗余旧路径已删除 |
| A5 | Scene Override 收口 | 区分合法 scene-only 数据与脏漂移 override | override 白名单落地与清理结果 | 看到 override 时能立刻判断是否合法 |
| G | Gate G 验收 | 检查 5 条 Phase A 标准是否全部满足 | 验收结果 | 全部通过后才进入体验重构 |

---

## 7. 分步执行细则

## Step A0 — 冻结治理边界

### 目标

先锁定这一轮是 **authority 收口治理**，不是全面重做 `Ship/VFX`。

### 要做什么

- 明确当前只治理 `Ship/VFX`
- 明确 `MainTrail` 是**可修改的读感参考**，不是不可碰的冻结基线；若治理需要，可直接改其实现与结构
- 明确本轮不做大规模 rename / 资源迁移
- 明确这轮的优先级高于体验 backlog

### 产出

- 一份明确的治理边界说明
- 与 `MigrationPlan` 的职责分界说明

### 完成标准

- 团队对本轮目标没有歧义
- 后续讨论不会把“顺手做体验重构”混进治理主线

---

## Step A1 — 工具权限审计

### 目标

把 `Implement_rules.md` 里的 authority matrix 变成 **当前代码现状审计表**。

### 必审对象

- `ShipPrefabRebuilder`
- `BoostTrailPrefabCreator`
- `ShipBoostTrailSceneBinder`
- `MaterialTextureLinker`
- `ShipBuilder`
- `BoostTrailDebugManager`

### 对每个工具都要回答

1. 它现在写哪一层：`Prefab / Scene / Runtime / Debug`
2. 它有没有越权写入
3. 它有没有 fallback / 名字查找 / 模糊搜索 / 隐式写回
4. 它应该被归类为：
   - `Authority`
   - `Bootstrap`
   - `Debug Only`
   - `Migration Only`
   - `Legacy`
5. 它后续是否需要：
   - 保留权力
   - 降权
   - 改名
   - 增加 warning
   - 禁止继续使用

### 产出

- 一份实际代码审计表
- 一份针对每个工具的整改建议

### 完成标准

- 每个工具都能明确回答“现在谁在写、以后谁该写”
- authority / bootstrap / debug-only / legacy 的边界清楚

### A1 审计结果（2026-03-16）

| 工具 | 当前主要写入层 | 现状判断 | 关键问题 / 证据 | 审计归类 | A2 建议 |
| --- | --- | --- | --- | --- | --- |
| `ShipPrefabRebuilder` | `Prefab` | **基本符合 authority**。负责 `Ship.prefab` 结构、`BoostTrailRoot` nested prefab 集成、`ShipView/ShipEngineVFX/DashAfterImageSpawner` 关键序列化回填。 | 仍保留多处 fallback / migration 痕迹：`ShipVisual` / `VisualChild` 双名兼容；`ShipGlowMaterial` 缺失时自动调用 `ShipGlowMaterialCreator`；`WireJuiceSettings()` 用 `AssetDatabase.FindAssets`；`FindSpriteExactOrByName()` 做名字搜索兜底；`EnsureDodgeSpriteTexture()` 仍依赖外部盘符素材路径。 | `Authority` | **保留权力**；在 A2/A4 中收口为“精确路径优先、失败显式报错”的 authority；把 `VisualChild` 兼容、外部盘符导入、名字搜索兜底拆出或降级为 `Migration Only`。 |
| `BoostTrailPrefabCreator` | `Prefab` | **基本符合 authority**。负责 `BoostTrailRoot.prefab` 层级、子节点生成、`BoostTrailView` 与 `BoostTrailDebugManager` 默认配置。 | 仍有 asset fallback：`FindSpriteExactOrByName()` 用 `AssetDatabase.FindAssets`；`FindActivationHaloSprite()` 走主贴图 → fallback 贴图 → overlay 三层回退；当前只用 warning 提醒 scene-only Bloom 未绑定，说明链路仍存在“缺引用时只能靠人工记得补”的风险。 | `Authority` | **保留权力**；A2 中明确其只负责 standalone prefab，不负责 scene 绑定；A3 中让缺 `Bloom` 绑定能被 validator 抓到；A4 中收紧 sprite fallback。 |
| `ShipBoostTrailSceneBinder` | `Scene` | **符合 scene-only authority**。负责 `BoostTrailBloomVolume` 场景对象、profile 绑定、`BoostTrailView._boostBloomVolume` 场景接线。 | 使用 `GameObject.Find(BoostBloomVolumeName)` 依赖名字；靠字符串 `Type.GetType(...)` 找 `Volume`；`WireObjectReference()` 遇到属性不存在时直接 `return false`，缺少显式错误。菜单名仍带 `(GG)` 迁移痕迹。 | `Authority` | **保留权力**；A2 中去掉 `(GG)` 并强化注释；A3 中补“属性缺失/类型缺失”显式报错；A4/A5 中去掉名字依赖或至少把其限制为显式 binder 行为而不是隐含规范。 |
| `MaterialTextureLinker` | `Material Asset` | **符合当前材质链 authority**。负责现役材质的 shader 强制与贴图回填。 | `AssignTex()` 虽然先走精确路径，但失败后仍会 `AssetDatabase.FindAssets` 全项目搜索，属于被 `Implement_rules.md` 明确限制的模糊 fallback。 | `Authority` | **保留权力**；A2 中把职责写清为“现役材质链入口”；A4 中移除全项目搜索 fallback，改成显式失败 + 审计提示。 |
| `ShipBuilder` | `Scene Bootstrap` | **不是 authority**。它创建场景里的 `Ship` 根对象、组件、视觉子节点、SO/输入资源自动接线，还会回填 `WeavingStateTransition`。 | 与 `ShipPrefabRebuilder` 的 ship visual/引用装配职责高度重叠；大量启发式查找：`FindAnyObjectByType<ShipMotor>`、多个 `AssetDatabase.FindAssets`、按类型名扫描 `WeavingStateTransition`、自动寻找 `DashAfterImage` prefab。保留 `VisualChild` 兼容。 | `Bootstrap` | **明确降权**；A2 中把它从 authority 讨论中摘出去，明确为“场景 bootstrap 工具，不负责 prefab authority”；必要时增加 warning，提示不要把它当成修 prefab / 修 VFX 结构的官方入口。 |
| `BoostTrailDebugManager` | `Runtime Debug / Play Mode` | **当前实现已基本符合 preview-only 目标**。运行时只保留显式预览按钮入口，不再从生命周期自动补线、恢复或持续覆盖 live chain。 | 现行代码已无 `Reset/OnValidate/Awake/LateUpdate/OnDisable` 这类自动接管逻辑；`BoostTrailDebugManagerEditor` 也只允许 **live scene instance** 触发 runtime 预览。剩余风险主要是 debug 字段若被手动改动并保存，可能形成 scene override，因此仍需 validator 监控。 | `Debug Only` | **保留为 debug-only 组件**；A5 已确认它不再是主链 blocker，是否进一步退役可转入 Phase B 决策。 |

### 补充发现（不计入 A1 主表，但会影响 A2）

- `ShipBoostDebugMenu` 仍作为独立菜单存在，会在 Play Mode 下通过 `FindFirstObjectByType<ShipBoost>()` 和按名字查找 `BoostActivationHalo` 直接改运行时对象；它应被视为 **`Legacy / Debug Only` 候选**，需要纳入 A2 菜单收口。
- `BoostTrailDebugManagerEditor` 现已禁止从 prefab Inspector 代理到场景现役实例；它仍会暴露序列化 debug 字段，因此后续仍需依靠 validator 监控 scene override，但已不再属于“自动接管 live chain”的风险源。

### A1 结论摘要

- 当前 4 个主工具的 authority 边界已经**大体可辨认**：
  - `ShipPrefabRebuilder` → `Ship.prefab`
  - `BoostTrailPrefabCreator` → `BoostTrailRoot.prefab`
  - `ShipBoostTrailSceneBinder` → scene-only Bloom 接线
  - `MaterialTextureLinker` → 现役材质贴图链
- 当前真正的问题，不是“完全不知道谁负责”，而是：
  1. **authority 工具内部还夹带 fallback / migration residue**
  2. **`ShipBuilder` 仍像半个旧时代总装配器**
  3. **debug 工具链历史上过强，需要被收口为 preview-only 并接受 scene override 审计**
- 因此 A1 的产出已经足够支持进入 `A2：菜单与职责收口`。

---

## Step A2 — 菜单与职责收口

### 目标

让工具层自身就表达出“谁是官方入口，谁只是辅助工具”。

### 要做什么

- 收口菜单路径、工具说明、类注释
- 明确这些身份：
  - `ShipPrefabRebuilder` = `Ship.prefab` 权威入口
  - `BoostTrailPrefabCreator` = `BoostTrailRoot.prefab` 权威入口
  - `ShipBoostTrailSceneBinder` = scene-only 绑定入口
  - `MaterialTextureLinker` = 现役材质贴图入口
  - `ShipBuilder` = bootstrap，不是 prefab authority
  - `BoostTrailDebugManager` = preview-only
- 对旧工具补上 `Legacy / Debug Only / Migration Only` 标识

### 产出

- 工具职责分层清单
- 菜单与注释口径统一结果

### 完成标准

- 新人只看工具名和菜单，就不会误以为多个工具都能写同一件事
- authority 不再靠口头记忆维持

### A2 结论摘要（2026-03-16）

- authority 入口现已收口为四条主菜单：`Rebuild Ship Prefab`、`Rebuild BoostTrailRoot Prefab`、`Bind BoostTrail Scene Bloom References`、`Link Active BoostTrail Material Textures`。
- `ShipBuilder` 已在菜单与类注释层被明确降权为 `Bootstrap`；`ShipBoostDebugMenu`、`BoostTrailDebugManager`、`BoostTrailDebugManagerEditor` 已被明确标记为 `Legacy / Preview-only Debug`，**但这只是 Phase A 中间态的隔离措施，不是最终保留策略**。
- 外部提示已同步到 `CopyGGTextures.ps1` 与 `BoostTrailPhase2_TestChecklist.md`，因此 A2 可视为“菜单 / 注释 / 脚本提示”三层口径已统一。
- 下一步进入 `A3：Validator / Audit MVP`，随后在 `A4/A5` 中基于审计结果删除不再需要的 legacy / 冗余脚本与入口。

---

## Step A3 — Validator / Audit MVP

### 目标

把“无静默失败”从规则变成工具能力。

### MVP 至少检查

- `Ship.prefab` 核心引用是否缺失
- `BoostTrailRoot.prefab` 核心引用是否缺失
- scene-only Bloom 绑定是否缺失
- 非法 scene override 是否存在
- 是否残留应删除的 legacy 组件 / 旧路径 / 冗余脚本
- debug 是否处于不该接管的状态
- 是否存在被禁止的 fallback / 模糊查找痕迹

### 推荐产出形式

- `ShipVfxValidator`
- 或 `BoostTrailAudit`
- 支持最小可用的 `Audit` 输出

### 完成标准

- 关键依赖缺失时不再 silent no-op
- 能通过工具快速得到“缺了什么、错在哪一层”

### A3 结论摘要（2026-03-16）

- 已新增 `ShipVfxValidator`，菜单路径为 `ProjectArk/Ship/VFX/Audit/Run Ship VFX Audit`，定位为 **只读审计入口**，不自动修资产或场景。
- 第一版审计已覆盖 4 类高价值问题：`Ship.prefab` 核心结构与序列化引用、`BoostTrailRoot.prefab` 核心结构与默认 debug 状态、scene-only `BoostTrailBloomVolume` / `_boostBloomVolume` 绑定与 override 白名单、authority 违规痕迹的静态代码扫描。
- 输出已分层为 `Error / Warning / Info`，并统一写入 `Console`；同时提供 `LastResults` 作为后续 `A4/A5` 的程序化抓手。
- 为兼容 Unity MCP 与自动化流程，审计菜单已收口为**无阻塞 Console 输出**；首次验证暴露出的阻塞弹窗问题已在本步修正。

---

## Step A4 — 删除双轨主链、隐式写回与冗余旧路径

### 目标

真正满足“无双轨主链”，并把已经完成职责切分后的冗余旧路径从仓库中移除。

### 重点排查

- `OnValidate`
- `Awake`
- `OnEnable`
- Play Mode 启动时自动补线
- runtime 自动修复 prefab / scene 引用
- debug 持续 override 正式运行态
- 只剩历史兼容意义、但已不再承担正式职责的旧脚本 / 旧菜单 / debug 旁路入口

### 处理原则

- Runtime 只负责播放，不修资产
- Editor 只在显式执行时写入
- Scene-only 数据只由 scene binder 管理
- 对已经失去职责、只剩历史兼容意义的脚本 / 菜单 / 旁路入口，默认处理是**删除**而不是继续保留
- 若旧路径确实必须暂留，必须同时满足：有明确 owner、短期必要性成立、退役条件已写清

### 产出

- fallback / 隐式写回 / 冗余旧路径清理清单
- 实际代码整改结果
- 明确保留项与删除项的原因说明

### 完成标准

- 关闭 debug 后，正式主链仍独立成立
- prefab / scene / runtime 不再互相偷偷兜底
- 不再保留无职责、无 owner、无短期必要性的 legacy / migration residue 文件与菜单入口

---

## Step A5 — Scene Override 白名单落地

### 目标

把合法 scene-only 数据与脏 override 漂移彻底分开。

### 允许保留的 scene-only 数据

- `BoostTrailBloomVolume`
- `BoostTrailView._boostBloomVolume`
- 与该 Bloom volume 相关的场景级 profile / settings 使用关系

### 必须跟随 prefab / builder 的内容

- `Ship.prefab` 核心结构与核心序列化引用
- `BoostTrailRoot.prefab` 核心子节点与核心序列化引用
- 现役材质与贴图映射

### 要做什么

- 把白名单写到 validator / audit 的检查逻辑里
- 清理当前已经确认非法的 override
- 为后续排查建立“合法 / 非法”快速判断口径

### 完成标准

- 以后看到 override 时，可以立刻判断是“合法 scene-only”还是“脏漂移”
- 不再用 runtime fallback 去掩盖 override 问题

---

## 8. Gate G — Phase A 验收门槛

在进入体验重构前，必须逐条确认：

- **唯一权威**：是否每类引用都只有一个明确 authority
- **无双轨主链**：是否不再依赖不必要 fallback 才能跑通
- **debug 不接管主链**：是否 debug 默认关闭后系统仍成立
- **override 白名单化**：是否 scene override 已可判定合法性
- **无静默失败**：是否关键依赖缺失时会报错或被审计抓到

### Clean Exit 补充约束

即使以上 5 条表面通过，若仓库中仍存在以下任一项，也**不得**视为 Phase A 完成：

- 已失去职责、只剩历史兼容意义的 `legacy` / `migration` 脚本
- 仍暴露给团队、但不再应被使用的旧菜单入口
- 没有 owner、没有短期保留理由、也没有退役条件的 debug 旁路工具
- 与 authority plan 冲突、但仅因“以后也许有用”而继续保留的冗余实现

若其中任一项存在，继续留在 Phase A，不进入 Phase B。

### Gate G 复核结果（2026-03-17）

- **唯一权威：通过**。`Ship.prefab`、`BoostTrailRoot.prefab`、scene-only Bloom 绑定、材质贴图链都已分别收口到明确入口，`ShipBuilder` 也已明确降权为 bootstrap，而非 prefab authority。
- **无双轨主链：通过**。`ShipView` / `BoostTrailView` 现役链路已回到单轨；A4 删除了 debug takeover、runtime fallback 与 legacy debug 菜单，当前不再依赖额外兼容分支才能跑通主链。
- **debug 不接管主链：通过**。`BoostTrailDebugManager` 只剩显式预览 API，`BoostTrailDebugManagerEditor` 也只允许 live scene instance 驱动运行时预览；默认关闭时，正式链路可独立成立。
- **override 白名单化：通过**。`BoostTrailView` 当前只允许 `_boostBloomVolume` 作为合法 scene-only override，A5 已把合法修正、非法漂移与 validator 误归因三者区分清楚。
- **无静默失败：通过**。`ShipVfxValidator` 已覆盖 prefab、scene、debug 与静态代码痕迹四类审计；关键依赖缺失时会直接报错或被审计抓到。
- **Clean Exit：通过**。当前已不存在无 owner 的 legacy / debug-only 菜单入口；保留的 `ShipBuilder` 与 `BoostTrailDebugManager` 仍有明确职责、使用边界与后续 Phase B 决策位置，不属于“失去职责却继续裸露给团队”的残留物。
- **结论**：`Gate G` 本轮复核通过，`Ship / VFX` 的 `Phase A` 治理可以收口，后续进入 `Phase B` 体验重构主线。

---

## 9. 通过 Gate 后怎么走

通过 Gate G 之后，才进入体验重构主线。

### 进入 Phase B 前的切换动作

- 把 `ShipVFX_MigrationPlan.md` 中的体验 backlog 重新标记为“治理通过后执行”
- 从 backlog 中选择一个条目或一个强耦合小簇推进
- 每次推进都固定回答：
  1. 目标：玩家此刻应该感受到什么
  2. 问题：当前版本最不对劲的点是什么
  3. 决策：保留 / 重做 / 合并 / 删除 / 延后
  4. 技术参考：是否参考 `MainTrail` 当前想保留的读感与 clean 结构原则；若它本身需要重构，也应直接改
  5. 范围：本次只动哪一层
  6. 验收：进入 Play Mode 后什么读感算通过

---

## 10. 推荐推进顺序

### 当前推荐的下一步

**`Gate G` 已完成复核，下一步正式进入 `Phase B` 的体验重构主线。**

原因：

- `A4` 与 `A5` 已把 authority、override、debug、validator 四条治理主线全部收口：主链回到 canonical 单轨，合法 scene-only 绑定已明确，debug-only 组件不再接管 live chain。
- `Gate G` 五条标准与 `Clean Exit` 已完成本轮复核，当前没有继续阻塞 `Phase B` 的治理级问题。
- 因此接下来的推进重心，不应再停留在“证明治理已完成”，而应回到 `Ship / VFX` 的体验目标与感知质量本身。

### 当前建议工作流

1. 从 `ShipVFX_MigrationPlan.md` 的体验 backlog 中选择一个条目或一个强耦合小簇，作为 `Phase B` 第一个切片
2. 推进前固定回答：玩家此刻应该感受到什么、当前最不对劲的点是什么、这次决定保留 / 重做 / 合并 / 删除哪一层
3. 继续保留 `ShipVfxValidator` 作为回归护栏；后续体验改动若触碰 authority / scene-only 绑定，必须重新过一遍审计
4. `BoostTrailDebugManager` 是否进一步退役，不再作为 `Phase A` blocker，而是根据 `Phase B` 的实际调试价值再做决策

### A5 当前结论（场景实态 + 审计修正）

- `SampleScene.unity` 当前 diff 中，scene-local `VisualChild` 与旧 `SpriteRenderer` 已退出场景实例层；这条历史 alias 正在被持续清退。
- `DashAfterImageSpawner._shipSpriteRenderer -> Ship_Sprite_Solid`、`ShipVisualJuice._visualChild -> ShipVisual`、`BoostTrailView._boostBloomVolume -> BoostTrailBloomVolume` 应继续视为 **A5 合法修正 / 合法 scene-only 绑定**。
- 当前 `BoostTrailDebugManager` 与 `BoostTrailDebugManagerEditor` 都已满足 preview-only 口径：前者只保留显式预览 API，后者也已禁止从 prefab Inspector 代理到 live runtime instance。
- 旧审计报出的 `m_Name`、`m_IsActive`、`m_LocalPosition.*`、`_stats`、`_engineParticles`、`m_Mass`、`_shipSpriteRenderer`、`_visualChild` 等并不属于 `BoostTrailView` 或 `BoostTrailDebugManager` 自身 override，而是外层 prefab instance 修改被误归因后的结果。
- 因此，A5 的关键产出已经从“继续删场景字段”收口为：**确认合法 scene-only 绑定、修正 validator 归因、并证明 debug-only 组件不再接管主链**。

---

## 11. 执行状态板

| 步骤 | 状态 | 备注 |
| --- | --- | --- |
| A0 冻结治理边界 | `已完成（文档层）` | 已在规则与对话中明确治理优先 |
| A1 工具权限审计 | `已完成（2026-03-16）` | 审计表已写入 `Step A1`，authority / bootstrap / debug-only 边界已收口到文档 |
| A2 菜单与职责收口 | `已完成（2026-03-16）` | authority / bootstrap / debug-only 菜单分层、类注释与外部提示已统一到代码和文档 |
| A3 Validator / Audit MVP | `已完成（2026-03-16）` | `ShipVfxValidator` 已落地，覆盖 prefab / scene / debug / 静态代码痕迹四类审计 |
| A4 删除双轨主链与冗余旧路径 | `已完成（2026-03-17，代码侧收口）` | 已完成三批清理：debug takeover / ShipView fallback / legacy debug menu、authority 工具中的 `FindAssets / GameObject.Find / Type.GetType / 名字搜索` residue、以及 `VisualChild` legacy alias + `ShipBuilder` bootstrap residue；代码侧已回到 canonical 单轨 |
| A5 Scene Override 落地 | `已完成（2026-03-17，场景边界收口）` | 已完成 `SampleScene` diff 初勘 + Unity 实态核查，确认合法 scene-only 绑定与 preview-only debug 边界，并修正 `ShipVfxValidator` 对整个 prefab instance override 的误归因 |
| Gate G | `已通过（2026-03-17，复核完成）` | 五条标准 + `Clean Exit` 已按代码 / 场景 / 文档三侧复核通过，后续可进入 `Phase B` |

---

## 12. 完成结论

- `Ship / VFX` 的 `Phase A` 已完成从“多入口可写”到“authority 收口、debug 降权、validator 落地”的治理切换
- `Phase A` 的价值在于把体验重构之前的结构性风险先收口，而不是直接交付更华丽的视觉效果
- 后续若继续推进 `Ship / VFX`，应把工作重心转向 `Phase B` 的体验切片，而不是继续延长 `Phase A`

## 13. 遗留事项 / 后续可选项

- 继续从 `ShipVFX_MigrationPlan.md` 中挑选 `Phase B` 的第一批体验条目
- 评估是否需要把 `BoostTrailDebugManager` 在 `Phase B` 中进一步退役或缩减
- 若 `CanonicalSpec`、`AssetRegistry` 或 `Implement_rules.md` 再次变更，需要回看本归档是否应补结论说明

## 14. 关联文档

- `Implement_rules.md`
- `Docs/2_Design/Ship/ShipVFX_CanonicalSpec.md`
- `Docs/2_Design/Ship/ShipVFX_MigrationPlan.md`
- `Docs/Plan/ProjectPlan.md`
- `Docs/5_ImplementationLog/ImplementationLog.md`
