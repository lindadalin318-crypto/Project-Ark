using UnityEngine;

namespace ProjectArk.Ship
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GGReplicaGlitchMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private GGReplicaGlitchView _view;
        [SerializeField, Min(0f)] private float _moveSpeed = 6f;
        [SerializeField, Min(1f)] private float _boostMultiplier = 2.15f;
        [SerializeField, Min(0f)] private float _dodgeSpeed = 18f;
        [SerializeField, Min(0f)] private float _dodgeDuration = 0.16f;

        private float _dodgeTimer;
        private Vector2 _lastMove = Vector2.right;

        public GGReplicaGlitchState CurrentState { get; private set; } = GGReplicaGlitchState.Idle;

        private void Awake()
        {
            if (_body == null) _body = GetComponent<Rigidbody2D>();
            if (_view == null) _view = GetComponent<GGReplicaGlitchView>();
            _body.gravityScale = 0f;
            _body.linearDamping = 4f;
        }

        private void FixedUpdate()
        {
            if (_dodgeTimer > 0f)
            {
                _dodgeTimer -= Time.fixedDeltaTime;
                if (_dodgeTimer <= 0f)
                {
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

            if (input.DodgePressed)
            {
                _dodgeTimer = _dodgeDuration;
                _body.linearVelocity = _lastMove * _dodgeSpeed;
                ApplyState(GGReplicaGlitchState.DodgeBurst);
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

            float speed = input.BoostHeld ? _moveSpeed * _boostMultiplier : _moveSpeed;
            _body.linearVelocity = move * speed;

            if (input.BoostHeld && move.sqrMagnitude > 0.001f)
            {
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

        private void ApplyState(GGReplicaGlitchState state)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            _view?.ApplyState(state);
        }
    }
}
