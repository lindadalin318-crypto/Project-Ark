using System.Linq;
using ProjectArk.Core;
using ProjectArk.SpaceLife.Data;
using ProjectArk.SpaceLife.Dialogue;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    [RequireComponent(typeof(Interactable))]
    public class NPCController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private NPCDataSO _npcData;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        private Interactable _interactable;
        private RelationshipManager _relationshipManager;
        private SpaceLifeDialogueCoordinator _dialogueCoordinator;

        public NPCDataSO NPCData => _npcData;
        public int CurrentRelationship => _relationshipManager != null && _npcData != null
            ? _relationshipManager.GetRelationship(_npcData)
            : 0;
        public string NPCName => _npcData != null ? _npcData.NpcName : "Unknown";

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            _relationshipManager = ServiceLocator.Get<RelationshipManager>();
            _dialogueCoordinator = ServiceLocator.Get<SpaceLifeDialogueCoordinator>();

            if (_npcData != null)
            {
                SetupInteractable();
                InitializeRelationship();
            }
        }

        private void InitializeRelationship()
        {
            if (_relationshipManager != null && _npcData != null &&
                !_relationshipManager.HasRelationshipRecord(_npcData) &&
                _npcData.StartingRelationship > 0)
            {
                _relationshipManager.SetRelationship(_npcData, _npcData.StartingRelationship);
            }
        }

        private void SetupInteractable()
        {
            if (_interactable != null)
            {
                _interactable.OnInteract.AddListener(OnInteract);
                _interactable.InteractionText = $"与 {NPCName} 交谈";
            }
        }

        private void OnInteract()
        {
            StartDialogue();
        }

        public void StartDialogue()
        {
            if (_npcData == null)
            {
                Debug.LogWarning("[NPCController] No NPC Data assigned!");
                return;
            }

            if (_dialogueCoordinator == null)
            {
                Debug.LogError($"[NPCController] SpaceLifeDialogueCoordinator missing for '{NPCName}'.", this);
                return;
            }

            _dialogueCoordinator.StartDialogueFromNpc(this);
        }

        public void ChangeRelationship(int amount)
        {
            if (_relationshipManager != null && _npcData != null)
            {
                _relationshipManager.ChangeRelationship(_npcData, amount);
                Debug.Log($"[NPCController] {NPCName} relationship: {CurrentRelationship}");
            }
        }

        public bool IsLikedGift(ItemSO gift)
        {
            return _npcData != null && _npcData.LikedGifts.Contains(gift);
        }

        public bool IsDislikedGift(ItemSO gift)
        {
            return _npcData != null && _npcData.DislikedGifts.Contains(gift);
        }

        private void OnDestroy()
        {
            if (_interactable != null)
            {
                _interactable.OnInteract.RemoveListener(OnInteract);
            }
        }
    }
}
