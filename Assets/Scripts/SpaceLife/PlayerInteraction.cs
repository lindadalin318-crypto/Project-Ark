using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.SpaceLife
{
    /// <summary>
    /// Detects nearby Interactable objects via trigger overlap and handles player interaction input.
    /// Requires a Collider2D (set as trigger) whose radius matches the interaction range.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Settings")]
        [SerializeField] private float _interactionRange = 2f;

        private InputAction _interactAction;
        private Interactable _nearestInteractable;
        private bool _interactRequested;
        private readonly List<Interactable> _nearbyInteractables = new();
        private CircleCollider2D _detectionCollider;

        private void Awake()
        {
            SetupDetectionCollider();

            if (_inputActions == null)
            {
                Debug.LogWarning("[PlayerInteraction] InputActions is NULL, trying to find it...");
#if UNITY_EDITOR
                TryFindInputActionAsset();
#endif
            }

            if (_inputActions != null)
            {
                var shipMap = _inputActions.FindActionMap("Ship");
                if (shipMap != null)
                {
                    _interactAction = shipMap.FindAction("Interact");
                }
            }
        }

        private void SetupDetectionCollider()
        {
            // Use existing CircleCollider2D or add one for detection
            _detectionCollider = GetComponent<CircleCollider2D>();
            if (_detectionCollider == null)
            {
                _detectionCollider = gameObject.AddComponent<CircleCollider2D>();
            }
            _detectionCollider.isTrigger = true;
            _detectionCollider.radius = _interactionRange;
        }

#if UNITY_EDITOR
        private void TryFindInputActionAsset()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("ShipActions t:InputActionAsset");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset != null)
                {
                    _inputActions = asset;
                    Debug.Log($"[PlayerInteraction] Auto-found InputActionAsset: {path}");
                    break;
                }
            }
        }
#endif

        private void OnEnable()
        {
            if (_inputActions == null) return;
            
            if (_interactAction != null)
            {
                _interactAction.Enable();
                _interactAction.performed += OnInteractActionPerformed;
            }
        }

        private void OnDisable()
        {
            if (_interactAction != null && _inputActions != null)
            {
                _interactAction.performed -= OnInteractActionPerformed;
                
                var shipMap = _inputActions.FindActionMap("Ship");
                if (shipMap != null && shipMap.enabled)
                {
                    _interactAction.Disable();
                }
            }
            _nearbyInteractables.Clear();
            _nearestInteractable = null;
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

            for (int i = _nearbyInteractables.Count - 1; i >= 0; i--)
            {
                // Clean up destroyed or disabled interactables
                if (_nearbyInteractables[i] == null || !_nearbyInteractables[i].isActiveAndEnabled)
                {
                    _nearbyInteractables.RemoveAt(i);
                    continue;
                }

                float distance = Vector2.Distance(transform.position, _nearbyInteractables[i].transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _nearestInteractable = _nearbyInteractables[i];
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Interactable>(out var interactable))
            {
                if (!_nearbyInteractables.Contains(interactable))
                {
                    _nearbyInteractables.Add(interactable);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent<Interactable>(out var interactable))
            {
                _nearbyInteractables.Remove(interactable);
                if (_nearestInteractable == interactable)
                {
                    _nearestInteractable = null;
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
