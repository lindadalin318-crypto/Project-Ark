using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// 飞船 Boost 系统 — 状态切换模型（完整对齐 Galactic Glitch 二进制分析结论）：
    ///
    ///   GG 真实架构（Player.prefab + GameAssembly.dll 分析确认）：
    ///     - Space 按下 → OnDodge → Dodge()：冲量 + IsDodgeState
    ///     - Dodge 同时触发 BoosterBurnoutPower.UsePower()（AfterDodge power）
    ///     - Player.Boost()：设置 isUsingBoost=true，施加 Boost 冲量
    ///     - Player.Update() 每帧检测 isUsingBoost → ToStateForce(IsBoostState=3)
    ///     - IsBoostState: linearDrag=2.5, maxMoveSpeed=9, angularAccel=40
    ///     - minTime=0.2s 后自动退回 IsBlueState
    ///
    ///   Project Ark 实现：
    ///     - Space → ShipDash 完成后自动调用 ForceActivate()（模拟 BoosterBurnoutPower 行为）
    ///     - OnBoostPressed（独立键）也可调用（可选，GG 中无此绑定）
    ///     - ForceActivate() 不检查冷却，直接进入 Boost（Dash 触发时绕过独立冷却）
    /// </summary>
    [RequireComponent(typeof(ShipMotor))]
    [RequireComponent(typeof(InputHandler))]
    public class ShipBoost : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        // ══════════════════════════════════════════════════════════════
        // Events
        // ══════════════════════════════════════════════════════════════

        /// <summary>进入 Boost 状态时触发。</summary>
        public event Action OnBoostStarted;

        /// <summary>退出 Boost 状态（恢复正常参数）时触发。</summary>
        public event Action OnBoostEnded;

        /// <summary>Boost 冷却完成时触发（可用于 UI 提示）。</summary>
        public event Action OnBoostReady;

        // ══════════════════════════════════════════════════════════════
        // Public State
        // ══════════════════════════════════════════════════════════════

        /// <summary>是否处于 Boost 状态（参数覆盖期间）。</summary>
        public bool IsBoosting { get; private set; }

        /// <summary>是否处于冷却中。</summary>
        public bool IsOnCooldown { get; private set; }

        /// <summary>冷却剩余时间（0~1 归一化，0 = 就绪，1 = 刚刚触发）。</summary>
        public float CooldownProgress => IsOnCooldown
            ? Mathf.Clamp01((_cooldownEndTime - Time.time) / Mathf.Max(0.01f, _stats.BoostCooldown))
            : 0f;

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        private ShipMotor    _motor;
        private InputHandler _inputHandler;
        private ShipAiming   _aiming;

        private float _cooldownEndTime;
        private bool  _buffered;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _motor        = GetComponent<ShipMotor>();
            _inputHandler = GetComponent<InputHandler>();
            _aiming       = GetComponent<ShipAiming>();
        }

        private void OnEnable()
        {
            _inputHandler.OnBoostPressed += HandleBoostInput;
        }

        private void OnDisable()
        {
            _inputHandler.OnBoostPressed -= HandleBoostInput;
        }

        private void Update()
        {
            if (!IsOnCooldown) return;
            if (Time.time < _cooldownEndTime) return;

            IsOnCooldown = false;
            OnBoostReady?.Invoke();

            if (_buffered)
            {
                _buffered = false;
                TryExecuteBoost();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Input Handling
        // ══════════════════════════════════════════════════════════════

        private void HandleBoostInput()
        {
            if (IsOnCooldown)
            {
                float remaining = _cooldownEndTime - Time.time;
                if (remaining <= _stats.BoostBufferWindow)
                    _buffered = true;
                return;
            }

            TryExecuteBoost();
        }

        private void TryExecuteBoost()
        {
            if (IsBoosting) return;
            ExecuteBoostAsync().Forget();
        }

        /// <summary>
        /// 由 ShipDash 在 Dodge 完成后调用，强制进入 Boost 状态（不检查冷却）。
        /// 对应 GG 的 BoosterBurnoutPower.UsePower() 在 AfterDodge 时触发的行为。
        /// </summary>
        public void ForceActivate()
        {
            if (IsBoosting) return;
            ExecuteBoostAsync().Forget();
        }

        // ══════════════════════════════════════════════════════════════
        // Boost Execution  (对应 GG StateData.Apply() → IsBoostState)
        // ══════════════════════════════════════════════════════════════

        private async UniTaskVoid ExecuteBoostAsync()
        {
            IsBoosting = true;

            // ── 1. 切换到 Boost 物理参数（对应 GG StateData.Apply()）
            //       linearDrag 2.5 → 阻力降低，飞船更难停下
            //       maxMoveSpeed 9 → 速度上限提升
            //       ShipAiming 会在 BoostAngularAcceleration 期间感知到 IsBoosting
            _motor.EnterBoostState(_stats.BoostLinearDrag, _stats.BoostMaxSpeed);

            // ── 2. 通知 VFX（引擎粒子增强等）
            OnBoostStarted?.Invoke();

            // ── 3. 持续 minTime（玩家在此期间加速可以突破正常速度上限）
            if (_stats.BoostDuration > 0f)
            {
                int ms = Mathf.RoundToInt(_stats.BoostDuration * 1000f);
                await UniTask.Delay(ms, cancellationToken: destroyCancellationToken);
            }

            // ── 4. 恢复正常参数（对应 GG StateData → IsBlueState）
            //       多余速度由 linearDrag 3 自然衰减（回到正常手感）
            _motor.ExitBoostState();
            IsBoosting = false;
            OnBoostEnded?.Invoke();

            // ── 5. 进入冷却
            IsOnCooldown   = true;
            _cooldownEndTime = Time.time + _stats.BoostCooldown;
        }
    }
}
