using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Handles ship physics-based movement using Rigidbody2D.
    /// Reads input from InputHandler. Does NOT handle rotation (that's ShipAiming).
    ///
    /// Movement model: direct velocity control with asymmetric acceleration/deceleration.
    /// Acceleration is faster than deceleration, giving a "quick start, gradual slide" feel.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(InputHandler))]
    public class ShipMotor : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        /// <summary>
        /// Fired each physics frame with normalized speed (0..1).
        /// Subscribe for VFX intensity, audio pitch, etc.
        /// </summary>
        public event Action<float> OnSpeedChanged;

        public Vector2 CurrentVelocity => _rigidbody.linearVelocity;
        public float CurrentSpeed => _rigidbody.linearVelocity.magnitude;
        public float NormalizedSpeed => _stats.MoveSpeed > 0f
            ? Mathf.Clamp01(_rigidbody.linearVelocity.magnitude / _stats.MoveSpeed)
            : 0f;

        private Rigidbody2D _rigidbody;
        private InputHandler _inputHandler;
        private float _previousNormalizedSpeed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _inputHandler = GetComponent<InputHandler>();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            EmitSpeedEvent();
        }

        private void HandleMovement()
        {
            Vector2 input = _inputHandler.MoveInput;
            Vector2 currentVel = _rigidbody.linearVelocity;

            if (input.sqrMagnitude > 0.001f)
            {
                // 加速：向目标速度逼近
                Vector2 desiredVelocity = input * _stats.MoveSpeed;
                Vector2 velocityDiff = desiredVelocity - currentVel;
                float maxStep = _stats.Acceleration * Time.fixedDeltaTime;

                if (velocityDiff.sqrMagnitude <= maxStep * maxStep)
                {
                    // 一步即达，不超调
                    _rigidbody.linearVelocity = desiredVelocity;
                }
                else
                {
                    _rigidbody.linearVelocity = currentVel + velocityDiff.normalized * maxStep;
                }
            }
            else
            {
                // 减速：线性衰减，保持运动方向（惯性滑行）
                float speed = currentVel.magnitude;
                float decelStep = _stats.Deceleration * Time.fixedDeltaTime;

                if (speed <= decelStep)
                {
                    _rigidbody.linearVelocity = Vector2.zero;
                }
                else
                {
                    _rigidbody.linearVelocity = currentVel.normalized * (speed - decelStep);
                }
            }

            // 最终安全限速
            if (_rigidbody.linearVelocity.sqrMagnitude > _stats.MoveSpeed * _stats.MoveSpeed)
            {
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _stats.MoveSpeed;
            }
        }

        private void EmitSpeedEvent()
        {
            float normalized = NormalizedSpeed;
            if (!Mathf.Approximately(normalized, _previousNormalizedSpeed))
            {
                OnSpeedChanged?.Invoke(normalized);
                _previousNormalizedSpeed = normalized;
            }
        }
    }
}
