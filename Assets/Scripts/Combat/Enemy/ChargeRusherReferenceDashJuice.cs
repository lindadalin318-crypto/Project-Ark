using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// ReferenceOnly dash juice for the Minishoot ChargeRusher prototype.
    /// Adds pooled afterimages and a short attack burst without changing gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ChargeRusherReferenceDashJuice : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private SpriteRenderer _sourceRenderer;

        [Header("State Timing")]
        [SerializeField, Min(0f)] private float _telegraphDuration = 0.38f;
        [SerializeField, Min(0f)] private float _dashDuration = 0.42f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.28f;

        [Header("Afterimages")]
        [SerializeField, Min(1)] private int _afterimageCount = 5;
        [SerializeField, Min(0f)] private float _afterimageInterval = 0.045f;
        [SerializeField, Min(0.01f)] private float _afterimageLifetime = 0.18f;
        [SerializeField] private Color _afterimageColor = new Color(1f, 0.72f, 0.18f, 0.46f);
        [SerializeField] private Vector3 _afterimageScale = new Vector3(1.08f, 1.08f, 1f);
        [SerializeField] private int _afterimageSortingOffset = -1;

        [Header("Attack Burst")]
        [SerializeField, Min(0f)] private float _burstDuration = 0.08f;
        [SerializeField] private Vector3 _burstScale = new Vector3(1.24f, 1.24f, 1f);
        [SerializeField] private Color _burstColor = new Color(1f, 0.92f, 0.32f, 1f);

        private ChargeRusherReferenceAfterimageSampler _sampler;
        private SpriteRenderer[] _afterimages;
        private float[] _afterimageAges;
        private int _nextAfterimageIndex;
        private string _lastStateName = string.Empty;
        private float _elapsedInState;
        private ChargeRusherReferencePhase _currentPhase = ChargeRusherReferencePhase.Idle;
        private Vector3 _baseScale;
        private Color _baseColor;
        private bool _burstActive;
        private float _burstTimer;

        private void Awake()
        {
            if (_brain == null)
                _brain = GetComponent<EnemyBrain>();

            if (_sourceRenderer == null)
                _sourceRenderer = GetComponent<SpriteRenderer>();

            _baseScale = transform.localScale;
            _baseColor = _sourceRenderer != null ? _sourceRenderer.color : Color.white;
            _sampler = new ChargeRusherReferenceAfterimageSampler(_afterimageInterval);
            BuildAfterimagePool();
            ValidateReferences();
        }

        private void OnDisable()
        {
            _lastStateName = string.Empty;
            _elapsedInState = 0f;
            _currentPhase = ChargeRusherReferencePhase.Idle;
            _sampler?.Reset();
            _burstActive = false;
            _burstTimer = 0f;
            transform.localScale = _baseScale;

            if (_sourceRenderer != null)
                _sourceRenderer.color = _baseColor;

            HideAllAfterimages();
        }

        private void Update()
        {
            string stateName = GetCurrentStateName();
            if (stateName != _lastStateName)
            {
                _lastStateName = stateName;
                _elapsedInState = 0f;
                _sampler.Reset();
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

            if (phase != _currentPhase)
            {
                if (phase == ChargeRusherReferencePhase.Dashing)
                    TriggerBurst();

                _currentPhase = phase;
            }

            if (_sampler.ShouldEmit(phase, Time.time))
                EmitAfterimage();

            UpdateBurst();
            UpdateAfterimages(Time.deltaTime);
        }

        private string GetCurrentStateName()
        {
            var currentState = _brain != null && _brain.StateMachine != null
                ? _brain.StateMachine.CurrentState
                : null;

            return currentState != null ? currentState.GetType().Name : string.Empty;
        }

        private void BuildAfterimagePool()
        {
            int count = Mathf.Max(1, _afterimageCount);
            _afterimages = new SpriteRenderer[count];
            _afterimageAges = new float[count];

            for (int i = 0; i < count; i++)
            {
                var afterimageObject = new GameObject($"ReferenceAfterimage_{i + 1:00}");
                afterimageObject.transform.SetParent(transform.parent, worldPositionStays: false);
                afterimageObject.SetActive(false);

                var renderer = afterimageObject.AddComponent<SpriteRenderer>();
                CopyRendererSettings(renderer);
                _afterimages[i] = renderer;
                _afterimageAges[i] = _afterimageLifetime;
            }
        }

        private void CopyRendererSettings(SpriteRenderer target)
        {
            if (_sourceRenderer == null || target == null)
                return;

            target.sprite = _sourceRenderer.sprite;
            target.flipX = _sourceRenderer.flipX;
            target.flipY = _sourceRenderer.flipY;
            target.sortingLayerID = _sourceRenderer.sortingLayerID;
            target.sortingOrder = _sourceRenderer.sortingOrder + _afterimageSortingOffset;
            target.sharedMaterial = _sourceRenderer.sharedMaterial;
            target.color = _afterimageColor;
        }

        private void EmitAfterimage()
        {
            if (_sourceRenderer == null || _afterimages == null || _afterimages.Length == 0)
                return;

            var renderer = _afterimages[_nextAfterimageIndex];
            _afterimageAges[_nextAfterimageIndex] = 0f;
            _nextAfterimageIndex = (_nextAfterimageIndex + 1) % _afterimages.Length;

            renderer.sprite = _sourceRenderer.sprite;
            renderer.flipX = _sourceRenderer.flipX;
            renderer.flipY = _sourceRenderer.flipY;
            renderer.sortingLayerID = _sourceRenderer.sortingLayerID;
            renderer.sortingOrder = _sourceRenderer.sortingOrder + _afterimageSortingOffset;
            renderer.transform.position = transform.position;
            renderer.transform.rotation = transform.rotation;
            renderer.transform.localScale = Vector3.Scale(transform.localScale, _afterimageScale);
            renderer.color = _afterimageColor;
            renderer.gameObject.SetActive(true);
        }

        private void UpdateAfterimages(float deltaTime)
        {
            if (_afterimages == null)
                return;

            for (int i = 0; i < _afterimages.Length; i++)
            {
                var renderer = _afterimages[i];
                if (renderer == null || !renderer.gameObject.activeSelf)
                    continue;

                _afterimageAges[i] += deltaTime;
                float normalizedAge = Mathf.Clamp01(_afterimageAges[i] / _afterimageLifetime);
                var color = _afterimageColor;
                color.a = _afterimageColor.a * (1f - normalizedAge);
                renderer.color = color;

                if (_afterimageAges[i] >= _afterimageLifetime)
                    renderer.gameObject.SetActive(false);
            }
        }

        private void HideAllAfterimages()
        {
            if (_afterimages == null)
                return;

            for (int i = 0; i < _afterimages.Length; i++)
            {
                if (_afterimages[i] == null)
                    continue;

                _afterimages[i].gameObject.SetActive(false);
                _afterimageAges[i] = _afterimageLifetime;
            }
        }

        private void TriggerBurst()
        {
            _burstActive = true;
            _burstTimer = 0f;

            if (_sourceRenderer != null)
                _sourceRenderer.color = _burstColor;

            transform.localScale = Vector3.Scale(_baseScale, _burstScale);
        }

        private void UpdateBurst()
        {
            if (!_burstActive)
                return;

            _burstTimer += Time.deltaTime;
            if (_burstTimer < _burstDuration)
                return;

            _burstActive = false;
            transform.localScale = _baseScale;

            if (_sourceRenderer != null)
                _sourceRenderer.color = _baseColor;
        }

        private void ValidateReferences()
        {
            if (_brain == null)
                Debug.LogError($"[{nameof(ChargeRusherReferenceDashJuice)}] Missing EnemyBrain on {name}.", this);

            if (_sourceRenderer == null)
                Debug.LogError($"[{nameof(ChargeRusherReferenceDashJuice)}] Missing SpriteRenderer on {name}.", this);
        }
    }
}
