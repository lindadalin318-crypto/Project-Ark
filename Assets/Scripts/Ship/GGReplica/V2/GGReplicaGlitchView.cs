using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// V2 visual owner for the isolated GGReplica Glitch ship. It drives module-style VFX roots
    /// inspired by the original Galactic Glitch PlayerView stack instead of swapping state buttons.
    /// </summary>
    public sealed class GGReplicaGlitchView : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private GameObject _boostModuleRoot;
        [SerializeField] private GameObject _lqTrailsContainer;
        [SerializeField] private GameObject _grabModuleRoot;
        [SerializeField] private GameObject _healModuleRoot;
        [SerializeField] private GameObject _dodgeModuleRoot;
        [SerializeField] private GameObject _fireAimModuleRoot;
        [SerializeField] private ParticleSystem[] _boostParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _burstParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private TrailRenderer[] _trailRenderers = System.Array.Empty<TrailRenderer>();
        [SerializeField] private SpriteRenderer[] _bodyRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private SpriteRenderer _dodgeGhostRenderer;

        public GGReplicaGlitchState CurrentState { get; private set; } = GGReplicaGlitchState.Idle;

        private void Awake()
        {
            ApplyState(GGReplicaGlitchState.Idle);
        }

        public void ApplyState(GGReplicaGlitchState state)
        {
            CurrentState = state;
            bool moving = state == GGReplicaGlitchState.Move;
            bool boosting = state == GGReplicaGlitchState.BoostHold;
            bool dodging = state == GGReplicaGlitchState.DodgeBurst;
            bool grabbing = state == GGReplicaGlitchState.GrabHold;
            bool healing = state == GGReplicaGlitchState.Heal;
            bool firing = state == GGReplicaGlitchState.FireAim;

            SetActive(_boostModuleRoot, boosting);
            SetActive(_lqTrailsContainer, boosting || moving || dodging);
            SetActive(_grabModuleRoot, grabbing);
            SetActive(_healModuleRoot, healing);
            SetActive(_dodgeModuleRoot, dodging);
            SetActive(_fireAimModuleRoot, firing);

            SetTrailEmitting(boosting || dodging);
            SetParticles(_boostParticles, boosting);
            SetParticles(_burstParticles, dodging || healing || firing);
            SetDodgeVisuals(dodging);
            SetBodyColor(boosting, dodging, grabbing, healing, firing);
        }

        private void SetTrailEmitting(bool emitting)
        {
            foreach (var trail in _trailRenderers)
            {
                if (trail == null) continue;
                trail.emitting = emitting;
            }
        }

        private static void SetParticles(ParticleSystem[] particles, bool active)
        {
            foreach (var particle in particles)
            {
                if (particle == null) continue;
                if (active && !particle.isPlaying)
                {
                    particle.Play();
                }
                else if (!active && particle.isPlaying)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void SetDodgeVisuals(bool dodging)
        {
            if (_dodgeGhostRenderer != null)
            {
                _dodgeGhostRenderer.enabled = dodging;
                _dodgeGhostRenderer.color = dodging ? new Color(0.65f, 0.15f, 1f, 0.65f) : Color.clear;
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localScale = dodging ? Vector3.one * 1.35f : Vector3.one;
                _coreRenderer.color = dodging ? new Color(0.75f, 0.05f, 1f, 0.9f) : Color.white;
            }
        }

        private void SetBodyColor(bool boosting, bool dodging, bool grabbing, bool healing, bool firing)
        {
            Color color = Color.white;
            if (boosting) color = new Color(1f, 0.75f, 1f, 1f);
            if (dodging) color = new Color(0.75f, 0.45f, 1f, 0.9f);
            if (grabbing) color = new Color(0.85f, 0.65f, 1f, 1f);
            if (healing) color = new Color(0.35f, 1f, 0.85f, 1f);
            if (firing) color = new Color(1f, 0.5f, 0.9f, 1f);

            foreach (var renderer in _bodyRenderers)
            {
                if (renderer != null) renderer.color = color;
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
