# Implementation Log — 2026-06

## 2026-06-01 00:50 — Ship/VFX B1 Legacy/Dormant Inventory

- **修改文件：**
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-06.md`
- **内容：** 完成 Ship/VFX Phase B1 的首轮 legacy / dormant inventory 审计登记。将 `ShipVisualValidationView` 与 `CanaryShipVisualValidation.prefab` 登记为 `Reference / Preview-only` 验证支线，明确它们不得接管正式 `Ship.prefab` runtime owner 链；将 `vfx_ring_glow_uneven.png` 与 `vfx_magnetic_rings.png` 标记为 `Dormant / Delete Candidate`，记录文本 + GUID 审计只发现登记表、历史计划/日志与自身 meta 引用；同时明确旧 BoostTrail 材质虽然不再属于 Ares-only 正式主链，但仍被 `MaterialTextureLinker` 与回归测试维护，不能直接删除。
- **目的：** 在不改变 runtime、prefab、scene 或资源文件的前提下，建立下一批 dormant / legacy 清退的安全清单，避免把 preview-only 支线误判为 live，也避免误删仍被审计工具使用的旧材质链。
- **技术：** 使用文件名搜索、GUID 搜索、现役规范 / 资产登记 / 实现规则交叉验证，按 `Live / Reference / Legacy / Dormant / Delete Candidate` 状态归类；本轮仅更新文档，不执行资源删除。

## 2026-06-01 10:35 — Ship/VFX B2 Halo Dormant Texture Cleanup

- **删除文件：**
  - `Assets/_Art/VFX/BoostTrail/Textures/vfx_ring_glow_uneven.png`
  - `Assets/_Art/VFX/BoostTrail/Textures/vfx_ring_glow_uneven.png.meta`
  - `Assets/_Art/VFX/BoostTrail/Textures/vfx_magnetic_rings.png`
  - `Assets/_Art/VFX/BoostTrail/Textures/vfx_magnetic_rings.png.meta`
- **修改文件：**
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-06.md`
- **内容：** 完成 B2 删除前复核并清退两个 Halo dormant 纹理。复核确认 `vfx_ring_glow_uneven.png` 与 `vfx_magnetic_rings.png` 的 GUID 只存在于自身 `.meta`，文件名只存在于自身 `.meta` 与文档 / 历史日志，没有 `Assets` 活资产引用或 `ProjectSettings` 引用；随后通过 Unity 资产系统删除 `.png` 与 `.meta`，并将资产登记状态更新为 `Removed`。
- **目的：** 清理已取消 Halo 方案遗留的低风险 dormant 资源，减少 BoostTrail 资产目录噪声，同时保留 registry 历史记录，避免未来误判为现役资源缺失。
- **技术：** 使用 GUID + 文件名双重引用审计，排除 `Library` 噪声；通过 Unity `manage_asset delete` 删除资产而非手动编造或保留 `.meta`；本轮不修改 runtime、prefab、scene、材质链或 `MaterialTextureLinker` 契约。

## 2026-06-01 10:48 — Ship/VFX B3 MaterialTextureLinker Legacy Audit-only Downgrade

- **修改文件：**
  - `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidatorTests.cs`
  - `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Implement_rules.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-06.md`
- **内容：** 完成 B3 旧 BoostTrail 材质链工具契约降级。`MaterialTextureLinker` 从现役材质 Apply 权威降级为 retained legacy material reference 的只读审计工具；旧 `Authority/Audit Active...` 与 `Authority/Link Active...` 菜单通过 validate 方法禁用，新增 `ProjectArk/Ship/VFX/Legacy/Audit Legacy BoostTrail Material References` 入口；公开 `LinkAllMaterialTextures(false)` 改为 no-op 提示，防止继续修复旧材质。`ShipVfxValidator` 聚合 scope 从 `Authority Audit/MaterialTextureLinker` 调整为 `Legacy Audit/MaterialTextureLinker`，并新增回归测试覆盖旧 Apply 入口不再修复 `mat_flame_trail._BaseMap`。
- **目的：** 消除“旧 Boost 材质既登记为 `Legacy Reference` 又被现役 Apply 工具维护”的双重口径，让正式 Ares-only Boost 主链与 retained legacy 审计样本分离；为后续是否删除 6 个旧材质保留更清晰的安全边界。
- **技术：** 按 TDD 先新增 `MaterialTextureLinkerApply_DoesNotRepairLegacyReferenceMaterialBindings` 红测，确认旧 Apply 会修复 legacy 材质；随后做最小行为降级、scope 文案迁移、`BoostTrailPrefabCreator` 后续提示改线与文档同步。B3 不删除任何材质 / shader / texture 资产，不修改 runtime、prefab 或 scene。

## 2026-06-01 12:29 — Ship/VFX Plan Current Next Action Correction

- **修改文件：**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-06.md`
- **内容：** 修正 Ship/VFX 计划文档尾部 `Current Next Action` 的过期指针。`Task 29: Pool And Reset Verification` 在正文中已全部完成并通过 Batch 11 completion gate，因此不再作为当前阻塞下一步；尾部改为明确当前计划内没有阻塞 Ship/VFX MVP 的待办，后续仅在有新 Console 证据时继续非阻塞 warning cleanup。
- **目的：** 避免后续继续重复执行已完成的 pool/reset 验证，减少计划文档与真实状态之间的认知漂移。
- **技术：** 文档一致性修正，不修改 runtime、prefab、scene、测试或资源资产；通过读取 Task 29 原文状态与尾部 `Current Next Action` 对比确认矛盾后再更新。

## 2026-06-02 09:41 — Ship/VFX Batch 6 Fire Hit Gate Closeout

- **修改文件：**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-06.md`
- **内容：** 完成 Batch 6 Fire / Hit completion gate 的计划收口。将 `Fire is short and does not alter body identity` 与 `Muzzle flash aligns with WeaponMount` 从未勾选同步为完成，并在 Batch 6 状态下补录 Fire 短闪 / 复位、WeaponMount 接线、编译和 Console 验证证据。
- **目的：** 消除 Batch 6 正文已完成但 gate 未同步的状态漂移，让 Fire / Hit 视觉反馈可以作为已完成 Ship/VFX 主链的一部分进入后续归档判断。
- **技术：** 不修改 runtime、prefab、scene、测试或资源资产；复核 `ShipFireVisuals`、`ShipHitVisuals`、`ShipPrefabRebuilder` 与相关测试后执行文档一致性更新，并用 `dotnet build Project-Ark.slnx` 与 Unity Console error 检查确认没有新增编译 / 导入错误。

## 2026-06-02 11:45 — Ship/VFX Batch 2 Lean Validation Closeout

- **修改文件：**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-06.md`
- **内容：** 完成 Batch 2 Lean validation 的计划收口。将 Lean Left / Right 源图的 Unity import 状态从 pending 更新为已验证，并将 Task 9 三个预览验证步骤与 Batch 2 completion gate 同步为完成。
- **目的：** 消除 Lean 源图和动画片段已存在但计划仍显示 pending 的状态漂移，让 Lean 作为当前 Ship/VFX 可玩切片的已完成资产进入后续归档判断。
- **技术：** 不修改 runtime、prefab、scene、测试或资源资产；通过 Unity Editor 只读检查确认 Body 与六张 Lean PNG 均为 `512 × 512`、`Sprite / Single`、PPU 320、center pivot、no mip maps、alpha transparency，并确认 `Canary_LeanLeft.anim` / `Canary_LeanRight.anim` 均存在且包含 4 个 sprite keys。额外用像素 alpha bounds 与 128px contact-sheet 复核 Lean 偏移属于姿态变化而非导入漂移。

## 2026-06-02 14:30 — Ship/VFX Batch 1 EnergyBars MVP Descope

- **修改文件：**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-06.md`
- **内容：** 根据当前 Ship/VFX MVP 收口决策，将 Batch 1 的 `EnergyBar_L/R` 从未完成阻塞项改为 future polish / descoped for MVP，并同步 Task 6 stack verification 与 Batch 1 completion gate 为完成状态。
- **目的：** 避免为了可选装饰层继续阻塞 Normal playable stack 归档；当前 MVP 以 Body + Shape + Outline + Core + WeaponMount 作为正式 Normal 可玩栈，EnergyBars 后续需要明确视觉方向时再单独进入 polish。
- **技术：** 仅做计划状态和验收口径更新，不新增 PNG、`.meta`、runtime、prefab、scene 或测试改动；保留 EnergyBars 官方命名路径作为未来可选资产占位，但明确其不参与当前 Batch 1 completion blocker。

## 2026-06-02 14:38 — Ship/VFX Batch 8 Overheat Gate Closeout

- **修改文件：**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-06.md`
- **内容：** 完成 Batch 8 Overheat completion gate 的计划收口。将 Overheat 危险升温读感、与 Hit 区分、恢复 Normal、运行时不污染 authored Material / ScriptableObject 四项 gate 同步为完成，并补录资产导入与 Task 29 runtime/reset 验证证据。
- **目的：** 消除 Overheat 资产和验证已完成但 Batch 8 gate 仍显示 pending 的状态漂移，让 Overheat 可作为当前 Ship/VFX MVP 视觉状态矩阵中的已验证危险反馈进入后续归档判断。
- **技术：** 不修改 runtime、prefab、scene、测试或资源资产；复核四个 Overheat PNG 均存在且为 `512 × 512` RGBA，Unity 导入设置确认 core emission / shape mask / spark 为 `Sprite / Single`、noise mask 为 `Default`，全部 PPU 320、center pivot、no mip maps。复用 Task 29 既有证据确认 Overheat repeated trigger、SetActive reset 和 Play Mode exit 均不会留下 sprite/color/alpha/scale 或 authored asset 污染。