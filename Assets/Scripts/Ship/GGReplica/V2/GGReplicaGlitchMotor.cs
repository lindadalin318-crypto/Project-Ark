using UnityEngine;

namespace ProjectArk.Ship
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GGReplicaGlitchMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private GGReplicaGlitchView _view;
        [SerializeField] private GGReplicaGlitchAudioFeedback _audioFeedback;
        [SerializeField] private GGReplicaShipFeelProfileSO _feelProfile;
        [SerializeField, Min(0f)] private float _moveSpeed = 6f;
        [SerializeField, Min(0f)] private float _angularAcceleration = 800f;
        [SerializeField, Min(0f)] private float _maxRotationSpeed = 360f;

        private float _dodgeTimer;
        private float _baseLinearDamping;
        private Vector2 _lastMove = Vector2.right;
        private Vector2 _lastAimDirection = Vector2.up;

        public GGReplicaGlitchState CurrentState { get; private set; } = GGReplicaGlitchState.Idle;

        private void Awake()
        {
            if (_body == null) _body = GetComponent<Rigidbody2D>();
            if (_view == null) _view = GetComponent<GGReplicaGlitchView>();
            if (_audioFeedback == null) _audioFeedback = GetComponent<GGReplicaGlitchAudioFeedback>();
            _body.gravityScale = 0f;
            _body.linearDamping = 4f;
            _baseLinearDamping = _body.linearDamping;
        }

        private void FixedUpdate()
        {
            UpdateRotation(Time.fixedDeltaTime);

            if (_dodgeTimer > 0f)
            {
                _dodgeTimer -= Time.fixedDeltaTime;
                if (_dodgeTimer <= 0f)
                {
                    _body.linearDamping = _baseLinearDamping;
                    ApplyState(_body.linearVelocity.sqrMagnitude > 0.01f ? GGReplicaGlitchState.Move : GGReplicaGlitchState.Idle);
                }
            }
        }

        public void ApplyInput(GGReplicaGlitchInputFrame input, float deltaTime)
        {
            if (_body == null) _body = GetComponent<Rigidbody2D>();

            Vector2 move = input.Move;
            if (move.sqrMagnitude > 0.001f)
            {
                _lastMove = move.normalized;
            }

            if (input.AimDirection.sqrMagnitude > 0.001f)
            {
                _lastAimDirection = input.AimDirection.normalized;
            }

            bool wantsBoost = input.BoostHeld && move.sqrMagnitude > 0.001f;
            bool leavingBoost = CurrentState == GGReplicaGlitchState.BoostHold && (!wantsBoost || input.DodgePressed || input.GrabHeld || input.HealHeld || input.FireHeld);
            if (leavingBoost)
            {
                _body.linearDamping = _baseLinearDamping;
            }

            if (input.DodgePressed)
            {
                _dodgeTimer = DodgeStateDuration;
                _body.linearDamping = DodgeLinearDamping;
                _body.linearVelocity = _lastMove * DodgeForce;
                ApplyState(GGReplicaGlitchState.DodgeBurst, true);
                return;
            }

            if (_dodgeTimer > 0f) return;

            if (input.GrabHeld)
            {
                _body.linearVelocity = Vector2.Lerp(_body.linearVelocity, move * _moveSpeed * 0.45f, Mathf.Clamp01(deltaTime * 10f));
                ApplyState(GGReplicaGlitchState.GrabHold);
                return;
            }

            if (input.HealHeld)
            {
                _body.linearVelocity = Vector2.Lerp(_body.linearVelocity, Vector2.zero, Mathf.Clamp01(deltaTime * 10f));
                ApplyState(GGReplicaGlitchState.Heal);
                return;
            }

            if (input.FireHeld)
            {
                _body.linearVelocity = Vector2.Lerp(_body.linearVelocity, move * _moveSpeed * 0.6f, Mathf.Clamp01(deltaTime * 10f));
                ApplyState(GGReplicaGlitchState.FireAim);
                return;
            }

            float speed = wantsBoost ? _moveSpeed * BoostSpeedMultiplier : _moveSpeed;
            _body.linearVelocity = move * speed;

            if (wantsBoost)
            {
                if (CurrentState != GGReplicaGlitchState.BoostHold)
                {
                    _body.linearVelocity += _lastMove * BoostStartImpulse;
                    _body.linearDamping = AfterBoostDrag;
                }

                ApplyState(GGReplicaGlitchState.BoostHold);
            }
            else if (move.sqrMagnitude > 0.001f)
            {
                ApplyState(GGReplicaGlitchState.Move);
            }
            else
            {
                ApplyState(GGReplicaGlitchState.Idle);
            }
        }

        private float DodgeForce => _feelProfile != null ? _feelProfile.DodgeForce : 13f;

        private float DodgeStateDuration => _feelProfile != null ? _feelProfile.DodgeStateDuration : 0.225f;

        private float DodgeLinearDamping => _feelProfile != null ? _feelProfile.DodgeLinearDamping : 1.7f;

        private float BoostSpeedMultiplier => _feelProfile != null ? _feelProfile.BoostSpeedMultiplier : 1.2f;

        private float BoostStartImpulse => _feelProfile != null ? _feelProfile.BoostStartImpulse : 4f;

        private float AfterBoostDrag => _feelProfile != null ? _feelProfile.AfterBoostDrag : 2.5f;

        private void UpdateRotation(float deltaTime)
        {
            if (_body == null || _lastAimDirection.sqrMagnitude < 0.001f) return;

            float targetAngle = Mathf.Atan2(_lastAimDirection.y, _lastAimDirection.x) * Mathf.Rad2Deg - 90f;
            float currentAngle = _body.rotation;
            float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);
            float maxRotationSpeed = Mathf.Max(1f, _maxRotationSpeed);
            float desiredAngularVelocity = Mathf.Clamp(angleDiff * (_angularAcceleration / maxRotationSpeed), -maxRotationSpeed, maxRotationSpeed);
            float maxDelta = _angularAcceleration * deltaTime;
            float angularVelocity = Mathf.MoveTowards(_body.angularVelocity, desiredAngularVelocity, maxDelta);
            float nextAngle = currentAngle + angularVelocity * deltaTime;
            _body.angularVelocity = angularVelocity;
            _body.MoveRotation(nextAngle);
            _body.rotation = nextAngle;
        }

        private void ApplyState(GGReplicaGlitchState state, bool forceReenter = false)
        {
            if (CurrentState == state && !forceReenter) return;
            CurrentState = state;
            _view?.ApplyState(state, forceReenter);
            _audioFeedback?.ApplyState(state);
        }
    }
}
