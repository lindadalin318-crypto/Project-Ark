using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using ProjectArk.Combat;
using ProjectArk.Heat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Top-level UI controller. Handles Star Chart panel toggle,
    /// time pause, and input action suppression during menu.
    /// Place on the Canvas root GameObject.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("UI Components")]
        [SerializeField] private StarChartPanel _starChartPanel;
        [SerializeField] private HeatBarHUD _heatBarHUD;

        [Header("Transition")]
        [Tooltip("Optional — drives camera zoom, post-processing and SFX when entering/exiting the Star Chart.")]
        [SerializeField] private WeavingStateTransition _weavingTransition;

        [Header("Inventory Data")]
        [SerializeField] private StarChartInventorySO _playerInventory;

        private InputAction _toggleStarChartAction;
        private InputAction _fireAction;
        private InputAction _fireSecondaryAction;

        private bool _isPanelOpen;

        private void Awake()
        {
            var shipMap = _inputActions.FindActionMap("Ship");
            _toggleStarChartAction = shipMap.FindAction("ToggleStarChart");
            _fireAction = shipMap.FindAction("Fire");
            _fireSecondaryAction = shipMap.FindAction("FireSecondary");

            // Auto-configure InputSystemUIInputModule with UI action map references
            ConfigureUIInputModule();
        }

        /// <summary>
        /// Wires up the InputSystemUIInputModule with actions from the "UI" action map.
        /// Without this, the module has no idea where the pointer is and buttons won't work.
        /// </summary>
        private void ConfigureUIInputModule()
        {
            var uiModule = FindAnyObjectByType<InputSystemUIInputModule>();
            if (uiModule == null)
            {
                Debug.LogWarning("[UIManager] InputSystemUIInputModule not found in scene.");
                return;
            }

            var uiMap = _inputActions.FindActionMap("UI");
            if (uiMap == null)
            {
                Debug.LogError("[UIManager] 'UI' action map not found in InputActionAsset.");
                return;
            }

            uiModule.actionsAsset = _inputActions;
            uiModule.point = InputActionReference.Create(_inputActions.FindAction("UI/Point"));
            uiModule.leftClick = InputActionReference.Create(_inputActions.FindAction("UI/Click"));
            uiModule.scrollWheel = InputActionReference.Create(_inputActions.FindAction("UI/ScrollWheel"));
            uiModule.move = InputActionReference.Create(_inputActions.FindAction("UI/Navigate"));
            uiModule.submit = InputActionReference.Create(_inputActions.FindAction("UI/Submit"));
            uiModule.cancel = InputActionReference.Create(_inputActions.FindAction("UI/Cancel"));

            // Ensure the UI map is always enabled
            uiMap.Enable();
        }

        private void Start()
        {
            // 查找游戏系统
            var controller = FindAnyObjectByType<StarChartController>();
            var heatSystem = FindAnyObjectByType<HeatSystem>();

            // 绑定热量条
            if (_heatBarHUD != null)
                _heatBarHUD.Bind(heatSystem);

            // 绑定星图面板
            if (_starChartPanel != null && controller != null && _playerInventory != null)
            {
                _starChartPanel.Bind(controller, _playerInventory);
                _starChartPanel.gameObject.SetActive(false);
            }
            else if (controller == null)
            {
                Debug.LogWarning("[UIManager] StarChartController not found in scene.");
            }

            _isPanelOpen = false;
        }

        private void OnEnable()
        {
            if (_toggleStarChartAction != null)
                _toggleStarChartAction.performed += OnToggleStarChartPerformed;
        }

        private void OnDisable()
        {
            if (_toggleStarChartAction != null)
                _toggleStarChartAction.performed -= OnToggleStarChartPerformed;

            // Restore game state without transition animations
            // (coroutines can't run on an inactive GameObject).
            if (_isPanelOpen)
            {
                _isPanelOpen = false;
                _starChartPanel?.Close();
                Time.timeScale = 1f;
                _fireAction?.Enable();
                _fireSecondaryAction?.Enable();
            }
        }

        private void OnToggleStarChartPerformed(InputAction.CallbackContext ctx)
        {
            Toggle();
        }

        /// <summary> Toggle the Star Chart panel open/closed. </summary>
        public void Toggle()
        {
            if (_isPanelOpen)
                ClosePanel();
            else
                OpenPanel();
        }

        private void OpenPanel()
        {
            _isPanelOpen = true;

            // 禁用射击输入（防止面板中左键触发开火）
            _fireAction?.Disable();
            _fireSecondaryAction?.Disable();

            // 暂停游戏
            Time.timeScale = 0f;

            // Trigger enter-weaving visual/audio transition (runs on unscaled time).
            _weavingTransition?.EnterWeavingState();

            // 打开面板
            _starChartPanel?.Open();
        }

        private void ClosePanel()
        {
            _isPanelOpen = false;

            // Trigger exit-weaving visual/audio transition (runs on unscaled time).
            _weavingTransition?.ExitWeavingState();

            // 关闭面板
            _starChartPanel?.Close();

            // 恢复游戏
            Time.timeScale = 1f;

            // 恢复射击输入
            _fireAction?.Enable();
            _fireSecondaryAction?.Enable();
        }
    }
}
