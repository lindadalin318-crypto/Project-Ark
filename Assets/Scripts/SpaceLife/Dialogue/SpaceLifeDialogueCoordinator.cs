using ProjectArk.Core;
using ProjectArk.Core.Save;
using ProjectArk.Level;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    public class SpaceLifeDialogueCoordinator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private DialogueDatabaseSO _dialogueDatabase;
        [SerializeField] private DialogueUI _dialogueUI;
        [SerializeField] private DialogueServiceRouter _serviceRouter;

        [Header("Save")]
        [SerializeField] private int _saveSlot;

        private SpaceLifeManager _spaceLifeManager;
        private RelationshipManager _relationshipManager;
        private WorldProgressManager _worldProgressManager;
        private PlayerSaveData _currentSaveData;
        private DialogueFlagStore _flagStore;
        private DialogueRunner _runner;
        private NPCController _currentNpcContext;
        private Sprite _currentSpeakerAvatar;
        private bool _hubLockApplied;

        private void Awake()
        {
            ResolveDependencies();
            ServiceLocator.Register(this);
        }

        public void StartDialogueFromNpc(NPCController npc)
        {
            if (npc == null || npc.NPCData == null)
            {
                Debug.LogError("[SpaceLifeDialogueCoordinator] Cannot start NPC dialogue without NPCData.", this);
                return;
            }

            BeginDialogue(
                npc.NPCData.NpcId,
                npc.CurrentRelationship,
                npc,
                npc.NPCData.Avatar);
        }

        public void StartDialogueFromTerminal(string ownerId, string displayName = "")
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                Debug.LogError("[SpaceLifeDialogueCoordinator] Terminal dialogue ownerId is null or empty.", this);
                return;
            }

            BeginDialogue(ownerId, relationshipValue: 0, npcContext: null, speakerAvatar: null);
        }

        private void BeginDialogue(string ownerId, int relationshipValue, NPCController npcContext, Sprite speakerAvatar)
        {
            ResolveDependencies();
            if (!ValidateCoreDependencies(ownerId))
            {
                return;
            }

            TeardownActiveDialogue(routeExit: null);

            _currentSaveData = SaveManager.Load(_saveSlot) ?? new PlayerSaveData();
            _flagStore = new DialogueFlagStore(_currentSaveData);
            _runner = new DialogueRunner(_dialogueDatabase, _flagStore);
            _currentNpcContext = npcContext;
            _currentSpeakerAvatar = speakerAvatar;

            var context = new DialogueContext(
                ownerId,
                ResolveWorldStage(),
                relationshipValue,
                _flagStore.GetActiveFlagKeys());

            DialogueNodeViewModel entryNode = _runner.StartDialogue(ownerId, context);
            if (entryNode == null)
            {
                Debug.LogError($"[SpaceLifeDialogueCoordinator] Failed to start dialogue for ownerId '{ownerId}'.", this);
                ClearRuntimeState();
                return;
            }

            LockHubInteractions();
            PresentCurrentNode();
        }

        private void PresentCurrentNode()
        {
            if (_runner?.CurrentNode == null || _dialogueUI == null)
            {
                return;
            }

            _dialogueUI.PresentNode(
                _runner.CurrentNode,
                HandleChoiceSelected,
                HandleContinueRequested,
                HandleCloseRequested,
                _currentSpeakerAvatar);
        }

        private void HandleChoiceSelected(string choiceId)
        {
            if (_runner == null)
            {
                return;
            }

            DialogueServiceExit serviceExit = _runner.Choose(choiceId);
            PersistDialogueState();

            if (_runner.CurrentNode != null && !serviceExit.HasExit && !serviceExit.ShouldEndDialogue)
            {
                PresentCurrentNode();
                return;
            }

            TeardownActiveDialogue(serviceExit);
        }

        private void HandleContinueRequested()
        {
            if (_runner == null)
            {
                return;
            }

            DialogueServiceExit serviceExit = _runner.Continue();
            PersistDialogueState();

            if (_runner.CurrentNode != null)
            {
                PresentCurrentNode();
                return;
            }

            TeardownActiveDialogue(serviceExit);
        }

        private void HandleCloseRequested()
        {
            PersistDialogueState();
            TeardownActiveDialogue(routeExit: null);
        }

        private void PersistDialogueState()
        {
            if (_currentSaveData != null)
            {
                SaveManager.Save(_currentSaveData, _saveSlot);
            }

            SyncRelationshipToNpcContext();
        }

        private void SyncRelationshipToNpcContext()
        {
            if (_currentNpcContext == null || _currentNpcContext.NPCData == null || _relationshipManager == null)
            {
                return;
            }

            DialogueContext context = _runner?.CurrentContext;
            if (context == null)
            {
                return;
            }

            int currentValue = _relationshipManager.GetRelationship(_currentNpcContext.NPCData);
            if (currentValue == context.RelationshipValue)
            {
                return;
            }

            _relationshipManager.SetRelationship(_currentNpcContext.NPCData, context.RelationshipValue);
        }

        private void TeardownActiveDialogue(DialogueServiceExit routeExit)
        {
            _dialogueUI?.HideDialogue();
            UnlockHubInteractions();

            NPCController npcContext = _currentNpcContext;
            ClearRuntimeState();

            if (routeExit != null && routeExit.HasExit)
            {
                _serviceRouter?.TryRoute(routeExit, npcContext);
            }
        }

        private bool ValidateCoreDependencies(string ownerId)
        {
            if (_dialogueDatabase == null)
            {
                Debug.LogError($"[SpaceLifeDialogueCoordinator] DialogueDatabase is missing for ownerId '{ownerId}'.", this);
                return false;
            }

            if (_dialogueUI == null)
            {
                Debug.LogError($"[SpaceLifeDialogueCoordinator] DialogueUI is missing for ownerId '{ownerId}'.", this);
                return false;
            }

            if (_serviceRouter == null)
            {
                Debug.LogError($"[SpaceLifeDialogueCoordinator] DialogueServiceRouter is missing for ownerId '{ownerId}'.", this);
                return false;
            }

            return true;
        }

        private int ResolveWorldStage()
        {
            if (_worldProgressManager != null)
            {
                return _worldProgressManager.CurrentWorldStage;
            }

            return _currentSaveData?.Progress?.WorldStage ?? 0;
        }

        private void LockHubInteractions()
        {
            if (_hubLockApplied || _spaceLifeManager == null)
            {
                return;
            }

            _spaceLifeManager.SetHubInteractionLocked(true);
            _hubLockApplied = true;
        }

        private void UnlockHubInteractions()
        {
            if (!_hubLockApplied || _spaceLifeManager == null)
            {
                return;
            }

            _spaceLifeManager.SetHubInteractionLocked(false);
            _hubLockApplied = false;
        }

        private void ClearRuntimeState()
        {
            _runner = null;
            _flagStore = null;
            _currentSaveData = null;
            _currentNpcContext = null;
            _currentSpeakerAvatar = null;
        }

        private void ResolveDependencies()
        {
            _dialogueUI ??= ServiceLocator.Get<DialogueUI>();
            _serviceRouter ??= ServiceLocator.Get<DialogueServiceRouter>();
            _spaceLifeManager ??= ServiceLocator.Get<SpaceLifeManager>();
            _relationshipManager ??= ServiceLocator.Get<RelationshipManager>();
            _worldProgressManager ??= ServiceLocator.Get<WorldProgressManager>();
        }

        private void OnDestroy()
        {
            UnlockHubInteractions();
            ServiceLocator.Unregister(this);
        }
    }
}
