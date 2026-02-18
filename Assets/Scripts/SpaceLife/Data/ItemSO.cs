
using UnityEngine;

namespace ProjectArk.SpaceLife.Data
{
    /// <summary>
    /// Authored data for an item that can be gifted to NPCs.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Project Ark/Space Life/Item")]
    public class ItemSO : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string _itemName;
        
        [TextArea(2, 4)]
        [SerializeField] private string _description;
        
        [SerializeField] private Sprite _icon;
        
        [Header("Gift Settings")]
        [SerializeField] private int _baseGiftValue = 10;

        public string ItemName => _itemName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int BaseGiftValue => _baseGiftValue;
    }
}

