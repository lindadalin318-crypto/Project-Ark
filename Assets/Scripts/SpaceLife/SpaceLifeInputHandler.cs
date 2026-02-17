
using ProjectArk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.SpaceLife
{
    public class SpaceLifeInputHandler : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        private InputAction _toggleSpaceLifeAction;
        private SpaceLifeManager _spaceLifeManager;

        private void Awake()
        {
            Debug.Log("[SpaceLifeInputHandler] Awake called");
            if (_inputActions == null)
            {
                Debug.LogWarning("[SpaceLifeInputHandler] InputActions is NULL, trying to find it...");
#if UNITY_EDITOR
                TryFindInputActionAsset();
#endif
            }

            if (_inputActions == null)
            {
                Debug.LogError("[SpaceLifeInputHandler] InputActions still NULL! Please assign it in Inspector!");
                return;
            }

            var spaceLifeMap = _inputActions.FindActionMap("SpaceLife");
            if (spaceLifeMap == null)
            {
                Debug.LogError("[SpaceLifeInputHandler] SpaceLife ActionMap not found!");
                return;
            }

            _toggleSpaceLifeAction = spaceLifeMap.FindAction("ToggleSpaceLife");
            if (_toggleSpaceLifeAction == null)
            {
                Debug.LogError("[SpaceLifeInputHandler] ToggleSpaceLife Action not found!");
            }
            else
            {
                Debug.Log("[SpaceLifeInputHandler] ToggleSpaceLife Action found!");
            }
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
                    Debug.Log($"[SpaceLifeInputHandler] Auto-found InputActionAsset: {path}");
                    break;
                }
            }
        }
#endif

        private void Start()
        {
            _spaceLifeManager = ServiceLocator.Get<SpaceLifeManager>();
            Debug.Log($"[SpaceLifeInputHandler] SpaceLifeManager from ServiceLocator: {_spaceLifeManager != null}");
        }

        private void OnEnable()
        {
            Debug.Log("[SpaceLifeInputHandler] OnEnable called");
            if (_inputActions == null)
            {
                Debug.LogError("[SpaceLifeInputHandler] No InputActions available!");
                return;
            }

            var spaceLifeMap = _inputActions.FindActionMap("SpaceLife");
            if (spaceLifeMap != null && !spaceLifeMap.enabled)
            {
                spaceLifeMap.Enable();
                Debug.Log("[SpaceLifeInputHandler] Enabled SpaceLife ActionMap");
            }

            if (_toggleSpaceLifeAction != null)
            {
                _toggleSpaceLifeAction.performed += OnToggleSpaceLifePerformed;
                Debug.Log("[SpaceLifeInputHandler] ToggleSpaceLife callback registered!");
            }
        }

        private void OnDisable()
        {
            if (_toggleSpaceLifeAction != null && _inputActions != null)
            {
                _toggleSpaceLifeAction.performed -= OnToggleSpaceLifePerformed;
            }
            
            if (_inputActions != null)
            {
                var spaceLifeMap = _inputActions.FindActionMap("SpaceLife");
                if (spaceLifeMap != null && spaceLifeMap.enabled)
                {
                    spaceLifeMap.Disable();
                }
            }
        }

        private void OnToggleSpaceLifePerformed(InputAction.CallbackContext ctx)
        {
            Debug.Log("[SpaceLifeInputHandler] ToggleSpaceLife PERFORMED!");
            if (_spaceLifeManager != null)
            {
                _spaceLifeManager.ToggleSpaceLife();
            }
            else
            {
                Debug.LogWarning("[SpaceLifeInputHandler] SpaceLifeManager not found!");
            }
        }
    }
}

