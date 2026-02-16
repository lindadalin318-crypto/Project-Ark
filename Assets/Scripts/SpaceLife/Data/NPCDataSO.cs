
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Data
{
    [CreateAssetMenu(fileName = "NPCData", menuName = "Project Ark/Space Life/NPC Data")]
    public class NPCDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string npcName;
        public Sprite avatar;
        public NPCRole role;
        
        [Header("Relationship")]
        [Range(0, 100)]
        public int startingRelationship = 0;
        
        [Header("Dialogues")]
        public List<DialogueLine> defaultDialogues = new List<DialogueLine>();
        public List<DialogueLine> friendlyDialogues = new List<DialogueLine>();
        public List<DialogueLine> bestFriendDialogues = new List<DialogueLine>();
        
        [Header("Gift Preferences")]
        public List<ItemSO> likedGifts = new List<ItemSO>();
        public List<ItemSO> dislikedGifts = new List<ItemSO>();
    }

    public enum NPCRole
    {
        CommunicationsOfficer,
        Navigator,
        MedicalOfficer,
        Engineer,
        Cook,
        Other
    }
}

