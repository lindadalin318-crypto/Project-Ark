using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Poolable VFX wrapper. Plays its ParticleSystem on activation
    /// and automatically returns to pool when playback completes.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledVFX : MonoBehaviour, IPoolable
    {
        private ParticleSystem _particleSystem;
        private PoolReference _poolRef;
        private float _returnTimer;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _poolRef = GetComponent<PoolReference>();
        }

        public void OnGetFromPool()
        {
            _particleSystem.Clear();
            _particleSystem.Play();

            // 计算总持续时间用于自动回收
            var main = _particleSystem.main;
            _returnTimer = main.duration + main.startLifetime.constantMax;
        }

        public void OnReturnToPool()
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            _returnTimer -= Time.deltaTime;
            if (_returnTimer <= 0f && !_particleSystem.isPlaying)
            {
                if (_poolRef != null)
                    _poolRef.ReturnToPool();
            }
        }
    }
}
