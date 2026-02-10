using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Heat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Always-visible heat bar HUD element.
    /// Subscribes to HeatSystem events for reactive updates.
    /// </summary>
    public class HeatBarHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _overheatFlash;
        [SerializeField] private TMP_Text _label;

        [Header("Visual Settings")]
        [SerializeField] private Gradient _heatGradient;
        [SerializeField] private float _flashSpeed = 4f;

        private HeatSystem _heatSystem;
        private bool _isOverheated;
        private float _flashTimer;

        private const string LABEL_OVERHEATED = "OVERHEATED";

        /// <summary>
        /// Inject the HeatSystem reference. Pass null to hide the bar.
        /// </summary>
        public void Bind(HeatSystem heatSystem)
        {
            // 取消旧订阅
            Unbind();

            _heatSystem = heatSystem;

            if (_heatSystem == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _heatSystem.OnHeatChanged += HandleHeatChanged;
            _heatSystem.OnOverheated += HandleOverheated;
            _heatSystem.OnCooldownComplete += HandleCooldownComplete;

            // 初始状态
            UpdateFill(_heatSystem.NormalizedHeat);
            UpdateLabel();
            SetOverheatVisual(false);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Update()
        {
            if (!_isOverheated || _overheatFlash == null) return;

            // 过热时红色闪烁动画
            _flashTimer += Time.unscaledDeltaTime * _flashSpeed;
            float alpha = (Mathf.Sin(_flashTimer) + 1f) * 0.5f * 0.6f;
            var color = _overheatFlash.color;
            color.a = alpha;
            _overheatFlash.color = color;
        }

        private void HandleHeatChanged(float normalizedHeat)
        {
            UpdateFill(normalizedHeat);
            if (!_isOverheated)
                UpdateLabel();
        }

        private void HandleOverheated()
        {
            SetOverheatVisual(true);
        }

        private void HandleCooldownComplete()
        {
            SetOverheatVisual(false);
        }

        private void UpdateFill(float normalizedHeat)
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = normalizedHeat;

                if (_heatGradient != null)
                    _fillImage.color = _heatGradient.Evaluate(normalizedHeat);
            }
        }

        private void SetOverheatVisual(bool overheated)
        {
            _isOverheated = overheated;

            if (_label != null)
                _label.text = overheated ? LABEL_OVERHEATED : FormatHeatLabel();

            if (_overheatFlash != null)
            {
                _flashTimer = 0f;
                var color = _overheatFlash.color;
                color.a = 0f;
                _overheatFlash.color = color;
            }
        }

        private string FormatHeatLabel()
        {
            if (_heatSystem == null) return "HEAT";
            return $"HEAT({_heatSystem.CurrentHeat:F0}/{_heatSystem.MaxHeat:F0})";
        }

        private void UpdateLabel()
        {
            if (_label != null)
                _label.text = FormatHeatLabel();
        }

        private void Unbind()
        {
            if (_heatSystem != null)
            {
                _heatSystem.OnHeatChanged -= HandleHeatChanged;
                _heatSystem.OnOverheated -= HandleOverheated;
                _heatSystem.OnCooldownComplete -= HandleCooldownComplete;
                _heatSystem = null;
            }
        }
    }
}
