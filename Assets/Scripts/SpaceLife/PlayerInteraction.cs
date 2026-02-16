
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.SpaceLife
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Settings")]
        [SerializeField] private float _interactionRange = 2f;

        private InputAction _interactAction;
        private Interactable _nearestInteractable;
        private bool _interactRequested;

        private void Awake()
        {
            var shipMap = _inputActions.FindActionMap("Ship");
            _interactAction = shipMap.FindAction("Interact");
        }

        private void OnEnable()
        {
            if (_interactAction != null)
            {
                _interactAction.Enable();
                _interactAction.performed += OnInteractActionPerformed;
            }
        }

        private void OnDisable()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractActionPerformed;
                _interactAction.Disable();
            }
        }

        private void Update()
        {
            FindNearestInteractable();
            HandleInteractionInput();
        }

        private void FindNearestInteractable()
        {
            _nearestInteractable = null;
            float nearestDistance = float.MaxValue;

            Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);

            foreach (var interactable in interactables)
            {
                float distance = Vector2.Distance(transform.position, interactable.transform.position);

                if (distance <= _interactionRange && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _nearestInteractable = interactable;
                }
            }
        }

        private void OnInteractActionPerformed(InputAction.CallbackContext ctx)
        {
            _interactRequested = true;
        }

        private void HandleInteractionInput()
        {
            if (_interactRequested && _nearestInteractable != null)
            {
                _nearestInteractable.Interact();
                _interactRequested = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}

