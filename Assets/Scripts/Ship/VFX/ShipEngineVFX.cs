using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Controls the ship's engine exhaust particle system based on movement speed.
    /// Emission rate and particle size scale with normalized speed.
    /// Boosts emission during dash.
    /// </summary>
    [RequireComponent(typeof(ShipMotor))]
    public class ShipEngineVFX : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The particle system for the engine exhaust trail.")]
        [SerializeField] private ParticleSystem _engineParticles;

        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        // ══════════════════════════════════════════════════════════════
        // Cached
        // ══════════════════════════════════════════════════════════════

        private ShipMotor _motor;
        private ShipDash _dash;
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule _mainModule;

        private float _baseStartSize;
        private bool _isDashing;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _motor = GetComponent<ShipMotor>();
            _dash = GetComponent<ShipDash>();

            if (_engineParticles == null)
            {
                Debug.LogWarning("[ShipEngineVFX] No ParticleSystem assigned. Disabling.");
                enabled = false;
                return;
            }

            _emission = _engineParticles.emission;
            _mainModule = _engineParticles.main;
            _baseStartSize = _mainModule.startSize.constant;
        }

        private void OnEnable()
        {
            if (_motor != null) _motor.OnSpeedChanged += OnSpeedChanged;
            if (_dash != null)
            {
                _dash.OnDashStarted += OnDashStarted;
                _dash.OnDashEnded += OnDashEnded;
            }
        }

        private void OnDisable()
        {
            if (_motor != null) _motor.OnSpeedChanged -= OnSpeedChanged;
            if (_dash != null)
            {
                _dash.OnDashStarted -= OnDashStarted;
                _dash.OnDashEnded -= OnDashEnded;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Speed → Particle Intensity
        // ══════════════════════════════════════════════════════════════

        private void OnSpeedChanged(float normalizedSpeed)
        {
            if (_juiceSettings == null) return;

            float minSpeed = _juiceSettings.EngineParticleMinSpeed;

            if (normalizedSpeed < minSpeed && !_isDashing)
            {
                _emission.rateOverTime = 0f;
                return;
            }

            // Scale emission and size with speed
            float speedFactor = _isDashing ? 1f : Mathf.InverseLerp(minSpeed, 1f, normalizedSpeed);
            float baseRate = _juiceSettings.EngineBaseEmissionRate;
            float dashMultiplier = _isDashing ? _juiceSettings.EngineDashEmissionMultiplier : 1f;

            _emission.rateOverTime = baseRate * speedFactor * dashMultiplier;

            // Scale particle size slightly with speed
            float sizeScale = Mathf.Lerp(0.5f, 1f, speedFactor) * (_isDashing ? 1.5f : 1f);
            var startSize = _mainModule.startSize;
            startSize.constant = _baseStartSize * sizeScale;
            _mainModule.startSize = startSize;
        }

        // ══════════════════════════════════════════════════════════════
        // Dash Burst
        // ══════════════════════════════════════════════════════════════

        private void OnDashStarted(Vector2 direction)
        {
            _isDashing = true;

            if (_juiceSettings == null) return;

            // Burst emission during dash
            float burstRate = _juiceSettings.EngineBaseEmissionRate * _juiceSettings.EngineDashEmissionMultiplier;
            _emission.rateOverTime = burstRate;
        }

        private void OnDashEnded()
        {
            _isDashing = false;
            // Next OnSpeedChanged will restore normal rate
        }
    }
}
