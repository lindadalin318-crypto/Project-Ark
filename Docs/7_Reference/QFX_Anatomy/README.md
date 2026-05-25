# QFX_Anatomy — Quirky Series · ProjectilesFX 内部构造拆解

## 用途

本目录是对外部资产包 **QFX / ProjectilesFX**（Unity Asset Store · Quirky Series）
的**内部构造拆解笔记**，目的是让 Project Ark 能够：

1. 真正读懂这个包是怎么构造的（不止于"会用"）
2. 在不写代码、不画新贴图的前提下**复刻 / 换皮**出 Project Ark 自己的 VFX 主题包
3. 把 QFX 当成"VFX SDK 半成品"来扩展，而非死素材

> 本目录不是现役真相源；现役 VFX 接入约定见 `2_TechnicalDesign/` 与 `3_WorkflowsAndRules/`。

## 当前文档

- [`01_Construction_Walkthrough.md`](./01_Construction_Walkthrough.md)
  — 以 `VFX_Cyber_Projectile.prefab` 为样本，逐层拆解
  Prefab → ParticleSystem → Material → Shader → Texture 的完整构造链路，
  并给出"如何复刻一个新主题"的可执行步骤。

## 关联评估

- 包对比与选型结论：
  详见此前与 BeanVFX / Hovl Studio 的横向评估（包结构、可复刻性、可扩展性）。
- 接入约定：
  Project Ark 现役 `PooledVFX` / `BoostTrailView` 体系，
  QFX prefab 可直接挂载（命名/槽位 1:1 对应 Flash/Projectile/Impact 三段式）。

## 备注

- QFX 包路径：`Assets/QFX/ProjectilesFX/`
- 主题数（截至 2026-05-23）：15 个色板（Cyber / Lightning / Demon / Vampire / Winter ...）
- Shader 数：4（PFX_Particles / PFX_Particles_Cutout / PFX_Aura / PFX_Trail）
- 总材质：181，总 prefab：61，脚本：3（仅 demo 用，无运行时依赖）
