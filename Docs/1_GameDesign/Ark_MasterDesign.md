# Project Ark 主设计文档（Master Design Document）

## 1. 游戏概述

### 1.1 项目名称
**Project Ark**（静默方舟）

### 1.2 游戏类型
Top-Down 2D Action Adventure + Metroidvania + Soulslike Atmosphere

### 1.3 核心体验一句话
驾驶飞船穿越手工打造的异星结构，探索、战斗、解锁能力、重组武器构筑，并在孤寂而压迫的世界里拼出自己的“星图”。

### 1.4 核心卖点
- **飞船式 3C**：俯视角高速移动 + Twin-stick 瞄准 + 机动感
- **星图武器构筑**：以“星核 / 棱镜 / 光帆 / 伴星”组合形成长期 RPG 式 build
- **银河恶魔城探索结构**：以房间、门控、能力回返构成世界理解
- **类魂叙事氛围**：低语式世界观、碎片化信息、压抑情绪与未知感
- **强 authoring / validation 架构**：策划、关卡和程序围绕统一场景 authoring 主链协作

---

## 2. 玩家循环

### 2.1 核心循环
1. 探索新房间与新路径
2. 进入战斗 / 环境压迫 / 门控挑战
3. 获取资源、部件、线索或能力
4. 回到星图与飞船构筑层做长期强化
5. 解锁新的世界理解与回返路线

### 2.2 单房体验循环
进入房间 → 读取语义 → 应对压力 → 获得证明 / 回报 → 退出或继续推进

---

## 3. 核心系统总览

### 3.1 3C
- `ShipMotor`
- `ShipAiming`
- `InputHandler`

### 3.2 战斗系统
- `StarChartController`
- `WeaponTrack`
- `SnapshotBuilder`
- `HeatSystem`
- `Projectile / LaserBeam / EchoWave / BoomerangModifier`

### 3.3 敌人系统
- `EnemyBrain` + HFSM
- `EnemyEntity`
- `EnemyPerception`
- `EnemyDirector`

### 3.4 关卡系统
- `Room` + `RoomManager` + `Door`
- `CheckpointSystem`
- `EncounterSystem` / `ArenaController`
- `MinimapManager`
- `WorldClock` / `WorldPhaseManager`

### 3.5 UI 系统
- 星图面板
- HUD（热量 / 血量）
- 编织态过渡

---

## 4. 当前阶段判断

- **当前阶段**：场景配置与验证阶段
- **核心目标**：把已完成模块真实挂进场景并跑通可玩闭环
- **当前重点**：关卡 authoring、场景接线、验证链与文档主树收口

---

## 5. 设计原则

### 5.1 Feel Before Features
战斗系统是否“有手感”优先于功能列表是否齐全。

### 5.2 Readable, Not Smart
敌人和关卡压力的核心是可读性，而不是作弊式复杂度。

### 5.3 Vertical Slice First
优先交付完整可玩的增量，而不是横向铺开十个半成品系统。

### 5.4 Iteration Speed First
能否快速进入 Play Mode 感受差异，是架构是否健康的重要标准。

---

## 6. 当前主文档索引

### 6.1 设计正文

| 文档 | 路径 | 内容 |
|------|------|------|
| 世界观圣经 | `Docs/1_GameDesign/World_Bible.md` | 完整世界观设定 |
| 示巴星圣经 | `Docs/1_GameDesign/Sheba_Planet_Bible.md` | 示巴星完整设定 |
| Bellringer 角色设计 | `Docs/1_GameDesign/Bellringer_Character_Design.md` | NPC 角色设定 |
| 示巴星需求主稿 | `Docs/1_GameDesign/Sheba_FeatureRequirements.md` | 示巴星当前现役需求主稿 |
| 示巴星房间语法 | `Docs/1_GameDesign/Sheba_RoomGrammar.md` | 关卡语法与区域语言 |
| 星图 UI 设计 | `Docs/1_GameDesign/StarChart_UI_Design.md` | 星图界面交互设计 |

### 6.2 技术与规则

| 文档 | 路径 | 内容 |
|------|------|------|
| Level 规范 | `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md` | 关卡系统现役技术真相源 |
| Level 工作流 | `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md` | 关卡搭建与验证工作流 |
| Ship / VFX 规范 | `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md` | Ship / VFX 现役技术真相源 |
| Ship / VFX 注册表 | `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md` | 现役对象映射与 owner 注册表 |
| 项目实现规则 | `Implement_rules.md` | 项目级实现规则与治理约束 |

### 6.3 数据、诊断与日志

| 文档 | 路径 | 内容 |
|------|------|------|
| 星图规划数据 | `Docs/4_GameData/StarChart/StarChart_Planning.csv` | 星图系统结构化规划数据 |
| 当前诊断入口 | `Docs/6_Diagnostics/README.md` | 当前支持矩阵与现状报告入口 |
| 实现日志入口 | `Docs/5_ImplementationLog/README.md` | 历史总账与月度日志入口 |
| 历史设计归档 | `Docs/8_Obsolete/README.md` | 已失效或已替代文档归档区 |

---

## 7. 维护说明

> 本文档是 `1_GameDesign` 的**唯一总设计入口**。世界设定详见 `World_Bible.md`，结构化数据详见 `Docs/4_GameData/`，技术真相源详见 `Docs/2_TechnicalDesign/`，实现细节与历史变更详见 `Docs/5_ImplementationLog/`。
