using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Abstract base class for all Star Chart components (Cores, Prisms, Sails, Satellites).
    /// Provides shared display data and slot sizing used by the inventory/equip UI.
    /// </summary>
    public abstract class StarChartItemSO : ScriptableObject
    {
        [Header("Display")]
        [SerializeField] private string _displayName;

        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [SerializeField] private Sprite _icon;

        [Header("Slot")]
        [Tooltip("How many grid units this item occupies (1-3). Legacy field kept for backward compatibility.")]
        [Range(1, 3)]
        [SerializeField] private int _slotSize = 1;

        [Tooltip("2D shape this item occupies on the track grid.")]
        [SerializeField] private ItemShape _shape = ItemShape.Shape1x1;

        [Header("Heat")]
        [Tooltip("Heat contribution when this item is part of a firing action")]
        [SerializeField] private float _heatCost;

        // --- Public read-only properties ---

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;

        /// <summary> Legacy 1D slot size. Derived from Shape for backward compatibility. </summary>
        public int SlotSize => _slotSize;

        /// <summary> 2D shape this item occupies on the track grid. </summary>
        public ItemShape Shape => _shape;

        public float HeatCost => _heatCost;

        /// <summary> Which category this item belongs to (Core, Prism, etc.). </summary>
        public abstract StarChartItemType ItemType { get; }
    }
}
