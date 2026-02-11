# 实施计划

- [ ] 1. 完善现有 5001–5006 的数据行
   - 更新 5003 的 `InternalName`/`DisplayName` 为 `Enemy_Drone_Worker`/`工蜂无人机`，同步更新 `PrefabPath`、`Description_Note`
   - 更新 5004 的 `InternalName`/`DisplayName` 为 `Enemy_Drill_Ceiling`/`天花板钻头`，同步更新 `PrefabPath`、`Description_Note`
   - 更新 5006 的 `InternalName`/`DisplayName` 为 `Enemy_Boss_RustQueen`/`锈蚀女王`，同步更新 `PrefabPath`、`Description_Note`
   - 为全部 6 行补齐 `DesignIntent`、`PlayerCounter`、`Description_Note` 与 EnemyPlanning 一致的文案
   - 验证所有 50 列数值符合需求 1 的基准线和需求 4 的规范（信号-窗口模型、感知规则、抗性规则）
   - _需求：1.1, 1.2, 1.3, 1.4, 1.5, 2.3, 4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 2. 新增 P1 示巴星 Minion 层怪物（5007 酸蚀爬行者、5008 自爆蜱虫）
   - 写入 5007 `Enemy_Crawler_Acid`/`酸蚀爬行者`：Minion, Melee_Rusher, HP=25, 死亡留酸液（BehaviorTags=DeathHazard）
   - 写入 5008 `Enemy_Tick_Volatile`/`自爆蜱虫`：Minion, Melee_Rusher, HP=15, Speed=7, 自爆伤害=30（BehaviorTags=SelfDestruct）
   - 确保近战型远程列留空，数值在 Minion 基准线范围内
   - _需求：2.1, 2.2, 4.1, 4.4, 4.5, 5.1, 5.2_

- [ ] 3. 新增 P1 示巴星 Defense/Specialist/Support 层怪物（5009 重型装载机、5010 晶体甲虫、5011 修理博比特）
   - 写入 5009 `Enemy_Loader_Heavy`/`重型装载机`：Defense, Shield_Charger, HP=100, Speed=1.5, 正面无敌（BehaviorTags=FrontShield）
   - 写入 5010 `Enemy_Beetle_Reflector`/`晶体甲虫`：Specialist, Stationary_Turret变体, HP=50, 反射激光（BehaviorTags=ReflectLaser,Resist_Lightning=0.8）
   - 写入 5011 `Enemy_Bot_Repair`/`修理博比特`：Support, Healer, HP=35, Speed=4.5, 零攻击（BehaviorTags=Healer）
   - _需求：2.1, 2.2, 4.1, 4.3, 4.4, 4.5, 5.2_

- [ ] 4. 新增 P1 示巴星 Elite 层和 Gimmick 怪物（5012 暴走工头、5013 盗矿地精）
   - 写入 5012 `Enemy_Foreman`/`暴走工头`：Elite, Melee_Charger, HP=180, Poise=60, 冲撞+召唤（BehaviorTags=SuperArmor;Summon）
   - 写入 5013 `Enemy_Goblin_Loot`/`盗矿地精`：Gimmick, Flee, HP=40, Speed=8, 零攻击,高EXP=50（BehaviorTags=FleeOnSight;HighLoot）
   - _需求：2.1, 2.2, 4.4, 4.5, 5.2, 5.3_

- [ ] 5. 新增 P1 示巴星 Mini-Boss（5014 挖掘者 9000）
   - 写入 5014 `Enemy_Excavator`/`挖掘者 9000`：Mini-Boss, Boss_Roamer, HP=1500, Poise=999, Speed=3, 高伤=35
   - LeashRange=999, MemoryDuration=999, BehaviorTags=SuperArmor;AreaDenial, ExpReward=150
   - _需求：2.1, 2.2, 4.1, 4.2, 4.4, 4.5, 5.2_

- [ ] 6. 新增 P2 机械坟场 Minion 和 Ranged 层怪物（5015 机械猎犬、5016 狙击炮塔、5017 迫击炮手）
   - 写入 5015 `Enemy_Dog_Scrap`/`机械猎犬`：Minion, Melee_Flanker, HP=45, Speed=5.5, 死亡自爆=20（BehaviorTags=Flanker;SelfDestruct）
   - 写入 5016 `Enemy_Turret_Sniper_P2`/`狙击炮塔`：Ranged, Stationary_Turret, HP=60, 不可移动, 伤害=40, 射程=20
   - 写入 5017 `Enemy_Bot_Mortar`/`迫击炮手`：Ranged, Ranged_Lobber, HP=55, Speed=2, 榴弹伤害=15（BehaviorTags=AreaDenial）
   - 整体数值比 P1 同级别提升 30%–50%
   - _需求：3.1, 3.2, 3.3, 4.1, 4.2, 4.4, 4.5, 5.1, 5.2_

- [ ] 7. 新增 P2 机械坟场 Defense 和 Specialist 层怪物（5018 方阵兵、5019 磁力钩锁、5020 干扰水晶、5021 隐形猎手）
   - 写入 5018 `Enemy_Phalanx`/`方阵兵`：Defense, Shield_Wall, HP=70, Speed=2.5, 激光墙伤害=10/秒（BehaviorTags=LaserWall;Paired）
   - 写入 5019 `Enemy_Hook_Magnet`/`磁力钩锁`：Specialist, Ranged_Utility, HP=65, Speed=2, 钩锁射程=12（BehaviorTags=ForcedPull;Stun）
   - 写入 5020 `Enemy_Pylon_Jammer`/`干扰水晶`：Specialist, Stationary_Aura, HP=80, 不可移动, 干扰半径=8（BehaviorTags=SkillJam;AuraDebuff）
   - 写入 5021 `Enemy_Stalker_Ghost`/`隐形猎手`：Specialist, Ambusher, HP=55, Speed=5, 暴击伤害=35（BehaviorTags=Invisible;CritStrike）
   - _需求：3.1, 3.2, 4.1, 4.3, 4.4, 5.2_

- [ ] 8. 新增 P2 机械坟场 Hazard 和 Anti-Pattern 层怪物（5022 感应地雷、5023 反应装甲兵）
   - 写入 5022 `Enemy_Mine_Proximity`/`感应地雷`：Hazard, Stationary_Trap, HP=10, 不可移动, 爆炸伤害=35（BehaviorTags=ProximityTrigger;Exploitable）
   - 写入 5023 `Enemy_Guard_Reactive`/`反应装甲兵`：Specialist, Counter_Attacker, HP=90, Speed=2, 反弹伤害=12（BehaviorTags=DamageReflect;RateLimit）
   - _需求：3.1, 3.2, 4.4, 5.2_

- [ ] 9. 新增 P2 机械坟场 Elite 和 Boss 层怪物（5024 电磁处刑者、5025 焚化炉、5026 游荡者）
   - 写入 5024 `Enemy_Electrocutioner`/`电磁处刑者`：Elite, Melee_Teleporter, HP=150, Speed=6.5, 连击伤害=18×3（BehaviorTags=Teleport;Silence;ComboAttack）
   - 写入 5025 `Enemy_Boss_Incinerator`/`焚化炉`：Boss, Boss_Phase_Incinerator, HP=3000, Poise=999, 全屏DOT=3/秒（BehaviorTags=SuperArmor;AuraDOT;Summon）
   - 写入 5026 `Enemy_MiniBoss_Roamer`/`游荡者`：Mini-Boss, Boss_Roamer_Kiter, HP=1200, Speed=5, 狙击伤害=30（BehaviorTags=FleeAndSnipe;Regen）
   - LeashRange=999, MemoryDuration=999 for Boss/Mini-Boss
   - _需求：3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 4.4, 4.5, 5.2, 5.3_

- [ ] 10. 更新 ImplementationLog.md 记录本次变更
   - 追加新条目，标题含功能名称和时间戳
   - 列出修改的文件（Enemy_Bestiary.csv）
   - 简述变更内容：完善6行+新增20行=26行完整怪物数据
   - 记录设计哲学摘要（P1教学生态/P2组合升级）
   - _需求：N/A（项目流程规范 [[memory:m3desi92]]）_
