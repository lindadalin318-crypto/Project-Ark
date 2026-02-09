# 实施计划：星图编织态交互体验 (Batch 6)

> 基于 [requirements.md](./requirements.md) 中定义的 4 个需求，共 8 个编码任务。

---

- [ ] 1. 创建 `WeavingStateTransition` 核心控制器脚本
   - 新建 `Assets/Scripts/UI/WeavingStateTransition.cs`
   - 命名空间 `ProjectArk.UI`，挂载在 Main Camera 所在 GameObject 上（或独立 GameObject）
   - 该脚本作为编织态过渡的唯一编排器，持有以下 `[SerializeField]` 引用：
     - `Camera _mainCamera`（主摄像机）
     - `Volume _postProcessVolume`（URP 后处理 Volume）
     - `Transform _shipTransform`（飞船位置，用于镜头锁定）
     - `RectTransform _starChartRoot`（星图面板根节点，用于视差）
     - `AudioSource _uiAudioSource`（音效播放源）
   - 提供 `public void EnterWeavingState()` 和 `public void ExitWeavingState()` 两个公共方法
   - 内部使用协程 `TransitionCoroutine()` 驱动所有过渡效果，用 `Time.unscaledDeltaTime` 驱动
   - 暴露 `[SerializeField]` 配置项：进入时长（默认 0.35s）、退出时长（默认 0.25s）、`AnimationCurve`（默认 EaseOutCubic）
   - _需求：1.1, 1.2, 1.4, 1.6_

- [ ] 2. 实现镜头过渡逻辑（orthographicSize 插值）
   - 在 `WeavingStateTransition` 中实现镜头推拉：
     - `[SerializeField] float _combatCameraSize = 5f`（战斗态正交尺寸）
     - `[SerializeField] float _weavingCameraSize = 3f`（编织态正交尺寸）
   - `EnterWeavingState()` 启动协程：从 `_combatCameraSize` 插值到 `_weavingCameraSize`，使用 `AnimationCurve.Evaluate(t)` 驱动，耗时 0.35s
   - `ExitWeavingState()` 启动协程：从 `_weavingCameraSize` 插值回 `_combatCameraSize`，耗时 0.25s
   - 过渡期间将摄像机位置锁定在 `_shipTransform.position`（保持 Z 轴偏移不变）
   - 使用 `Time.unscaledDeltaTime` 确保 timeScale=0 下仍正常运行
   - 如果已有过渡协程在运行，先 `StopCoroutine` 再启动新协程（防止快速切换冲突）
   - _需求：1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

- [ ] 3. 实现后处理氛围切换（DoF + Vignette）
   - 在 `WeavingStateTransition` 中获取 Volume 的 Override：
     - `using UnityEngine.Rendering;` + `using UnityEngine.Rendering.Universal;`
     - 在 `Awake()` / `Start()` 中通过 `_postProcessVolume.profile.TryGet<DepthOfField>(out var dof)` 和 `TryGet<Vignette>(out var vignette)` 缓存引用
   - 进入编织态时：
     - 启用 DoF（`dof.active = true`），设置 `focusDistance` 和 `focalLength` 参数（`[SerializeField]` 可配置）
     - 将 Vignette intensity 从当前值平滑过渡至目标值（默认 0.5），`[SerializeField] float _weavingVignetteIntensity = 0.5f`
   - 退出编织态时：
     - 关闭 DoF（`dof.active = false`）
     - 将 Vignette intensity 平滑过渡回默认值（`[SerializeField] float _combatVignetteIntensity = 0.1f`）
   - 后处理过渡与镜头过渡在同一个协程中并行执行（同一个 `t` 参数驱动）
   - 重要：确认场景中 Main Camera 的 URP Camera Data 组件的 `Render Post Processing` 已启用（在编辑器配置指引中注明）
   - _需求：2.1, 2.2, 2.3, 2.4, 2.5_

- [ ] 4. 创建 `UIParallaxEffect` 视差微动脚本
   - 新建 `Assets/Scripts/UI/UIParallaxEffect.cs`
   - 命名空间 `ProjectArk.UI`，挂载在星图面板根节点上
   - `[SerializeField] float _maxOffset = 15f`（最大偏移像素数）
   - `[SerializeField] float _smoothSpeed = 5f`（平滑跟随速度）
   - 在 `Update()` 中（使用 `Time.unscaledDeltaTime`）：
     - 读取鼠标位置，计算相对屏幕中心的归一化偏移 `(-1, 1)`
     - 将偏移量乘以 `_maxOffset`，取反（视差效果 = 反向位移）
     - 用 `Vector2.Lerp` 平滑过渡到目标偏移，应用到 `RectTransform.anchoredPosition`
   - 提供 `public void ResetOffset()` 方法，立即将偏移清零
   - 手柄支持：检测 `Gamepad.current?.rightStick.ReadValue()`，在有手柄输入时切换数据源
   - 脚本在面板关闭时自动 `ResetOffset()`（通过 `OnDisable`）
   - _需求：3.1, 3.2, 3.3, 3.4_

- [ ] 5. 在 `UIManager` 中集成过渡控制器
   - 修改 `Assets/Scripts/UI/UIManager.cs`：
     - 添加 `[SerializeField] private WeavingStateTransition _weavingTransition;` 字段
     - 在 `OpenPanel()` 中，调用 `_weavingTransition?.EnterWeavingState()` （在 `Time.timeScale = 0` 之后调用，因为协程用 unscaledDelta）
     - 在 `ClosePanel()` 中，调用 `_weavingTransition?.ExitWeavingState()` （在 `Time.timeScale = 1` 之前调用）
     - 使用 null-conditional 确保 `_weavingTransition` 为可选依赖（未配置时静默跳过）
   - _需求：1.1, 2.1, 4.1, 4.2_

- [ ] 6. 实现音效反馈逻辑
   - 在 `WeavingStateTransition` 中添加音效字段：
     - `[SerializeField] AudioClip _openSfx`
     - `[SerializeField] AudioClip _closeSfx`
     - `[SerializeField] AudioSource _sfxSource`
   - `EnterWeavingState()` 开头：若 `_openSfx != null && _sfxSource != null`，播放 `_sfxSource.PlayOneShot(_openSfx)`
   - `ExitWeavingState()` 开头：若 `_closeSfx != null && _sfxSource != null`，播放 `_sfxSource.PlayOneShot(_closeSfx)`
   - AudioSource 需设置 `ignoreListenerPause = true`，确保 `timeScale=0` 时也能播放
   - 若 clip 为 null，静默跳过，不报错（满足需求 4.4）
   - _需求：4.1, 4.2, 4.3, 4.4_

- [ ] 7. 创建 `WeavingTransitionSO` 配置资产（可选优化）
   - 新建 `Assets/Scripts/UI/WeavingTransitionSettingsSO.cs`
   - 将所有可配置参数集中到一个 ScriptableObject 中：
     - 进入/退出时长、相机尺寸、AnimationCurve、Vignette 范围、DoF 参数
   - `WeavingStateTransition` 改为引用此 SO（`[SerializeField] WeavingTransitionSettingsSO _settings`）
   - 如 SO 为 null，使用脚本中的默认值作为 fallback
   - 创建菜单路径：`[CreateAssetMenu(fileName = "WeavingTransitionSettings", menuName = "ProjectArk/UI/WeavingTransitionSettings")]`
   - 好处：设计师可独立调整参数，无需进入 Prefab 编辑；支持多套预设切换
   - _需求：1.6, 2.5（数据驱动架构原则）_

- [ ] 8. 更新实现日志 `ImplementationLog.md`
   - 在 `Docs/ImplementationLog/ImplementationLog.md` 中追加本次 Batch 6 的所有变更记录
   - 包含：新建文件列表、修改文件列表、功能描述、技术要点
   - 格式遵循项目日志规范（标题含功能名称和时间，列出文件路径，简述内容/目的/技术）
   - _需求：项目管理规范_

---

## 编辑器配置提醒（非编码任务，实施后手动操作）

完成上述编码任务后，需在 Unity 编辑器中执行：

1. **Main Camera** → URP Camera Data 组件 → 勾选 `Render Post Processing`
2. 场景中创建一个 **Volume** GameObject（Global Volume，Layer = Default）→ 挂载 `Volume` 组件 → 指向 `DefaultVolumeProfile`
3. 将 `WeavingStateTransition` 脚本挂载到合适的 GameObject（推荐与 UIManager 同层级）
4. 将 `UIParallaxEffect` 脚本挂载到 StarChartPanel 根节点
5. 连线所有 `[SerializeField]` 引用
6. （可选）通过 Create > ProjectArk > UI > WeavingTransitionSettings 创建配置资产
