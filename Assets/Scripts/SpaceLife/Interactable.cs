
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

        [Header("Events")]
        public UnityEvent OnInteract;

        private bool _isInRange;
        private GameObject _indicator;
        private PlayerController2D _cachedPlayer;

        public string InteractionText
        {
            get => _interactionText;
            set => _interactionText = value;
        }

        public float InteractionRange => _interactionRange;
        public bool IsInRange => _isInRange;

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

        private void UpdateIndicator()
        {
            if (!_showIndicator) return;

            if (_isInRange && _indicator == null)
            {
                CreateIndicator();
            }
            else if (!_isInRange && _indicator != null)
            {
                DestroyIndicator();
            }
        }

        private void CreateIndicator()
        {
            _indicator = new GameObject("InteractionIndicator");
            _indicator.transform.SetParent(transform);
            _indicator.transform.localPosition = Vector3.up * 1.5f;

            var spriteRenderer = _indicator.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.yellow;
            spriteRenderer.sortingOrder = 100;
        }

        private void DestroyIndicator()
        {
            if (_indicator != null)
            {
                Destroy(_indicator);
                _indicator = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}

