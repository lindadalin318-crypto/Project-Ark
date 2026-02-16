
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpForce = 8f;
        [SerializeField] private float _groundCheckDistance = 0.5f;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private bool _isGrounded;
        private bool _jumpRequested;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            HandleInput();
            CheckGrounded();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
            ApplyJump();
        }

        private void HandleInput()
        {
            float horizontal = Input.GetAxis("Horizontal");
            _moveInput = new Vector2(horizontal, 0f);

            if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) && _isGrounded)
            {
                _jumpRequested = true;
            }
        }

        private void CheckGrounded()
        {
            _isGrounded = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                _groundCheckDistance,
                _groundLayer
            );
        }

        private void ApplyMovement()
        {
            Vector2 velocity = _rb.linearVelocity;
            velocity.x = _moveInput.x * _moveSpeed;
            _rb.linearVelocity = velocity;

            if (_moveInput.x != 0f && _spriteRenderer != null)
            {
                _spriteRenderer.flipX = _moveInput.x < 0f;
            }

            UpdateAnimation();
        }

        private void ApplyJump()
        {
            if (_jumpRequested && _isGrounded)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
                _jumpRequested = false;
            }
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            bool isMoving = Mathf.Abs(_moveInput.x) > 0.1f;
            _animator.SetBool("IsMoving", isMoving);
            _animator.SetBool("IsGrounded", _isGrounded);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * _groundCheckDistance);
        }
    }
}

