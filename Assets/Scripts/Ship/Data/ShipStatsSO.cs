using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Data asset for ship movement and aiming configuration.
    /// All tunable values live here — never hardcode in MonoBehaviours.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShipStats", menuName = "ProjectArk/Ship/Ship Stats")]
    public class ShipStatsSO : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Maximum movement speed in units/second.")]
        [SerializeField] private float _moveSpeed = 12f;

        [Tooltip("How quickly the ship reaches max speed (units/sec²). Higher = snappier.")]
        [SerializeField] private float _acceleration = 45f;

        [Tooltip("How quickly the ship slows down when no input (units/sec²). Lower = more slide.")]
        [SerializeField] private float _deceleration = 25f;

        [Header("Aiming")]
        [Tooltip("Rotation speed in degrees/second. 0 = instant snap to target.")]
        [SerializeField] private float _rotationSpeed = 720f;

        [Header("Survival")]
        [Tooltip("Maximum hit points for the ship.")]
        [SerializeField] private float _maxHP = 100f;

        [Tooltip("Duration of the white flash when the ship takes damage (seconds).")]
        [SerializeField] private float _hitFlashDuration = 0.1f;

        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float RotationSpeed => _rotationSpeed;
        public float MaxHP => _maxHP;
        public float HitFlashDuration => _hitFlashDuration;
    }
}
