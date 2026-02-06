using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Rotates the ship transform to face the aim target.
    /// Reads aim data from InputHandler.
    /// Uses MoveTowardsAngle for constant angular speed (not Slerp, which feels mushy).
    ///
    /// Convention: sprite "forward" = local +Y (up). Atan2 offset = -90°.
    /// </summary>
    [RequireComponent(typeof(InputHandler))]
    public class ShipAiming : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        /// <summary>
        /// Fired when aim angle changes. Degrees, 0 = right, 90 = up (Unity 2D convention).
        /// Subscribe for weapon firing direction, trail orientation, etc.
        /// </summary>
        public event Action<float> OnAimAngleChanged;

        /// <summary> Current facing direction as a normalized vector. </summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.up;

        /// <summary> Current Z rotation in degrees. </summary>
        public float AimAngle { get; private set; }

        private InputHandler _inputHandler;

        private void Awake()
        {
            _inputHandler = GetComponent<InputHandler>();
        }

        private void LateUpdate()
        {
            UpdateRotation();
        }

        private void UpdateRotation()
        {
            if (!_inputHandler.HasAimInput)
                return;

            Vector2 aimDir = _inputHandler.AimDirection;
            if (aimDir.sqrMagnitude < 0.001f)
                return;

            // Atan2 返回 +X 方向为 0°。Sprite 朝上(+Y)，所以减 90° 对齐。
            float targetAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;

            float newAngle;
            if (Mathf.Approximately(_stats.RotationSpeed, 0f))
            {
                // 即时吸附
                newAngle = targetAngle;
            }
            else
            {
                float currentAngle = transform.eulerAngles.z;
                float maxStep = _stats.RotationSpeed * Time.deltaTime;
                newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxStep);
            }

            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);

            AimAngle = newAngle;
            FacingDirection = transform.up;

            OnAimAngleChanged?.Invoke(AimAngle);
        }
    }
}
