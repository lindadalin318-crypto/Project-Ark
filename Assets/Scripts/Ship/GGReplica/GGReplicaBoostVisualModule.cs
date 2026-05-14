using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// MVP boost visual module for the isolated GGReplica PlayerView lane.
    /// </summary>
    public sealed class GGReplicaBoostVisualModule : MonoBehaviour
    {
        [SerializeField] private GameObject _boostVisualRoot;
        [SerializeField] private ParticleSystem[] _particles = Array.Empty<ParticleSystem>();

        public void ApplyState(GGReplicaViewState state)
        {
            bool boosting = state == GGReplicaViewState.Boost;
            if (_boostVisualRoot != null)
            {
                _boostVisualRoot.SetActive(boosting);
            }

            foreach (var particle in _particles)
            {
                if (particle == null) continue;
                if (boosting && !particle.isPlaying)
                {
                    particle.Play();
                }
                else if (!boosting && particle.isPlaying)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }
}
