using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// 飞船物理移动 — Twin-Stick 世界方向移动模型（对应 GG 鼠标模式手感）：
    ///
    ///   移动模型：
    ///     - WASD / 左摇杆 → 世界空间方向施力（W=上, S=下, A=左, D=右）
    ///     - 力的大小 = forwardAcceleration * 输入强度（完整 WASD 向量）
    ///     - 速度上限由代码 clamp（非 boost 时 = maxSpeed，boost 时 = maxSpeed * multiplier）
    ///     - 无输入时 Rigidbody2D.linearDrag 自然衰减（物理减速，无"刹车"代码）
    ///
    ///   朝向与移动分离：
    ///     - 移动方向由 WASD 决定（世界空间）
    ///     - 船头朝向由 ShipAiming（鼠标/右摇杆）独立控制
    ///     - 这正是 GG 鼠标模式的手感：WASD 移动 + 鼠标瞄准完全独立
    ///
    ///   Boost / Dash：
    ///     - 通过 AddExternalImpulse() 叠加冲量，方向分别为船头方向/输入方向
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(InputHandler))]
    public class ShipMotor : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        // ══════════════════════════════════════════════════════════════
        // Events
        // ══════════════════════════════════════════════════════════════

        /// <summary>每物理帧发送归一化速度 (0..1)，供 VFX / Audio 订阅。</summary>
        public event Action<float> OnSpeedChanged;

        // ══════════════════════════════════════════════════════════════
        // Public Properties
        // ══════════════════════════════════════════════════════════════

        public Vector2 CurrentVelocity  => _rb.linearVelocity;
        public float   CurrentSpeed     => _rb.linearVelocity.magnitude;
        public float   NormalizedSpeed  => _stats.MaxSpeed > 0f
            ? Mathf.Clamp01(_rb.linearVelocity.magnitude / _stats.MaxSpeed)
            : 0f;

        /// <summary>是否处于 Boost 状态（ShipBoost 写入）。</summary>
        public bool IsBoosting { get; private set; }

        /// <summary>
        /// 进入 Boost 物理状态。对应 GG StateData.Apply()：
        /// 临时替换 linearDrag / 速度上限 / 角加速度为 Boost 专属参数。
        /// </summary>
        public void EnterBoostState(float boostLinearDrag, float boostMaxSpeed)
        {
            IsBoosting = true;
            _boostMaxSpeed = boostMaxSpeed;
            _rb.linearDamping = boostLinearDrag;
        }

        /// <summary>
        /// 退出 Boost 物理状态，恢复正常参数。
        /// </summary>
        public void ExitBoostState()
        {
            IsBoosting = false;
            _boostMaxSpeed = 0f;
            _rb.linearDamping = _stats.LinearDrag;
        }

        // Boost 期间的速度上限（由 ShipBoost 写入）
        private float _boostMaxSpeed;

        // ── 向后兼容旧 ShipBoost 接口 ──
        /// <summary>向后兼容：Boost 速度倍率不再使用，由 BoostMaxSpeed 直接替代。</summary>
        public float BoostSpeedMultiplier { get; set; } = 1f;

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        private Rigidbody2D _rb;
        private InputHandler _inputHandler;
        private float _previousNormalizedSpeed;

        // 当前帧移动输入（由 Update 写，FixedUpdate 读，避免漏帧）
        private Vector2 _moveInputThisFrame;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _inputHandler = GetComponent<InputHandler>();
        }

        private void Start()
        {
            ApplyPhysicsSettings();
        }

        private void Update()
        {
            // 缓存输入向量给 FixedUpdate 使用（避免 Update/FixedUpdate 频率差异漏帧）
            _moveInputThisFrame = _inputHandler.MoveInput;
        }

        private void FixedUpdate()
        {
            ApplyThrust();
            ClampSpeed();
            EmitSpeedEvent();
        }

        // ══════════════════════════════════════════════════════════════
        // Physics Settings
        // ══════════════════════════════════════════════════════════════

        private void ApplyPhysicsSettings()
        {
            if (_stats == null) return;
            _rb.linearDamping  = _stats.LinearDrag;
            _rb.angularDamping = _stats.AngularDrag;
            _rb.gravityScale   = 0f;
        }

        // ══════════════════════════════════════════════════════════════
        // World-Space Thrust  (Twin-Stick: WASD → world direction)
        // ══════════════════════════════════════════════════════════════

        private void ApplyThrust()
        {
            if (_moveInputThisFrame.sqrMagnitude < 0.01f) return;

            // WASD 直接映射世界方向施力（W=世界上, D=世界右）
            // 输入向量已在 InputHandler 中 clamp 到 magnitude ≤ 1
            _rb.AddForce(_moveInputThisFrame * _stats.ForwardAcceleration, ForceMode2D.Force);
        }

        private void ClampSpeed()
        {
            float speedLimit = IsBoosting ? _boostMaxSpeed : _stats.MaxSpeed;
            float sqrLimit = speedLimit * speedLimit;

            if (_rb.linearVelocity.sqrMagnitude > sqrLimit)
                _rb.linearVelocity = _rb.linearVelocity.normalized * speedLimit;
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — External Impulse (Boost / Dash / 击退)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// 施加外部冲量（ForceMode2D.Impulse）。
        /// Boost 和 Dash 都通过此接口叠加速度，不干预正常移动逻辑。
        /// 对应 GGSteering.AddForce(force, Impulse)。
        /// </summary>
        public void AddExternalImpulse(Vector2 impulse)
        {
            _rb.AddForce(impulse, ForceMode2D.Impulse);
        }

        /// <summary>
        /// 直接设置速度（用于特殊场景，如剧情传送等）。
        /// 正常游戏中尽量用 AddExternalImpulse。
        /// </summary>
        public void SetVelocity(Vector2 velocity)
        {
            _rb.linearVelocity = velocity;
        }

        /// <summary>
        /// 向后兼容 ShipDash 旧接口。等同于 SetVelocity。
        /// </summary>
        public void SetVelocityOverride(Vector2 velocity) => SetVelocity(velocity);

        /// <summary>
        /// 向后兼容 ShipDash 旧接口。GG 模型中无需清除覆盖，空实现。
        /// </summary>
        public void ClearVelocityOverride() { }

        /// <summary>
        /// 向后兼容旧代码 ApplyImpulse。等同于 AddExternalImpulse。
        /// </summary>
        public void ApplyImpulse(Vector2 impulse) => AddExternalImpulse(impulse);

        // ══════════════════════════════════════════════════════════════
        // Speed Event
        // ══════════════════════════════════════════════════════════════

        private void EmitSpeedEvent()
        {
            float normalized = NormalizedSpeed;
            if (!Mathf.Approximately(normalized, _previousNormalizedSpeed))
            {
                OnSpeedChanged?.Invoke(normalized);
                _previousNormalizedSpeed = normalized;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Backward Compatibility
        // ══════════════════════════════════════════════════════════════

        /// <summary>向后兼容旧代码 IsDashing 标志（ShipDash 写入，ShipMotor 现在不依赖它）。</summary>
        public bool IsDashing { get; set; }
    }
}
