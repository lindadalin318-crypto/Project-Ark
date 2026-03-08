using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// 飞船旋转控制，完全对齐 Galactic Glitch 的 GGSteering 角加速度模型：
    ///
    ///   旋转模型：
    ///     - 玩家输入"想往哪转" → 计算目标角度和角度差
    ///     - 用 angularAcceleration 逐帧修改 Rigidbody2D.angularVelocity
    ///     - 角速度 clamp 到 maxRotationSpeed
    ///     - 松开方向键后，angularDrag（在 ShipStatsSO 中配置）自然衰减转速
    ///
    ///   与旧 MoveTowardsAngle 模型的根本区别：
    ///     - 旧模型：直接计算目标角度，每帧 step 推过去（无惯性）
    ///     - 新模型：控制"角速度"，有加速和惯性，转向有重量感
    ///
    ///   输入映射（对应 GG 的 Twin-Stick 瞄准）：
    ///     - 鼠标模式：船头朝向鼠标，根据角度差决定旋转方向和强度
    ///     - 手柄模式：右摇杆直接决定目标朝向
    ///
    ///   注意：ProjectArk 是 Twin-Stick，朝向由瞄准方向（鼠标/右摇杆）决定，
    ///         而非 GG 那样由左摇杆控制转向。这是刻意保留的差异。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(InputHandler))]
    public class ShipAiming : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        // ══════════════════════════════════════════════════════════════
        // Events
        // ══════════════════════════════════════════════════════════════

        /// <summary>瞄准角度改变时触发（degrees，0 = right，90 = up）。</summary>
        public event Action<float> OnAimAngleChanged;

        // ══════════════════════════════════════════════════════════════
        // Public Properties
        // ══════════════════════════════════════════════════════════════

        /// <summary>当前船头朝向的归一化向量。</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.up;

        /// <summary>当前 Z 轴旋转角（degrees）。</summary>
        public float AimAngle { get; private set; }

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        private Rigidbody2D  _rb;
        private InputHandler  _inputHandler;
        private ShipMotor     _motor;
        private ShipStateController _stateController; // optional — null-safe

        // ══════════════════════════════════════════════════════════════
        // Runtime Parameters  (written by ShipStateData.Apply)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Runtime angular acceleration (deg/s²). Written by ShipStateData.Apply() on state transitions.
        /// Falls back to _stats.AngularAcceleration if ShipStateController is absent.
        /// </summary>
        public float RuntimeAngularAcceleration { get; set; } = 800f;

        /// <summary>
        /// Runtime max rotation speed (deg/s). Written by ShipStateData.Apply() on state transitions.
        /// Falls back to _stats.MaxRotationSpeed if ShipStateController is absent.
        /// </summary>
        public float RuntimeMaxRotationSpeed { get; set; } = 360f;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _rb              = GetComponent<Rigidbody2D>();
            _inputHandler    = GetComponent<InputHandler>();
            _motor           = GetComponent<ShipMotor>();
            _stateController = GetComponent<ShipStateController>(); // optional

            // Initialize runtime params from SO as fallback defaults
            if (_stats != null)
            {
                RuntimeAngularAcceleration = _stats.AngularAcceleration;
                RuntimeMaxRotationSpeed    = _stats.MaxRotationSpeed;
            }
        }

        private void FixedUpdate()
        {
            UpdateAngularVelocity();
            SyncFacingDirection();
        }

        // ══════════════════════════════════════════════════════════════
        // Angular Velocity Steering  (GGSteering.RotateTowardsAimTarget)
        // ══════════════════════════════════════════════════════════════

        private void UpdateAngularVelocity()
        {
            if (!_inputHandler.HasAimInput) return;

            Vector2 aimDir = _inputHandler.AimDirection;
            if (aimDir.sqrMagnitude < 0.001f) return;

            // 目标角度（sprite 朝上 = +Y，所以 atan2 减 90°）
            float targetAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;
            float currentAngle = transform.eulerAngles.z;

            // 计算最短角度差（-180 ~ +180）
            float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);

            // 当前状态的角加速度和最大角速度（由 ShipStateController 通过 ShipStateData.Apply 写入）
            float angularAccel  = RuntimeAngularAcceleration;
            float maxRotSpeed   = RuntimeMaxRotationSpeed;

            // 用角度差的符号决定加速方向，角度差越大越猛（但 clamp 避免过冲）
            // GGSteering 同款：靠近目标时自动减速（因为 angularDiff 会缩小）
            float desiredAngularVelocity = Mathf.Clamp(
                angleDiff * (angularAccel / maxRotSpeed),
                -maxRotSpeed,
                maxRotSpeed
            );

            // 以 angularAcceleration 向目标角速度靠近（平滑加速，不是瞬间到位）
            float maxDelta = angularAccel * Time.fixedDeltaTime;
            float newAngularVelocity = Mathf.MoveTowards(
                _rb.angularVelocity,
                desiredAngularVelocity,
                maxDelta
            );

            _rb.angularVelocity = newAngularVelocity;
        }

        private void SyncFacingDirection()
        {
            float angle = transform.eulerAngles.z;
            if (!Mathf.Approximately(angle, AimAngle))
            {
                AimAngle = angle;
                FacingDirection = transform.up;
                OnAimAngleChanged?.Invoke(AimAngle);
            }
        }
    }
}
