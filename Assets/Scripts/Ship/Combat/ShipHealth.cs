using System;
using System.Collections;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Player ship health component. Implements IDamageable so enemy attacks
    /// (and any other damage sources) can interact with the ship.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShipHealth : MonoBehaviour, IDamageable
    {
        // ──────────────────── Inspector ────────────────────
        [Header("Data")]
        [SerializeField] private ShipStatsSO _stats;

        // ──────────────────── Runtime State ────────────────────
        private float _currentHP;
        private bool _isDead;

        // ──────────────────── Cached Components ────────────────────
        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private InputHandler _inputHandler;

        // ──────────────────── Events ────────────────────
        /// <summary> Fired when the ship takes damage. (damage, currentHP) </summary>
        public event Action<float, float> OnDamageTaken;

        /// <summary> Fired when the ship dies (HP reaches 0). </summary>
        public event Action OnDeath;

        // ──────────────────── Public Properties ────────────────────
        /// <summary> Current hit points. </summary>
        public float CurrentHP => _currentHP;

        /// <summary> Maximum hit points from stats. </summary>
        public float MaxHP => _stats != null ? _stats.MaxHP : 100f;

        /// <inheritdoc/>
        public bool IsAlive => !_isDead;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _inputHandler = GetComponent<InputHandler>();

            InitializeHP();
        }

        private void InitializeHP()
        {
            if (_stats == null)
            {
                Debug.LogError($"[ShipHealth] {gameObject.name} has no ShipStatsSO assigned!");
                _currentHP = 100f;
                return;
            }

            _currentHP = _stats.MaxHP;
            _isDead = false;
        }

        // ──────────────────── IDamageable ────────────────────

        /// <inheritdoc/>
        public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
        {
            if (_isDead) return;

            // Apply damage
            _currentHP -= damage;

            // Apply knockback impulse
            if (knockbackForce > 0f && _rigidbody != null)
                _rigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);

            // Hit flash feedback
            if (_spriteRenderer != null)
                StartCoroutine(HitFlashCoroutine());

            // Notify listeners
            OnDamageTaken?.Invoke(damage, _currentHP);

            Debug.Log($"[ShipHealth] Took {damage} damage. HP: {_currentHP}/{MaxHP}");

            // Death check
            if (_currentHP <= 0f)
                Die();
        }

        // ──────────────────── Death ────────────────────

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            _currentHP = 0f;

            // Disable player input to stop ship control
            if (_inputHandler != null)
                _inputHandler.enabled = false;

            Debug.Log("[ShipHealth] Ship destroyed!");

            // Notify listeners
            OnDeath?.Invoke();

            // NOTE: Game Over / Respawn logic is NOT implemented here.
            // Other systems should subscribe to OnDeath and handle accordingly.
        }

        // ──────────────────── Visual Feedback ────────────────────

        private IEnumerator HitFlashCoroutine()
        {
            Color originalColor = _spriteRenderer.color;

            // Flash to white
            _spriteRenderer.color = Color.white;

            float duration = _stats != null ? _stats.HitFlashDuration : 0.1f;
            yield return new WaitForSeconds(duration);

            // Restore original color (if still alive)
            if (!_isDead && _spriteRenderer != null)
                _spriteRenderer.color = originalColor;
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Reset health to maximum. Useful for respawn or level restart.
        /// </summary>
        public void ResetHealth()
        {
            InitializeHP();

            // Re-enable input if it was disabled
            if (_inputHandler != null)
                _inputHandler.enabled = true;
        }
    }
}
