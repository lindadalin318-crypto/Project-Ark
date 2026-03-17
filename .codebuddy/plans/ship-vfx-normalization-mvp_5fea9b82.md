---
name: ship-vfx-normalization-mvp
overview: 对现有 Ship / BoostTrail / VFX 相关代码、Prefab、资产与文档做首批渐进式规范化设计，先建立统一规范、资产映射和迁移边界，再分批实施低风险收口。
todos:
  - id: build-spec-and-registry
    content: 用 [subagent:code-explorer] [skill:spec-miner] 建立现役清单、资产映射和规范文档
    status: completed
  - id: align-live-docs
    content: 收口 `Ship_VFX_Player_Perception_Reference.md` 与 `BoostTrail_Shader_Implementation_Status.md`
    status: completed
    dependencies:
      - build-spec-and-registry
  - id: unify-tooling-authority
    content: 用 [skill:architecture-designer] [skill:unity-developer] 统一 Ship 与 BoostTrail 工具职责
    status: completed
    dependencies:
      - build-spec-and-registry
  - id: normalize-high-value-names
    content: 用 [skill:unity-developer] [mcp:unityMCP] 修正高收益歧义命名与节点口径
    status: completed
    dependencies:
      - build-spec-and-registry
      - unify-tooling-authority
  - id: validate-and-log
    content: 用 [mcp:unityMCP] 验证 prefab/scene 引用、编译结果并追加实现日志
    status: completed
    dependencies:
      - align-live-docs
      - unify-tooling-authority
      - normalize-high-value-names
---

## User Requirements

统一现有 Ship 与 BoostTrail 相关特效的命名、结构分层、文档格式和资产路径表达方式，先输出并按方案执行一套渐进式规范化方案，不做高风险的一次性全量改名或搬目录。

## Product Overview

本次工作面向现有飞船视觉链路，覆盖运行时特效、Prefab 结构、编辑器工具、参考文档和资产归属说明。首批以“规范收口”为主，建立唯一标准、唯一映射和唯一职责边界，保证后续维护与迁移可追踪。

## Core Features

- 建立一份现役特效标准，明确哪些命名、结构和路径属于当前有效口径
- 建立资产映射表，统一旧名、规范名、当前路径、引用位置和状态分类
- 统一工具职责边界，避免多个生成器或绑定器同时定义同一结构
- 收口现有参考文档与实现状态文档，区分现役规范、历史记录、上游参考
- 在不改变当前玩家看到的 Dash、Boost、尾焰、拖尾等视觉表现前提下，为后续重命名和迁移提供安全路径

视觉效果上，首批规范化不追求新增特效或修改读感，目标是保持当前游戏内观感不变，只让内部命名、结构、文档和路径表达变得统一、清晰、可维护。

## Tech Stack Selection

- 引擎：Unity 6000.3.7f1
- 渲染：URP 2D 17.3.0
- 语言：C#
- 现有模块根目录：
- `/Users/dada/Documents/GitHub/Project-Ark/Assets/Scripts/Ship`
- `/Users/dada/Documents/GitHub/Project-Ark/Assets/_Prefabs/Ship`
- `/Users/dada/Documents/GitHub/Project-Ark/Assets/_Prefabs/VFX`
- `/Users/dada/Documents/GitHub/Project-Ark/Assets/_Art/Ship/Glitch`
- `/Users/dada/Documents/GitHub/Project-Ark/Assets/_Art/VFX/BoostTrail`
- `/Users/dada/Documents/GitHub/Project-Ark/Docs/Reference`

## Implementation Approach

采用“先立规范、再收工具、后改命名、最后评估迁移”的渐进式方案。

高层做法是：

1. 冻结现役集合，先把当前有效链路写成规范与映射；
2. 把 Editor 工具职责收口成单一真相源；
3. 再处理高收益、低风险的歧义命名；
4. 最后再决定是否做物理 rename 或目录移动。

### Key Technical Decisions

- **MVP 不做全仓 rename 或 move**：当前存在 `Ship.prefab`、`BoostTrailRoot.prefab`、`SampleScene.unity`、nested prefab 与 scene-only 引用，直接大规模移动会放大 GUID 与 fileID 风险。
- **先文档和映射，后物理资产迁移**：`Movement_3`、`Boost_16`、`Primary_4` 一类编号资产先进入 registry，先统一“语义名”，后续再评估是否改文件名。
- **工具职责单一化**：以当前代码为基准，将 `ShipPrefabRebuilder`、`BoostTrailPrefabCreator`、`ShipBoostTrailSceneBinder`、`MaterialTextureLinker` 分别收敛为结构重建、VFX prefab 生成、场景绑定、材质回填的单一职责工具；`ShipBuilder` 仅保留受限职责或退为参考入口。
- **现役与参考分层**：`Docs/Reference/GalacticGlitch_BoostTrail_VFX_Plan.md` 与 `Assets/_Art/Ship/Glitch/Reference/` 明确视为参考来源，不再充当现役规范。

### Performance and Reliability

- MVP 以文档、工具和序列化口径收口为主，**不引入运行时额外开销**，不改变现有每帧逻辑复杂度。
- 当前主要风险不在性能，而在**Prefab/Scene 序列化断链**与**AssetDatabase 模糊搜索误绑**。
- 规范化后应减少 Editor 期重复搜索和误绑定；相关编辑时复杂度主要为编辑期 O(n) 资产扫描，可接受。
- 风险控制重点：
- 不在首批移动 `.png`、`.mat`、`.shader` 实体路径
- 优先验证 `Ship.prefab`、`BoostTrailRoot.prefab`、`SampleScene.unity`
- 改完工具后用 Unity Editor 与构建双验证

## Implementation Notes

- 保持 `/Users/dada/Documents/GitHub/Project-Ark/Assets/_Prefabs/Ship/Ship.prefab` 与 `/Users/dada/Documents/GitHub/Project-Ark/Assets/_Prefabs/VFX/BoostTrailRoot.prefab` 的当前视觉输出不变。
- `ShipBuilder.cs` 与 `ShipPrefabRebuilder.cs` 不能继续并行定义 Ship 结构；本批需明确唯一权威入口。
- `ShipBoostTrailSceneBinder.cs` 仅处理 scene-only 依赖，例如 Bloom Volume，避免 prefab 级与 scene 级职责混杂。
- `MaterialTextureLinker.cs` 继续只维护现役材质集合，避免 legacy 资源回流。
- 所有批次结束都需同步更新 `/Users/dada/Documents/GitHub/Project-Ark/Docs/ImplementationLog/ImplementationLog.md`。

## Architecture Design

### Target Structure

- **Runtime 层**
- `ShipView.cs`
- `BoostTrailView.cs`
- `ShipEngineVFX.cs`
- `DashAfterImageSpawner.cs`
- `ShipVisualJuice.cs`
- **Prefab 结构层**
- `Ship.prefab`
- `BoostTrailRoot.prefab`
- **Editor 工具层**
- `ShipPrefabRebuilder.cs`
- `BoostTrailPrefabCreator.cs`
- `ShipBoostTrailSceneBinder.cs`
- `MaterialTextureLinker.cs`
- **规范与映射层**
- Canonical Spec
- Asset Registry
- Migration Plan
- **参考输入层**
- `GalacticGlitch_*` 文档
- `Assets/_Art/Ship/Glitch/Reference/`
- `Tools/CopyGGTextures.ps1`

### Responsibility Boundaries

- `ShipPrefabRebuilder.cs`：Ship 结构与关键引用的唯一重建器
- `BoostTrailPrefabCreator.cs`：BoostTrailRoot 的唯一 prefab 生成器
- `ShipBoostTrailSceneBinder.cs`：场景级依赖绑定器
- `MaterialTextureLinker.cs`：材质与纹理回填器
- `ShipBuilder.cs`：迁移期受限入口，后续评估降级或退役

## Directory Structure

### Directory Structure Summary

本次方案以 `/Docs/Reference` 先建立标准层，以 `/Assets/Scripts/Ship/Editor` 收口工具职责，以 prefab 与 scene 做验证闭环；MVP 首批不物理移动底层美术资源。

```text
/Users/dada/Documents/GitHub/Project-Ark/
├── Docs/
│   ├── Reference/
│   │   ├── ShipVFX_CanonicalSpec.md                 # [NEW] Ship/BoostTrail/VFX 现役规范。定义命名规则、目录归属、职责边界、文档格式和状态分类，作为唯一规范入口。
│   │   ├── ShipVFX_AssetRegistry.md                 # [NEW] 资产映射表。记录 canonical 名、旧名、当前路径、资产类型、状态、引用位置、备注；MVP 不改资产文件名，先统一映射。
│   │   ├── ShipVFX_MigrationPlan.md                 # [NEW] 分批迁移计划。列出批次目标、风险、验证方法、延期项，指导后续 rename/move。
│   │   ├── Ship_VFX_Player_Perception_Reference.md # [MODIFY] 保留“玩家感知视角”定位，去掉承担规范职责的内容，并与新规范文档互链。
│   │   └── BoostTrail_Shader_Implementation_Status.md # [MODIFY] 保留主拖尾实现状态说明，并与 canonical spec、asset registry 对齐。
│   └── ImplementationLog/
│       └── ImplementationLog.md                     # [MODIFY] 记录每一批规范化的具体改动、目的和技术手段。
├── Assets/
│   ├── Scripts/
│   │   └── Ship/
│   │       ├── Editor/
│   │       │   ├── ShipPrefabRebuilder.cs          # [MODIFY] 收口为 Ship 结构唯一真相源，明确管理边界与生成结果。
│   │       │   ├── ShipBuilder.cs                  # [MODIFY] 降级、受限或转参考入口，避免与 Rebuilder 并行定义结构。
│   │       │   ├── BoostTrailPrefabCreator.cs      # [MODIFY] 明确只负责 BoostTrailRoot prefab 生成与命名口径。
│   │       │   ├── MaterialTextureLinker.cs        # [MODIFY] 只维护现役材质与纹理映射，禁止 legacy 回填。
│   │       │   └── ShipBoostTrailSceneBinder.cs    # [MODIFY] 只处理 scene-only 引用和验证，不再混入 prefab 逻辑。
│   │       └── Data/
│   │           └── ShipShipState.cs                # [MODIFY] 作为首批异常命名治理对象；MVP 先统一语义和引用口径，再评估物理重命名。
│   ├── _Prefabs/
│   │   ├── Ship/
│   │   │   └── Ship.prefab                         # [MODIFY] 仅在命名批次收口节点名与关键引用，不在 MVP 首批移动路径。
│   │   └── VFX/
│   │       └── BoostTrailRoot.prefab               # [MODIFY] 统一节点命名口径与生成来源，保持视觉结构不变。
│   └── Scenes/
│       └── SampleScene.unity                       # [MODIFY] 验证 scene-only 绑定与命名收口后的引用稳定性。
└── Tools/
    └── CopyGGTextures.ps1                          # [MODIFY] 限定为参考资源导入脚本，避免继续充当现役规范来源。
```

## Key Structures

### Asset Registry Recommended Fields

- `CanonicalName`
- `CurrentName`
- `AssetType`
- `CurrentPath`
- `Status`（Live / Legacy / Reference / Unknown）
- `Owner`
- `UsedByScript`
- `UsedByPrefab`
- `SceneOnly`
- `Notes`

### Naming Focus in MVP

- 先规范 **文档名、工具职责名、节点命名口径、语义名映射**
- 后置 **底层资源物理 rename / 目录迁移**

## Agent Extensions

### Skill

- **spec-miner**
- Purpose: 从现有脚本、Prefab、文档中提炼现役规格、历史口径和真实依赖关系。
- Expected outcome: 产出准确的现役清单、命名映射和规范边界，避免按印象治理。

- **architecture-designer**
- Purpose: 为 Ship 与 BoostTrail 的工具链、规范层和资产层设计单一真相源与职责边界。
- Expected outcome: 形成稳定的分批架构方案，避免 `ShipBuilder` 与 `ShipPrefabRebuilder` 等入口继续并行失真。

- **unity-developer**
- Purpose: 评估 Unity Prefab、Scene、材质、Editor 工具的改动风险，并约束迁移顺序。
- Expected outcome: 在不破坏当前视觉表现的前提下完成命名和结构收口。

### SubAgent

- **code-explorer**
- Purpose: 对 Ship、BoostTrail、VFX 范围内的文件、引用点和路径进行定向扫描。
- Expected outcome: 形成完整且可验证的修改文件清单与依赖图，支撑规范文档和迁移表。

### MCP

- **unityMCP**
- Purpose: 在执行阶段验证 prefab、scene、菜单工具和编译状态，检查 Unity 侧真实引用是否稳定。
- Expected outcome: 及时发现 fileID、scene-only 引用或 prefab 断链问题，确保规范化可落地。