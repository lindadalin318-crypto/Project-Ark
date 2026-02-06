using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Reads input from the New Input System and exposes processed
    /// movement and aim data. Attach to the Ship GameObject.
    /// Pure input adapter — knows nothing about physics or ship logic.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;

        /// <summary> Normalized movement direction. Magnitude 0..1. </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary> World-space position the ship should aim toward. </summary>
        public Vector2 AimWorldPosition { get; private set; }

        /// <summary> Normalized direction from ship to aim target. </summary>
        public Vector2 AimDirection { get; private set; } = Vector2.up;

        /// <summary> True if the player is actively providing aim input. </summary>
        public bool HasAimInput { get; private set; }

        // 当输入设备切换时触发 (true = gamepad, false = keyboard+mouse)
        public event Action<bool> OnDeviceSwitched;

        private InputAction _moveAction;
        private InputAction _aimPositionAction;
        private InputAction _aimStickAction;
        private Camera _mainCamera;
        private bool _isUsingGamepad;

        private const float AIM_STICK_DEAD_ZONE = 0.1f;
        private const float GAMEPAD_AIM_PROJECTION_DISTANCE = 5f;
        private const float AIM_MIN_DISTANCE = 0.01f;

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindAnyObjectByType<Camera>();
                Debug.LogWarning("[InputHandler] Camera.main is null, fell back to FindAnyObjectByType.");
            }

            var shipMap = _inputActions.FindActionMap("Ship");
            _moveAction = shipMap.FindAction("Move");
            _aimPositionAction = shipMap.FindAction("AimPosition");
            _aimStickAction = shipMap.FindAction("AimStick");
        }

        private void OnEnable()
        {
            var shipMap = _inputActions.FindActionMap("Ship");
            shipMap.Enable();

            // 监听 performed 事件来自动检测设备切换
            _aimPositionAction.performed += OnMouseAimPerformed;
            _aimStickAction.performed += OnGamepadAimPerformed;
        }

        private void OnDisable()
        {
            _aimPositionAction.performed -= OnMouseAimPerformed;
            _aimStickAction.performed -= OnGamepadAimPerformed;

            var shipMap = _inputActions.FindActionMap("Ship");
            shipMap.Disable();
        }

        private void Update()
        {
            ReadMoveInput();

            if (_isUsingGamepad)
                UpdateAimFromGamepad();
            else
                UpdateAimFromMouse();
        }

        private void ReadMoveInput()
        {
            MoveInput = _moveAction.ReadValue<Vector2>();

            // 安全 clamp：模拟摇杆可能略微超过 1.0
            if (MoveInput.sqrMagnitude > 1f)
                MoveInput = MoveInput.normalized;
        }

        private void UpdateAimFromMouse()
        {
            Vector2 screenPos = _aimPositionAction.ReadValue<Vector2>();
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_mainCamera.transform.position.z));
            AimWorldPosition = (Vector2)worldPos;

            Vector2 toTarget = AimWorldPosition - (Vector2)transform.position;
            // 鼠标恰好在飞船上时，保持上一次方向
            if (toTarget.sqrMagnitude > AIM_MIN_DISTANCE * AIM_MIN_DISTANCE)
            {
                AimDirection = toTarget.normalized;
            }

            HasAimInput = true;
        }

        private void UpdateAimFromGamepad()
        {
            Vector2 stickValue = _aimStickAction.ReadValue<Vector2>();

            if (stickValue.sqrMagnitude > AIM_STICK_DEAD_ZONE * AIM_STICK_DEAD_ZONE)
            {
                AimDirection = stickValue.normalized;
                AimWorldPosition = (Vector2)transform.position + AimDirection * GAMEPAD_AIM_PROJECTION_DISTANCE;
                HasAimInput = true;
            }
            else
            {
                // 右摇杆回中时保持上一次瞄准方向，不重置
                HasAimInput = false;
            }
        }

        private void OnMouseAimPerformed(InputAction.CallbackContext ctx)
        {
            if (_isUsingGamepad)
            {
                _isUsingGamepad = false;
                OnDeviceSwitched?.Invoke(false);
            }
        }

        private void OnGamepadAimPerformed(InputAction.CallbackContext ctx)
        {
            if (!_isUsingGamepad)
            {
                _isUsingGamepad = true;
                OnDeviceSwitched?.Invoke(true);
            }
        }
    }
}
