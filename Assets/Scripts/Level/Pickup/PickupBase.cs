using UnityEngine;
using Cysharp.Threading.Tasks;
using PrimeTween;

namespace ProjectArk.Level
{
    /// <summary>
    /// Abstract base class for all pickups (keys, health orbs, heat vents, etc.).
    /// Handles player detection, auto vs interact pickup, bob animation,
    /// and pickup consume animation.
    /// 
    /// Subclasses implement OnPickedUp() for specific effects.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class PickupBase : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Pickup Behavior")]
        [Tooltip("If true, picked up automatically on contact. If false, requires Interact input.")]
        [SerializeField] private bool _autoPickup = true;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        [Header("Bob Animation")]
        [Tooltip("Enable floating bob animation.")]
        [SerializeField] private bool _enableBob = true;

        [Tooltip("Vertical bob amplitude (world units).")]
        [SerializeField] private float _bobAmplitude = 0.15f;

        [Tooltip("Bob cycle duration (seconds).")]
        [SerializeField] private float _bobDuration = 1.5f;

        [Header("Pickup Animation")]
        [Tooltip("Duration of the shrink-and-vanish animation on pickup.")]
        [SerializeField] private float _consumeAnimDuration = 0.3f;

        // ──────────────────── Runtime State ────────────────────

        private bool _consumed;
        private bool _playerInRange;
        private Vector3 _originalLocalPosition;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Whether this pickup has already been consumed. </summary>
        public bool IsConsumed => _consumed;

        // ──────────────────── Abstract ────────────────────

        /// <summary>
        /// Called when the player successfully picks up this item.
        /// Implement specific effects (add key, heal, etc.) in subclasses.
        /// </summary>
        protected abstract void OnPickedUp(GameObject player);

        // ──────────────────── Lifecycle ────────────────────

        protected virtual void Awake()
        {
            // Ensure trigger
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[PickupBase] {gameObject.name}: Collider was not set as trigger. Auto-fixed.");
            }

            _originalLocalPosition = transform.localPosition;
        }

        protected virtual void OnEnable()
        {
            _consumed = false;
            transform.localScale = Vector3.one;
            transform.localPosition = _originalLocalPosition;

            // Start bob animation
            if (_enableBob)
                StartBob();
        }

        // ──────────────────── Player Detection ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_consumed) return;
            if (!IsPlayerLayer(other.gameObject)) return;

            _playerInRange = true;

            if (_autoPickup)
            {
                Consume(other.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            _playerInRange = false;
        }

        private bool IsPlayerLayer(GameObject obj)
        {
            return (_playerLayer.value & (1 << obj.layer)) != 0;
        }

        // ──────────────────── Interact Pickup (non-auto) ────────────────────

        /// <summary>
        /// Call this from an external system (e.g. InputHandler.OnInteractPerformed)
        /// when the player presses Interact while in range.
        /// Only relevant when _autoPickup is false.
        /// </summary>
        public void TryInteractPickup(GameObject player)
        {
            if (_consumed || !_playerInRange || _autoPickup) return;
            Consume(player);
        }

        // ──────────────────── Consume ────────────────────

        private void Consume(GameObject player)
        {
            if (_consumed) return;
            _consumed = true;

            // Apply effect
            OnPickedUp(player);

            // Play consume animation then deactivate
            PlayConsumeAnimation().Forget();
        }

        private async UniTaskVoid PlayConsumeAnimation()
        {
            // Shrink to zero
            Tween.Scale(transform, Vector3.zero, _consumeAnimDuration, Ease.InBack);

            int delayMs = Mathf.RoundToInt(_consumeAnimDuration * 1000f);
            await UniTask.Delay(delayMs, cancellationToken: destroyCancellationToken);

            // Deactivate (or return to pool if pooled)
            gameObject.SetActive(false);
        }

        // ──────────────────── Bob Animation ────────────────────

        private void StartBob()
        {
            Tween.LocalPositionY(transform,
                startValue: _originalLocalPosition.y - _bobAmplitude,
                endValue: _originalLocalPosition.y + _bobAmplitude,
                duration: _bobDuration,
                ease: Ease.InOutSine,
                cycles: -1, // 无限循环
                cycleMode: CycleMode.Yoyo);
        }
    }
}
