using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// ReferenceOnly Minishoot-style visual skin for the Piercer AICharge evidence chain.
    /// Reads the existing brain state and applies presentation only; it never drives gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PiercerReferenceVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private AudioSource _audioSource;

        [Header("Sprites")]
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Sprite _pauseSprite;
        [SerializeField] private Sprite _anticipationSprite;
        [SerializeField] private Sprite _dashSprite;
        [SerializeField] private Sprite _recoverySprite;

        [Header("Audio")]
        [SerializeField] private AudioClip _anticipationClip;
        [SerializeField] private AudioClip _dashClip;

        [Header("Original AICharge Timing")]
        [SerializeField, Min(0f)] private float _pauseDuration = 1f;
        [SerializeField, Min(0f)] private float _anticipationDuration = 0.2f;
        [SerializeField, Min(0f)] private float _dashDuration = 0.45f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.25f;

        [Header("Reference Preview")]
        [SerializeField] private bool _loopPreview = true;
        [SerializeField, Min(0f)] private float _idleGapDuration = 0.3f;
        [SerializeField] private bool _showDebugPhaseLabel;
        [SerializeField] private Vector3 _debugPhaseLabelOffset = new Vector3(0f, 1.1f, 0f);

        [Header("Feel")]
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _pauseColor = new Color(0.82f, 0.92f, 1f, 1f);
        [SerializeField] private Color _anticipationColor = new Color(1f, 0.44f, 0.26f, 1f);
        [SerializeField] private Color _dashColor = new Color(1f, 0.82f, 0.34f, 1f);
        [SerializeField] private Color _recoveryColor = new Color(0.62f, 0.78f, 1f, 1f);
        [SerializeField, Min(0f)] private float _pausePulseSpeed = 8f;
        [SerializeField, Min(0f)] private float _anticipationShakeSpeed = 80f;
        [SerializeField, Range(0f, 0.5f)] private float _pulseAmount = 0.08f;
        [SerializeField, Range(0f, 0.25f)] private float _shakeAmount = 0.035f;

        private PiercerReferencePhase _currentPhase = PiercerReferencePhase.Idle;
        private string _lastStateName = string.Empty;
        private float _elapsedInState;
        private Vector3 _baseScale;
        private Vector3 _baseLocalPosition;

        private void Awake()
        {
            if (_brain == null)
                _brain = GetComponent<EnemyBrain>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            _baseScale = transform.localScale;
            _baseLocalPosition = transform.localPosition;
            ValidateReferences();
            ApplyPhase(PiercerReferencePhase.Idle, force: true);
        }

        private void OnDisable()
        {
            _currentPhase = PiercerReferencePhase.Idle;
            _lastStateName = string.Empty;
            _elapsedInState = 0f;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _idleColor;
                if (_idleSprite != null)
                    _spriteRenderer.sprite = _idleSprite;
            }

            transform.localScale = _baseScale;
            transform.localPosition = _baseLocalPosition;
        }

        private void Update()
        {
            string stateName = GetCurrentStateName();
            if (stateName != _lastStateName)
            {
                _lastStateName = stateName;
                _elapsedInState = 0f;
            }
            else
            {
                _elapsedInState += Time.deltaTime;
            }

            PiercerReferencePhaseSnapshot snapshot = ResolveCurrentSnapshot();
            ApplyPhase(snapshot.Phase, force: false);
            ApplyJuice(snapshot.Phase);
        }

        /// <summary>
        /// Applies ReferenceOnly preview timing from a debug harness without changing gameplay logic.
        /// </summary>
        public void ConfigurePreviewTiming(
            float pauseDuration,
            float anticipationDuration,
            float dashDuration,
            float recoveryDuration,
            float idleGapDuration,
            bool loopPreview,
            bool showDebugPhaseLabel)
        {
            _pauseDuration = Mathf.Max(0f, pauseDuration);
            _anticipationDuration = Mathf.Max(0f, anticipationDuration);
            _dashDuration = Mathf.Max(0f, dashDuration);
            _recoveryDuration = Mathf.Max(0f, recoveryDuration);
            _idleGapDuration = Mathf.Max(0f, idleGapDuration);
            _loopPreview = loopPreview;
            _showDebugPhaseLabel = showDebugPhaseLabel;
        }

        /// <summary>
        /// Resets ReferenceOnly preview timers so Play Mode iteration starts from a readable pause window.
        /// </summary>
        public void ResetPreviewCycle()
        {
            _lastStateName = string.Empty;
            _elapsedInState = 0f;
            ApplyPhase(PiercerReferencePhase.Idle, force: true);
            ApplyJuice(PiercerReferencePhase.Idle);
        }

        public PiercerReferencePhaseSnapshot ResolveCurrentSnapshot()
        {
            return PiercerReferencePhaseResolver.ResolveSnapshot(
                _lastStateName,
                _elapsedInState,
                _pauseDuration,
                _anticipationDuration,
                _dashDuration,
                _recoveryDuration,
                _idleGapDuration,
                _loopPreview);
        }

        private string GetCurrentStateName()
        {
            var currentState = _brain != null && _brain.StateMachine != null
                ? _brain.StateMachine.CurrentState
                : null;

            return currentState != null ? currentState.GetType().Name : string.Empty;
        }

        private void ApplyPhase(PiercerReferencePhase phase, bool force)
        {
            if (!force && phase == _currentPhase)
                return;

            _currentPhase = phase;

            switch (phase)
            {
                case PiercerReferencePhase.Pause:
                    SetSpriteAndColor(_pauseSprite, _pauseColor);
                    break;
                case PiercerReferencePhase.Anticipation:
                    SetSpriteAndColor(_anticipationSprite, _anticipationColor);
                    PlayOneShot(_anticipationClip);
                    break;
                case PiercerReferencePhase.Dashing:
                    SetSpriteAndColor(_dashSprite, _dashColor);
                    PlayOneShot(_dashClip);
                    break;
                case PiercerReferencePhase.Recovery:
                    SetSpriteAndColor(_recoverySprite, _recoveryColor);
                    break;
                default:
                    SetSpriteAndColor(_idleSprite, _idleColor);
                    break;
            }
        }

        private void ApplyJuice(PiercerReferencePhase phase)
        {
            transform.localPosition = _baseLocalPosition;

            if (phase == PiercerReferencePhase.Pause)
            {
                float pulse = 1f + Mathf.Sin(Time.time * _pausePulseSpeed) * _pulseAmount;
                transform.localScale = _baseScale * pulse;
                return;
            }

            if (phase == PiercerReferencePhase.Anticipation)
            {
                float shake = Mathf.Sin(Time.time * _anticipationShakeSpeed) * _shakeAmount;
                transform.localPosition = _baseLocalPosition + new Vector3(shake, 0f, 0f);
                transform.localScale = _baseScale * (1f + _pulseAmount);
                return;
            }

            if (phase == PiercerReferencePhase.Dashing)
            {
                transform.localScale = _baseScale * (1f + _pulseAmount * 0.5f);
                return;
            }

            transform.localScale = _baseScale;
        }

        private void SetSpriteAndColor(Sprite sprite, Color color)
        {
            if (_spriteRenderer == null)
                return;

            if (sprite != null)
                _spriteRenderer.sprite = sprite;

            _spriteRenderer.color = color;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (_audioSource == null || clip == null)
                return;

            _audioSource.PlayOneShot(clip);
        }

        private void ValidateReferences()
        {
            if (_brain == null)
                Debug.LogError($"[{nameof(PiercerReferenceVisual)}] Missing EnemyBrain on {name}.", this);

            if (_spriteRenderer == null)
                Debug.LogError($"[{nameof(PiercerReferenceVisual)}] Missing SpriteRenderer on {name}.", this);

            if (_idleSprite == null)
                Debug.LogError($"[{nameof(PiercerReferenceVisual)}] Missing idle sprite on {name}.", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showDebugPhaseLabel)
                return;

            PiercerReferencePhaseSnapshot snapshot = ResolveCurrentSnapshot();
            UnityEditor.Handles.Label(transform.position + _debugPhaseLabelOffset, snapshot.DetailedLabel);
        }
#endif
    }
}
