using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Prototype inventory: a designer-populated list of Star Chart items
    /// the player currently owns. Serves as the data source for the
    /// Star Chart UI's inventory panel.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerInventory", menuName = "ProjectArk/UI/Star Chart Inventory")]
    public class StarChartInventorySO : ScriptableObject
    {
        [SerializeField] private List<StarChartItemSO> _ownedItems = new();

        /// <summary> All items the player owns. </summary>
        public IReadOnlyList<StarChartItemSO> OwnedItems => _ownedItems;

        /// <summary> Filter: star cores only. </summary>
        public IEnumerable<StarCoreSO> Cores =>
            _ownedItems.OfType<StarCoreSO>();

        /// <summary> Filter: prisms only. </summary>
        public IEnumerable<PrismSO> Prisms =>
            _ownedItems.OfType<PrismSO>();

        /// <summary> Filter: light sails only. </summary>
        public IEnumerable<LightSailSO> LightSails =>
            _ownedItems.OfType<LightSailSO>();

        /// <summary> Filter: satellites only. </summary>
        public IEnumerable<SatelliteSO> Satellites =>
            _ownedItems.OfType<SatelliteSO>();

        /// <summary> Filter by item type enum. Null returns all. </summary>
        public IEnumerable<StarChartItemSO> GetByType(StarChartItemType? type)
        {
            if (type == null) return _ownedItems;
            return _ownedItems.Where(item => item != null && item.ItemType == type.Value);
        }
    }
}
