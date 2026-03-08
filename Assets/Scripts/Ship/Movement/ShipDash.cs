using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// 飞船闪避 Dash，完整对齐 Galactic Glitch Space 键机制（二进制分析确认）：
    ///
    ///   GG 真实流程（GameAssembly.dll 反汇编确认）：
    ///     1. Space → OnDodge → Player.Dodge()：施加 dodgeForce=13 冲量 + ToStateForce(IsDodgeState)
    ///        IsDodgeState: linearDrag=1.7, 持续 minTime=0.225s，无敌帧 0.15s
    ///     2. Space 同时触发 BoosterBurnoutPower（AfterDodge power）→ Player.Boost()：
    ///        设置 isUsingBoost=true + 施加 Boost 方向冲量
    ///     3. Player.Update() 每帧检测 isUsingBoost → ToStateForce(IsBoostState=3)：
    ///        IsBoostState: linearDrag=2.5, maxMoveSpeed=9, angularAccel=40
    ///
    ///   Project Ark 实现：
    ///     - Space → ShipDash：冲量 + 无敌帧（对应步骤1）
    ///     - ShipDash 完成后自动触发 ShipBoost.ForceActivate()（对应步骤2+3）
    ///     - ShipBoost 独立键位（可选绑定）也支持单独触发
    /// </summary>
    [RequireComponent(typeof(ShipMotor))]
    [RequireComponent(typeof(InputHandler))]
    public class ShipDash : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        // ══════════════════════════════════════════════════════════════
        // Events
        // ══════════════════════════════════════════════════════════════

        /// <summary>Dash 开始时触发，携带冲刺方向。</summary>
        public event Action<Vector2> OnDashStarted;

        /// <summary>Dash 无敌帧结束时触发（可视为 Dash 的逻辑结束点）。</summary>
        public event Action OnDashEnded;

        // ══════════════════════════════════════════════════════════════
        // Public State
        // ══════════════════════════════════════════════════════════════

        /// <summary>是否在无敌帧期间（可用于 VFX / 受伤判定）。</summary>
        public bool IsDashing { get; private set; }

        /// <summary>当前 Dash 方向（Dash 期间有效）。</summary>
        public Vector2 DashDirection { get; private set; }

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        private ShipMotor    _motor;
        private InputHandler  _inputHandler;
        private ShipAiming    _aiming;
        private ShipHealth    _health;
        private ShipBoost     _boost; // Dash 结束后自动触发 Boost（GG 同款链式触发）
        private ShipStateController _stateController; // optional — null-safe

        private bool  _isCoolingDown;
        private float _cooldownEndTime;
        private bool  _buffered; // 简单单次缓冲（替代旧 InputBuffer）

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _motor           = GetComponent<ShipMotor>();
            _inputHandler    = GetComponent<InputHandler>();
            _aiming          = GetComponent<ShipAiming>();
            _health          = GetComponent<ShipHealth>();
            _boost           = GetComponent<ShipBoost>(); // 可选，无 ShipBoost 时退化为纯 Dash
            _stateController = GetComponent<ShipStateController>(); // optional
        }

        private void OnEnable()
        {
            _inputHandler.OnDashPressed += HandleDashInput;
        }

        private void OnDisable()
        {
            _inputHandler.OnDashPressed -= HandleDashInput;
        }

        private void Update()
        {
            if (!_isCoolingDown) return;
            if (Time.time < _cooldownEndTime) return;

            _isCoolingDown = false;

            // 消费缓冲输入
            if (_buffered)
            {
                _buffered = false;
                TryExecuteDash();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Input Handling
        // ══════════════════════════════════════════════════════════════

        private void HandleDashInput()
        {
            if (_isCoolingDown)
            {
                // 在缓冲窗口内按键 → 记录缓冲
                float remainingCooldown = _cooldownEndTime - Time.time;
                if (remainingCooldown <= _stats.DashBufferWindow)
                    _buffered = true;
                return;
            }

            TryExecuteDash();
        }

        private void TryExecuteDash()
        {
            if (IsDashing) return;
            ExecuteDashAsync().Forget();
        }

        // ══════════════════════════════════════════════════════════════
        // Dash Execution
        // ══════════════════════════════════════════════════════════════

        private async UniTaskVoid ExecuteDashAsync()
        {
            // ── 1. 确定冲刺方向（GG 同款：优先移动输入，无输入则船头方向）
            Vector2 moveInput = _inputHandler.MoveInput;
            Vector2 dashDir = moveInput.sqrMagnitude > 0.1f
                ? moveInput.normalized
                : (_aiming != null ? _aiming.FacingDirection : (Vector2)transform.up);

            DashDirection = dashDir;

            // ── 2. 施加冲量（一次性，物理自然衰减）
            _motor.AddExternalImpulse(dashDir * _stats.DashImpulse);

            // ── 3. 切换到 Dash 状态（包含禁用碰撞体 + 应用 Dash 物理参数）
            IsDashing = true;
            _motor.IsDashing = true;

            if (_stateController != null)
                _stateController.ToStateForce(ShipShipState.Dash);
            else if (_stats.DashIFrames && _health != null)
                _health.SetInvulnerable(true); // Legacy: 直接开启无敌帧

            // ── 4. 通知 VFX
            OnDashStarted?.Invoke(dashDir);

            // ── 5. 等待无敌帧时长（冲量本身由物理处理，无需等速度降下来）
            if (_stats.DashIFrameDuration > 0f)
            {
                int ms = Mathf.RoundToInt(_stats.DashIFrameDuration * 1000f);
                await UniTask.Delay(ms, cancellationToken: destroyCancellationToken);
            }

            // ── 6. 退出 Dash 状态
            if (_stateController != null)
                _stateController.ToStateForce(ShipShipState.Normal);
            else if (_stats.DashIFrames && _health != null)
                _health.SetInvulnerable(false); // Legacy

            IsDashing = false;
            _motor.IsDashing = false;
            OnDashEnded?.Invoke();

            // ── 7. 触发 Boost 状态（GG 真实机制：Dodge 结束后自动进入 IsBoostState）
            //       对应 BoosterBurnoutPower.UsePower() 在 AfterDodge 时被调用
            _boost?.ForceActivate();

            // ── 8. 进入冷却
            _isCoolingDown   = true;
            _cooldownEndTime = Time.time + _stats.DashCooldown;
        }
    }
}
