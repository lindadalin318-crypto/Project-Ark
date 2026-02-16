
using System.Collections.Generic;
using ProjectArk.SpaceLife.Data;
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

        public NPCDataSO NPCData => _npcData;
        public int CurrentRelationship => RelationshipManager.Instance != null && _npcData != null
            ? RelationshipManager.Instance.GetRelationship(_npcData)
            : 0;
        public string NPCName => _npcData != null ? _npcData.npcName : "Unknown";

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
            if (_npcData != null)
            {
                SetupInteractable();
                InitializeRelationship();
            }
        }

        private void InitializeRelationship()
        {
            if (RelationshipManager.Instance != null && _npcData != null)
            {
                int current = RelationshipManager.Instance.GetRelationship(_npcData);
                if (current == 0 && _npcData.startingRelationship > 0)
                {
                    RelationshipManager.Instance.SetRelationship(_npcData, _npcData.startingRelationship);
                }
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
            if (NPCInteractionUI.Instance != null)
            {
                NPCInteractionUI.Instance.ShowInteractionUI(this);
            }
            else
            {
                StartDialogue();
            }
        }

        public void StartDialogue()
        {
            if (_npcData == null)
            {
                Debug.LogWarning("[NPCController] No NPC Data assigned!");
                return;
            }

            DialogueLine startingLine = GetAppropriateDialogue();

            if (startingLine != null && DialogueUI.Instance != null)
            {
                DialogueUI.Instance.ShowDialogue(startingLine, this);
            }
            else
            {
                Debug.Log($"[NPCController] {NPCName} has no dialogue to show!");
            }
        }

        private DialogueLine GetAppropriateDialogue()
        {
            List<DialogueLine> dialoguePool;
            int relationship = CurrentRelationship;

            if (relationship >= 80)
            {
                dialoguePool = _npcData.bestFriendDialogues;
            }
            else if (relationship >= 50)
            {
                dialoguePool = _npcData.friendlyDialogues;
            }
            else
            {
                dialoguePool = _npcData.defaultDialogues;
            }

            if (dialoguePool.Count > 0)
            {
                return dialoguePool[Random.Range(0, dialoguePool.Count)];
            }

            return null;
        }

        public void ChangeRelationship(int amount)
        {
            if (RelationshipManager.Instance != null && _npcData != null)
            {
                RelationshipManager.Instance.ChangeRelationship(_npcData, amount);
                Debug.Log($"[NPCController] {NPCName} relationship: {CurrentRelationship}");
            }
        }

        public bool IsLikedGift(ItemSO gift)
        {
            return _npcData != null && _npcData.likedGifts.Contains(gift);
        }

        public bool IsDislikedGift(ItemSO gift)
        {
            return _npcData != null && _npcData.dislikedGifts.Contains(gift);
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

