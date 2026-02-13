using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Data asset for a key item. Keys unlock doors/barriers in the level.
    /// Create assets in Assets/_Data/Level/Keys/.
    /// </summary>
    [CreateAssetMenu(fileName = "New Key Item", menuName = "ProjectArk/Level/Key Item")]
    public class KeyItemSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this key. Used by save system and Lock checks.")]
        [SerializeField] private string _keyID;

        [Tooltip("Display name shown in UI.")]
        [SerializeField] private string _displayName;

        [Header("Presentation")]
        [Tooltip("Icon displayed in inventory/UI.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Short description for tooltip/inventory.")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Unique key identifier (save/lock key). </summary>
        public string KeyID => _keyID;

        /// <summary> Display name for UI. </summary>
        public string DisplayName => _displayName;

        /// <summary> Inventory icon (nullable). </summary>
        public Sprite Icon => _icon;

        /// <summary> Description text. </summary>
        public string Description => _description;
    }
}
