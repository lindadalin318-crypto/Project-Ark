using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Runtime material-state layer for the isolated GGReplica PlayerView lane.
    /// Uses MaterialPropertyBlock so authored GGReplica material assets are never mutated at runtime.
    /// </summary>
    public sealed class GGReplicaMaterialVisualModule : MonoBehaviour
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int StateId = Shader.PropertyToID("_State");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int BoostAmountId = Shader.PropertyToID("_BoostAmount");
        private static readonly int HealAmountId = Shader.PropertyToID("_HealAmount");
        private static readonly int SchemeAlphaId = Shader.PropertyToID("_SchemeAlpha");
        private static readonly int GlitchStrengthId = Shader.PropertyToID("_GlitchStrength");
        private static readonly int TrailIntensityId = Shader.PropertyToID("_TrailIntensity");
        private static readonly int EdgeBoostId = Shader.PropertyToID("_EdgeBoost");
        private static readonly int GrabEmphasisId = Shader.PropertyToID("_GrabEmphasis");

        [SerializeField] private SpriteRenderer _highlightRenderer;
        [SerializeField] private SpriteRenderer _viewRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private SpriteRenderer _dodgeRenderer;
        [SerializeField] private SpriteRenderer _grabRightRenderer;
        [SerializeField] private SpriteRenderer _grabLeftRenderer;
        [SerializeField] private TrailRenderer _playerLqTrail;

        private MaterialPropertyBlock _block;

        public void ApplyState(GGReplicaViewState state)
        {
            float intensity = GetHighlightIntensity(state);
            Color tint = GetHighlightTint(state);
            float pulse = state == GGReplicaViewState.Dodge ? 1f : 0f;
            float boostAmount = state == GGReplicaViewState.Boost ? 1f : 0f;
            float healAmount = state == GGReplicaViewState.Heal ? 1f : 0f;
            float grabEmphasis = state == GGReplicaViewState.Grab ? 1f : 0f;
            float trailIntensity = state == GGReplicaViewState.Boost ? 1f : 0f;

            SetHighlightBlock(_highlightRenderer, intensity, tint, boostAmount, healAmount);
            SetViewBlock(_viewRenderer, state);
            SetFloat(_coreRenderer, PulseId, pulse);
            SetFloat(_dodgeRenderer, PulseId, pulse);
            SetFloat(_grabRightRenderer, GrabEmphasisId, grabEmphasis);
            SetFloat(_grabLeftRenderer, GrabEmphasisId, grabEmphasis);
            SetTrailBlock(_playerLqTrail, trailIntensity);

            if (_playerLqTrail != null)
            {
                _playerLqTrail.emitting = state == GGReplicaViewState.Boost;
            }
        }

        private static float GetHighlightIntensity(GGReplicaViewState state)
        {
            return state switch
            {
                GGReplicaViewState.Boost => 12f,
                GGReplicaViewState.Dodge => 10f,
                GGReplicaViewState.Grab => 10f,
                GGReplicaViewState.Heal => 11f,
                GGReplicaViewState.Fire => 10f,
                GGReplicaViewState.HeavyFire => 12f,
                GGReplicaViewState.WeaponUseMoment => 10f,
                _ => 8f
            };
        }

        private static Color GetHighlightTint(GGReplicaViewState state)
        {
            return state == GGReplicaViewState.Heal
                ? new Color(0.35f, 1f, 0.85f, 1f)
                : new Color(0.54509807f, 0.09019608f, 1f, 1f);
        }

        private static float GetSchemeAlpha(GGReplicaViewState state)
        {
            return state switch
            {
                GGReplicaViewState.Dodge => 0.7f,
                GGReplicaViewState.Boost => 0.6f,
                GGReplicaViewState.Grab => 0.55f,
                _ => 0.45f
            };
        }

        private static float GetGlitchStrength(GGReplicaViewState state)
        {
            return state switch
            {
                GGReplicaViewState.Dodge => 0.7f,
                GGReplicaViewState.Boost => 0.55f,
                GGReplicaViewState.Grab => 0.45f,
                GGReplicaViewState.Heal => 0.25f,
                _ => 0.3f
            };
        }

        private void SetFloat(Renderer renderer, int propertyId, float value)
        {
            if (renderer == null) return;
            EnsureBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetFloat(propertyId, value);
            renderer.SetPropertyBlock(_block);
            _block.Clear();
        }

        private void SetHighlightBlock(Renderer renderer, float intensity, Color tint, float boostAmount, float healAmount)
        {
            if (renderer == null) return;
            EnsureBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetFloat(IntensityId, intensity);
            _block.SetColor(TintId, tint);
            _block.SetFloat(BoostAmountId, boostAmount);
            _block.SetFloat(HealAmountId, healAmount);
            renderer.SetPropertyBlock(_block);
            _block.Clear();
        }

        private void SetViewBlock(Renderer renderer, GGReplicaViewState state)
        {
            if (renderer == null) return;
            EnsureBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetFloat(StateId, (int)state);
            _block.SetFloat(SchemeAlphaId, GetSchemeAlpha(state));
            _block.SetFloat(GlitchStrengthId, GetGlitchStrength(state));
            renderer.SetPropertyBlock(_block);
            _block.Clear();
        }

        private void SetTrailBlock(Renderer renderer, float trailIntensity)
        {
            if (renderer == null) return;
            EnsureBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetFloat(TrailIntensityId, trailIntensity);
            _block.SetFloat(EdgeBoostId, trailIntensity);
            renderer.SetPropertyBlock(_block);
            _block.Clear();
        }

        private void EnsureBlock()
        {
            _block ??= new MaterialPropertyBlock();
        }
    }
}
