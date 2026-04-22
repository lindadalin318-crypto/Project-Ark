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
        [Header("Save")]
        [SerializeField] private int _saveSlot = 0;

        [Header("Hotkeys")]
        [SerializeField] private Key _stageZeroKey = Key.F6;
        [SerializeField] private Key _stageOneKey = Key.F7;
        [SerializeField] private bool _logBindingsOnStart = true;

        private void Start()
        {
            if (_logBindingsOnStart)
            {
                Debug.Log($"[SpaceLifeDialogueSampleDebugControls] Press {_stageZeroKey} to save WorldStage 0, or {_stageOneKey} to save WorldStage 1. Re-open the terminal dialogue after switching.", this);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasPressedThisFrame(keyboard, _stageZeroKey))
            {
                SetWorldStage(0);
            }
            else if (WasPressedThisFrame(keyboard, _stageOneKey))
            {
                SetWorldStage(1);
            }
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

        private static bool WasPressedThisFrame(Keyboard keyboard, Key key)
        {
            KeyControl keyControl = keyboard[key];
            return keyControl != null && keyControl.wasPressedThisFrame;
        }
    }
}
