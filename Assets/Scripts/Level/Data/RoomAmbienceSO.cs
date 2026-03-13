using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Room-level ambience preset. Defines local overrides for post-processing,
    /// particles, BGM, and lighting that a BiomeTrigger can apply.
    /// 
    /// This is the room-level equivalent of WorldPhaseSO (which drives global ambience).
    /// Room ambience overrides global phase ambience while the player is in the trigger zone.
    /// </summary>
    [CreateAssetMenu(fileName = "New RoomAmbience", menuName = "ProjectArk/Level/Room Ambience")]
    public class RoomAmbienceSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Human-readable name for this ambience preset (e.g., 'Silence Zone', 'Crystal Cavern').")]
        [SerializeField] private string _presetName;

        [Header("Post-Processing")]
        [Tooltip("Override ambient color filter. Alpha=0 means no override.")]
        [SerializeField] private Color _ambientColorOverride = new Color(1f, 1f, 1f, 0f);

        [Tooltip("Override vignette intensity. Negative = no override.")]
        [SerializeField] private float _vignetteIntensityOverride = -1f;

        [Header("Audio")]
        [Tooltip("BGM to play in this zone. Null = keep current BGM.")]
        [SerializeField] private AudioClip _bgmOverride;

        [Tooltip("If true, applies low-pass filter while in this zone.")]
        [SerializeField] private bool _applyLowPass;

        [Tooltip("Low-pass cutoff frequency (Hz). Only used if ApplyLowPass is true.")]
        [SerializeField] private float _lowPassCutoffHz = 800f;

        [Header("Particles")]
        [Tooltip("Prefab of a ParticleSystem to activate in this zone. Null = no particles.")]
        [SerializeField] private ParticleSystem _particlePrefab;

        [Header("Transition")]
        [Tooltip("Duration (seconds) to blend into this ambience.")]
        [SerializeField] private float _transitionDuration = 1f;

        // ──────────────────── Public Properties ────────────────────

        public string PresetName => _presetName;

        /// <summary> Ambient color override. Alpha=0 means no override. </summary>
        public Color AmbientColorOverride => _ambientColorOverride;
        public bool HasColorOverride => _ambientColorOverride.a > 0f;

        /// <summary> Vignette intensity override. Negative means no override. </summary>
        public float VignetteIntensityOverride => _vignetteIntensityOverride;
        public bool HasVignetteOverride => _vignetteIntensityOverride >= 0f;

        public AudioClip BGMOverride => _bgmOverride;
        public bool HasBGMOverride => _bgmOverride != null;

        public bool ApplyLowPass => _applyLowPass;
        public float LowPassCutoffHz => _lowPassCutoffHz;

        public ParticleSystem ParticlePrefab => _particlePrefab;
        public bool HasParticles => _particlePrefab != null;

        public float TransitionDuration => _transitionDuration;
    }
}
