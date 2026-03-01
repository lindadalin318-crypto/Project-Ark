using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Detail panel showing a selected item's stats, type tag, equipped status, and an Equip/Unequip button.
    /// </summary>
    public class ItemDetailView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _contentRoot;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _typeLabel;       // e.g. "CORE" colored by type
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _equippedLabel;   // "EQUIPPED" / "NOT EQUIPPED"
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

            // Icon
            if (_icon != null)
            {
                _icon.sprite = item.Icon;
                _icon.color = item.Icon != null ? Color.white : StarChartTheme.GetTypeColor(item.ItemType);
            }

            // Name
            if (_nameText != null)
                _nameText.text = item.DisplayName.ToUpper();

            // Type label (colored by type)
            if (_typeLabel != null)
            {
                _typeLabel.text = item.ItemType switch
                {
                    StarChartItemType.Core      => "◈  CORE",
                    StarChartItemType.Prism     => "◈  PRISM",
                    StarChartItemType.LightSail => "◈  SAIL",
                    StarChartItemType.Satellite => "◈  SAT",
                    _                           => item.ItemType.ToString().ToUpper()
                };
                _typeLabel.color = StarChartTheme.GetTypeColor(item.ItemType);
                _typeLabel.gameObject.SetActive(true);
            }

            // Description
            if (_descriptionText != null)
                _descriptionText.text = item.Description;

            // Stats
            if (_statsText != null)
                _statsText.text = BuildStatsText(item);

            // Equipped status label
            if (_equippedLabel != null)
            {
                _equippedLabel.text = isEquipped ? "● EQUIPPED" : "○ NOT EQUIPPED";
                _equippedLabel.color = isEquipped
                    ? StarChartTheme.HighlightValid
                    : new Color(1f, 1f, 1f, 0.35f);
                _equippedLabel.gameObject.SetActive(true);
            }

            // Action button
            if (_actionButtonLabel != null)
                _actionButtonLabel.text = isEquipped ? "UNEQUIP" : "EQUIP";

            if (_actionButton != null)
            {
                var btnImg = _actionButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isEquipped
                        ? new Color(0.6f, 0.15f, 0.15f, 0.85f)   // red tint for UNEQUIP
                        : new Color(0f, StarChartTheme.Cyan.g * 0.4f, StarChartTheme.Cyan.b * 0.5f, 1f);
                }
            }
        }

        /// <summary> Hide the detail panel. </summary>
        public void Clear()
        {
            _currentItem = null;

            if (_contentRoot != null)
                _contentRoot.SetActive(false);

            if (_typeLabel != null)
                _typeLabel.gameObject.SetActive(false);

            if (_equippedLabel != null)
                _equippedLabel.gameObject.SetActive(false);
        }

        private void HandleActionButtonClick()
        {
            if (_currentItem != null)
                OnActionClicked?.Invoke(_currentItem, !_currentIsEquipped);
        }

        private string BuildStatsText(StarChartItemSO item)
        {
            var sb = new System.Text.StringBuilder();

            // Slot size
            sb.AppendLine($"SIZE  {item.SlotSize}");

            // Heat cost
            if (!Mathf.Approximately(item.HeatCost, 0f))
                sb.AppendLine($"HEAT  ↑ {item.HeatCost:F0}");

            // Type-specific stats
            switch (item)
            {
                case StarCoreSO core:
                    sb.AppendLine($"FAMILY  {core.Family}");
                    sb.AppendLine($"DAMAGE  ↑ {core.BaseDamage:F0}");
                    sb.AppendLine($"FIRE RATE  ↑ {core.FireRate:F1}/s");
                    sb.AppendLine($"SPEED  ↑ {core.ProjectileSpeed:F0}");
                    break;

                case PrismSO prism:
                    sb.AppendLine($"FAMILY  {prism.Family}");
                    var mods = prism.StatModifiers;
                    if (mods != null)
                    {
                        for (int i = 0; i < mods.Length; i++)
                        {
                            var m = mods[i];
                            string arrow = m.Value >= 0 ? "↑" : "↓";
                            string op    = m.Operation == ModifierOperation.Add ? "+" : "×";
                            sb.AppendLine($"{m.Stat.ToString().ToUpper()}  {arrow} {op}{Mathf.Abs(m.Value):F1}");
                        }
                    }
                    break;

                case LightSailSO sail:
                    if (!string.IsNullOrEmpty(sail.EffectDescription))
                        sb.AppendLine($"EFFECT  {sail.EffectDescription}");
                    break;

                case SatelliteSO sat:
                    if (!string.IsNullOrEmpty(sat.TriggerDescription))
                        sb.AppendLine($"TRIGGER  {sat.TriggerDescription}");
                    if (!string.IsNullOrEmpty(sat.ActionDescription))
                        sb.AppendLine($"ACTION  {sat.ActionDescription}");
                    sb.AppendLine($"COOLDOWN  {sat.InternalCooldown:F1}s");
                    break;
            }

            return sb.ToString();
        }
    }
}
