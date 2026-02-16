
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.SpaceLife
{
    public class SpaceLifeInputHandler : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        private InputAction _toggleSpaceLifeAction;

        private void Awake()
        {
            var shipMap = _inputActions.FindActionMap("Ship");
            _toggleSpaceLifeAction = shipMap.FindAction("ToggleSpaceLife");
        }

        private void OnEnable()
        {
            if (_toggleSpaceLifeAction != null)
            {
                _toggleSpaceLifeAction.Enable();
                _toggleSpaceLifeAction.performed += OnToggleSpaceLifePerformed;
            }
        }

        private void OnDisable()
        {
            if (_toggleSpaceLifeAction != null)
            {
                _toggleSpaceLifeAction.performed -= OnToggleSpaceLifePerformed;
                _toggleSpaceLifeAction.Disable();
            }
        }

        private void OnToggleSpaceLifePerformed(InputAction.CallbackContext ctx)
        {
            if (SpaceLifeManager.Instance != null)
            {
                SpaceLifeManager.Instance.ToggleSpaceLife();
            }
            else
            {
                Debug.LogWarning("[SpaceLifeInputHandler] SpaceLifeManager not found!");
            }
        }
    }
}

