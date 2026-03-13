using PrimeTween;
using ProjectArk.Combat;
using ProjectArk.Combat.Enemy;
using ProjectArk.Core;
using ProjectArk.Core.Audio;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Camera director trigger.
    /// When the player enters the trigger zone, temporarily adjusts camera parameters.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CameraTrigger : MonoBehaviour
    {
        [Header("Priority")]
        [Tooltip("Priority for trigger arbitration. Higher numbers win. Same priority falls back to last-entered.")]
        [SerializeField] private int _priority;

        [Header("Zoom")]
        [Tooltip("Target orthographic size while in this zone. Set to 0 for no zoom override.")]
        [SerializeField] private float _targetOrthoSize;

        [Header("Focus Override")]
        [Tooltip("Optional target to follow while in this zone. Null = keep following the default player target.")]
        [SerializeField] private Transform _lookAtOverride;

        [Header("Position Lock")]
        [Tooltip("Optional static anchor that the camera should lock/follow while in this zone.")]
        [SerializeField] private Transform _positionLock;

        [Header("Transition")]
        [Tooltip("Duration (seconds) to blend camera parameters.")]
        [SerializeField] private float _transitionDuration = 0.5f;

        [Tooltip("Easing curve for the transition.")]
        [SerializeField] private Ease _ease = Ease.InOutSine;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship. If left empty, falls back to Player tag matching.")]
        [SerializeField] private LayerMask _playerLayer;

        [Header("Effects")]
        [Tooltip("Sound effect played when entering this zone.")]
        [SerializeField] private AudioClip _enterSFX;

        [Tooltip("If true, clear active projectiles when entering this zone.")]
        [SerializeField] private bool _clearProjectilesOnEnter;

        [Header("References")]
        [Tooltip("CameraDirector in the scene. If not assigned, resolved from ServiceLocator at runtime.")]
        [SerializeField] private CameraDirector _director;

        private bool _playerInZone;
        private bool _isActiveOnStack;

        public int Priority => _priority;
        public float TargetOrthoSize => _targetOrthoSize;
        public Transform LookAtOverride => _lookAtOverride;
        public Transform PositionLock => _positionLock;
        public float TransitionDuration => _transitionDuration;
        public Ease TransitionEase => _ease;

        private void Awake()
        {
            var boxCollider = GetComponent<BoxCollider2D>();
            if (!boxCollider.isTrigger)
            {
                boxCollider.isTrigger = true;
                Debug.LogWarning($"[CameraTrigger] {gameObject.name}: BoxCollider2D was not set as trigger. Auto-fixed.");
            }

            ResolveDirector();
        }

        private void OnDisable()
        {
            if (_isActiveOnStack && _director != null)
            {
                _director.PopTrigger(this);
                _isActiveOnStack = false;
            }

            _playerInZone = false;
        }

        private void OnDestroy()
        {
            if (_isActiveOnStack && _director != null)
            {
                _director.PopTrigger(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other.gameObject) || _playerInZone)
            {
                return;
            }

            ResolveDirector();
            _playerInZone = true;

            if (_enterSFX != null)
            {
                var audio = ServiceLocator.TryGet<AudioManager>();
                audio?.PlaySFX2D(_enterSFX);
            }

            if (_clearProjectilesOnEnter)
            {
                ClearAllProjectiles();
            }

            if (_director == null)
            {
                Debug.LogWarning($"[CameraTrigger] {gameObject.name}: CameraDirector not found. Trigger ignored.");
                return;
            }

            _director.PushTrigger(this);
            _isActiveOnStack = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other.gameObject) || !_playerInZone)
            {
                return;
            }

            _playerInZone = false;

            if (_isActiveOnStack && _director != null)
            {
                _director.PopTrigger(this);
                _isActiveOnStack = false;
            }
        }

        public void ApplyToCamera(CameraDirector director)
        {
            if (director == null || director.VCam == null)
            {
                return;
            }

            if (_targetOrthoSize > 0f)
            {
                director.SetOrthoSize(_targetOrthoSize, _transitionDuration, _ease);
            }
            else
            {
                director.RestoreOrthoSize(_transitionDuration);
            }

            if (_positionLock != null)
            {
                director.SetMode(CameraMode.LOCKED, 0f);
                director.SetFollowTarget(_positionLock, _transitionDuration);
                director.ClearLookAtTarget();
            }
            else if (_lookAtOverride != null)
            {
                director.SetMode(CameraMode.FOLLOWING, 0f);
                director.SetFollowTarget(_lookAtOverride, _transitionDuration);
                director.ClearLookAtTarget();
            }
            else
            {
                director.SetMode(CameraMode.FOLLOWING, 0f);
                director.RestoreFollowTarget();
                director.ClearLookAtTarget();
            }
        }

        private void ResolveDirector()
        {
            if (_director == null)
            {
                _director = ServiceLocator.TryGet<CameraDirector>();
            }
        }

        private bool IsPlayer(GameObject obj)
        {
            if (_playerLayer.value != 0)
            {
                return (_playerLayer.value & (1 << obj.layer)) != 0;
            }

            return obj.CompareTag("Player");
        }

        private void ClearAllProjectiles()
        {
            int cleared = 0;
            cleared += ReturnAllToPool<Projectile>();
            cleared += ReturnAllToPool<LaserBeam>();
            cleared += ReturnAllToPool<EchoWave>();
            cleared += ReturnAllToPool<EnemyProjectile>();
            cleared += ReturnAllToPool<EnemyLaserBeam>();

            if (cleared > 0)
            {
                Debug.Log($"[CameraTrigger] {gameObject.name}: Cleared {cleared} pooled projectile objects.");
            }
        }

        private static int ReturnAllToPool<T>() where T : Component
        {
            var items = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            int count = 0;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    continue;
                }

                var poolRef = items[i].GetComponent<PoolReference>();
                if (poolRef == null)
                {
                    continue;
                }

                poolRef.ReturnToPool();
                count++;
            }

            return count;
        }
    }
}
