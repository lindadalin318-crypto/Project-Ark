
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    [RequireComponent(typeof(Interactable))]
    public class Door : MonoBehaviour
    {
        [Header("Door Settings")]
        [SerializeField] private Transform _targetPosition;
        [SerializeField] private Room _targetRoom;
        [SerializeField] private bool _autoOpen = true;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private Sprite _closedSprite;

        private Interactable _interactable;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            SetupInteractable();
        }

        private void SetupInteractable()
        {
            if (_interactable != null)
            {
                _interactable.OnInteract.AddListener(OnInteract);
                _interactable.InteractionText = "进入";
            }
        }

        private void OnInteract()
        {
            UseDoor();
        }

        public void UseDoor()
        {
            PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
            if (player == null) return;

            if (_targetPosition != null)
            {
                player.transform.position = _targetPosition.position;
            }

            if (_targetRoom != null && RoomManager.Instance != null)
            {
                RoomManager.Instance.SetCurrentRoom(_targetRoom);
            }

            ToggleOpen();
        }

        public void ToggleOpen()
        {
            _isOpen = !_isOpen;
            UpdateVisuals();
        }

        public void SetOpen(bool open)
        {
            _isOpen = open;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_animator != null)
            {
                _animator.SetBool("IsOpen", _isOpen);
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = _isOpen ? _openSprite : _closedSprite;
            }
        }

        private void OnDestroy()
        {
            if (_interactable != null)
            {
                _interactable.OnInteract.RemoveListener(OnInteract);
            }
        }
    }
}

