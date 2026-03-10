using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Drives the Boost trail particle systems (Glow layer + Ember layer).
    /// Activates on Boost start, deactivates on Boost end.
    /// Two-layer design mirrors GG's mat_boost_trail_glow + mat_boost_ember_trail:
    ///   - Glow layer:  orange-yellow → red  (HDR, large soft particles)
    ///   - Ember layer: magenta → deep-red   (HDR, small sharp sparks)
    /// All parameters sourced from ShipJuiceSettingsSO.
    /// </summary>
    [RequireComponent(typeof(ShipBoost))]
    public class ShipBoostTrailVFX : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Glow trail ParticleSystem (child of Ship_Sprite_Back). Matches GG mat_boost_trail_glow.")]
        [SerializeField] private ParticleSystem _boostTrailParticles;

        [Tooltip("Ember trail ParticleSystem (child of Ship_Sprite_Back). Matches GG mat_boost_ember_trail. Optional.")]
        [SerializeField] private ParticleSystem _boostEmberParticles;

        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        // ══════════════════════════════════════════════════════════════
        // Cached
        // ══════════════════════════════════════════════════════════════

        private ShipBoost _boost;

        private bool _hasEmber;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _boost = GetComponent<ShipBoost>();

            if (_boostTrailParticles == null)
            {
                Debug.LogWarning("[ShipBoostTrailVFX] No BoostTrailParticles (glow) assigned. Disabling.");
                enabled = false;
                return;
            }

            _boostTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _hasEmber = _boostEmberParticles != null;
            if (_hasEmber)
            {
                _boostEmberParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
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

            StopAll(clearImmediately: true);
        }

        // ══════════════════════════════════════════════════════════════
        // Boost Handlers
        // ══════════════════════════════════════════════════════════════

        private void HandleBoostStarted()
        {
            if (_juiceSettings == null) return;

            ApplyGlowSettings();
            _boostTrailParticles.Play();

            if (_hasEmber)
            {
                ApplyEmberSettings();
                _boostEmberParticles.Play();
            }
        }

        private void HandleBoostEnded()
        {
            StopAll(clearImmediately: false);
        }

        // ══════════════════════════════════════════════════════════════
        // Glow Layer — mat_boost_trail_glow
        // Orange-yellow (HDR 1.89, 0.828, 0.426) → Red (0.973, 0.106, 0.246)
        // ══════════════════════════════════════════════════════════════

        private void ApplyGlowSettings()
        {
            var s = _juiceSettings;
            var glowEmission = _boostTrailParticles.emission;
            var glowMain = _boostTrailParticles.main;

            glowEmission.rateOverTime = s.BoostTrailEmissionRate;

            glowMain.loop            = true;
            glowMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            glowMain.startLifetime   = s.BoostTrailLifetime;
            glowMain.startSpeed      = 0f;
            glowMain.maxParticles    = 300;

            glowMain.startSize = new ParticleSystem.MinMaxCurve(
                s.BoostTrailStartSizeMin,
                s.BoostTrailStartSizeMax);

            // Velocity: local -Y (ship tail direction)
            var vel = _boostTrailParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.Local;
            vel.x       = 0f;
            vel.y       = new ParticleSystem.MinMaxCurve(-s.BoostTrailStartSpeed);
            vel.z       = 0f;

            // Color: orange-yellow → red → transparent
            // GG: Color_b3dc=(1.89,0.828,0.426) → Color_d33d=(0.973,0.106,0.246)
            var col = _boostTrailParticles.colorOverLifetime;
            col.enabled = true;
            Color startColor = s.BoostTrailColorStart;
            Color midColor   = s.BoostTrailColorEnd;
            Color endColor   = new Color(midColor.r, midColor.g, midColor.b, 0f);
            col.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(startColor, 0f),
                        new GradientColorKey(midColor,   0.6f),
                        new GradientColorKey(endColor,   1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f,  0f),
                        new GradientAlphaKey(0.8f, 0.5f),
                        new GradientAlphaKey(0f,  1f)
                    }
                });
        }

        // ══════════════════════════════════════════════════════════════
        // Ember Layer — mat_boost_ember_trail
        // Magenta (HDR 2.0, 0.0, 1.083) → Deep-red (0.849, 0.076, 0.215)
        // ══════════════════════════════════════════════════════════════

        private void ApplyEmberSettings()
        {
            var s = _juiceSettings;
            var emberEmission = _boostEmberParticles.emission;
            var emberMain = _boostEmberParticles.main;

            emberEmission.rateOverTime = s.BoostEmberEmissionRate;

            emberMain.loop            = true;
            emberMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            emberMain.startLifetime   = s.BoostEmberLifetime;
            emberMain.startSpeed      = 0f;
            emberMain.maxParticles    = 200;

            emberMain.startSize = new ParticleSystem.MinMaxCurve(
                s.BoostEmberStartSizeMin,
                s.BoostEmberStartSizeMax);

            // Velocity: local -Y with slight random spread (ember scatter)
            // NOTE: x/y/z must all use the same MinMaxCurve mode to avoid
            // "Particle Velocity curves must all be in the same mode" error.
            // Use RandomBetweenTwoConstants (two-float ctor) for all three axes.
            var vel = _boostEmberParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.Local;
            vel.x       = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            vel.y       = new ParticleSystem.MinMaxCurve(-s.BoostEmberStartSpeed, -s.BoostEmberStartSpeed * 0.5f);
            vel.z       = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Color: magenta → deep-red → transparent
            // GG: Color_b3dc=(2.0,0.0,1.083) → Color_d33d=(0.849,0.076,0.215)
            var col = _boostEmberParticles.colorOverLifetime;
            col.enabled = true;
            Color startColor = s.BoostEmberColorStart;
            Color midColor   = s.BoostEmberColorEnd;
            Color endColor   = new Color(midColor.r, midColor.g, midColor.b, 0f);
            col.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(startColor, 0f),
                        new GradientColorKey(midColor,   0.5f),
                        new GradientColorKey(endColor,   1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f,  0f),
                        new GradientAlphaKey(0.6f, 0.4f),
                        new GradientAlphaKey(0f,  1f)
                    }
                });
        }

        // ══════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════

        private void StopAll(bool clearImmediately)
        {
            var mode = clearImmediately
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;

            if (_boostTrailParticles != null)
                _boostTrailParticles.Stop(!clearImmediately, mode);

            if (_hasEmber && _boostEmberParticles != null)
                _boostEmberParticles.Stop(!clearImmediately, mode);
        }
    }
}
