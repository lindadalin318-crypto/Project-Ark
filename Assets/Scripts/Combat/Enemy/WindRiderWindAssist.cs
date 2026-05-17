using ProjectArk.Core;
using ProjectArk.HyperWind;
using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// E1 Wind Rider MVP assist layer. Keeps the readable ChargeRusher telegraph/charge/recovery model,
    /// but adds an environmental velocity layer when the enemy moves with the sampled HyperWind direction.
    /// </summary>
    [RequireComponent(typeof(EnemyEntity))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WindRiderWindAssist : MonoBehaviour
    {
        [SerializeField] private bool _enableWindAssist = true;
        [SerializeField] [Min(0f)] private float _windBoostMultiplier = 1.25f;
        [SerializeField] [Range(-1f, 1f)] private float _alignmentThreshold = 0.25f;
        [SerializeField] [Min(0f)] private float _minimumSpeedForAssist = 0.2f;
        [SerializeField] private Color _windAlignedColor = new(0.35f, 0.95f, 1f, 1f);
        [SerializeField] private Color _baseFallbackColor = new(1f, 0.45f, 0.15f, 1f);

        private EnemyEntity _entity;
        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private IWindFieldService _windFieldService;
        private Vector2 _appliedAssistVelocity;
        private bool _assistApplied;
        private Color _baseColor;

        public Vector2 CurrentAssistVelocity => _appliedAssistVelocity;

        private void Awake()
        {
            _entity = GetComponent<EnemyEntity>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseColor = _spriteRenderer != null ? _spriteRenderer.color : _baseFallbackColor;
        }

        private void OnDisable()
        {
            RemoveAssistVelocity();
            RestoreColor();
        }

        private void LateUpdate()
        {
            // EnemyBrain / EnemyEntity writes the authoritative movement velocity during Update.
            // Treat wind assist as a transient late-frame layer on top of that fresh base velocity;
            // do not subtract last frame's assist here, or we would over-correct after MoveAtSpeed() overwrites velocity.
            _appliedAssistVelocity = Vector2.zero;
            _assistApplied = false;

            if (!_enableWindAssist || _entity == null || !_entity.IsAlive || _rigidbody == null)

            {
                RestoreColor();
                return;
            }

            if (_windFieldService == null && !ServiceLocator.TryGet(out _windFieldService))
            {
                RestoreColor();
                return;
            }

            Vector2 baseVelocity = _rigidbody.linearVelocity;
            if (baseVelocity.magnitude < _minimumSpeedForAssist)
            {
                RestoreColor();
                return;
            }

            WindSample sample = _windFieldService.Sample(_rigidbody.position);
            if (!sample.HasWind)
            {
                RestoreColor();
                return;
            }

            float alignment = Vector2.Dot(baseVelocity.normalized, sample.Direction);
            if (alignment < _alignmentThreshold)
            {
                RestoreColor();
                return;
            }

            _appliedAssistVelocity = sample.Velocity * _windBoostMultiplier * alignment;
            _rigidbody.linearVelocity += _appliedAssistVelocity;
            _assistApplied = true;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.Lerp(_baseColor, _windAlignedColor, Mathf.Clamp01(alignment));
            }
        }

        private void RemoveAssistVelocity()
        {
            if (!_assistApplied || _rigidbody == null)
            {
                return;
            }

            _rigidbody.linearVelocity -= _appliedAssistVelocity;
            _appliedAssistVelocity = Vector2.zero;
            _assistApplied = false;
        }

        private void RestoreColor()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
            }
        }

        private void OnValidate()
        {
            _windBoostMultiplier = Mathf.Max(0f, _windBoostMultiplier);
            _minimumSpeedForAssist = Mathf.Max(0f, _minimumSpeedForAssist);
        }
    }
}
