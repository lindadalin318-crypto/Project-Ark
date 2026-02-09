using UnityEngine;

namespace ProjectArk.UI
{
    /// <summary>
    /// Data-driven configuration for the Weaving State visual/audio transition.
    /// All timing, camera, post-processing and SFX parameters live here so
    /// designers can tweak them from the Inspector without touching Prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "WeavingTransitionSettings", menuName = "ProjectArk/UI/WeavingTransitionSettings")]
    public class WeavingTransitionSettingsSO : ScriptableObject
    {
        [Header("Timing")]
        [Tooltip("Duration (seconds) for the enter-weaving transition.")]
        [SerializeField] private float _enterDuration = 0.35f;

        [Tooltip("Duration (seconds) for the exit-weaving transition.")]
        [SerializeField] private float _exitDuration = 0.25f;

        [Tooltip("Interpolation curve used for all transition properties.")]
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Camera")]
        [Tooltip("Orthographic size during normal combat.")]
        [SerializeField] private float _combatCameraSize = 5f;

        [Tooltip("Orthographic size during weaving (smaller = closer zoom).")]
        [SerializeField] private float _weavingCameraSize = 3f;

        [Header("Post-Processing — Vignette")]
        [Tooltip("Vignette intensity during normal combat.")]
        [SerializeField] private float _combatVignetteIntensity = 0.1f;

        [Tooltip("Vignette intensity during weaving.")]
        [SerializeField] private float _weavingVignetteIntensity = 0.5f;

        [Header("Post-Processing — Depth of Field")]
        [Tooltip("Enable DoF during weaving state.")]
        [SerializeField] private bool _enableDoFInWeaving = true;

        [Tooltip("DoF focus distance when in weaving state.")]
        [SerializeField] private float _weavingFocusDistance = 5f;

        [Tooltip("DoF focal length when in weaving state.")]
        [SerializeField] private float _weavingFocalLength = 50f;

        [Header("Audio")]
        [Tooltip("Sound played when entering weaving state. Null = silent.")]
        [SerializeField] private AudioClip _openSfx;

        [Tooltip("Sound played when exiting weaving state. Null = silent.")]
        [SerializeField] private AudioClip _closeSfx;

        // --- Public accessors ---
        public float EnterDuration => _enterDuration;
        public float ExitDuration => _exitDuration;
        public AnimationCurve TransitionCurve => _transitionCurve;

        public float CombatCameraSize => _combatCameraSize;
        public float WeavingCameraSize => _weavingCameraSize;

        public float CombatVignetteIntensity => _combatVignetteIntensity;
        public float WeavingVignetteIntensity => _weavingVignetteIntensity;

        public bool EnableDoFInWeaving => _enableDoFInWeaving;
        public float WeavingFocusDistance => _weavingFocusDistance;
        public float WeavingFocalLength => _weavingFocalLength;

        public AudioClip OpenSfx => _openSfx;
        public AudioClip CloseSfx => _closeSfx;
    }
}
