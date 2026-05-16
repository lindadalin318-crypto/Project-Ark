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
        [SerializeField] private Transform _bodyLayersRoot;
        [SerializeField] private GGReplicaShipFeelProfileSO _feelProfile;
        [SerializeField] private GGReplicaPlayerSkinSO _playerSkin;
        [SerializeField] private GameObject _boostModuleRoot;
        [SerializeField] private GameObject _lqTrailsContainer;
        [SerializeField] private GameObject _grabModuleRoot;
        [SerializeField] private GameObject _fluxyGrabModuleRoot;
        [SerializeField] private GameObject _healModuleRoot;
        [SerializeField] private GameObject _dodgeModuleRoot;
        [SerializeField] private GameObject _fireAimModuleRoot;
        [SerializeField] private ParticleSystem[] _boostParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _boostBurstParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _dodgeBurstParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _dodgeTrailParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _healParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _fireAimParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _burstParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private TrailRenderer[] _trailRenderers = System.Array.Empty<TrailRenderer>();
        [SerializeField] private SpriteRenderer[] _bodyRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] private SpriteRenderer _solidRenderer;
        [SerializeField] private SpriteRenderer _liquidRenderer;
        [SerializeField] private SpriteRenderer _highlightRenderer;
        [SerializeField] private SpriteRenderer[] _grabRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] private SpriteRenderer[] _grabFluxyRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] private LineRenderer _grabThrowPointer;
        [SerializeField] private SpriteRenderer _grabLockRenderer;
        [SerializeField] private SpriteRenderer _grabReleaseRenderer;
        [SerializeField] private ParticleSystem[] _grabReleaseParticles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private LineRenderer _grabReleaseThrowLine;
        [SerializeField] private SpriteRenderer[] _healRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] private SpriteRenderer[] _fireAimRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private SpriteRenderer _dodgeGhostRenderer;
        [SerializeField] private SpriteRenderer _dodgeHalfRenderer;
        [SerializeField] private SpriteRenderer _dodgeAdditiveCoreRenderer;
        [SerializeField] private TrailRenderer _fluxyTrailRenderer;

        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int FlowPowerId = Shader.PropertyToID("_FlowPower");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");

        private Vector3[] _grabClosedPositions = System.Array.Empty<Vector3>();
        private MaterialPropertyBlock _fluxyTrailBlock;
        private MaterialPropertyBlock _grabFluxyBlock;
        private bool _fluxyDefaultsCaptured;
        private float _fluxyDefaultTime;
        private float _fluxyDefaultWidth;
        private Color _fluxyDefaultStartColor;
        private Color _fluxyDefaultEndColor;
        private Vector3 _fluxyDefaultScale;
        private float _boostBurstTimer;
        private float _dodgeVisualTimer;
        private float _grabHoldTimer;
        private float _grabReleaseTimer;
        private float _healPulseTimer;
        private float _fireAimPulseTimer;

        public GGReplicaGlitchState CurrentState { get; private set; } = GGReplicaGlitchState.Idle;

        private void Awake()
        {
            ApplyState(GGReplicaGlitchState.Idle);
        }

        public void ApplyState(GGReplicaGlitchState state)
        {
            GGReplicaGlitchState previousState = CurrentState;
            CurrentState = state;
            bool enteringBoost = state == GGReplicaGlitchState.BoostHold && previousState != GGReplicaGlitchState.BoostHold;
            bool enteringDodge = state == GGReplicaGlitchState.DodgeBurst && previousState != GGReplicaGlitchState.DodgeBurst;
            bool enteringGrab = state == GGReplicaGlitchState.GrabHold && previousState != GGReplicaGlitchState.GrabHold;
            bool exitingGrab = previousState == GGReplicaGlitchState.GrabHold && state != GGReplicaGlitchState.GrabHold;
            bool enteringHeal = state == GGReplicaGlitchState.Heal && previousState != GGReplicaGlitchState.Heal;
            bool enteringFireAim = state == GGReplicaGlitchState.FireAim && previousState != GGReplicaGlitchState.FireAim;
            bool moving = state == GGReplicaGlitchState.Move;
            bool boosting = state == GGReplicaGlitchState.BoostHold;
            bool dodging = state == GGReplicaGlitchState.DodgeBurst;
            bool grabbing = state == GGReplicaGlitchState.GrabHold;
            bool healing = state == GGReplicaGlitchState.Heal;
            bool firing = state == GGReplicaGlitchState.FireAim;

            SetActive(_boostModuleRoot, boosting);
            SetActive(_lqTrailsContainer, boosting || moving || dodging);
            SetActive(_grabModuleRoot, grabbing);
            SetActive(_fluxyGrabModuleRoot, grabbing || exitingGrab || _grabReleaseTimer > 0f);
            SetActive(_healModuleRoot, healing);
            SetActive(_dodgeModuleRoot, dodging);
            SetActive(_fireAimModuleRoot, firing);

            SetTrailEmitting(boosting || dodging);
            SetParticles(_boostParticles, boosting);
            SetParticles(_dodgeTrailParticles, dodging);
            SetParticles(_healParticles, healing);
            SetParticles(_fireAimParticles, firing);
            ApplyViewSpritePack(state);
            ApplyBurstParticles(boosting, dodging, healing, firing, enteringBoost, enteringDodge);
            ApplyDodgeVisuals(dodging, enteringDodge);
            if (!dodging)
            {
                SetFluxyTrailState(boosting || moving, boosting ? 0.65f : moving ? 0.35f : 0f, false);
            }

            ApplyGrabVisuals(grabbing, enteringGrab, exitingGrab);
            ApplyHealVisuals(healing, enteringHeal);
            ApplyFireAimVisuals(firing, enteringFireAim);
            SetBodyColor(boosting, dodging, grabbing, healing, firing);
        }

        private void Update()
        {
            TickVisuals(Time.deltaTime);
        }

        private void TickVisuals(float deltaTime)
        {
            if (_boostBurstTimer > 0f)
            {
                _boostBurstTimer -= deltaTime;
                if (_boostBurstTimer <= 0f && CurrentState == GGReplicaGlitchState.BoostHold)
                {
                    StopParticles(BoostBurstParticles);
                }
            }

            if (_dodgeVisualTimer > 0f)
            {
                _dodgeVisualTimer -= deltaTime;
                if (CurrentState == GGReplicaGlitchState.DodgeBurst)
                {
                    float duration = Mathf.Max(0.0001f, DodgeVisualDuration);
                    float intensity = Mathf.Clamp01(_dodgeVisualTimer / duration);
                    SetDodgeVisuals(intensity > 0f, intensity);
                    if (_dodgeVisualTimer <= 0f)
                    {
                        StopParticles(DodgeBurstParticles);
                        StopParticles(_dodgeTrailParticles);
                    }
                }
            }

            if (CurrentState == GGReplicaGlitchState.Heal)
            {
                _healPulseTimer += deltaTime;
                float pulse = 0.5f + Mathf.Sin(_healPulseTimer * 14f) * 0.5f;
                SetHealVisuals(true, pulse);
            }

            if (CurrentState == GGReplicaGlitchState.FireAim)
            {
                _fireAimPulseTimer += deltaTime;
                float pulse = 0.5f + Mathf.Sin(_fireAimPulseTimer * 18f) * 0.5f;
                SetFireAimVisuals(true, pulse);
            }

            if (CurrentState == GGReplicaGlitchState.GrabHold)
            {
                _grabHoldTimer += deltaTime;
                bool locked = _grabHoldTimer >= GrabLockDelay;
                SetGrabFluxyVisuals(true, locked ? 1f : 0.45f);
                SetGrabLockVisuals(locked, locked ? 1f : 0f);
            }

            if (_grabReleaseTimer > 0f)
            {
                _grabReleaseTimer -= deltaTime;
                float intensity = Mathf.Clamp01(_grabReleaseTimer / GrabReleaseDuration);
                SetGrabReleaseVisuals(intensity > 0f, intensity);
                SetGrabReleaseThrowVisuals(intensity > 0f, intensity);
                if (intensity <= 0f)
                {
                    StopParticles(_grabReleaseParticles);
                }

                SetActive(_fluxyGrabModuleRoot, intensity > 0f);
            }
        }

        private void SetTrailEmitting(bool emitting)
        {
            foreach (var trail in _trailRenderers)
            {
                if (trail == null) continue;
                trail.emitting = emitting;
            }
        }

        private void ApplyViewSpritePack(GGReplicaGlitchState state)
        {
            if (_playerSkin == null) return;
            if (!_playerSkin.TryGetPack(ToViewState(state), out var pack) || pack == null) return;

            ApplySpriteIfPresent(_solidRenderer, pack.SolidSprite);
            ApplySpriteIfPresent(_liquidRenderer, pack.LiquidSprite);
            ApplySpriteIfPresent(_highlightRenderer, pack.HighlightSprite);

            Transform bodyRoot = _bodyLayersRoot != null ? _bodyLayersRoot : _solidRenderer != null ? _solidRenderer.transform.parent : null;
            if (bodyRoot != null)
            {
                bodyRoot.localPosition = pack.SpritesOffset;
            }
        }

        private static void ApplySpriteIfPresent(SpriteRenderer renderer, Sprite sprite)
        {
            if (renderer != null && sprite != null)
            {
                renderer.sprite = sprite;
            }
        }

        private static GGReplicaViewState ToViewState(GGReplicaGlitchState state)
        {
            switch (state)
            {
                case GGReplicaGlitchState.BoostHold:
                    return GGReplicaViewState.Boost;
                case GGReplicaGlitchState.DodgeBurst:
                    return GGReplicaViewState.Dodge;
                case GGReplicaGlitchState.GrabHold:
                    return GGReplicaViewState.Grab;
                case GGReplicaGlitchState.Heal:
                    return GGReplicaViewState.Heal;
                case GGReplicaGlitchState.FireAim:
                    return GGReplicaViewState.Fire;
                default:
                    return GGReplicaViewState.Idle;
            }
        }

        private static void SetParticles(ParticleSystem[] particles, bool active)
        {
            if (active)
            {
                PlayParticles(particles);
            }
            else
            {
                StopParticles(particles);
            }
        }

        private static void PlayParticles(ParticleSystem[] particles)
        {
            foreach (var particle in particles)
            {
                if (particle == null) continue;
                if (!particle.isPlaying)
                {
                    particle.Play();
                }
            }
        }

        private static void StopParticles(ParticleSystem[] particles)
        {
            foreach (var particle in particles)
            {
                if (particle == null) continue;
                if (particle.isPlaying)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void ApplyBurstParticles(bool boosting, bool dodging, bool healing, bool firing, bool enteringBoost, bool enteringDodge)
        {
            if (boosting)
            {
                if (enteringBoost)
                {
                    _boostBurstTimer = BoostIgniteDuration;
                    PlayParticles(BoostBurstParticles);
                }

                return;
            }

            _boostBurstTimer = 0f;
            if (dodging)
            {
                if (enteringDodge)
                {
                    PlayParticles(DodgeBurstParticles);
                }

                return;
            }

            StopParticles(_burstParticles);
        }

        private void ApplyDodgeVisuals(bool dodging, bool enteringDodge)
        {
            if (dodging)
            {
                if (enteringDodge)
                {
                    _dodgeVisualTimer = DodgeVisualDuration;
                }

                SetDodgeVisuals(true, 1f);
                return;
            }

            _dodgeVisualTimer = 0f;
            SetDodgeVisuals(false, 0f);
        }

        private void SetDodgeVisuals(bool dodging, float intensity)
        {
            float clamped = Mathf.Clamp01(intensity);
            if (_dodgeGhostRenderer != null)
            {
                _dodgeGhostRenderer.enabled = dodging;
                _dodgeGhostRenderer.color = dodging ? new Color(0.65f, 0.15f, 1f, 0.75f * clamped) : Color.clear;
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, clamped);
                _coreRenderer.color = dodging ? new Color(0.75f, 0.05f, 1f, Mathf.Lerp(0.2f, 0.9f, clamped)) : Color.white;
            }

            if (_dodgeHalfRenderer != null)
            {
                _dodgeHalfRenderer.enabled = dodging;
                _dodgeHalfRenderer.transform.localScale = dodging ? Vector3.one * Mathf.Lerp(1.08f, 1.28f, clamped) : Vector3.one;
                _dodgeHalfRenderer.color = dodging ? new Color(1f, 0.78f, 0.05f, Mathf.Lerp(0.35f, 0.8f, clamped)) : Color.clear;
            }

            if (_dodgeAdditiveCoreRenderer != null)
            {
                _dodgeAdditiveCoreRenderer.enabled = dodging;
                _dodgeAdditiveCoreRenderer.transform.localScale = dodging ? Vector3.one * Mathf.Lerp(1.28f, 1.65f, clamped) : Vector3.one;
                _dodgeAdditiveCoreRenderer.color = dodging ? new Color(1f, 0.35f, 0.05f, Mathf.Lerp(0.55f, 0.95f, clamped)) : Color.clear;
            }

            SetFluxyTrailState(dodging, clamped, dodging);
        }

        private void SetFluxyTrailState(bool active, float intensity, bool dodge)
        {
            if (_fluxyTrailRenderer == null) return;
            CaptureFluxyDefaults();

            float clamped = Mathf.Clamp01(intensity);
            _fluxyTrailRenderer.emitting = active;
            if (!active)
            {
                _fluxyTrailRenderer.time = _fluxyDefaultTime;
                _fluxyTrailRenderer.widthMultiplier = _fluxyDefaultWidth;
                _fluxyTrailRenderer.startColor = _fluxyDefaultStartColor;
                _fluxyTrailRenderer.endColor = _fluxyDefaultEndColor;
                _fluxyTrailRenderer.transform.localScale = _fluxyDefaultScale;
                SetFluxyTrailBlock(0f, 3.77f, 6f);
                return;
            }

            float widthTarget = dodge ? 1.65f : 1.1f;
            float timeTarget = dodge ? 1.4f : 1.05f;
            float scaleTarget = dodge ? 1.22f : 1.05f;
            _fluxyTrailRenderer.widthMultiplier = _fluxyDefaultWidth * Mathf.Lerp(1f, widthTarget, clamped);
            _fluxyTrailRenderer.time = _fluxyDefaultTime * Mathf.Lerp(1f, timeTarget, clamped);
            _fluxyTrailRenderer.transform.localScale = _fluxyDefaultScale * Mathf.Lerp(1f, scaleTarget, clamped);
            if (dodge)
            {
                _fluxyTrailRenderer.startColor = new Color(1f, 0.2f, 1f, Mathf.Lerp(0.35f, 0.95f, clamped));
                _fluxyTrailRenderer.endColor = new Color(1f, 0.62f, 0.05f, Mathf.Lerp(0.08f, 0.28f, clamped));
                SetFluxyTrailBlock(Mathf.Lerp(0.62f, 0.95f, clamped), Mathf.Lerp(3.77f, 6.4f, clamped), Mathf.Lerp(6f, 8f, clamped));
            }
            else
            {
                _fluxyTrailRenderer.startColor = new Color(0.75f, 0f, 1f, Mathf.Lerp(0.25f, 0.62f, clamped));
                _fluxyTrailRenderer.endColor = new Color(0.25f, 0.9f, 1f, Mathf.Lerp(0.05f, 0.18f, clamped));
                SetFluxyTrailBlock(Mathf.Lerp(0.25f, 0.62f, clamped), Mathf.Lerp(3.77f, 4.4f, clamped), Mathf.Lerp(6f, 6.5f, clamped));
            }
        }

        private void CaptureFluxyDefaults()
        {
            if (_fluxyDefaultsCaptured || _fluxyTrailRenderer == null) return;
            _fluxyDefaultTime = _fluxyTrailRenderer.time;
            _fluxyDefaultWidth = _fluxyTrailRenderer.widthMultiplier;
            _fluxyDefaultStartColor = _fluxyTrailRenderer.startColor;
            _fluxyDefaultEndColor = _fluxyTrailRenderer.endColor;
            _fluxyDefaultScale = _fluxyTrailRenderer.transform.localScale;
            _fluxyDefaultsCaptured = true;
        }

        private void SetFluxyTrailBlock(float alpha, float flowPower, float noiseScale)
        {
            if (_fluxyTrailRenderer == null) return;
            _fluxyTrailBlock ??= new MaterialPropertyBlock();
            _fluxyTrailRenderer.GetPropertyBlock(_fluxyTrailBlock);
            _fluxyTrailBlock.SetFloat(AlphaId, alpha);
            _fluxyTrailBlock.SetFloat(FlowPowerId, flowPower);
            _fluxyTrailBlock.SetFloat(NoiseScaleId, noiseScale);
            _fluxyTrailRenderer.SetPropertyBlock(_fluxyTrailBlock);
        }

        private void ApplyGrabVisuals(bool grabbing, bool enteringGrab, bool exitingGrab)
        {
            if (enteringGrab)
            {
                _grabHoldTimer = 0f;
                _grabReleaseTimer = 0f;
                SetGrabReleaseVisuals(false, 0f);
                SetGrabReleaseThrowVisuals(false, 0f);
                StopParticles(_grabReleaseParticles);
            }

            EnsureGrabClosedPositions();
            for (int i = 0; i < _grabRenderers.Length; i++)
            {
                var renderer = _grabRenderers[i];
                if (renderer == null) continue;

                Vector3 closed = _grabClosedPositions[i];
                float side = closed.x >= 0f ? 1f : -1f;
                renderer.transform.localPosition = grabbing ? closed + new Vector3(side * 0.18f, 0.03f, 0f) : closed;
                renderer.transform.localScale = grabbing ? Vector3.one * 1.15f : Vector3.one;
                renderer.color = grabbing ? Color.white : Color.white;
            }

            SetGrabFluxyVisuals(grabbing, grabbing ? 0.45f : 0f);
            SetGrabLockVisuals(false, 0f);
            if (exitingGrab)
            {
                _grabHoldTimer = 0f;
                _grabReleaseTimer = GrabReleaseDuration;
                SetGrabReleaseVisuals(true, 1f);
                SetGrabReleaseThrowVisuals(true, 1f);
                PlayParticles(_grabReleaseParticles);
            }
        }

        private void SetGrabFluxyVisuals(bool grabbing, float intensity)
        {
            float clamped = Mathf.Clamp01(intensity);
            for (int i = 0; i < _grabFluxyRenderers.Length; i++)
            {
                var renderer = _grabFluxyRenderers[i];
                if (renderer == null) continue;

                float side = i == 0 ? 1f : -1f;
                renderer.enabled = grabbing;
                renderer.transform.localPosition = grabbing ? new Vector3(side * Mathf.Lerp(0.48f, 0.78f, clamped), -0.08f, 0f) : Vector3.zero;
                renderer.transform.localScale = grabbing ? Vector3.one * Mathf.Lerp(1.1f, 1.45f, clamped) : Vector3.one;
                renderer.color = grabbing ? new Color(1f, 0f, 1f, Mathf.Lerp(0.45f, 0.86f, clamped)) : Color.clear;
                SetGrabFluxyBlock(renderer, grabbing ? Mathf.Lerp(0.45f, 0.86f, clamped) : 0f, grabbing ? Mathf.Lerp(4.2f, 6.2f, clamped) : 3.77f);
            }

            if (_grabThrowPointer != null)
            {
                _grabThrowPointer.enabled = grabbing;
                _grabThrowPointer.positionCount = 2;
                _grabThrowPointer.useWorldSpace = false;
                _grabThrowPointer.startWidth = grabbing ? Mathf.Lerp(0.04f, 0.08f, clamped) : 0.04f;
                _grabThrowPointer.endWidth = grabbing ? Mathf.Lerp(0.015f, 0.035f, clamped) : 0.015f;
                _grabThrowPointer.startColor = grabbing ? new Color(1f, 0f, 1f, Mathf.Lerp(0.36f, 0.72f, clamped)) : Color.clear;
                _grabThrowPointer.endColor = grabbing ? new Color(0.25f, 1f, 1f, Mathf.Lerp(0.12f, 0.35f, clamped)) : Color.clear;
                _grabThrowPointer.SetPosition(0, new Vector3(-0.74f, -0.08f, 0f));
                _grabThrowPointer.SetPosition(1, new Vector3(0.74f, -0.08f, 0f));
                SetGrabFluxyBlock(_grabThrowPointer, grabbing ? Mathf.Lerp(0.36f, 0.72f, clamped) : 0f, grabbing ? Mathf.Lerp(4.2f, 5.8f, clamped) : 3.77f);
            }
        }

        private void SetGrabLockVisuals(bool locked, float intensity)
        {
            if (_grabLockRenderer == null) return;
            float clamped = Mathf.Clamp01(intensity);
            _grabLockRenderer.enabled = locked;
            _grabLockRenderer.transform.localScale = locked ? Vector3.one * Mathf.Lerp(1.05f, 1.36f, clamped) : Vector3.one;
            _grabLockRenderer.color = locked ? new Color(0.25f, 1f, 1f, Mathf.Lerp(0.45f, 0.82f, clamped)) : Color.clear;
            SetGrabFluxyBlock(_grabLockRenderer, locked ? Mathf.Lerp(0.48f, 0.82f, clamped) : 0f, locked ? Mathf.Lerp(4.8f, 6.6f, clamped) : 3.77f);
        }

        private void SetGrabReleaseVisuals(bool active, float intensity)
        {
            if (_grabReleaseRenderer == null) return;
            float clamped = Mathf.Clamp01(intensity);
            _grabReleaseRenderer.enabled = active;
            _grabReleaseRenderer.transform.localScale = active ? Vector3.one * Mathf.Lerp(1.8f, 0.9f, 1f - clamped) : Vector3.one;
            _grabReleaseRenderer.color = active ? new Color(1f, 0.2f, 1f, Mathf.Lerp(0.1f, 0.78f, clamped)) : Color.clear;
            SetGrabFluxyBlock(_grabReleaseRenderer, active ? Mathf.Lerp(0.1f, 0.78f, clamped) : 0f, active ? Mathf.Lerp(6.4f, 3.77f, 1f - clamped) : 3.77f);
        }

        private void SetGrabReleaseThrowVisuals(bool active, float intensity)
        {
            if (_grabReleaseThrowLine == null) return;
            float clamped = Mathf.Clamp01(intensity);
            _grabReleaseThrowLine.enabled = active;
            _grabReleaseThrowLine.useWorldSpace = false;
            _grabReleaseThrowLine.positionCount = 3;
            _grabReleaseThrowLine.startWidth = active ? Mathf.Lerp(0.035f, 0.12f, clamped) : 0.035f;
            _grabReleaseThrowLine.endWidth = active ? Mathf.Lerp(0.01f, 0.04f, clamped) : 0.01f;
            _grabReleaseThrowLine.startColor = active ? new Color(1f, 0f, 1f, Mathf.Lerp(0.05f, 0.86f, clamped)) : Color.clear;
            _grabReleaseThrowLine.endColor = active ? new Color(0.25f, 1f, 1f, Mathf.Lerp(0.03f, 0.42f, clamped)) : Color.clear;
            float reach = Mathf.Lerp(0.18f, 0.92f, clamped);
            float lift = Mathf.Lerp(0.02f, 0.2f, clamped);
            _grabReleaseThrowLine.SetPosition(0, new Vector3(-reach, -0.08f, 0f));
            _grabReleaseThrowLine.SetPosition(1, new Vector3(0f, lift, 0f));
            _grabReleaseThrowLine.SetPosition(2, new Vector3(reach, -0.08f, 0f));
            SetGrabFluxyBlock(_grabReleaseThrowLine, active ? Mathf.Lerp(0.08f, 0.86f, clamped) : 0f, active ? Mathf.Lerp(6.8f, 3.77f, 1f - clamped) : 3.77f);
        }

        private void SetGrabFluxyBlock(Renderer renderer, float alpha, float flowPower)
        {
            if (renderer == null) return;
            _grabFluxyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_grabFluxyBlock);
            _grabFluxyBlock.SetFloat(AlphaId, alpha);
            _grabFluxyBlock.SetFloat(FlowPowerId, flowPower);
            _grabFluxyBlock.SetFloat(NoiseScaleId, 6f);
            renderer.SetPropertyBlock(_grabFluxyBlock);
        }

        private void EnsureGrabClosedPositions()
        {
            if (_grabClosedPositions.Length == _grabRenderers.Length) return;

            _grabClosedPositions = new Vector3[_grabRenderers.Length];
            for (int i = 0; i < _grabRenderers.Length; i++)
            {
                _grabClosedPositions[i] = _grabRenderers[i] != null ? _grabRenderers[i].transform.localPosition : Vector3.zero;
            }
        }

        private void ApplyHealVisuals(bool healing, bool enteringHeal)
        {
            if (!healing)
            {
                _healPulseTimer = 0f;
                SetHealVisuals(false, 0f);
                return;
            }

            if (enteringHeal)
            {
                _healPulseTimer = 0f;
            }

            SetHealVisuals(true, 1f);
        }

        private void SetHealVisuals(bool healing, float intensity)
        {
            float clamped = Mathf.Clamp01(intensity);
            for (int i = 0; i < _healRenderers.Length; i++)
            {
                var renderer = _healRenderers[i];
                if (renderer == null) continue;

                renderer.enabled = healing;
                renderer.transform.localScale = healing ? Vector3.one * Mathf.Lerp(1.08f, 1.35f, clamped) : Vector3.one;
                renderer.color = healing ? new Color(0.25f, 1f, 0.85f, Mathf.Lerp(0.62f, 0.95f, clamped)) : Color.clear;
            }
        }

        private void ApplyFireAimVisuals(bool firing, bool enteringFireAim)
        {
            if (!firing)
            {
                _fireAimPulseTimer = 0f;
                SetFireAimVisuals(false, 0f);
                return;
            }

            if (enteringFireAim)
            {
                _fireAimPulseTimer = 0f;
            }

            SetFireAimVisuals(true, 1f);
        }

        private void SetFireAimVisuals(bool firing, float intensity)
        {
            float clamped = Mathf.Clamp01(intensity);
            for (int i = 0; i < _fireAimRenderers.Length; i++)
            {
                var renderer = _fireAimRenderers[i];
                if (renderer == null) continue;

                renderer.enabled = firing;
                renderer.transform.localScale = firing ? Vector3.one * Mathf.Lerp(1.03f, 1.22f, clamped) : Vector3.one;
                renderer.color = firing ? new Color(1f, Mathf.Lerp(0.18f, 0.45f, clamped), Mathf.Lerp(0.85f, 1f, clamped), Mathf.Lerp(0.72f, 1f, clamped)) : Color.clear;
            }
        }

        private ParticleSystem[] BoostBurstParticles => _boostBurstParticles.Length > 0 ? _boostBurstParticles : _burstParticles;

        private ParticleSystem[] DodgeBurstParticles => _dodgeBurstParticles.Length > 0 ? _dodgeBurstParticles : _burstParticles;

        private float BoostIgniteDuration => _feelProfile != null ? _feelProfile.BoostIgniteDuration : 0.08f;

        private float DodgeVisualDuration => _feelProfile != null ? _feelProfile.DodgeStateDuration : 0.225f;

        private const float GrabLockDelay = 0.18f;

        private const float GrabReleaseDuration = 0.16f;

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
