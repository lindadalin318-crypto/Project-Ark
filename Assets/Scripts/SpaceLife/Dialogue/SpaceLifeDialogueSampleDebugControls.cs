using System.Collections.Generic;
using ProjectArk.Core;
using ProjectArk.Core.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Explicit sample-only controls for toggling saved world stage while validating the hub dialogue slice.
    /// </summary>
    public class SpaceLifeDialogueSampleDebugControls : MonoBehaviour
    {
        private const string EngineerFirstMetFlag = "spacelife.engineer.first_met";
        private const string EngineerNpcId = "engineer_hub";

        [Header("Save")]
        [SerializeField] private int _saveSlot = 0;

        [Header("Hotkeys")]
        [SerializeField] private Key _resetEngineerDialogueKey = Key.F5;
        [SerializeField] private Key _stageZeroKey = Key.F6;
        [SerializeField] private Key _stageOneKey = Key.F7;
        [SerializeField] private bool _logBindingsOnStart = true;

        private void Start()
        {
            if (_logBindingsOnStart)
            {
                Debug.Log($"[SpaceLifeDialogueSampleDebugControls] Press {_resetEngineerDialogueKey} to reset Engineer sample dialogue progress, {_stageZeroKey} to save WorldStage 0, or {_stageOneKey} to save WorldStage 1. Re-open the dialogue after switching.", this);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasPressedThisFrame(keyboard, _resetEngineerDialogueKey))
            {
                ResetEngineerDialogueProgress();
            }
            else if (WasPressedThisFrame(keyboard, _stageZeroKey))
            {
                SetWorldStage(0);
            }
            else if (WasPressedThisFrame(keyboard, _stageOneKey))
            {
                SetWorldStage(1);
            }
        }

        [ContextMenu("Reset Sample Dialogue/Engineer First Meeting")]
        public void ResetEngineerDialogueProgress()
        {
            PlayerSaveData data = SaveManager.Load(_saveSlot) ?? new PlayerSaveData();
            data.Progress ??= new ProgressSaveData();
            data.Progress.Flags ??= new List<SaveFlag>();
            data.Progress.RelationshipValues ??= new List<RelationshipValueSaveData>();

            RemoveFlag(data.Progress.Flags, EngineerFirstMetFlag);
            RemoveRelationship(data.Progress.RelationshipValues, EngineerNpcId);
            SaveManager.Save(data, _saveSlot);

            RelationshipManager relationshipManager = ServiceLocator.Get<RelationshipManager>();
            relationshipManager?.LoadFromSave();

            Debug.Log($"[SpaceLifeDialogueSampleDebugControls] Reset Engineer sample dialogue progress in slot {_saveSlot}. Next interaction should enter engineer_first_meeting.", this);
        }

        [ContextMenu("Set Sample World Stage/0")]
        public void SetStageZero()
        {
            SetWorldStage(0);
        }

        [ContextMenu("Set Sample World Stage/1")]
        public void SetStageOne()
        {
            SetWorldStage(1);
        }

        private void SetWorldStage(int stage)
        {
            PlayerSaveData data = SaveManager.Load(_saveSlot) ?? new PlayerSaveData();
            data.Progress ??= new ProgressSaveData();
            data.Progress.WorldStage = stage;
            SaveManager.Save(data, _saveSlot);

            Debug.Log($"[SpaceLifeDialogueSampleDebugControls] Saved WorldStage = {stage}. Close and reopen the terminal dialogue to observe gated nodes.", this);
        }

        private static void RemoveFlag(List<SaveFlag> flags, string key)
        {
            if (flags == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            for (int i = flags.Count - 1; i >= 0; i--)
            {
                SaveFlag flag = flags[i];
                if (flag == null)
                {
                    flags.RemoveAt(i);
                    continue;
                }

                if (string.Equals(flag.Key, key, System.StringComparison.Ordinal))
                {
                    flags.RemoveAt(i);
                }
            }
        }

        private static void RemoveRelationship(List<RelationshipValueSaveData> relationships, string npcId)
        {
            if (relationships == null || string.IsNullOrWhiteSpace(npcId))
            {
                return;
            }

            for (int i = relationships.Count - 1; i >= 0; i--)
            {
                RelationshipValueSaveData relationship = relationships[i];
                if (relationship == null)
                {
                    relationships.RemoveAt(i);
                    continue;
                }

                if (string.Equals(relationship.NpcId, npcId, System.StringComparison.Ordinal))
                {
                    relationships.RemoveAt(i);
                }
            }
        }

        private static bool WasPressedThisFrame(Keyboard keyboard, Key key)
        {
            KeyControl keyControl = keyboard[key];
            return keyControl != null && keyControl.wasPressedThisFrame;
        }
    }
}
