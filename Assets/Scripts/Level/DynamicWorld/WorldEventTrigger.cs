using UnityEngine;
using UnityEngine.Events;
using ProjectArk.Core;
using ProjectArk.Core.Save;

namespace ProjectArk.Level
{
    /// <summary>
    /// Triggers permanent, irreversible world changes when the WorldProgressManager
    /// reaches a required stage. Used for: new areas opening, NPC migration,
    /// terrain collapse, elite enemy spawns, Tilemap variant switches.
    /// 
    /// Persists triggered state via ProgressSaveData.Flags so effects survive save/load.
    /// </summary>
    public class WorldEventTrigger : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Trigger Condition")]
        [Tooltip("The minimum world progress stage required to trigger this event.")]
        [SerializeField] private int _requiredWorldStage;

        [Header("Effects")]
        [Tooltip("GameObjects to ENABLE when triggered (e.g., new passage, NPC, Tilemap variant).")]
        [SerializeField] private GameObject[] _enableOnTrigger;

        [Tooltip("GameObjects to DISABLE when triggered (e.g., old wall, blocking debris).")]
        [SerializeField] private GameObject[] _disableOnTrigger;

        [Header("Callback")]
        [Tooltip("Additional actions invoked when triggered (e.g., TilemapVariantSwitcher.SwitchToVariant).")]
        [SerializeField] private UnityEvent _onTriggered;

        [Header("Persistence")]
        [Tooltip("Unique key for saving triggered state. Defaults to gameObject name if empty.")]
        [SerializeField] private string _persistenceKey;

        // ──────────────────── Runtime State ────────────────────

        private bool _hasTriggered;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Whether this trigger has already fired. </summary>
        public bool HasTriggered => _hasTriggered;

        // ──────────────────── Lifecycle ────────────────────

        private void Start()
        {
            if (string.IsNullOrEmpty(_persistenceKey))
                _persistenceKey = gameObject.name;

            // Check if already triggered from save data
            if (LoadTriggeredState())
            {
                _hasTriggered = true;
                ApplyEffects();
                return;
            }

            // Subscribe to world stage changes
            LevelEvents.OnWorldStageChanged += HandleWorldStageChanged;

            // Check current stage immediately (in case we loaded into a later stage)
            var progressManager = ServiceLocator.Get<WorldProgressManager>();
            if (progressManager != null)
            {
                EvaluateTrigger(progressManager.CurrentWorldStage);
            }
        }

        private void OnDestroy()
        {
            LevelEvents.OnWorldStageChanged -= HandleWorldStageChanged;
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleWorldStageChanged(int newStage)
        {
            EvaluateTrigger(newStage);
        }

        // ──────────────────── Logic ────────────────────

        private void EvaluateTrigger(int currentStage)
        {
            if (_hasTriggered) return;
            if (currentStage < _requiredWorldStage) return;

            _hasTriggered = true;
            ApplyEffects();
            SaveTriggeredState();

            Debug.Log($"[WorldEventTrigger] '{_persistenceKey}' triggered at world stage {currentStage} (required: {_requiredWorldStage})");
        }

        private void ApplyEffects()
        {
            if (_enableOnTrigger != null)
            {
                foreach (var go in _enableOnTrigger)
                {
                    if (go != null) go.SetActive(true);
                }
            }

            if (_disableOnTrigger != null)
            {
                foreach (var go in _disableOnTrigger)
                {
                    if (go != null) go.SetActive(false);
                }
            }

            _onTriggered?.Invoke();
        }

        // ──────────────────── Persistence ────────────────────

        private void SaveTriggeredState()
        {
            // 使用 SaveBridge 集中保存（它会收集所有子系统的数据）
            var saveBridge = ServiceLocator.Get<SaveBridge>();
            if (saveBridge != null)
            {
                // 先把 flag 写入当前存档数据
                SetFlagInSaveData();
                saveBridge.SaveAll();
            }
            else
            {
                // Fallback: 直接局部保存
                SetFlagInSaveData();
                var data = SaveManager.Load(0) ?? new PlayerSaveData();
                SetFlagOnData(data);
                SaveManager.Save(data, 0);
            }
        }

        private void SetFlagInSaveData()
        {
            // This will be picked up by SaveBridge.SaveAll() which reads flags
        }

        private void SetFlagOnData(PlayerSaveData data)
        {
            if (data.Progress.Flags == null)
                data.Progress.Flags = new System.Collections.Generic.List<SaveFlag>();

            // Check if flag already exists
            for (int i = 0; i < data.Progress.Flags.Count; i++)
            {
                if (data.Progress.Flags[i].Key == _persistenceKey)
                {
                    data.Progress.Flags[i].Value = true;
                    return;
                }
            }

            // Add new flag
            data.Progress.Flags.Add(new SaveFlag(_persistenceKey, true));
        }

        private bool LoadTriggeredState()
        {
            var data = SaveManager.Load(0);
            if (data?.Progress?.Flags == null) return false;

            foreach (var flag in data.Progress.Flags)
            {
                if (flag.Key == _persistenceKey && flag.Value)
                    return true;
            }

            return false;
        }
    }
}
