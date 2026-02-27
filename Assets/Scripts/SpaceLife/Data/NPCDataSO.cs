
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Data
{
    /// <summary>
    /// Authored data for an NPC character in SpaceLife mode.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCData", menuName = "Project Ark/Space Life/NPC Data")]
    public class NPCDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string _npcName;
        [SerializeField] private Sprite _avatar;
        [SerializeField] private NPCRole _role;
        
        [Header("Relationship")]
        [Range(0, 100)]
        [SerializeField] private int _startingRelationship;
        
        [Header("Dialogue Nodes (flat list — options reference by index)")]
        [Tooltip("All dialogue lines for this NPC. DialogueOption.NextLineIndex points into this list.")]
        [SerializeField] private List<DialogueLine> _dialogueNodes = new();

        [Header("Dialogue Entry Points")]
        [Tooltip("Index of the starting node used when relationship < 50. -1 = no dialogue.")]
        [SerializeField] private int _defaultEntryIndex = -1;
        [Tooltip("Index of the starting node used when relationship >= 50.")]
        [SerializeField] private int _friendlyEntryIndex = -1;
        [Tooltip("Index of the starting node used when relationship >= 80.")]
        [SerializeField] private int _bestFriendEntryIndex = -1;

        [Header("Gift Preferences")]
        [SerializeField] private List<ItemSO> _likedGifts = new();
        [SerializeField] private List<ItemSO> _dislikedGifts = new();

        public string NpcName => _npcName;
        public Sprite Avatar => _avatar;
        public NPCRole Role => _role;
        public int StartingRelationship => _startingRelationship;

        /// <summary>Flat pool of all dialogue lines for this NPC.</summary>
        public IReadOnlyList<DialogueLine> DialogueNodes => _dialogueNodes;

        public int DefaultEntryIndex => _defaultEntryIndex;
        public int FriendlyEntryIndex => _friendlyEntryIndex;
        public int BestFriendEntryIndex => _bestFriendEntryIndex;

        public IReadOnlyList<ItemSO> LikedGifts => _likedGifts;
        public IReadOnlyList<ItemSO> DislikedGifts => _dislikedGifts;

        /// <summary>
        /// Returns the entry DialogueLine for the given relationship value, or null if none configured.
        /// </summary>
        public DialogueLine GetEntryLine(int relationship)
        {
            int index = relationship >= 80 ? _bestFriendEntryIndex
                      : relationship >= 50 ? _friendlyEntryIndex
                      : _defaultEntryIndex;

            return GetNodeAt(index);
        }

        /// <summary>
        /// Returns the DialogueLine at <paramref name="index"/>, or null if out of range / -1.
        /// </summary>
        public DialogueLine GetNodeAt(int index)
        {
            if (index < 0 || index >= _dialogueNodes.Count) return null;
            return _dialogueNodes[index];
        }
    }
}

