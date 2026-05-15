using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// V2 audio owner for the isolated GGReplica Glitch ship. It plays original Galactic Glitch
    /// Boost/Dodge clips from GGReplicaShipVisualProfileSO based on input-driven V2 states.
    /// </summary>
    public sealed class GGReplicaGlitchAudioFeedback : MonoBehaviour
    {
        [SerializeField] private GGReplicaShipVisualProfileSO _profile;
        [SerializeField] private AudioSource _audioSource;

        private AudioClip _lastOneShotClip;

        private void Awake()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        }

        private void OnDisable()
        {
            StopLoop();
            _lastOneShotClip = null;
        }

        public void ApplyState(GGReplicaGlitchState state)
        {
            switch (state)
            {
                case GGReplicaGlitchState.BoostHold:
                    PlayBoost();
                    break;
                case GGReplicaGlitchState.DodgeBurst:
                    StopLoop();
                    PlayOneShot(_profile != null ? _profile.DodgeClip : null);
                    break;
                case GGReplicaGlitchState.FireAim:
                    StopLoop();
                    PlayOneShot(_profile != null ? _profile.FireClip : null);
                    break;
                case GGReplicaGlitchState.Heal:
                    StopLoop();
                    PlayOneShot(_profile != null ? _profile.HealClip : null);
                    break;
                default:
                    StopLoop();
                    break;
            }
        }

        private void PlayBoost()
        {
            if (_profile == null || _audioSource == null) return;

            if (_audioSource.clip != _profile.BoostLoopClip)
            {
                PlayOneShot(_profile.BoostIgniteClip);
            }

            if (_profile.BoostLoopClip == null) return;

            _audioSource.clip = _profile.BoostLoopClip;
            _audioSource.loop = true;
            if (_audioSource.isActiveAndEnabled && !_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;

            _lastOneShotClip = clip;
            if (_audioSource.isActiveAndEnabled)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

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
