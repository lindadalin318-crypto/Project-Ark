
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
        
        [Header("Dialogues")]
        [SerializeField] private List<DialogueLine> _defaultDialogues = new();
        [SerializeField] private List<DialogueLine> _friendlyDialogues = new();
        [SerializeField] private List<DialogueLine> _bestFriendDialogues = new();
        
        [Header("Gift Preferences")]
        [SerializeField] private List<ItemSO> _likedGifts = new();
        [SerializeField] private List<ItemSO> _dislikedGifts = new();

        public string NpcName => _npcName;
        public Sprite Avatar => _avatar;
        public NPCRole Role => _role;
        public int StartingRelationship => _startingRelationship;
        public IReadOnlyList<DialogueLine> DefaultDialogues => _defaultDialogues;
        public IReadOnlyList<DialogueLine> FriendlyDialogues => _friendlyDialogues;
        public IReadOnlyList<DialogueLine> BestFriendDialogues => _bestFriendDialogues;
        public IReadOnlyList<ItemSO> LikedGifts => _likedGifts;
        public IReadOnlyList<ItemSO> DislikedGifts => _dislikedGifts;
    }
}

