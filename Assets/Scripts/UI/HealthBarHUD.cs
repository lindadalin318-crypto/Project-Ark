using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Ship;

namespace ProjectArk.UI
{
    /// <summary>
    /// Always-visible health bar HUD element for the player ship.
    /// Subscribes to ShipHealth events for reactive updates.
    /// Follows the same Bind() pattern as HeatBarHUD.
    /// </summary>
    public class HealthBarHUD : MonoBehaviour
    {
        // ──────────────────── Inspector ────────────────────
        [Header("UI References")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _damageFlash;
        [SerializeField] private TMP_Text _label;

        [Header("Visual Settings")]
        [SerializeField] private Gradient _healthGradient;
        [SerializeField] private float _flashFadeSpeed = 4f;
        [SerializeField] private float _lowHealthThreshold = 0.3f;
        [SerializeField] private float _lowHealthPulseSpeed = 3f;

        // ──────────────────── Runtime State ────────────────────
        private ShipHealth _shipHealth;
        private float _flashAlpha;
        private bool _isLowHealth;

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Inject the ShipHealth reference. Pass null to hide the bar.
        /// </summary>
        public void Bind(ShipHealth shipHealth)
        {
            // Unsubscribe from old reference
            Unbind();

            _shipHealth = shipHealth;

            if (_shipHealth == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _shipHealth.OnDamageTaken += HandleDamageTaken;
            _shipHealth.OnDeath += HandleDeath;

            // Initialize visual state
            float normalized = _shipHealth.CurrentHP / _shipHealth.MaxHP;
            UpdateFill(normalized);
            UpdateLabel();
            _flashAlpha = 0f;
            _isLowHealth = normalized <= _lowHealthThreshold;
        }

        // ──────────────────── Lifecycle ────────────────────

        private void OnDestroy()
        {
            Unbind();
        }

        private void Update()
        {
            // Fade out damage flash
            if (_flashAlpha > 0f && _damageFlash != null)
            {
                _flashAlpha -= Time.unscaledDeltaTime * _flashFadeSpeed;
                if (_flashAlpha < 0f) _flashAlpha = 0f;

                var color = _damageFlash.color;
                color.a = _flashAlpha;
                _damageFlash.color = color;
            }

            // Low health pulse animation
            if (_isLowHealth && _fillImage != null)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * _lowHealthPulseSpeed) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(0.5f, 1f, pulse);
                var fillColor = _fillImage.color;
                fillColor.a = alpha;
                _fillImage.color = fillColor;
            }
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleDamageTaken(float damage, float currentHP)
        {
            if (_shipHealth == null) return;

            float normalized = currentHP / _shipHealth.MaxHP;
            UpdateFill(Mathf.Clamp01(normalized));
            UpdateLabel();

            // Trigger damage flash
            _flashAlpha = 0.6f;
            if (_damageFlash != null)
            {
                var color = _damageFlash.color;
                color.a = _flashAlpha;
                _damageFlash.color = color;
            }

            // Check low health
            _isLowHealth = normalized <= _lowHealthThreshold;
        }

        private void HandleDeath()
        {
            UpdateFill(0f);
            _isLowHealth = false;

            if (_label != null)
                _label.text = "DESTROYED";
        }

        // ──────────────────── Visual Updates ────────────────────

        private void UpdateFill(float normalizedHP)
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = normalizedHP;

                if (_healthGradient != null)
                {
                    var gradientColor = _healthGradient.Evaluate(normalizedHP);
                    gradientColor.a = _fillImage.color.a; // Preserve pulse alpha
                    _fillImage.color = gradientColor;
                }
            }
        }

        private void UpdateLabel()
        {
            if (_label != null && _shipHealth != null)
                _label.text = $"HP {_shipHealth.CurrentHP:F0}/{_shipHealth.MaxHP:F0}";
        }

        private void Unbind()
        {
            if (_shipHealth != null)
            {
                _shipHealth.OnDamageTaken -= HandleDamageTaken;
                _shipHealth.OnDeath -= HandleDeath;
                _shipHealth = null;
            }
        }
    }
}
