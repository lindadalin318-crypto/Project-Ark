using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Replica-only Dash audio bridge. It consumes GGReplicaShipVisualProfileSO
    /// and does not route through the live Ship/VFX audio chain.
    /// </summary>
    public class GGReplicaDashVfxAdapter : MonoBehaviour
    {
        [SerializeField] private GGReplicaShipVisualProfileSO _profile;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private ShipDash _dash;

        private AudioClip _lastPlayedClip;

        private void Awake()
        {
            if (_dash == null) _dash = GetComponent<ShipDash>();
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (_dash != null)
            {
                _dash.OnDashStarted += HandleDashStarted;
            }
        }

        private void OnDisable()
        {
            if (_dash != null)
            {
                _dash.OnDashStarted -= HandleDashStarted;
            }

            _lastPlayedClip = null;
        }

        private void HandleDashStarted(Vector2 direction)
        {
            if (_profile == null || _audioSource == null) return;
            if (_profile.DodgeClip == null) return;

            _lastPlayedClip = _profile.DodgeClip;
            if (_audioSource.isActiveAndEnabled)
            {
                _audioSource.PlayOneShot(_profile.DodgeClip);
            }
        }
    }
}
