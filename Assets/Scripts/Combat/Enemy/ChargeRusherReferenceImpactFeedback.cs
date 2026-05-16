using ProjectArk.Core;
using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// ReferenceOnly impact feedback for the Minishoot ChargeRusher prototype.
    /// Observes dash contact and plays presentation only; it never applies damage.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ChargeRusherReferenceImpactFeedback : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private SpriteRenderer _sourceRenderer;
        [SerializeField] private AudioSource _audioSource;

        [Header("State Timing")]
        [SerializeField, Min(0f)] private float _telegraphDuration = 0.38f;
        [SerializeField, Min(0f)] private float _dashDuration = 0.42f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.28f;

        [Header("Impact Spark")]
        [SerializeField, Min(0.01f)] private float _sparkLifetime = 0.12f;
        [SerializeField] private Color _sparkColor = new Color(1f, 0.94f, 0.38f, 0.9f);
        [SerializeField] private Vector3 _sparkScale = new Vector3(1.55f, 1.55f, 1f);
        [SerializeField] private int _sparkSortingOffset = 2;

        [Header("Hit Flash")]
        [SerializeField, Min(0f)] private float _flashDuration = 0.06f;
        [SerializeField] private Color _flashColor = new Color(1f, 1f, 0.78f, 1f);
        [SerializeField] private Vector3 _flashScale = new Vector3(1.18f, 1.18f, 1f);

        [Header("Hit Stop")]
        [SerializeField, Min(0f)] private float _hitStopDuration = 0.025f;

        [Header("Camera Shake")]
        [SerializeField, Min(0f)] private float _cameraShakeDuration = 0.08f;
        [SerializeField, Min(0f)] private float _cameraShakeAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float _cameraShakeFrequency = 26f;

        [Header("Audio")]
        [SerializeField] private AudioClip _impactClip;

        private readonly ChargeRusherReferenceImpactGate _impactGate = new ChargeRusherReferenceImpactGate();
        private SpriteRenderer _sparkRenderer;
        private string _lastStateName = string.Empty;
        private float _elapsedInState;
        private ChargeRusherReferencePhase _currentPhase = ChargeRusherReferencePhase.Idle;
        private Vector3 _baseScale;
        private Color _baseColor;
        private bool _sparkActive;
        private float _sparkTimer;
        private bool _flashActive;
        private float _flashTimer;
        private bool _hasLoggedMissingCameraShake;

        private void Awake()
        {
            if (_brain == null)
                _brain = GetComponent<EnemyBrain>();

            if (_sourceRenderer == null)
                _sourceRenderer = GetComponent<SpriteRenderer>();

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            _baseScale = transform.localScale;
            _baseColor = _sourceRenderer != null ? _sourceRenderer.color : Color.white;
            BuildSparkRenderer();
            ValidateReferences();
        }

        private void OnDisable()
        {
            _lastStateName = string.Empty;
            _elapsedInState = 0f;
            _currentPhase = ChargeRusherReferencePhase.Idle;
            _impactGate.Reset();
            _sparkActive = false;
            _sparkTimer = 0f;
            _flashActive = false;
            _flashTimer = 0f;
            transform.localScale = _baseScale;

            if (_sourceRenderer != null)
                _sourceRenderer.color = _baseColor;

            if (_sparkRenderer != null)
                _sparkRenderer.gameObject.SetActive(false);
        }

        private void Update()
        {
            UpdatePhase(Time.deltaTime);
            UpdateSpark(Time.deltaTime);
            UpdateFlash(Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTriggerImpact(other != null ? other.transform.position : transform.position);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Vector3 position = transform.position;
            if (collision != null && collision.contactCount > 0)
                position = collision.GetContact(0).point;

            TryTriggerImpact(position);
        }

        private void UpdatePhase(float deltaTime)
        {
            string stateName = GetCurrentStateName();
            if (stateName != _lastStateName)
            {
                _lastStateName = stateName;
                _elapsedInState = 0f;
            }
            else
            {
                _elapsedInState += deltaTime;
            }

            _currentPhase = ChargeRusherReferencePhaseResolver.Resolve(
                stateName,
                _elapsedInState,
                _telegraphDuration,
                _dashDuration,
                _recoveryDuration);
            _impactGate.UpdatePhase(_currentPhase);
        }

        private string GetCurrentStateName()
        {
            var currentState = _brain != null && _brain.StateMachine != null
                ? _brain.StateMachine.CurrentState
                : null;

            return currentState != null ? currentState.GetType().Name : string.Empty;
        }

        private void TryTriggerImpact(Vector3 impactPosition)
        {
            if (!_impactGate.TryTrigger(_currentPhase))
                return;

            TriggerHitStop();
            TriggerCameraShake();
            PlaySpark(impactPosition);
            StartFlash();
            PlayOneShot(_impactClip);
        }

        private void TriggerHitStop()
        {
            if (_hitStopDuration <= 0f)
                return;

            HitStopEffect.Trigger(_hitStopDuration);
        }

        private void TriggerCameraShake()
        {
            if (_cameraShakeDuration <= 0f)
                return;

            if (ServiceLocator.TryGet<CameraShakeService>(out var cameraShake))
            {
                cameraShake.Shake(_cameraShakeDuration, _cameraShakeAmplitude, _cameraShakeFrequency);
                return;
            }

            if (_hasLoggedMissingCameraShake)
                return;

            _hasLoggedMissingCameraShake = true;
            Debug.LogWarning($"[{nameof(ChargeRusherReferenceImpactFeedback)}] CameraShakeService is missing; impact camera shake skipped.", this);
        }

        private void BuildSparkRenderer()
        {
            var sparkObject = new GameObject("ReferenceImpactSpark");
            sparkObject.transform.SetParent(transform.parent, worldPositionStays: false);
            sparkObject.SetActive(false);

            _sparkRenderer = sparkObject.AddComponent<SpriteRenderer>();
            CopySourceRendererSettings(_sparkRenderer);
        }

        private void CopySourceRendererSettings(SpriteRenderer target)
        {
            if (_sourceRenderer == null || target == null)
                return;

            target.sprite = _sourceRenderer.sprite;
            target.flipX = _sourceRenderer.flipX;
            target.flipY = _sourceRenderer.flipY;
            target.sortingLayerID = _sourceRenderer.sortingLayerID;
            target.sortingOrder = _sourceRenderer.sortingOrder + _sparkSortingOffset;
            target.sharedMaterial = _sourceRenderer.sharedMaterial;
            target.color = _sparkColor;
        }

        private void PlaySpark(Vector3 impactPosition)
        {
            if (_sparkRenderer == null || _sourceRenderer == null)
                return;

            _sparkTimer = 0f;
            _sparkActive = true;
            _sparkRenderer.sprite = _sourceRenderer.sprite;
            _sparkRenderer.flipX = _sourceRenderer.flipX;
            _sparkRenderer.flipY = _sourceRenderer.flipY;
            _sparkRenderer.sortingLayerID = _sourceRenderer.sortingLayerID;
            _sparkRenderer.sortingOrder = _sourceRenderer.sortingOrder + _sparkSortingOffset;
            _sparkRenderer.transform.position = impactPosition;
            _sparkRenderer.transform.rotation = transform.rotation;
            _sparkRenderer.transform.localScale = Vector3.Scale(transform.localScale, _sparkScale);
            _sparkRenderer.color = _sparkColor;
            _sparkRenderer.gameObject.SetActive(true);
        }

        private void UpdateSpark(float deltaTime)
        {
            if (!_sparkActive || _sparkRenderer == null)
                return;

            _sparkTimer += deltaTime;
            float normalizedAge = Mathf.Clamp01(_sparkTimer / _sparkLifetime);
            var color = _sparkColor;
            color.a = _sparkColor.a * (1f - normalizedAge);
            _sparkRenderer.color = color;

            if (_sparkTimer >= _sparkLifetime)
            {
                _sparkActive = false;
                _sparkRenderer.gameObject.SetActive(false);
            }
        }

        private void StartFlash()
        {
            _flashActive = true;
            _flashTimer = 0f;
            transform.localScale = Vector3.Scale(_baseScale, _flashScale);

            if (_sourceRenderer != null)
                _sourceRenderer.color = _flashColor;
        }

        private void UpdateFlash(float deltaTime)
        {
            if (!_flashActive)
                return;

            _flashTimer += deltaTime;
            if (_flashTimer < _flashDuration)
                return;

            _flashActive = false;
            transform.localScale = _baseScale;

            if (_sourceRenderer != null)
                _sourceRenderer.color = _baseColor;
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
                Debug.LogError($"[{nameof(ChargeRusherReferenceImpactFeedback)}] Missing EnemyBrain on {name}.", this);

            if (_sourceRenderer == null)
                Debug.LogError($"[{nameof(ChargeRusherReferenceImpactFeedback)}] Missing SpriteRenderer on {name}.", this);
        }
    }
}
