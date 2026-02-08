using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Detail panel showing a selected item's stats and an Equip/Unequip button.
    /// </summary>
    public class ItemDetailView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _contentRoot;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionButtonLabel;

        /// <summary> Fired when the action button is clicked. (item, true=equip / false=unequip) </summary>
        public event Action<StarChartItemSO, bool> OnActionClicked;

        private StarChartItemSO _currentItem;
        private bool _currentIsEquipped;

        private void Awake()
        {
            if (_actionButton != null)
                _actionButton.onClick.AddListener(HandleActionButtonClick);

            Clear();
        }

        /// <summary> Display item details and set action button state. </summary>
        public void ShowItem(StarChartItemSO item, bool isEquipped)
        {
            _currentItem = item;
            _currentIsEquipped = isEquipped;

            if (_contentRoot != null)
                _contentRoot.SetActive(true);

            if (_icon != null)
            {
                _icon.sprite = item.Icon;
                _icon.color = item.Icon != null ? Color.white : Color.clear;
            }

            if (_nameText != null)
                _nameText.text = item.DisplayName;

            if (_descriptionText != null)
                _descriptionText.text = item.Description;

            if (_statsText != null)
                _statsText.text = BuildStatsText(item);

            if (_actionButtonLabel != null)
                _actionButtonLabel.text = isEquipped ? "UNEQUIP" : "EQUIP";
        }

        /// <summary> Hide the detail panel. </summary>
        public void Clear()
        {
            _currentItem = null;

            if (_contentRoot != null)
                _contentRoot.SetActive(false);
        }

        private void HandleActionButtonClick()
        {
            if (_currentItem != null)
                OnActionClicked?.Invoke(_currentItem, !_currentIsEquipped);
        }

        private string BuildStatsText(StarChartItemSO item)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Type: {item.ItemType}");
            sb.AppendLine($"Size: {item.SlotSize}");

            if (!Mathf.Approximately(item.HeatCost, 0f))
                sb.AppendLine($"Heat: {item.HeatCost:F0}");

            // 类型特有属性
            switch (item)
            {
                case StarCoreSO core:
                    sb.AppendLine($"Family: {core.Family}");
                    sb.AppendLine($"Damage: {core.BaseDamage:F0}");
                    sb.AppendLine($"Fire Rate: {core.FireRate:F1}/s");
                    sb.AppendLine($"Speed: {core.ProjectileSpeed:F0}");
                    break;

                case PrismSO prism:
                    sb.AppendLine($"Family: {prism.Family}");
                    var mods = prism.StatModifiers;
                    if (mods != null)
                    {
                        for (int i = 0; i < mods.Length; i++)
                        {
                            var m = mods[i];
                            string op = m.Operation == ModifierOperation.Add ? "+" : "×";
                            sb.AppendLine($"  {m.Stat}: {op}{m.Value:F1}");
                        }
                    }
                    break;

                case LightSailSO sail:
                    if (!string.IsNullOrEmpty(sail.EffectDescription))
                        sb.AppendLine($"Effect: {sail.EffectDescription}");
                    break;

                case SatelliteSO sat:
                    if (!string.IsNullOrEmpty(sat.TriggerDescription))
                        sb.AppendLine($"Trigger: {sat.TriggerDescription}");
                    if (!string.IsNullOrEmpty(sat.ActionDescription))
                        sb.AppendLine($"Action: {sat.ActionDescription}");
                    sb.AppendLine($"Cooldown: {sat.InternalCooldown:F1}s");
                    break;
            }

            return sb.ToString();
        }
    }
}
