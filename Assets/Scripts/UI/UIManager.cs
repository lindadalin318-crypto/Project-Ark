using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("Inventory Data")]
        [SerializeField] private StarChartInventorySO _playerInventory;

        private InputAction _pauseAction;
        private InputAction _fireAction;
        private InputAction _fireSecondaryAction;

        private bool _isPanelOpen;

        private void Awake()
        {
            var shipMap = _inputActions.FindActionMap("Ship");
            _pauseAction = shipMap.FindAction("Pause");
            _fireAction = shipMap.FindAction("Fire");
            _fireSecondaryAction = shipMap.FindAction("FireSecondary");
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
            if (_pauseAction != null)
                _pauseAction.performed += OnPausePerformed;
        }

        private void OnDisable()
        {
            if (_pauseAction != null)
                _pauseAction.performed -= OnPausePerformed;

            // 确保退出时恢复时间
            if (_isPanelOpen)
                ClosePanel();
        }

        private void OnPausePerformed(InputAction.CallbackContext ctx)
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

            // 打开面板
            _starChartPanel?.Open();
        }

        private void ClosePanel()
        {
            _isPanelOpen = false;

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
