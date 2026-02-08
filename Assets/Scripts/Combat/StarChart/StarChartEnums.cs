namespace ProjectArk.Combat
{
    /// <summary> High-level category for all Star Chart components. </summary>
    public enum StarChartItemType
    {
        Core,       // 星核 — 发射源
        Prism,      // 棱镜 — 修正器
        LightSail,  // 光帆 — 驾驶风格
        Satellite   // 伴星 — 自动化模组
    }

    /// <summary> Star Core sub-families, each with distinct projectile behavior. </summary>
    public enum CoreFamily
    {
        Matter,  // 实相系 — 物理子弹 (Rigidbody Projectile)
        Light,   // 光谱系 — 激光/折射 (Raycast / LineRenderer)
        Echo,    // 波动系 — 声波/震荡 (Expansion Collider)
        Anomaly  // 异象系 — 浮游雷/回旋镖 (Custom Behavior)
    }

    /// <summary> Prism sub-families, each modifying projectiles differently. </summary>
    public enum PrismFamily
    {
        Fractal,   // 分形 — 分裂/多重/连发 (生成规则修改)
        Rheology,  // 流变 — 加速/巨大/反弹 (数值与物理修改)
        Tint       // 晕染 — 元素附魔/状态注入 (组件注入)
    }

    /// <summary> How a stat modifier is applied. </summary>
    public enum ModifierOperation
    {
        Add,      // 加法：base + value
        Multiply  // 乘法：base * value
    }

    /// <summary> Weapon stats that can be modified by Prisms. </summary>
    public enum WeaponStatType
    {
        Damage,
        ProjectileSpeed,
        Lifetime,
        Spread,
        Knockback,
        RecoilForce,
        FireRate,
        ProjectileCount,
        ProjectileSize,
        HeatCost
    }
}
