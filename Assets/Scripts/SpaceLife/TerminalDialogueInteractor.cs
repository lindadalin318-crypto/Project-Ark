using ProjectArk.Core;
using ProjectArk.SpaceLife.Dialogue;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    [RequireComponent(typeof(Interactable))]
    public class TerminalDialogueInteractor : MonoBehaviour
    {
        [Header("Dialogue Owner")]
        [SerializeField] private string _ownerId;
        [SerializeField] private DialogueOwnerType _ownerType = DialogueOwnerType.Terminal;
        [SerializeField] private string _displayName = "终端";

        private Interactable _interactable;
        private SpaceLifeDialogueCoordinator _dialogueCoordinator;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            _dialogueCoordinator = ServiceLocator.Get<SpaceLifeDialogueCoordinator>();
            SetupInteractable();
        }

        private void SetupInteractable()
        {
            if (_interactable == null)
            {
                return;
            }

            _interactable.OnInteract.AddListener(OnInteract);
            _interactable.InteractionText = string.IsNullOrWhiteSpace(_displayName)
                ? "访问终端"
                : $"访问 {_displayName}";
        }

        private void OnInteract()
        {
            if (_ownerType != DialogueOwnerType.Terminal)
            {
                Debug.LogError($"[TerminalDialogueInteractor] OwnerType must stay Terminal for '{name}'.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(_ownerId))
            {
                Debug.LogError($"[TerminalDialogueInteractor] OwnerId is missing on '{name}'.", this);
                return;
            }

            if (_dialogueCoordinator == null)
            {
                Debug.LogError($"[TerminalDialogueInteractor] SpaceLifeDialogueCoordinator not found for '{name}'.", this);
                return;
            }

            _dialogueCoordinator.StartDialogueFromTerminal(_ownerId, _displayName);
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
