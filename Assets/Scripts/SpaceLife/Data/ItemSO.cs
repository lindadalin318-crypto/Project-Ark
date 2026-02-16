
using UnityEngine;

namespace ProjectArk.SpaceLife.Data
{
    [CreateAssetMenu(fileName = "Item", menuName = "Project Ark/Space Life/Item")]
    public class ItemSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName;
        
        [TextArea(2, 4)]
        public string description;
        
        public Sprite icon;
        
        [Header("Gift Settings")]
        public int baseGiftValue = 10;
    }
}

