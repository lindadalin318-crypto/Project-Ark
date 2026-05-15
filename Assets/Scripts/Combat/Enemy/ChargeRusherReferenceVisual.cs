using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// ReferenceOnly Minishoot-style visual skin for ChargeRusher.
    /// Reads the existing brain state and applies presentation only; it never drives gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ChargeRusherReferenceVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private AudioSource _audioSource;

        [Header("Sprites")]
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Sprite _telegraphSprite;
        [SerializeField] private Sprite _dashSprite;
        [SerializeField] private Sprite _recoverySprite;

        [Header("Audio")]
        [SerializeField] private AudioClip _telegraphClip;
        [SerializeField] private AudioClip _dashClip;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _telegraphDuration = 0.35f;
        [SerializeField, Min(0f)] private float _dashDuration = 0.45f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.25f;

        [Header("Feel")]
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _telegraphColor = new Color(1f, 0.38f, 0.24f, 1f);
        [SerializeField] private Color _dashColor = new Color(1f, 0.82f, 0.34f, 1f);
        [SerializeField] private Color _recoveryColor = new Color(0.72f, 0.86f, 1f, 1f);
        [SerializeField, Min(0f)] private float _telegraphPulseSpeed = 18f;
        [SerializeField, Min(0f)] private float _dashPulseSpeed = 28f;
        [SerializeField, Range(0f, 0.5f)] private float _pulseAmount = 0.18f;

        private ChargeRusherReferencePhase _currentPhase = ChargeRusherReferencePhase.Idle;
        private string _lastStateName = string.Empty;
        private float _elapsedInState;
        private Vector3 _baseScale;

        private void Awake()
        {
            if (_brain == null)
                _brain = GetComponent<EnemyBrain>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            _baseScale = transform.localScale;
            ValidateReferences();
            ApplyPhase(ChargeRusherReferencePhase.Idle, force: true);
        }

        private void OnDisable()
        {
            _currentPhase = ChargeRusherReferencePhase.Idle;
            _lastStateName = string.Empty;
            _elapsedInState = 0f;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _idleColor;
                if (_idleSprite != null)
                    _spriteRenderer.sprite = _idleSprite;
            }

            transform.localScale = _baseScale;
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

            var phase = ChargeRusherReferencePhaseResolver.Resolve(
                stateName,
                _elapsedInState,
                _telegraphDuration,
                _dashDuration,
                _recoveryDuration);

            ApplyPhase(phase, force: false);
            ApplyPulse(phase);
        }

        private string GetCurrentStateName()
        {
            var currentState = _brain != null && _brain.StateMachine != null
                ? _brain.StateMachine.CurrentState
                : null;

            return currentState != null ? currentState.GetType().Name : string.Empty;
        }

        private void ApplyPhase(ChargeRusherReferencePhase phase, bool force)
        {
            if (!force && phase == _currentPhase)
                return;

            _currentPhase = phase;

            switch (phase)
            {
                case ChargeRusherReferencePhase.Telegraph:
                    SetSpriteAndColor(_telegraphSprite, _telegraphColor);
                    PlayOneShot(_telegraphClip);
                    break;
                case ChargeRusherReferencePhase.Dashing:
                    SetSpriteAndColor(_dashSprite, _dashColor);
                    PlayOneShot(_dashClip);
                    break;
                case ChargeRusherReferencePhase.Recovery:
                    SetSpriteAndColor(_recoverySprite, _recoveryColor);
                    break;
                default:
                    SetSpriteAndColor(_idleSprite, _idleColor);
                    break;
            }
        }

        private void ApplyPulse(ChargeRusherReferencePhase phase)
        {
            if (phase == ChargeRusherReferencePhase.Telegraph)
            {
                float pulse = 1f + Mathf.Sin(Time.time * _telegraphPulseSpeed) * _pulseAmount;
                transform.localScale = _baseScale * pulse;
                return;
            }

            if (phase == ChargeRusherReferencePhase.Dashing)
            {
                float pulse = 1f + Mathf.Sin(Time.time * _dashPulseSpeed) * (_pulseAmount * 0.5f);
                transform.localScale = _baseScale * pulse;
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
                Debug.LogError($"[{nameof(ChargeRusherReferenceVisual)}] Missing EnemyBrain on {name}.", this);

            if (_spriteRenderer == null)
                Debug.LogError($"[{nameof(ChargeRusherReferenceVisual)}] Missing SpriteRenderer on {name}.", this);

            if (_idleSprite == null)
                Debug.LogError($"[{nameof(ChargeRusherReferenceVisual)}] Missing idle sprite on {name}.", this);
        }
    }
}
