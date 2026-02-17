using ProjectArk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.SpaceLife
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _acceleration = 15f;
        [SerializeField] private float _deceleration = 20f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        private Rigidbody2D _rb;
        private InputAction _moveAction;
        private Vector2 _moveInput;
        private Vector2 _currentVelocity;

        public Vector2 MoveInput => _moveInput;
        public Vector2 CurrentVelocity => _currentVelocity;

        private void Awake()
        {
            ServiceLocator.Register(this);
            
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_inputActions == null)
            {
                Debug.LogWarning("[PlayerController2D] InputActions is NULL, trying to find it...");
#if UNITY_EDITOR
                TryFindInputActionAsset();
#endif
            }

            if (_inputActions != null)
            {
                var spaceLifeMap = _inputActions.FindActionMap("SpaceLife");
                if (spaceLifeMap != null)
                {
                    _moveAction = spaceLifeMap.FindAction("Move");
                }
            }
        }

#if UNITY_EDITOR
        private void TryFindInputActionAsset()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("ShipActions t:InputActionAsset");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset != null)
                {
                    _inputActions = asset;
                    Debug.Log($"[PlayerController2D] Auto-found InputActionAsset: {path}");
                    break;
                }
            }
        }
#endif

        private void OnEnable()
        {
            if (_inputActions == null) return;
            
            var spaceLifeMap = _inputActions.FindActionMap("SpaceLife");
            if (spaceLifeMap != null && !spaceLifeMap.enabled)
            {
                spaceLifeMap.Enable();
            }
            
            if (_moveAction != null)
                _moveAction.Enable();
        }

        private void OnDisable()
        {
            if (_moveAction != null && _inputActions != null)
            {
                _moveAction.Disable();
            }
            
            if (_inputActions != null)
            {
                var spaceLifeMap = _inputActions.FindActionMap("SpaceLife");
                if (spaceLifeMap != null && spaceLifeMap.enabled)
                {
                    spaceLifeMap.Disable();
                }
            }
        }

        private void Update()
        {
            ReadMovementInput();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        private void ReadMovementInput()
        {
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();

                if (_moveInput.sqrMagnitude > 1f)
                    _moveInput = _moveInput.normalized;
            }
        }

        private void ApplyMovement()
        {
            Vector2 targetVelocity = _moveInput * _moveSpeed;

            float accel = _moveInput.sqrMagnitude > 0.01f ? _acceleration : _deceleration;
            _currentVelocity = Vector2.MoveTowards(_currentVelocity, targetVelocity, accel * Time.fixedDeltaTime);
            _rb.linearVelocity = _currentVelocity;

            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            bool isMoving = _moveInput.sqrMagnitude > 0.01f;
            _animator.SetBool("IsMoving", isMoving);

            if (isMoving)
            {
                _animator.SetFloat("MoveX", _moveInput.x);
                _animator.SetFloat("MoveY", _moveInput.y);

                if (_spriteRenderer != null && Mathf.Abs(_moveInput.x) > 0.1f)
                {
                    _spriteRenderer.flipX = _moveInput.x < 0f;
                }
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }
    }
}
