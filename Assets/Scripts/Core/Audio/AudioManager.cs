using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectArk.Core.Audio
{
    /// <summary>
    /// Central audio manager. Pools AudioSources for SFX playback, manages music
    /// crossfade, and exposes volume controls via AudioMixer parameters.
    /// Register with ServiceLocator in Awake; place on a persistent GameObject.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ──────────────────── Inspector ────────────────────
        [Header("Mixer")]
        [Tooltip("Main audio mixer. Create via: Assets/Audio/MainMixer with groups Master > Music, SFX, UI.")]
        [SerializeField] private AudioMixer _mixer;

        [Header("Mixer Exposed Parameters")]
        [SerializeField] private string _masterVolumeParam = "MasterVolume";
        [SerializeField] private string _musicVolumeParam = "MusicVolume";
        [SerializeField] private string _sfxVolumeParam = "SFXVolume";

        [Header("SFX Pool")]
        [Tooltip("Number of pooled AudioSources for simultaneous SFX.")]
        [SerializeField] [Min(4)] private int _sfxPoolSize = 8;

        [Header("Music")]
        [SerializeField] private AudioSource _musicSourceA;
        [SerializeField] private AudioSource _musicSourceB;

        // ──────────────────── Runtime State ────────────────────
        private readonly List<AudioSource> _sfxPool = new();
        private int _nextSfxIndex;
        private AudioSource _activeMusicSource;
        private bool _isCrossfading;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register<AudioManager>(this);
            InitializeSFXPool();
            InitializeMusicSources();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<AudioManager>(this);
        }

        // ──────────────────── SFX ────────────────────

        /// <summary>
        /// Play a one-shot SFX clip using a pooled AudioSource.
        /// Supports positional audio and pitch variance.
        /// </summary>
        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariance = 0f)
        {
            if (clip == null) return;

            var source = GetNextSFXSource();
            source.transform.position = position;
            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.volume = volume;
            source.PlayOneShot(clip);
        }

        /// <summary>
        /// Play a 2D SFX clip (no spatialization).
        /// </summary>
        public void PlaySFX2D(AudioClip clip, float volume = 1f, float pitchVariance = 0f)
        {
            if (clip == null) return;

            var source = GetNextSFXSource();
            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.volume = volume;
            source.spatialBlend = 0f;
            source.PlayOneShot(clip);
        }

        // ──────────────────── Music ────────────────────

        /// <summary>
        /// Start playing a music track with optional crossfade.
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
        {
            if (clip == null) return;

            if (_activeMusicSource == null)
            {
                _activeMusicSource = _musicSourceA;
            }

            // Simple swap: stop active, play new on the other source
            var newSource = (_activeMusicSource == _musicSourceA) ? _musicSourceB : _musicSourceA;

            if (_activeMusicSource.isPlaying)
                _activeMusicSource.Stop();

            newSource.clip = clip;
            newSource.volume = 1f;
            newSource.Play();
            _activeMusicSource = newSource;
        }

        /// <summary>
        /// Stop the currently playing music.
        /// </summary>
        public void StopMusic(float fadeDuration = 1f)
        {
            if (_activeMusicSource != null && _activeMusicSource.isPlaying)
                _activeMusicSource.Stop();
        }

        // ──────────────────── Volume Controls ────────────────────

        /// <summary> Set master volume (0..1 normalized). </summary>
        public void SetMasterVolume(float normalized01)
        {
            SetMixerVolume(_masterVolumeParam, normalized01);
        }

        /// <summary> Set SFX volume (0..1 normalized). </summary>
        public void SetSFXVolume(float normalized01)
        {
            SetMixerVolume(_sfxVolumeParam, normalized01);
        }

        /// <summary> Set music volume (0..1 normalized). </summary>
        public void SetMusicVolume(float normalized01)
        {
            SetMixerVolume(_musicVolumeParam, normalized01);
        }

        // ──────────────────── Low-Pass Filter ────────────────────

        /// <summary>
        /// Apply a low-pass filter effect (e.g., for weaving state).
        /// Requires an exposed "LowPassCutoff" parameter on the mixer.
        /// </summary>
        public void ApplyLowPassFilter(float cutoffHz, float fadeDuration)
        {
            if (_mixer == null) return;
            _mixer.SetFloat("LowPassCutoff", cutoffHz);
        }

        /// <summary>
        /// Remove the low-pass filter (restore full frequency range).
        /// </summary>
        public void RemoveLowPassFilter(float fadeDuration)
        {
            if (_mixer == null) return;
            _mixer.SetFloat("LowPassCutoff", 22000f);
        }

        // ──────────────────── Internals ────────────────────

        private void InitializeSFXPool()
        {
            var poolParent = new GameObject("SFX_Pool");
            poolParent.transform.SetParent(transform);

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(poolParent.transform);

                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;

                // Route through SFX mixer group if available
                if (_mixer != null)
                {
                    var groups = _mixer.FindMatchingGroups("SFX");
                    if (groups != null && groups.Length > 0)
                        source.outputAudioMixerGroup = groups[0];
                }

                _sfxPool.Add(source);
            }
        }

        private void InitializeMusicSources()
        {
            if (_musicSourceA == null)
            {
                var goA = new GameObject("Music_A");
                goA.transform.SetParent(transform);
                _musicSourceA = goA.AddComponent<AudioSource>();
                _musicSourceA.playOnAwake = false;
                _musicSourceA.loop = true;
                _musicSourceA.spatialBlend = 0f;
            }

            if (_musicSourceB == null)
            {
                var goB = new GameObject("Music_B");
                goB.transform.SetParent(transform);
                _musicSourceB = goB.AddComponent<AudioSource>();
                _musicSourceB.playOnAwake = false;
                _musicSourceB.loop = true;
                _musicSourceB.spatialBlend = 0f;
            }

            // Route music sources through Music mixer group
            if (_mixer != null)
            {
                var groups = _mixer.FindMatchingGroups("Music");
                if (groups != null && groups.Length > 0)
                {
                    _musicSourceA.outputAudioMixerGroup = groups[0];
                    _musicSourceB.outputAudioMixerGroup = groups[0];
                }
            }

            _activeMusicSource = _musicSourceA;
        }

        private AudioSource GetNextSFXSource()
        {
            var source = _sfxPool[_nextSfxIndex % _sfxPool.Count];
            _nextSfxIndex++;
            return source;
        }

        private void SetMixerVolume(string paramName, float normalized01)
        {
            if (_mixer == null) return;

            // Convert 0..1 to decibels (-80dB to 0dB)
            float dB = normalized01 > 0.001f
                ? Mathf.Log10(Mathf.Clamp01(normalized01)) * 20f
                : -80f;
            _mixer.SetFloat(paramName, dB);
        }
    }
}
