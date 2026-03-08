# 实施计划：Glitch 飞船完整特效复刻

- [ ] 1. 扩展 `ShipJuiceSettingsSO` — 新增所有特效参数字段
   - 新增 Boost 尾迹粒子参数：`_boostTrailEmissionRate`（80）、`_boostTrailLifetime`（0.3s）、`_boostTrailStartSpeed`（3.0）、`_boostTrailStartSizeMin/Max`（0.08/0.15）
   - 新增 TrailRenderer 参数：`_boostTrailTime`（0.25s）、`_boostTrailStartWidth`（0.12）、`_boostTrailMinVertexDistance`（0.05）
   - 新增推进器脉冲参数：`_boostBackScalePeak`（1.3）、`_boostBackPulsePeriod`（0.3s）、`_boostBackPulseScale`（1.1）
   - 新增引擎粒子精确参数：`_engineIdleEmissionRate`（5）、`_engineMaxEmissionRate`（40）、`_engineDashEmissionRate`（120）、`_engineStartSizeMin/Max`（0.04/0.08）
   - _需求：2.2、3.2、4.1、5.1、5.2、5.3、6.4_

- [ ] 2. 创建 `BoostTrailParticlesSO` 配置并实现 `ShipBoostTrailVFX.cs`
   - 新建 `ShipBoostTrailVFX.cs`（MonoBehaviour），持有 `BoostTrailParticles` ParticleSystem 引用
   - `OnBoostStarted`：配置粒子参数（emission/lifetime/speed/size/color）并调用 `Play()`
   - `OnBoostEnded`：调用 `Stop(false)`，已有粒子自然消亡
   - 粒子材质使用 Additive 混合（`ShipGlowMaterial` 复用或新建 `BoostParticleMaterial`）
   - `simulationSpace = World`，确保粒子不跟随飞船旋转
   - _需求：2.1、2.2、2.3、2.4、2.5_

- [ ] 3. 实现 `BoostTrail` TrailRenderer 控制逻辑（集成进 `ShipView`）
   - 在 `ShipView` 中新增 `[SerializeField] TrailRenderer _boostTrail` 字段
   - `OnBoostStarted`：设置 TrailRenderer 参数（time/startWidth/endWidth/color/minVertexDistance）并设 `emitting = true`
   - `OnBoostEnded`：设 `emitting = false`，尾迹自然消退
   - `OnDisable/ResetVFX`：调用 `_boostTrail.Clear()` 并设 `emitting = false`
   - _需求：3.1、3.2、3.3、3.4、3.5、6.3、6.5_

- [ ] 4. 实现 `Dodge_Sprite` 静态残影逻辑（集成进 `ShipView`）
   - 在 `ShipView` 中新增 `[SerializeField] SpriteRenderer _dodgeSprite` 字段
   - `OnDashStarted`：记录飞船世界坐标，将 `_dodgeSprite` 的 Transform 移至该世界坐标（`SetParent(null)` 解耦），激活 GO，设颜色为青绿色 Alpha=1，用 PrimeTween `Tween.Alpha` 在 `AfterImageFadeDuration` 内衰减至 0
   - `OnDashEnded`：重置 Alpha=0，将 GO 重新 SetParent 回 `ShipVisual`，停用 GO
   - Fallback：若 `_dodgeSprite.sprite == null`，使用 `_solidRenderer.sprite`
   - _需求：1.1、1.2、1.3、1.4、1.5_

- [ ] 5. 实现 Boost 推进器脉冲动画（PrimeTween fallback，无需 Animator）
   - 在 `ShipView` 中新增 `[SerializeField] Transform _backSpriteTransform` 字段（指向 `Ship_Sprite_Back`）
   - `OnBoostStarted`：用 `Tween.Scale` 播放入场脉冲（1.0 → 1.3 → 1.0，0.15s），完成后启动循环脉冲 Sequence（1.0 ↔ 1.1，周期 0.3s）
   - `OnBoostEnded`：停止循环 Sequence，用 `Tween.Scale` 在 0.1s 内恢复至 `Vector3.one`
   - `OnDisable/ResetVFX`：立即停止所有 Scale Tween，强制恢复 `Vector3.one`
   - _需求：4.1、4.2、4.3、4.4_

- [ ] 6. 精确对齐引擎粒子参数（修改 `ShipEngineVFX.cs`）
   - 将 `ConfigureEngineParticles()` 中的粒子参数替换为 `ShipJuiceSettingsSO` 中的精确值
   - 实现四档发射率切换：怠速（5/s）→ 常规飞行（0～40/s 线性插值）→ Dash（120/s）→ Boost（80/s，由 `ShipBoostTrailVFX` 独立处理，引擎粒子保持 Boost 档）
   - 设置 `simulationSpace = Local`，确保粒子方向跟随飞船旋转
   - 将 `EngineExhaust` GO 位置设为 `Ship_Sprite_Back` 局部坐标 `(0, -0.15, 0)`
   - _需求：5.1、5.2、5.3、5.4、5.5_

- [ ] 7. 扩展 `ShipView` — 统一引用管理与 `ResetVFX()` 接口
   - 新增所有特效组件的 `[SerializeField]` 字段：`_boostTrailParticles`、`_boostTrail`、`_dodgeSprite`、`_backSpriteTransform`
   - 实现 `public void ResetVFX()` 方法：停止所有粒子、清空 Trail、重置 Alpha、恢复 Scale、取消所有 Tween
   - `OnDisable` 调用 `ResetVFX()`，确保场景切换/对象池回收时特效干净清除
   - 验证 `OnEnable/OnDisable` 事件订阅/取消订阅覆盖所有新增事件
   - _需求：6.1、6.2、6.3、6.4、6.5_

- [ ] 8. 扩展 `ShipPrefabRebuilder.cs` — 自动创建所有新增 GO 节点并连线
   - 在 `Ship_Sprite_Back` 下创建 `BoostTrailParticles`（ParticleSystem）GO
   - 在 `Ship_Sprite_Back` 下创建 `BoostTrail`（TrailRenderer）GO，配置初始参数
   - 在 `ShipVisual` 下创建 `Dodge_Sprite`（SpriteRenderer，SortOrder=-1，Alpha=0，初始 inactive）GO
   - 自动查找 `player_test_fire.png` 并赋值给 `Dodge_Sprite.sprite`
   - 将所有新增组件引用通过 `SerializedObject.FindProperty` 连线到 `ShipView` 对应字段
   - _需求：7.1、7.2、7.3、7.4、7.5_

- [ ] 9. 更新 `ImplementationLog.md` 并执行 Play Mode 验证清单
   - 运行 `ProjectArk > Ship > Rebuild Ship Prefab Sprite Layers` 验证 Prefab 重建幂等性
   - Play Mode 验证：常规飞行引擎粒子参数 → Dash 残影+闪烁 → Boost 尾迹粒子+Trail+推进器脉冲
   - 验证 `OnDisable` 后无特效残留（切换场景测试）
   - 追加 ImplementationLog.md 记录本轮所有新建/修改文件
   - _需求：6.3、6.5（验收）_
