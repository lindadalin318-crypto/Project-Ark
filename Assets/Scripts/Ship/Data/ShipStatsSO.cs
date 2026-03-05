using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// 飞船全部数值配置。
    /// 移动模型完全对齐 Galactic Glitch（AssetRipper 反编译真实数值）：
    ///
    ///   GG 真实架构（Player.prefab 解包确认）：
    ///     - 状态机驱动参数：根据飞船当前状态（普通/Boost/攻击/Dodge）切换不同数值组
    ///     - 普通飞行(IsBlueState)：linearDrag=3, moveAccel=100, maxMoveSpeed=7.5, angularAccel=80, maxRotSpeed=80
    ///     - Boost飞行(IsBoostState)：linearDrag=2.5, maxMoveSpeed=9, angularAccel=40, maxRotSpeed=80
    ///     - 主攻击(IsMainAttackState)：maxMoveSpeed=7.5, angularAccel=720, maxRotSpeed=720
    ///     - Dodge(IsDodgeState)：linearDrag=1.7, moveAccel=12, maxMoveSpeed=4, angularAccel=20, maxRotSpeed=50
    ///     - Rigidbody2D.mass=100, m_LinearDrag=2.5(基准), m_AngularDrag=0
    ///
    ///   Project Ark 实现：
    ///     - 前向推力模型（只沿 transform.up 加速）+ angularVelocity 旋转
    ///     - 无状态机，但参数完全对齐 GG IsBlueState（正常飞行状态）
    ///     - Boost = 冲量+放宽速度上限（对应 GG IsBoostState 参数差异）
    /// </summary>
    [CreateAssetMenu(fileName = "NewShipStats", menuName = "ProjectArk/Ship/Ship Stats")]
    public class ShipStatsSO : ScriptableObject
    {
        // ══════════════════════════════════════════════════════════════
        // Rotation  (对应 GGSteering.angularAcceleration / maxRotationSpeed)
        // ══════════════════════════════════════════════════════════════

        [Header("Rotation  (mass=1) angularAccel=800 → 快速有惯性; maxRotSpeed=360")]
        [Tooltip("旋转角加速度 (deg/s²)。mass=1时建议600~1200，值越大转向响应越快。GG原值80是基于mass=100设计的。")]
        [Min(1f)]
        [SerializeField] private float _angularAcceleration = 800f;

        [Tooltip("最大旋转角速度 (deg/s)。mass=1建议240~480。360 = 一秒内能转完一整圈。")]
        [Min(1f)]
        [SerializeField] private float _maxRotationSpeed = 360f;

        [Tooltip("Rigidbody2D.angularDrag。0 = 旋转无阻尼（靠 maxRotSpeed clamp 限制）。建议 0~2。")]
        [Min(0f)]
        [SerializeField] private float _angularDrag = 0f;

        // ══════════════════════════════════════════════════════════════
        // Forward Thrust  ★GG真实值: IsBlueState moveAccel=100, maxMoveSpeed=7.5
        // ══════════════════════════════════════════════════════════════

        [Header("Movement  (mass=1) accel=20 → 响应快; maxSpeed=8 units/s")]
        [Tooltip("移动加速度 (units/s²)。mass=1时 F=ma→加速度=ForwardAcceleration。建议15~30，太大则瞬间达速缺乏惯性感。")]
        [Min(0.1f)]
        [SerializeField] private float _forwardAcceleration = 20f;

        [Tooltip("最大线速度 (units/s)。建议6~12。8 units/s = 在标准摄像头视野中流畅移动。")]
        [Min(0.1f)]
        [SerializeField] private float _maxSpeed = 8f;

        [Tooltip("Rigidbody2D.linearDrag。越高减速越快，手感越'粘'。建议2~5。3 = 松手约0.3秒停下。")]
        [Min(0f)]
        [SerializeField] private float _linearDrag = 3f;

        // ══════════════════════════════════════════════════════════════
        // Boost  ★GG真实: IsBoostState 是状态切换，不是冲量
        //   GG IsBoostState: linearDrag=2.5, maxMoveSpeed=9, angularAccel=40, maxRotSpeed=80, minTime=0.2s
        //   对比正常: linearDrag=3, maxMoveSpeed=7.5, angularAccel=80, maxRotSpeed=80
        //   核心效果：降阻力 + 放宽速度上限 + 降低旋转响应（更难急转弯，给追击感）
        // ══════════════════════════════════════════════════════════════

        [Header("Boost  状态切换模型 (对齐 GG IsBoostState)")]
        [Tooltip("Boost 状态下的 linearDrag。★GG原值 2.5（正常为 3）。降低阻力让飞船更难减速。")]
        [Min(0f)]
        [SerializeField] private float _boostLinearDrag = 2.5f;

        [Tooltip("Boost 状态下的最大移动速度 (units/s)。★GG原值 9（正常为 7.5）。")]
        [Min(0.1f)]
        [SerializeField] private float _boostMaxSpeed = 9f;

        [Tooltip("Boost 状态下的角加速度 (deg/s²)。★GG原值 40（正常为 80），进入 Boost 后转向变迟钝。mass=1 对应约 400。")]
        [Min(1f)]
        [SerializeField] private float _boostAngularAcceleration = 400f;

        [Tooltip("Boost 状态最短持续时间 (s)。★GG原值 minTime=0.2s。持续期间保持 Boost 参数。")]
        [Min(0f)]
        [SerializeField] private float _boostDuration = 0.2f;

        [Tooltip("Boost 冷却时间 (s)。建议 0.8~1.2s。")]
        [Min(0f)]
        [SerializeField] private float _boostCooldown = 1.0f;

        [Tooltip("Boost 输入缓冲窗口 (s)。冷却结束前这么久内的按键会自动触发。")]
        [Min(0f)]
        [SerializeField] private float _boostBufferWindow = 0.15f;

        // ══════════════════════════════════════════════════════════════
        // Dash  (短距离闪避，独立于 Boost)
        // ══════════════════════════════════════════════════════════════

        [Header("Dash  快速闪避冲量; impulse=12 → 瞬移感强烈")]
        [Tooltip("Dash 冲量 (mass=1 时 = 速度变化量 units/s)。建议8~15。12 = 明显的位移感，不至于穿越太远。")]
        [Min(0f)]
        [SerializeField] private float _dashImpulse = 12f;

        [Tooltip("Dash 无敌帧时长 (s)。★GG真实值: dodgeInvulnerabilityTime=0.15s。")]
        [Min(0f)]
        [SerializeField] private float _dashIFrameDuration = 0.15f;

        [Tooltip("Dash 冷却时间 (s)。★GG真实值: dodgeRechargeTime=0.5s。")]
        [Min(0f)]
        [SerializeField] private float _dashCooldown = 0.5f;

        [Tooltip("Dash 输入缓冲窗口 (s)。")]
        [Min(0f)]
        [SerializeField] private float _dashBufferWindow = 0.15f;

        // ══════════════════════════════════════════════════════════════
        // Survival
        // ══════════════════════════════════════════════════════════════

        [Header("Survival")]
        [Tooltip("最大血量。")]
        [SerializeField] private float _maxHP = 100f;

        [Tooltip("受击白闪时长 (s)。")]
        [SerializeField] private float _hitFlashDuration = 0.1f;

        // ══════════════════════════════════════════════════════════════
        // Hit Feedback
        // ══════════════════════════════════════════════════════════════

        [Header("Hit Feedback")]
        [Tooltip("HitStop 冻帧时长 (s)。0 = 禁用。")]
        [Min(0f)]
        [SerializeField] private float _hitStopDuration = 0.05f;

        [Tooltip("受击无敌帧时长 (s)。")]
        [Min(0f)]
        [SerializeField] private float _iFrameDuration = 1.0f;

        [Tooltip("无敌帧闪烁间隔 (s)。")]
        [Min(0.01f)]
        [SerializeField] private float _iFrameBlinkInterval = 0.1f;

        [Tooltip("基础屏幕震动强度。")]
        [Min(0f)]
        [SerializeField] private float _screenShakeBaseIntensity = 0.3f;

        [Tooltip("每点伤害附加屏幕震动强度。")]
        [Min(0f)]
        [SerializeField] private float _screenShakeDamageScale = 0.01f;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Rotation
        // ══════════════════════════════════════════════════════════════

        public float AngularAcceleration   => _angularAcceleration;
        public float MaxRotationSpeed      => _maxRotationSpeed;
        public float AngularDrag           => _angularDrag;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Forward Thrust
        // ══════════════════════════════════════════════════════════════

        public float ForwardAcceleration   => _forwardAcceleration;
        public float MaxSpeed              => _maxSpeed;
        public float LinearDrag            => _linearDrag;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Boost
        // ══════════════════════════════════════════════════════════════

        public float BoostLinearDrag        => _boostLinearDrag;
        public float BoostMaxSpeed         => _boostMaxSpeed;
        public float BoostAngularAcceleration => _boostAngularAcceleration;
        public float BoostDuration         => _boostDuration;
        public float BoostCooldown         => _boostCooldown;
        public float BoostBufferWindow     => _boostBufferWindow;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Dash
        // ══════════════════════════════════════════════════════════════

        public float DashImpulse           => _dashImpulse;
        public float DashIFrameDuration    => _dashIFrameDuration;
        public float DashCooldown          => _dashCooldown;
        public float DashBufferWindow      => _dashBufferWindow;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Survival
        // ══════════════════════════════════════════════════════════════

        public float MaxHP                 => _maxHP;
        public float HitFlashDuration      => _hitFlashDuration;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Hit Feedback
        // ══════════════════════════════════════════════════════════════

        public float HitStopDuration       => _hitStopDuration;
        public float IFrameDuration        => _iFrameDuration;
        public float IFrameBlinkInterval   => _iFrameBlinkInterval;
        public float ScreenShakeBaseIntensity  => _screenShakeBaseIntensity;
        public float ScreenShakeDamageScale    => _screenShakeDamageScale;

        // ══════════════════════════════════════════════════════════════
        // Backward Compatibility Aliases（过渡期，防止其他脚本编译报错）
        // ══════════════════════════════════════════════════════════════

        /// <summary> 向后兼容旧代码 DashSpeed 引用。等同于 DashImpulse。 </summary>
        public float DashSpeed => _dashImpulse;
        /// <summary> 向后兼容旧代码 DashDuration 引用。GG 模型中 Dash 是冲量，无固定时长，返回 0.1 占位。 </summary>
        public float DashDuration => 0.1f;
        /// <summary> 向后兼容旧代码 DashIFrames。等同于 DashIFrameDuration > 0。 </summary>
        public bool DashIFrames => _dashIFrameDuration > 0f;
        /// <summary> 向后兼容旧代码 DashExitSpeedRatio。GG 模型中不需要，固定返回 0。 </summary>
        public float DashExitSpeedRatio => 0f;
        /// <summary> 向后兼容旧代码 MoveSpeed。等同于 MaxSpeed。 </summary>
        public float MoveSpeed => _maxSpeed;
        /// <summary> 向后兼容旧代码 RotationSpeed。等同于 MaxRotationSpeed。 </summary>
        public float RotationSpeed => _maxRotationSpeed;
    }
}
