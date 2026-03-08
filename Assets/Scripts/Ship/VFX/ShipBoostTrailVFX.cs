using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Drives the dedicated Boost trail particle system (BoostTrailParticles).
    /// Activates on Boost start, deactivates on Boost end.
    /// All parameters are sourced from ShipJuiceSettingsSO — no hardcoded values.
    /// </summary>
    [RequireComponent(typeof(ShipBoost))]
    public class ShipBoostTrailVFX : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The dedicated Boost trail ParticleSystem (child of Ship_Sprite_Back).")]
        [SerializeField] private ParticleSystem _boostTrailParticles;

        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        // ══════════════════════════════════════════════════════════════
        // Cached
        // ══════════════════════════════════════════════════════════════

        private ShipBoost _boost;
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule _main;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _boost = GetComponent<ShipBoost>();

            if (_boostTrailParticles == null)
            {
                Debug.LogWarning("[ShipBoostTrailVFX] No BoostTrailParticles assigned. Disabling.");
                enabled = false;
                return;
            }

            _emission = _boostTrailParticles.emission;
            _main     = _boostTrailParticles.main;

            // Ensure particles start stopped
            _boostTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnEnable()
        {
            if (_boost != null)
            {
                _boost.OnBoostStarted += HandleBoostStarted;
                _boost.OnBoostEnded   += HandleBoostEnded;
            }
        }

        private void OnDisable()
        {
            if (_boost != null)
            {
                _boost.OnBoostStarted -= HandleBoostStarted;
                _boost.OnBoostEnded   -= HandleBoostEnded;
            }

            StopTrailParticles(clearImmediately: true);
        }

        // ══════════════════════════════════════════════════════════════
        // Boost Handlers
        // ══════════════════════════════════════════════════════════════

        private void HandleBoostStarted()
        {
            if (_boostTrailParticles == null || _juiceSettings == null) return;

            ApplyParticleSettings();
            _boostTrailParticles.Play();
        }

        private void HandleBoostEnded()
        {
            // Stop emitting but let existing particles finish their lifetime naturally
            StopTrailParticles(clearImmediately: false);
        }

        // ══════════════════════════════════════════════════════════════
        // Parameter Configuration
        // ══════════════════════════════════════════════════════════════

        private void ApplyParticleSettings()
        {
            // Emission rate
            _emission.rateOverTime = _juiceSettings.BoostTrailEmissionRate;

            // Main module — Local space so particles track ship rotation
            // Particles emit in local -Y direction (ship tail), direction follows ship heading
            _main.loop            = true;
            _main.simulationSpace = ParticleSystemSimulationSpace.Local;
            _main.startLifetime   = _juiceSettings.BoostTrailLifetime;
            _main.startSpeed      = 0f; // Speed driven by velocityOverLifetime below

            // Velocity over lifetime: emit in local -Y direction (ship tail)
            var vel = _boostTrailParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.Local;
            vel.x       = 0f;
            vel.y       = new ParticleSystem.MinMaxCurve(-_juiceSettings.BoostTrailStartSpeed);
            vel.z       = 0f;

            // Random start size between min and max
            _main.startSize = new ParticleSystem.MinMaxCurve(
                _juiceSettings.BoostTrailStartSizeMin,
                _juiceSettings.BoostTrailStartSizeMax);

            // Color: teal → transparent over lifetime
            var colorOverLifetime = _boostTrailParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Color startColor = _juiceSettings.BoostTrailColor;
            Color endColor   = new Color(startColor.r, startColor.g, startColor.b, 0f);
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(startColor, 0f),
                        new GradientColorKey(endColor,   1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });

            // Max particles cap (performance guard)
            _main.maxParticles = 200;
        }

        private void StopTrailParticles(bool clearImmediately)
        {
            if (_boostTrailParticles == null) return;

            if (clearImmediately)
                _boostTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else
                _boostTrailParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
