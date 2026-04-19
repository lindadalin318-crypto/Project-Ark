using System;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// A destroyable environmental object that implements <see cref="IDamageable"/>.
    /// When destroyed, writes a persistent flag via <see cref="RoomFlagRegistry"/> so
    /// the object stays destroyed across room re-entries and save/load.
    ///
    /// Place under the <c>Elements/</c> child of a Room GameObject.
    /// Naming convention: <c>Destroyable_{flagKey}</c> (e.g., Destroyable_CrystalWall_01).
    ///
    /// The flag key is derived from <see cref="_flagKey"/> (Inspector) or falls back to
    /// <c>gameObject.name</c>. The room ID is read from the parent <see cref="Room"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DestroyableObject : MonoBehaviour, IDamageable
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Destroyable")]
        [Tooltip("Max HP before destruction.")]
        [SerializeField] private float _maxHP = 1f;

        [Tooltip("Persistent flag key within the parent room. If empty, uses gameObject.name.")]
        [SerializeField] private string _flagKey;

        [Header("Visuals")]
        [Tooltip("Sprite shown when destroyed. If null, the GameObject is simply deactivated.")]
        [SerializeField] private Sprite _destroyedSprite;

        [Tooltip("Optional particle effect to play on destruction.")]
        [SerializeField] private ParticleSystem _destroyVFX;

        [Tooltip("Optional destruction SFX.")]
        [SerializeField] private AudioClip _destroySFX;

        // ──────────────────── Runtime State ────────────────────

        private float _currentHP;
        private bool _isDestroyed;
        private string _resolvedFlagKey;
        private string _ownerRoomID;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;

        // ──────────────────── Events ────────────────────

        /// <summary>
        /// Raised exactly once when this object is destroyed by runtime damage.
        /// Persistence restore paths do not rebroadcast the event.
        /// </summary>
        public event Action OnDestroyed;

        // ──────────────────── IDamageable ────────────────────

        /// <inheritdoc/>
        public bool IsAlive => !_isDestroyed;

        /// <summary>
        /// Whether the object has already entered its destroyed state.
        /// </summary>
        public bool IsDestroyed => _isDestroyed;

        /// <inheritdoc/>
        public void TakeDamage(DamagePayload payload)
        {
            if (_isDestroyed) return;

            _currentHP -= payload.BaseDamage;

            if (_currentHP <= 0f)
            {
                Die();
            }
        }

        /// <inheritdoc/>
        [Obsolete("Use TakeDamage(DamagePayload) instead")]
        public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
        {
            TakeDamage(new DamagePayload(damage, knockbackDirection, knockbackForce));
        }

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _currentHP = _maxHP;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();

            // Resolve flag key
            _resolvedFlagKey = string.IsNullOrEmpty(_flagKey) ? gameObject.name : _flagKey;

            // Resolve owner room ID
            var room = GetComponentInParent<Room>();
            if (room != null)
            {
                _ownerRoomID = room.RoomID;
            }
            else
            {
                Debug.LogError($"[DestroyableObject] {gameObject.name}: No parent Room found! DestroyableObject must be a child of a Room GameObject.");
            }
        }

        private void Start()
        {
            // Check if already destroyed from persistent state
            if (!string.IsNullOrEmpty(_ownerRoomID))
            {
                var registry = ServiceLocator.Get<RoomFlagRegistry>();
                if (registry != null && registry.GetFlag(_ownerRoomID, _resolvedFlagKey))
                {
                    // Already destroyed in a previous visit — apply destroyed visual immediately.
                    ApplyDestroyedState(playEffects: false, raiseDestroyedEvent: false);
                }
            }
        }

        // ──────────────────── Death ────────────────────

        private void Die()
        {
            if (_isDestroyed) return;

            ApplyDestroyedState(playEffects: true, raiseDestroyedEvent: true);

            // Persist destruction via RoomFlagRegistry
            if (!string.IsNullOrEmpty(_ownerRoomID))
            {
                var registry = ServiceLocator.Get<RoomFlagRegistry>();
                if (registry != null)
                {
                    registry.SetFlag(_ownerRoomID, _resolvedFlagKey, true);
                }
                else
                {
                    Debug.LogWarning($"[DestroyableObject] {gameObject.name}: RoomFlagRegistry not found. Destruction will not persist.");
                }
            }
        }

        private void ApplyDestroyedState(bool playEffects, bool raiseDestroyedEvent)
        {
            if (_isDestroyed)
            {
                return;
            }

            _isDestroyed = true;
            _currentHP = 0f;

            if (playEffects)
            {
                // VFX
                if (_destroyVFX != null)
                {
                    _destroyVFX.Play();
                }

                // SFX
                if (_destroySFX != null)
                {
                    var audio = ServiceLocator.Get<Core.Audio.AudioManager>();
                    if (audio != null)
                    {
                        audio.PlaySFX2D(_destroySFX);
                    }
                }
            }

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // Visual: swap to destroyed sprite or hide the intact renderer.
            if (_destroyedSprite != null && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = _destroyedSprite;
                _spriteRenderer.enabled = true;
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }

            if (raiseDestroyedEvent)
            {
                OnDestroyed?.Invoke();
            }
        }
    }
}
