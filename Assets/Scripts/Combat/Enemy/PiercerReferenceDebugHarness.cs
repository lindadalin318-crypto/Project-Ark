using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// ReferenceOnly Play Mode harness for quickly tuning the Piercer signal window.
    /// It configures PiercerReferenceVisual only; it does not drive damage, collision, or EnemyDirector tokens.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PiercerReferenceBrain))]
    [RequireComponent(typeof(PiercerReferenceVisual))]
    public sealed class PiercerReferenceDebugHarness : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private bool _resetOnDisable = true;
        [SerializeField] private bool _showDebugPhaseLabel = true;

        [Header("Preview Timing")]
        [SerializeField, Min(0f)] private float _pauseDuration = 1f;
        [SerializeField, Min(0f)] private float _anticipationDuration = 0.2f;
        [SerializeField, Min(0f)] private float _dashDuration = 0.45f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.25f;
        [SerializeField, Min(0f)] private float _idleGapDuration = 0.3f;
        [SerializeField] private bool _loopPreview = true;

        [Header("References")]
        [SerializeField] private PiercerReferenceVisual _visual;

        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public PiercerReferencePhaseSnapshot CurrentSnapshot => _visual != null
            ? _visual.ResolveCurrentSnapshot()
            : new PiercerReferencePhaseSnapshot(PiercerReferencePhase.Idle, 0f, 0f, 0f, 0f, string.Empty);

        private void Awake()
        {
            if (_visual == null)
                _visual = GetComponent<PiercerReferenceVisual>();

            ValidateReferences();
            ApplyPreviewConfiguration();
        }

        private void OnEnable()
        {
            ApplyPreviewConfiguration();

            if (_playOnEnable)
                PlayPreview();
        }

        private void OnDisable()
        {
            if (_resetOnDisable)
                ResetPreview();

            _isPlaying = false;
        }

        private void OnValidate()
        {
            _pauseDuration = Mathf.Max(0f, _pauseDuration);
            _anticipationDuration = Mathf.Max(0f, _anticipationDuration);
            _dashDuration = Mathf.Max(0f, _dashDuration);
            _recoveryDuration = Mathf.Max(0f, _recoveryDuration);
            _idleGapDuration = Mathf.Max(0f, _idleGapDuration);
        }

        /// <summary>
        /// Starts the ReferenceOnly visual preview from the beginning of the signal window.
        /// </summary>
        public void PlayPreview()
        {
            ApplyPreviewConfiguration();
            _visual.ResetPreviewCycle();
            _isPlaying = true;
        }

        /// <summary>
        /// Stops the ReferenceOnly visual preview and returns the visual to its idle presentation.
        /// </summary>
        public void ResetPreview()
        {
            if (_visual == null)
                return;

            _visual.ResetPreviewCycle();
            _isPlaying = false;
        }

        /// <summary>
        /// Re-applies Inspector timing values to the Piercer visual without touching gameplay systems.
        /// </summary>
        public void ApplyPreviewConfiguration()
        {
            if (_visual == null)
                return;

            _visual.ConfigurePreviewTiming(
                _pauseDuration,
                _anticipationDuration,
                _dashDuration,
                _recoveryDuration,
                _idleGapDuration,
                _loopPreview,
                _showDebugPhaseLabel);
        }

        private void ValidateReferences()
        {
            if (_visual == null)
                Debug.LogError($"[{nameof(PiercerReferenceDebugHarness)}] Missing PiercerReferenceVisual on {name}.", this);
        }
    }
}
