using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Test-scene helper for switching between the live ship and the isolated GGReplica ship,
    /// plus forcing replica visual states during A/B validation.
    /// </summary>
    public class GGReplicaTestSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject _liveShip;
        [SerializeField] private GameObject _replicaShip;
        [SerializeField] private GGReplicaShipViewAdapter _replicaView;

        private bool _replicaActive = true;

        public void SetReplicaActive(bool active)
        {
            _replicaActive = active;
            ApplyActiveState();
        }

        public void ForceReplicaState(GGReplicaVisualState state)
        {
            if (_replicaView != null)
            {
                _replicaView.ForceVisualState(state);
            }
        }

        private void Start()
        {
            ApplyActiveState();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                SetReplicaActive(!_replicaActive);
            }

            if (keyboard.f2Key.wasPressedThisFrame) ForceReplicaState(GGReplicaVisualState.Normal);
            if (keyboard.f3Key.wasPressedThisFrame) ForceReplicaState(GGReplicaVisualState.Boost);
            if (keyboard.f4Key.wasPressedThisFrame) ForceReplicaState(GGReplicaVisualState.Dodge);
            if (keyboard.f5Key.wasPressedThisFrame) ForceReplicaState(GGReplicaVisualState.Fire);
            if (keyboard.f6Key.wasPressedThisFrame) ForceReplicaState(GGReplicaVisualState.FireBoost);
        }

        private void ApplyActiveState()
        {
            if (_liveShip != null) _liveShip.SetActive(!_replicaActive);
            if (_replicaShip != null) _replicaShip.SetActive(_replicaActive);
        }
    }
}
