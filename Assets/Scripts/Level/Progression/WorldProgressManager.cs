using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Save;

namespace ProjectArk.Level
{
    /// <summary>
    /// Tracks irreversible world progress milestones (boss defeats, area unlocks).
    /// Subscribes to LevelEvents.OnBossDefeated and automatically advances
    /// through data-driven WorldProgressStageSO definitions.
    /// 
    /// Registers with ServiceLocator. Place on a persistent manager GameObject.
    /// </summary>
    public class WorldProgressManager : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Stage Definitions")]
        [Tooltip("All world progress stages, ordered by StageIndex. Must be sequential starting from 0.")]
        [SerializeField] private WorldProgressStageSO[] _stages;

        [Header("Save")]
        [Tooltip("Save slot index.")]
        [SerializeField] private int _saveSlot = 0;

        // ──────────────────── Runtime State ────────────────────

        private int _currentStage;
        private readonly HashSet<string> _defeatedBossIDs = new();

        // ──────────────────── Constants ────────────────────

        private const string WORLD_STAGE_FLAG = "world_stage";

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Current world stage index. </summary>
        public int CurrentWorldStage => _currentStage;

        /// <summary> Read-only set of defeated boss IDs. </summary>
        public IReadOnlyCollection<string> DefeatedBossIDs => _defeatedBossIDs;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            // Load progress from save
            LoadProgress();

            // Subscribe to boss defeat events
            LevelEvents.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDestroy()
        {
            LevelEvents.OnBossDefeated -= HandleBossDefeated;
            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleBossDefeated(string bossID)
        {
            if (string.IsNullOrEmpty(bossID)) return;

            if (_defeatedBossIDs.Add(bossID))
            {
                Debug.Log($"[WorldProgressManager] Boss defeated: {bossID}");
                CheckStageAdvancement();
                SaveProgress();
            }
        }

        // ──────────────────── Stage Advancement ────────────────────

        private void CheckStageAdvancement()
        {
            if (_stages == null || _stages.Length == 0) return;

            // Check all stages above current to see if we can advance
            for (int i = _currentStage + 1; i < _stages.Length; i++)
            {
                var stage = _stages[i];
                if (stage == null) continue;

                if (AreRequirementsMet(stage))
                {
                    AdvanceToStage(stage.StageIndex);
                }
                else
                {
                    // Stages are sequential — stop at first unmet requirement
                    break;
                }
            }
        }

        private bool AreRequirementsMet(WorldProgressStageSO stage)
        {
            if (stage.RequiredBossIDs == null || stage.RequiredBossIDs.Length == 0)
                return true;

            foreach (var bossID in stage.RequiredBossIDs)
            {
                if (!_defeatedBossIDs.Contains(bossID))
                    return false;
            }

            return true;
        }

        private void AdvanceToStage(int newStage)
        {
            int oldStage = _currentStage;
            _currentStage = newStage;

            Debug.Log($"[WorldProgressManager] Stage advanced: {oldStage} → {newStage}");

            // Broadcast
            LevelEvents.RaiseWorldStageChanged(newStage);

            // Apply stage effects (unlock doors, etc.)
            var stage = _stages[newStage];
            if (stage != null && stage.UnlockDoorIDs != null)
            {
                // TODO: Find doors by ID and unlock them
                // This requires a Door registry or RoomManager.FindDoorByID()
                // For now, log the intended unlocks
                foreach (var doorID in stage.UnlockDoorIDs)
                {
                    Debug.Log($"[WorldProgressManager] Stage {newStage} would unlock door: {doorID}");
                }
            }
        }

        // ──────────────────── Save/Load ────────────────────

        private void SaveProgress()
        {
            // Delegate to SaveBridge for centralized save (collects all subsystems)
            var saveBridge = ServiceLocator.Get<SaveBridge>();
            if (saveBridge != null)
            {
                saveBridge.SaveAll();
            }
            else
            {
                // Fallback: direct partial save if SaveBridge not available
                var data = SaveManager.Load(_saveSlot) ?? new PlayerSaveData();
                data.Progress.DefeatedBossIDs.Clear();
                foreach (var bossID in _defeatedBossIDs)
                {
                    data.Progress.DefeatedBossIDs.Add(bossID);
                }
                data.Progress.WorldStage = _currentStage;
                SaveManager.Save(data, _saveSlot);
            }
        }

        private void LoadProgress()
        {
            var data = SaveManager.Load(_saveSlot);
            if (data == null) return;

            // Read defeated bosses
            _defeatedBossIDs.Clear();
            if (data.Progress.DefeatedBossIDs != null)
            {
                foreach (var bossID in data.Progress.DefeatedBossIDs)
                {
                    _defeatedBossIDs.Add(bossID);
                }
            }

            // Read world stage
            _currentStage = data.Progress.WorldStage;

            Debug.Log($"[WorldProgressManager] Loaded: stage={_currentStage}, bosses defeated={_defeatedBossIDs.Count}");
        }
    }
}
