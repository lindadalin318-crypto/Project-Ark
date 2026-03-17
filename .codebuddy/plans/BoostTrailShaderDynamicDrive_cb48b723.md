---
name: BoostTrailShaderDynamicDrive
overview: 让 TrailMainEffect.shader 真正吃到 Boost 运行时强度，实现主拖尾动态联动
todos:
  - id: modify-shader-properties
    content: 在 TrailMainEffect.shader 的 Properties 块中添加 _BoostIntensity 属性
    status: completed
  - id: modify-shader-cbuffer
    content: 在 CBUFFER 中添加 _BoostIntensity 声明
    status: completed
    dependencies:
      - modify-shader-properties
  - id: modify-shader-shade
    content: 在 ShadeDisturbance() 函数中接入动态参数驱动
    status: completed
    dependencies:
      - modify-shader-cbuffer
  - id: modify-boostview-field
    content: 在 BoostTrailView.cs 中新增 _mpbMainTrail 字段
    status: completed
  - id: modify-boostview-awake
    content: 在 Awake() 中初始化 _mpbMainTrail
    status: completed
    dependencies:
      - modify-boostview-field
  - id: modify-boostview-setintensity
    content: 在 SetBoostIntensity() 中写入 _mainTrail
    status: completed
    dependencies:
      - modify-boostview-awake
  - id: validate-unity
    content: Unity 验证：刷新、编译、检查
    status: completed
    dependencies:
      - modify-shader-shade
      - modify-boostview-setintensity
---

## 用户需求

按 `Docs/Reference/BoostTrail_Shader_Implementation_Status.md` 实现 BoostTrail 主拖尾动态驱动。

**目标**：让主拖尾在 Boost 时动态"活起来"，随强度变化。

**范围**：

- 仅修改 `TrailMainEffect.shader` 与 `BoostTrailView.cs`
- 本轮不新增 ScriptableObject，不改 legacy 路径

**验收标准**：

- 运行时联动成立：Boost 开始后主拖尾扰动/亮度/边缘发光增强
- 退出平滑：Boost 结束后主拖尾自然衰减
- 兼容 legacy 路径：`_UseLegacySlots > 0.5` 不受影响
- 工程稳定：MaterialPropertyBlock 不泄漏

## 技术方案

### 1. TrailMainEffect.shader 修改

**Properties 新增**：

```
_BoostIntensity ("Boost Intensity", Range(0, 1)) = 1
```

**CBUFFER 新增**：

```
float _BoostIntensity;
```

**ShadeDisturbance() 改动**（在函数开头建立局部强度）：

```
float intensity = saturate(_BoostIntensity);
```

然后用 lerp 驱动 4 个核心参数：

- 扰动强度：`distortStrength = lerp(_DistortStrength * 0.35, _DistortStrength, intensity)`
- 流动速度：`flowSpeed = lerp(_FlowSpeed * 0.45, _FlowSpeed, intensity)`  
- 亮度：`brightness = lerp(_Brightness * 0.55, _Brightness, intensity)`
- Alpha：`alphaScale = lerp(_Alpha * 0.25, _Alpha, intensity)`

### 2. BoostTrailView.cs 修改

**新增字段**：

```
private MaterialPropertyBlock _mpbMainTrail;
```

**Awake() 初始化**：

```
_mpbMainTrail = new MaterialPropertyBlock();
```

**SetBoostIntensity() 追加**：

```
if (_mainTrail != null)
{
    _mainTrail.GetPropertyBlock(_mpbMainTrail);
    _mpbMainTrail.SetFloat(BoostIntensityID, value);
    _mainTrail.SetPropertyBlock(_mpbMainTrail);
}
```

### 架构约束

- 优先沿用现有 `MaterialPropertyBlock` 思路
- 避免为 `TrailRenderer` 创建运行时材质实例
- 保持与 energyLayer2/3/Field 一致的控制模式