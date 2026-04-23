using ProjectArk.Core;
using ProjectArk.Core.Save;
using ProjectArk.Level;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Coordinates the SpaceLife Hub dialogue session: owns the
    /// <see cref="DialogueRunner"/>, feeds view models to the UI presenter, persists
    /// flag / relationship changes, and routes service exits (Gift / Upgrade / Intel).
    /// </summary>
    /// <remarks>
    /// <b>Master Plan v1.1 Phase 3 (§7.1 + §7.3):</b>
    /// <list type="bullet">
    /// <item>§7.1: UI reference is typed as <see cref="IDialoguePresenter"/> — concrete
    /// MonoBehaviour subtype is never referenced. The presenter is resolved exclusively
    /// through <see cref="ServiceLocator"/>; there is no serialized Inspector slot, to
    /// keep scene override discipline (Master Plan v1.1 §4.1 Scene Override whitelist).</item>
    /// <item>§7.3 Dependency Audit: <see cref="Start"/> validates every required dependency
    /// (deferred from <see cref="Awake"/> because dependencies register themselves in their
    /// own <c>Awake()</c>, and Unity does not guarantee Awake ordering across unrelated
    /// components). Any missing dependency triggers a consolidated
    /// <see cref="Debug.LogError"/> and the component is disabled (no silent no-op —
    /// aligns with Implement_rules.md §3.5).</item>
    /// </list>
    /// </remarks>
    public class SpaceLifeDialogueCoordinator : MonoBehaviour
    {
        [Header("Authored Data")]
        [SerializeField] private DialogueDatabaseSO _dialogueDatabase;

        [Header("Scene References")]
        [SerializeField] private DialogueServiceRouter _serviceRouter;

        [Header("Save")]
        [SerializeField] private int _saveSlot;

        // Phase 3 §7.1: presenter is resolved only through ServiceLocator; no serialized slot.
        private IDialoguePresenter _dialogueUI;

        private SpaceLifeManager _spaceLifeManager;
        private RelationshipManager _relationshipManager;
        private WorldProgressManager _worldProgressManager;
        private PlayerSaveData _currentSaveData;
        private DialogueFlagStore _flagStore;
        private DialogueRunner _runner;
        private NPCController _currentNpcContext;
        private Sprite _currentSpeakerAvatar;
        private bool _hubLockApplied;
        private bool _presenterSubscribed;
        private bool _dependencyAuditPassed;

        private void Awake()
        {
            // Register early so other components' Start() can resolve us. The actual
            // dependency audit is deferred to Start() because some dependencies (e.g.
            // DialogueUIPresenter) register themselves in their own Awake() — Unity
            // does not guarantee Awake order across unrelated components.
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            ResolveDependencies();
            _dependencyAuditPassed = AuditDependenciesOnStart();

            if (!_dependencyAuditPassed)
            {
                // Aligns with Implement_rules.md §3.5 "禁止 silent no-op":
                // missing critical dependencies must not silently degrade to a broken runtime.
                // NOTE: enabled=false only suppresses Unity messages (Update/FixedUpdate). Public
                // API calls (StartDialogueFromNpc / StartDialogueFromTerminal) also early-out
                // via the _dependencyAuditPassed gate below.
                ServiceLocator.Unregister(this);
                enabled = false;
            }
        }

        public void StartDialogueFromNpc(NPCController npc)
        {
            if (!_dependencyAuditPassed)
            {
                Debug.LogError(
                    "[SpaceLifeDialogueCoordinator] StartDialogueFromNpc rejected: dependency audit failed at Start(). " +
                    "Check earlier LogError for the missing-dependency list.",
                    this);
                return;
            }

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
            if (!_dependencyAuditPassed)
            {
                Debug.LogError(
                    "[SpaceLifeDialogueCoordinator] StartDialogueFromTerminal rejected: dependency audit failed at Start(). " +
                    "Check earlier LogError for the missing-dependency list.",
                    this);
                return;
            }

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                Debug.LogError("[SpaceLifeDialogueCoordinator] Terminal dialogue ownerId is null or empty.", this);
                return;
            }

            BeginDialogue(ownerId, relationshipValue: 0, npcContext: null, speakerAvatar: null);
        }

        private void BeginDialogue(string ownerId, int relationshipValue, NPCController npcContext, Sprite speakerAvatar)
        {
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

            EnsurePresenterSubscription();
            LockHubInteractions();
            ApplySpeakerAvatar();
            PresentCurrentNode();
        }

        private void PresentCurrentNode()
        {
            if (_runner?.CurrentNode == null || _dialogueUI == null)
            {
                return;
            }

            _dialogueUI.ShowNode(_runner.CurrentNode);
        }

        /// <summary>
        /// Event handler for <see cref="IDialoguePresenter.OnChoiceSelected"/>.
        /// Maps reserved choice ids (<c>__continue__</c>, <c>__close__</c>) to
        /// <c>Runner.Continue</c> / teardown, and forwards real choice ids to
        /// <c>Runner.Choose</c>.
        /// </summary>
        private void HandleChoiceSelected(string choiceId)
        {
            if (_runner == null)
            {
                return;
            }

            if (string.Equals(choiceId, DialoguePresenterReservedChoices.Continue, System.StringComparison.Ordinal))
            {
                HandleContinueRequested();
                return;
            }

            if (string.Equals(choiceId, DialoguePresenterReservedChoices.Close, System.StringComparison.Ordinal))
            {
                HandleCloseRequested();
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

        /// <summary>
        /// Phase 3 §7.3 Dependency Audit. Runs once in <see cref="Start"/> (after all
        /// <c>Awake()</c> registrations complete) and reports every missing critical
        /// dependency in a single consolidated <see cref="Debug.LogError"/>.
        /// </summary>
        /// <returns><c>true</c> when all critical dependencies resolved; <c>false</c> otherwise.</returns>
        private bool AuditDependenciesOnStart()
        {
            System.Text.StringBuilder missing = null;

            if (_dialogueDatabase == null)
            {
                AppendMissing(ref missing, nameof(_dialogueDatabase));
            }

            if (_serviceRouter == null)
            {
                AppendMissing(ref missing, nameof(_serviceRouter));
            }

            if (_dialogueUI == null)
            {
                AppendMissing(ref missing, $"{nameof(IDialoguePresenter)} (via ServiceLocator)");
            }

            if (_spaceLifeManager == null)
            {
                AppendMissing(ref missing, $"{nameof(SpaceLifeManager)} (via ServiceLocator)");
            }

            if (_relationshipManager == null)
            {
                AppendMissing(ref missing, $"{nameof(RelationshipManager)} (via ServiceLocator)");
            }

            // NOTE: WorldProgressManager is intentionally an OPTIONAL dependency.
            // SpaceLife is a standalone hub slice and must not hard-depend on the Level
            // module's runtime managers (Implement_rules.md §3.7 三层职责隔离). When the
            // manager is absent, ResolveWorldStage() falls back to PlayerSaveData.Progress.WorldStage
            // (defaulting to 0 if no save exists), which is sufficient for demo playthroughs
            // and stage-gated content still resolves correctly once the Level module is loaded.

            if (missing == null)
            {
                return true;
            }

            Debug.LogError(
                $"[SpaceLifeDialogueCoordinator] Start dependency audit failed. Disabling self. Missing: [{missing}]. " +
                $"See Master Plan v1.1 §7.3 / Implement_rules.md §3.5 (禁止 silent no-op).",
                this);
            return false;
        }

        private static void AppendMissing(ref System.Text.StringBuilder buffer, string name)
        {
            if (buffer == null)
            {
                buffer = new System.Text.StringBuilder(name);
                return;
            }

            buffer.Append(", ").Append(name);
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
                Debug.LogError(
                    $"[SpaceLifeDialogueCoordinator] IDialoguePresenter is missing for ownerId '{ownerId}'.",
                    this);
                return false;
            }

            if (_serviceRouter == null)
            {
                Debug.LogError($"[SpaceLifeDialogueCoordinator] DialogueServiceRouter is missing for ownerId '{ownerId}'.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolves the current <c>WorldStage</c> for dialogue context evaluation.
        /// Prefers the live <see cref="WorldProgressManager"/> when present (full-flow
        /// integration with the Level module); otherwise falls back to the serialized
        /// <see cref="PlayerSaveData.Progress"/> stage (standalone SpaceLife demo), or 0
        /// when no save data exists.
        /// </summary>
        /// <remarks>
        /// See audit note in <see cref="AuditDependenciesOnStart"/>: WorldProgressManager
        /// is intentionally optional so SpaceLife can run as an isolated module slice.
        /// </remarks>
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

            _spaceLifeManager.AcquireHubLock(this);
            _hubLockApplied = true;
        }

        private void UnlockHubInteractions()
        {
            if (!_hubLockApplied || _spaceLifeManager == null)
            {
                return;
            }

            _spaceLifeManager.ReleaseHubLock(this);
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

        private void EnsurePresenterSubscription()
        {
            if (_presenterSubscribed || _dialogueUI == null)
            {
                return;
            }

            _dialogueUI.OnChoiceSelected += HandleChoiceSelected;
            _presenterSubscribed = true;
        }

        private void ApplySpeakerAvatar()
        {
            if (_dialogueUI == null)
            {
                return;
            }

            _dialogueUI.SetSpeakerAvatar(_currentSpeakerAvatar);
        }

        private void ResolveDependencies()
        {
            // Phase 3 §7.1: presenter only via ServiceLocator (no serialized fallback).
            _dialogueUI ??= ServiceLocator.Get<IDialoguePresenter>();
            _serviceRouter ??= ServiceLocator.Get<DialogueServiceRouter>();
            _spaceLifeManager ??= ServiceLocator.Get<SpaceLifeManager>();
            _relationshipManager ??= ServiceLocator.Get<RelationshipManager>();
            _worldProgressManager ??= ServiceLocator.Get<WorldProgressManager>();
        }

        private void OnDestroy()
        {
            if (_presenterSubscribed && _dialogueUI != null)
            {
                _dialogueUI.OnChoiceSelected -= HandleChoiceSelected;
                _presenterSubscribed = false;
            }

            UnlockHubInteractions();
            ServiceLocator.Unregister(this);
        }
    }
}
