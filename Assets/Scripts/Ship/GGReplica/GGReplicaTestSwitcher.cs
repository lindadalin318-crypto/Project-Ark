using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Test-scene helper for switching between the live ship and the isolated GGReplica ship,
    /// plus forcing original Galactic Glitch ViewState ints during A/B validation.
    /// </summary>
    public class GGReplicaTestSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject _liveShip;
        [SerializeField] private GameObject _replicaShip;
        [SerializeField] private GGReplicaPlayerViewAdapter _replicaView;

        private bool _replicaActive = true;

        public bool ReplicaActive => _replicaActive;

        public void SetReplicaActive(bool active)
        {
            _replicaActive = active;
            ApplyActiveState();
        }

        public void ForceReplicaViewState(int state)
        {
            if (_replicaView != null)
            {
                _replicaView.ChangeViewState(state);
            }
        }

        private void Start()
        {
            ApplyActiveState();
        }

        private void Update()
        {
            HandleKeyboardInput();
        }

        private void OnGUI()
        {
            const float width = 300f;
            const float buttonHeight = 24f;
            GUILayout.BeginArea(new Rect(12f, 12f, width, 390f), GUI.skin.box);
            GUILayout.Label("GGReplica A/B Controls");
            GUILayout.Label("Tab/F1: Toggle Live / Replica");
            GUILayout.Label("0-9: original ViewState ints, U: Undefined(15)");

            if (GUILayout.Button(_replicaActive ? "Showing: GGReplica" : "Showing: Live Ship", GUILayout.Height(buttonHeight)))
                SetReplicaActive(!_replicaActive);

            StateButton("0  Idle", 0, buttonHeight);
            StateButton("1  Boost", 1, buttonHeight);
            StateButton("2  Dodge", 2, buttonHeight);
            StateButton("3  Aim", 3, buttonHeight);
            StateButton("4  Fire", 4, buttonHeight);
            StateButton("5  HeavyFire", 5, buttonHeight);
            StateButton("6  HeavyAim", 6, buttonHeight);
            StateButton("7  Grab", 7, buttonHeight);
            StateButton("8  WeaponUseMoment", 8, buttonHeight);
            StateButton("9  Heal", 9, buttonHeight);
            StateButton("U  Undefined", 15, buttonHeight);
            GUILayout.EndArea();
        }

        private void StateButton(string label, int state, float height)
        {
            if (GUILayout.Button(label, GUILayout.Height(height)))
            {
                ForceReplicaViewState(state);
            }
        }

        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (WasPressed(keyboard.f1Key) || WasPressed(keyboard.tabKey))
            {
                SetReplicaActive(!_replicaActive);
            }

            if (WasPressed(keyboard.digit0Key) || WasPressed(keyboard.numpad0Key)) ForceReplicaViewState(0);
            if (WasPressed(keyboard.digit1Key) || WasPressed(keyboard.numpad1Key)) ForceReplicaViewState(1);
            if (WasPressed(keyboard.digit2Key) || WasPressed(keyboard.numpad2Key)) ForceReplicaViewState(2);
            if (WasPressed(keyboard.digit3Key) || WasPressed(keyboard.numpad3Key)) ForceReplicaViewState(3);
            if (WasPressed(keyboard.digit4Key) || WasPressed(keyboard.numpad4Key)) ForceReplicaViewState(4);
            if (WasPressed(keyboard.digit5Key) || WasPressed(keyboard.numpad5Key)) ForceReplicaViewState(5);
            if (WasPressed(keyboard.digit6Key) || WasPressed(keyboard.numpad6Key)) ForceReplicaViewState(6);
            if (WasPressed(keyboard.digit7Key) || WasPressed(keyboard.numpad7Key)) ForceReplicaViewState(7);
            if (WasPressed(keyboard.digit8Key) || WasPressed(keyboard.numpad8Key)) ForceReplicaViewState(8);
            if (WasPressed(keyboard.digit9Key) || WasPressed(keyboard.numpad9Key)) ForceReplicaViewState(9);
            if (WasPressed(keyboard.uKey)) ForceReplicaViewState(15);
        }

        private static bool WasPressed(KeyControl key) => key != null && key.wasPressedThisFrame;

        private void ApplyActiveState()
        {
            if (_liveShip != null) _liveShip.SetActive(!_replicaActive);
            if (_replicaShip != null) _replicaShip.SetActive(_replicaActive);
        }
    }
}
