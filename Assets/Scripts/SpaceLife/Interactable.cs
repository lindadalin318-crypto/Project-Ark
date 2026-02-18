
using ProjectArk.Core;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectArk.SpaceLife
{
    public class Interactable : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _interactionText = "交互";
        [SerializeField] private float _interactionRange = 2f;
        [SerializeField] private bool _showIndicator = true;

        [Header("Indicator")]
        [Tooltip("Optional: assign a pre-created indicator child. If empty, one will be auto-created at Awake.")]
        [SerializeField] private GameObject _indicator;

        [Header("Events")]
        public UnityEvent OnInteract;

        private bool _isInRange;
        private PlayerController2D _cachedPlayer;

        public string InteractionText
        {
            get => _interactionText;
            set => _interactionText = value;
        }

        public float InteractionRange => _interactionRange;
        public bool IsInRange => _isInRange;

        private void Awake()
        {
            EnsureIndicator();
        }

        private void Start()
        {
            _cachedPlayer = ServiceLocator.Get<PlayerController2D>();
        }

        private void Update()
        {
            CheckPlayerInRange();
            UpdateIndicator();
        }

        private void CheckPlayerInRange()
        {
            if (_cachedPlayer == null)
            {
                _isInRange = false;
                return;
            }

            float distance = Vector2.Distance(transform.position, _cachedPlayer.transform.position);
            _isInRange = distance <= _interactionRange;
        }

        public virtual void Interact()
        {
            if (!_isInRange) return;

            Debug.Log($"[Interactable] Interacted with {gameObject.name}");
            OnInteract?.Invoke();
        }

        /// <summary>
        /// Ensures an indicator child exists. Created once at Awake, toggled via SetActive.
        /// </summary>
        private void EnsureIndicator()
        {
            if (_indicator != null)
            {
                _indicator.SetActive(false);
                return;
            }

            // Auto-create a simple indicator child
            _indicator = new GameObject("InteractionIndicator");
            _indicator.transform.SetParent(transform);
            _indicator.transform.localPosition = Vector3.up * 1.5f;

            var spriteRenderer = _indicator.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.yellow;
            spriteRenderer.sortingOrder = 100;

            _indicator.SetActive(false);
        }

        private void UpdateIndicator()
        {
            if (!_showIndicator || _indicator == null) return;

            bool shouldShow = _isInRange;
            if (_indicator.activeSelf != shouldShow)
            {
                _indicator.SetActive(shouldShow);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}

