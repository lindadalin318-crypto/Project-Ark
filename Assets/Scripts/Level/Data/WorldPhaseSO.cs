using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Data-driven definition of a single world time phase within a planet's rotation cycle.
    /// Examples: Radiation Tide, Calm Period, Storm Period, Silent Hour.
    /// 
    /// WorldPhaseManager holds an array of these and evaluates which phase is active
    /// based on the normalized time from WorldClock.
    /// </summary>
    [CreateAssetMenu(fileName = "New World Phase", menuName = "ProjectArk/Level/World Phase")]
    public class WorldPhaseSO : ScriptableObject
    {
        // ──────────────────── Identity ────────────────────

        [Header("Identity")]
        [Tooltip("Human-readable phase name (e.g., 'Radiation Tide', 'Calm Period').")]
        [SerializeField] private string _phaseName;

        [Tooltip("Short description for UI tooltip.")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        // ──────────────────── Time Range ────────────────────

        [Header("Time Range (Normalized 0..1)")]
        [Tooltip("Start of this phase within the cycle (0..1). Must be < EndTime.")]
        [Range(0f, 1f)]
        [SerializeField] private float _startTime;

        [Tooltip("End of this phase within the cycle (0..1). Must be > StartTime.")]
        [Range(0f, 1f)]
        [SerializeField] private float _endTime = 0.25f;

        // ──────────────────── Ambience ────────────────────

        [Header("Ambience")]
        [Tooltip("Ambient color tint applied to post-processing during this phase.")]
        [SerializeField] private Color _ambientColor = Color.white;

        [Tooltip("Background music for this phase. Null = keep current BGM.")]
        [SerializeField] private AudioClip _phaseBGM;

        [Tooltip("Whether to apply a low-pass audio filter during this phase (e.g., storm muffling).")]
        [SerializeField] private bool _applyLowPassFilter;

        [Tooltip("Low-pass cutoff frequency (Hz). Only used if _applyLowPassFilter is true.")]
        [SerializeField] private float _lowPassCutoffHz = 800f;

        // ──────────────────── Gameplay Modifiers ────────────────────

        [Header("Gameplay Modifiers")]
        [Tooltip("Multiplier applied to enemy damage during this phase.")]
        [SerializeField] private float _enemyDamageMultiplier = 1f;

        [Tooltip("Multiplier applied to enemy health during this phase.")]
        [SerializeField] private float _enemyHealthMultiplier = 1f;

        [Tooltip("Whether hidden paths/passages become visible during this phase.")]
        [SerializeField] private bool _hiddenPathsVisible;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Human-readable phase name. </summary>
        public string PhaseName => _phaseName;

        /// <summary> Short description. </summary>
        public string Description => _description;

        /// <summary> Normalized start time (0..1). </summary>
        public float StartTime => _startTime;

        /// <summary> Normalized end time (0..1). </summary>
        public float EndTime => _endTime;

        /// <summary> Ambient color for post-processing. </summary>
        public Color AmbientColor => _ambientColor;

        /// <summary> Phase BGM (null = no change). </summary>
        public AudioClip PhaseBGM => _phaseBGM;

        /// <summary> Whether to apply low-pass filter. </summary>
        public bool ApplyLowPassFilter => _applyLowPassFilter;

        /// <summary> Low-pass cutoff frequency. </summary>
        public float LowPassCutoffHz => _lowPassCutoffHz;

        /// <summary> Enemy damage multiplier. </summary>
        public float EnemyDamageMultiplier => _enemyDamageMultiplier;

        /// <summary> Enemy health multiplier. </summary>
        public float EnemyHealthMultiplier => _enemyHealthMultiplier;

        /// <summary> Whether hidden paths are visible. </summary>
        public bool HiddenPathsVisible => _hiddenPathsVisible;

        // ──────────────────── Utility ────────────────────

        /// <summary>
        /// Check if a given normalized time falls within this phase's time range.
        /// </summary>
        public bool ContainsTime(float normalizedTime)
        {
            // Normal range (doesn't wrap around midnight)
            if (_startTime <= _endTime)
            {
                return normalizedTime >= _startTime && normalizedTime < _endTime;
            }

            // Wrap-around range (e.g., 0.9 → 0.1 wraps through midnight)
            return normalizedTime >= _startTime || normalizedTime < _endTime;
        }
    }
}
