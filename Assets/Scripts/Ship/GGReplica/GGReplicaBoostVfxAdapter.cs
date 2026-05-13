using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Replica-only Boost audio bridge. It consumes GGReplicaShipVisualProfileSO
    /// and does not route through the live Ship/VFX audio chain.
    /// </summary>
    public class GGReplicaBoostVfxAdapter : MonoBehaviour
    {
        [SerializeField] private GGReplicaShipVisualProfileSO _profile;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private ShipBoost _boost;

        private AudioClip _lastPlayedClip;

        private void Awake()
        {
            if (_boost == null) _boost = GetComponent<ShipBoost>();
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (_boost != null)
            {
                _boost.OnBoostStarted += HandleBoostStarted;
                _boost.OnBoostEnded += HandleBoostEnded;
            }
        }

        private void OnDisable()
        {
            if (_boost != null)
            {
                _boost.OnBoostStarted -= HandleBoostStarted;
                _boost.OnBoostEnded -= HandleBoostEnded;
            }

            StopLoop();
            _lastPlayedClip = null;
        }

        private void HandleBoostStarted()
        {
            if (_profile == null || _audioSource == null) return;

            if (_profile.BoostIgniteClip != null)
            {
                _lastPlayedClip = _profile.BoostIgniteClip;
                if (_audioSource.isActiveAndEnabled)
                {
                    _audioSource.PlayOneShot(_profile.BoostIgniteClip);
                }
            }

            if (_profile.BoostLoopClip != null)
            {
                _lastPlayedClip = _profile.BoostLoopClip;
                _audioSource.clip = _profile.BoostLoopClip;
                _audioSource.loop = true;
                if (_audioSource.isActiveAndEnabled)
                {
                    _audioSource.Play();
                }
            }
        }

        private void HandleBoostEnded() => StopLoop();

        private void StopLoop()
        {
            if (_audioSource == null) return;

            if (_audioSource.loop || _audioSource.clip != null)
            {
                _audioSource.Stop();
                _audioSource.loop = false;
                _audioSource.clip = null;
            }
        }
    }
}
