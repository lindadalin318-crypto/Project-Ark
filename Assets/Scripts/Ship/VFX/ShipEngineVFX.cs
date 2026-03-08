using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Controls the ship's engine exhaust particle system based on movement speed.
    /// Implements four emission tiers matching GG Glitch ship:
    ///   Idle (5/s) → Normal flight (0–40/s lerp) → Dash (120/s) → Boost (80/s via BoostTrailVFX)
    /// Particle size and direction track ship rotation via Local simulation space.
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
        private ShipDash  _dash;
        private ShipBoost _boost;
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule     _mainModule;

        private bool  _isDashing;
        private bool  _isBoosting;
        private float _lastNormalizedSpeed;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _motor = GetComponent<ShipMotor>();
            _dash  = GetComponent<ShipDash>();
            _boost = GetComponent<ShipBoost>();

            if (_engineParticles == null)
            {
                Debug.LogWarning("[ShipEngineVFX] No ParticleSystem assigned. Disabling.");
                enabled = false;
                return;
            }

            _emission   = _engineParticles.emission;
            _mainModule = _engineParticles.main;

            // Particles follow ship rotation (direction always tracks ship heading)
            _mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;

            ApplyPreciseBaseSettings();

            // playOnAwake is disabled in Prefab; start manually after settings are applied
            _engineParticles.Play();
        }

        private void OnEnable()
        {
            if (_motor != null) _motor.OnSpeedChanged += OnSpeedChanged;
            if (_dash  != null)
            {
                _dash.OnDashStarted += OnDashStarted;
                _dash.OnDashEnded   += OnDashEnded;
            }
            if (_boost != null)
            {
                _boost.OnBoostStarted += OnBoostStarted;
                _boost.OnBoostEnded   += OnBoostEnded;
            }
        }

        private void OnDisable()
        {
            if (_motor != null) _motor.OnSpeedChanged -= OnSpeedChanged;
            if (_dash  != null)
            {
                _dash.OnDashStarted -= OnDashStarted;
                _dash.OnDashEnded   -= OnDashEnded;
            }
            if (_boost != null)
            {
                _boost.OnBoostStarted -= OnBoostStarted;
                _boost.OnBoostEnded   -= OnBoostEnded;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Base Settings (applied once at Awake)
        // ══════════════════════════════════════════════════════════════

        private void ApplyPreciseBaseSettings()
        {
            if (_juiceSettings == null) return;

            // Random start size between min and max (matches GG 0.04–0.08)
            _mainModule.startSize = new ParticleSystem.MinMaxCurve(
                _juiceSettings.EngineStartSizeMin,
                _juiceSettings.EngineStartSizeMax);

            // Start at idle emission rate
            _emission.rateOverTime = _juiceSettings.EngineIdleEmissionRate;

            // Color over lifetime: teal → transparent (matches GG rgba(0.28, 0.43, 0.43))
            var colorOverLifetime = _engineParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Color baseColor = _juiceSettings.EngineParticleColor;
            Color fadeColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(baseColor, 0f),
                        new GradientColorKey(baseColor, 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });
        }

        // ══════════════════════════════════════════════════════════════
        // Speed → Particle Intensity (Normal flight tier)
        // ══════════════════════════════════════════════════════════════

        private void OnSpeedChanged(float normalizedSpeed)
        {
            _lastNormalizedSpeed = normalizedSpeed;

            // Dash and Boost tiers override speed-based emission
            if (_isDashing || _isBoosting) return;

            if (_juiceSettings == null) return;

            float minSpeed = _juiceSettings.EngineParticleMinSpeed;

            if (normalizedSpeed < minSpeed)
            {
                // Idle tier: keep a low idle emission for thruster presence
                _emission.rateOverTime = _juiceSettings.EngineIdleEmissionRate;
                return;
            }

            // Normal flight tier: lerp from idle to max based on speed
            float t = Mathf.InverseLerp(minSpeed, 1f, normalizedSpeed);
            float rate = Mathf.Lerp(
                _juiceSettings.EngineIdleEmissionRate,
                _juiceSettings.EngineMaxEmissionRate,
                t);
            _emission.rateOverTime = rate;

            // Scale particle size with speed (0.5x at idle → 1x at full speed)
            float sizeScale = Mathf.Lerp(0.5f, 1f, t);
            _mainModule.startSize = new ParticleSystem.MinMaxCurve(
                _juiceSettings.EngineStartSizeMin  * sizeScale,
                _juiceSettings.EngineStartSizeMax  * sizeScale);
        }

        // ══════════════════════════════════════════════════════════════
        // Dash Tier — 120/s burst
        // ══════════════════════════════════════════════════════════════

        private void OnDashStarted(Vector2 direction)
        {
            _isDashing = true;
            if (_juiceSettings == null) return;

            _emission.rateOverTime = _juiceSettings.EngineDashEmissionRate;

            // Larger particles during dash (×1.5)
            _mainModule.startSize = new ParticleSystem.MinMaxCurve(
                _juiceSettings.EngineStartSizeMin * 1.5f,
                _juiceSettings.EngineStartSizeMax * 1.5f);
        }

        private void OnDashEnded()
        {
            _isDashing = false;

            // Restore size to normal
            if (_juiceSettings != null)
            {
                _mainModule.startSize = new ParticleSystem.MinMaxCurve(
                    _juiceSettings.EngineStartSizeMin,
                    _juiceSettings.EngineStartSizeMax);
            }

            // Let OnSpeedChanged restore emission rate on next frame
            OnSpeedChanged(_lastNormalizedSpeed);
        }

        // ══════════════════════════════════════════════════════════════
        // Boost Tier — engine holds at max (BoostTrailVFX handles dedicated trail)
        // ══════════════════════════════════════════════════════════════

        private void OnBoostStarted()
        {
            _isBoosting = true;
            if (_juiceSettings == null) return;

            // Engine holds at max emission during boost
            _emission.rateOverTime = _juiceSettings.EngineMaxEmissionRate;
        }

        private void OnBoostEnded()
        {
            _isBoosting = false;
            // Let OnSpeedChanged restore emission rate on next frame
            OnSpeedChanged(_lastNormalizedSpeed);
        }
    }
}
