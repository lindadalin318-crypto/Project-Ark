# 实施计划：Enemy_Bestiary.CSV 配置表重设计

- [ ] 1. 扩展 `EnemyStatsSO.cs` 新增字段
   - 新增抗性字段：`Resist_Physical`, `Resist_Fire`, `Resist_Ice`, `Resist_Lightning`, `Resist_Void`，均标注 `[Range(0f, 1f)]`，默认 0f
   - 新增掉落与生成字段：`DropTableID`(string), `SpawnWeight`(float, 默认 1f)
   - 新增元数据字段：`PlanetID`(string)
   - 新增行为标签字段：`BehaviorTags`(List\<string\>)
   - 确保所有新字段有合理的 `[Header]` 分组和 `[Tooltip]` 说明
   - 不修改已有字段的名称或类型，保持向后兼容
   - _需求：5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 2. 重建 `Enemy_Bestiary.csv` 表头结构
   - 按 A~L 共 12 个分组顺序创建 50 列表头行
   - 列名严格遵循需求文档中的完整列清单（PascalCase，与 EnemyStatsSO 字段一致）
   - 旧列名映射：`MaxHealth` → `MaxHP`，`Poise` → `MaxPoise`，`AggroRange` → `SightRange`，`Description` → `Description_Note`
   - _需求：4.1, 4.2, 4.3_

- [ ] 3. 迁移已有 6 行敌人数据到新表格
   - 将旧表中 ID 5001–5006 的数据逐字段迁移到新列位置
   - 旧字段值正确映射到新列名（如 MaxHealth 的值填入 MaxHP 列）
   - 新增列（TelegraphDuration, AttackActiveDuration, RecoveryDuration, 抗性, 远程参数等）留空或填入 EnemyStatsSO 默认值
   - 近战型敌人的远程攻击分组 (F) 全部留空
   - BaseColor 列根据已有 BaseColor 值拆分为 R/G/B/A 四列
   - _需求：7.1, 7.2, 7.3, 1.3_

- [ ] 4. 补充已有敌人的缺失数值
   - 根据现有 `EnemyAssetCreator.cs` 中 Rusher 和 Shooter 的预设数值，为已有 6 行敌人补填：AttackDamage, AttackKnockback, TelegraphDuration, AttackActiveDuration, RecoveryDuration, SightAngle, HearingRange, LeashRange, MemoryDuration, HitFlashDuration 等
   - 射手型敌人（如有）补填远程攻击分组所有字段
   - 确保补填数值与 `EnemyAssetCreator` 中已有的硬编码预设一致
   - _需求：1.1, 1.2, 7.3_

- [ ] 5. 创建 `BestiaryImporter.cs` Editor 脚本 — CSV 解析与字段映射框架
   - 在 `Assets/Scripts/Combat/Editor/` 下新建 `BestiaryImporter.cs`
   - 添加菜单项 `ProjectArk > Import Enemy Bestiary`
   - 实现 CSV 文件读取与行解析逻辑（处理逗号分隔、引号转义）
   - 定义列名到 `EnemyStatsSO` 字段的映射字典
   - 识别并跳过 `_Note` 后缀列和不映射到代码的纯策划列（Rank, AI_Archetype, FactionID, ExpReward, DesignIntent, PlayerCounter）
   - _需求：6.1, 4.2, 4.3_

- [ ] 6. 实现 `BestiaryImporter.cs` — SO 资产创建与更新逻辑
   - 按 `InternalName`（EnemyID）查找已有 SO 资产，存在则更新字段，不存在则在 `Assets/_Data/Enemies/` 下新建
   - 实现基础类型字段赋值：float, int, string 通过反射或显式映射赋值
   - 实现 `BaseColor_R/G/B/A` 四列合并为 `Color` 的特殊处理
   - 实现 `ProjectilePrefab` 路径字符串转 `GameObject` 引用（`AssetDatabase.LoadAssetAtPath`）
   - 实现 `BehaviorTags` 分号分隔字符串拆分为 `List<string>`
   - 空字段跳过处理（保留 SO 默认值不覆盖）
   - _需求：6.2, 6.3, 6.4, 6.6, 6.7, 1.3_

- [ ] 7. 实现 `BestiaryImporter.cs` — 导入结果报告与错误处理
   - 导入完成后弹出 `EditorUtility.DisplayDialog` 汇总：成功数、更新数、新建数、跳过数、耗时
   - 对无效行（缺少 ID 或 InternalName）输出 `Debug.LogWarning` 并跳过
   - 对 Prefab 路径找不到的情况输出警告但不中断导入流程
   - _需求：6.5_

- [ ] 8. 更新 `ImplementationLog.md` 记录本次变更
   - 记录 EnemyStatsSO 扩展的新增字段清单
   - 记录 Enemy_Bestiary.csv 的表头重设计（15列 → 50列）
   - 记录 BestiaryImporter 的功能和使用方式
   - 记录数据迁移情况（6 行已有数据的兼容处理）
   - _需求：全部_
