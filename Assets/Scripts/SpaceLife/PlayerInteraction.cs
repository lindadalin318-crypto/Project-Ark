
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private float _interactionRange = 2f;

        private Interactable _nearestInteractable;

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

        private void HandleInteractionInput()
        {
            if (Input.GetKeyDown(_interactKey) && _nearestInteractable != null)
            {
                _nearestInteractable.Interact();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}

