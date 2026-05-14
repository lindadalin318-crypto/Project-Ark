using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// GG-like visual coordinator for the isolated GGReplica ship prefab.
    /// Mirrors original AnimationClip event entry points through ChangeViewState(int).
    /// </summary>
    public class GGReplicaPlayerViewAdapter : MonoBehaviour
    {
        [Header("Skin")]
        [SerializeField] private GGReplicaPlayerSkinSO _skin;

        [Header("Sprite Root")]
        [SerializeField] private Transform _spritesRoot;

        [Header("Body Layers")]
        [SerializeField] private SpriteRenderer _shipLiquidRenderer;
        [SerializeField] private SpriteRenderer _shipHighlightRenderer;
        [SerializeField] private SpriteRenderer _dodgeRenderer;
        [SerializeField] private SpriteRenderer _shipSolidRenderer;
        [SerializeField] private SpriteRenderer _shipBackRenderer;
        [SerializeField] private SpriteRenderer _shipGrabRightRenderer;
        [SerializeField] private SpriteRenderer _shipGrabLeftRenderer;

        [Header("Fixed Layers")]
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private SpriteRenderer _eyeRenderer;
        [SerializeField] private SpriteRenderer _viewSilhouetteRenderer;
        [SerializeField] private SpriteRenderer _dodgeHalfRenderer;

        [Header("Modules")]
        [SerializeField] private GGReplicaCoreVisualModule _coreModule;
        [SerializeField] private GGReplicaBoostVisualModule _boostModule;
        [SerializeField] private GGReplicaShapeVisualModule _shapeModule;
        [SerializeField] private GGReplicaMaterialVisualModule _materialModule;

        private GGReplicaViewSpritePack _currentSpritePack;

        public GGReplicaViewState CurrentState { get; private set; } = GGReplicaViewState.Undefined;
        public GGReplicaViewSpritePack CurrentSpritePack => CopyPack(_currentSpritePack);

        private void OnEnable()
        {
            ApplyFixedSkinFields();
            SetTransientLayerVisibility(CurrentState);
        }

        public void ChangeViewState(int state) => ChangeViewState((GGReplicaViewState)state, strict: false);

        public void ChangeViewState(GGReplicaViewState state, bool strict = false)
        {
            if (!ValidateRequiredReferences()) return;

            if (!_skin.TryGetPack(state, out var pack))
            {
                string message = $"[GGReplicaPlayerViewAdapter] Missing sprite pack for {state} ({(int)state}).";
                if (strict)
                {
                    Debug.LogError(message, this);
                }
                else
                {
                    Debug.LogWarning(message, this);
                }

                return;
            }

            CurrentState = state;
            _currentSpritePack = CopyPack(pack);
            ApplyFixedSkinFields();
            ApplySpritePack(pack);
            SetTransientLayerVisibility(state);
            NotifyModules(state);
        }

        public GGReplicaViewSpritePack GetCurrentSpritePack() => CurrentSpritePack;

        private void NotifyModules(GGReplicaViewState state)
        {
            _coreModule?.ApplyState(state);
            _boostModule?.ApplyState(state);
            _shapeModule?.ApplyState(state);
            _materialModule?.ApplyState(state);
        }

        private bool ValidateRequiredReferences()
        {
            bool valid = true;
            valid &= RequireReference(_skin, "player skin");
            valid &= RequireReference(_spritesRoot, "sprites root");
            valid &= RequireReference(_shipSolidRenderer, "solid renderer");
            valid &= RequireReference(_shipLiquidRenderer, "liquid renderer");
            valid &= RequireReference(_shipHighlightRenderer, "highlight renderer");
            valid &= RequireReference(_shipBackRenderer, "back renderer");
            valid &= RequireReference(_shipGrabRightRenderer, "grab right renderer");
            valid &= RequireReference(_shipGrabLeftRenderer, "grab left renderer");
            valid &= RequireReference(_coreRenderer, "core renderer");
            valid &= RequireReference(_eyeRenderer, "eye renderer");
            valid &= RequireReference(_viewSilhouetteRenderer, "view silhouette renderer");
            valid &= RequireReference(_dodgeRenderer, "dodge renderer");
            valid &= RequireReference(_dodgeHalfRenderer, "dodge half renderer");
            return valid;
        }

        private bool RequireReference(Object reference, string label)
        {
            if (reference != null) return true;
            Debug.LogError($"[GGReplicaPlayerViewAdapter] Missing required reference: {label}.", this);
            return false;
        }

        private void ApplyFixedSkinFields()
        {
            if (_skin == null) return;

            if (_shipBackRenderer != null) _shipBackRenderer.sprite = _skin.ShipSpriteBack;
            if (_shipGrabRightRenderer != null) _shipGrabRightRenderer.sprite = _skin.ShipSpriteSolidGrabR;
            if (_shipGrabLeftRenderer != null) _shipGrabLeftRenderer.sprite = _skin.ShipSpriteSolidGrabL;
            if (_coreRenderer != null) _coreRenderer.sprite = _skin.ReactorSprite;
            if (_eyeRenderer != null) _eyeRenderer.sprite = _skin.EyeSprite;
            if (_viewSilhouetteRenderer != null) _viewSilhouetteRenderer.sprite = _skin.ViewSilhouetteSprite;
            if (_dodgeRenderer != null) _dodgeRenderer.sprite = _skin.DodgeSprite;
            if (_dodgeHalfRenderer != null) _dodgeHalfRenderer.sprite = _skin.DodgeHalfSprite;
            if (_shipHighlightRenderer != null) _shipHighlightRenderer.color = _skin.ShipHighlightColor;
        }

        private void ApplySpritePack(GGReplicaViewSpritePack pack)
        {
            if (_shipSolidRenderer != null && pack.SolidSprite != null) _shipSolidRenderer.sprite = pack.SolidSprite;
            if (_shipLiquidRenderer != null && pack.LiquidSprite != null) _shipLiquidRenderer.sprite = pack.LiquidSprite;
            if (_shipHighlightRenderer != null && pack.HighlightSprite != null) _shipHighlightRenderer.sprite = pack.HighlightSprite;
            if (_spritesRoot != null) _spritesRoot.localPosition = pack.SpritesOffset;
        }

        private void SetTransientLayerVisibility(GGReplicaViewState state)
        {
            bool dodging = state == GGReplicaViewState.Dodge;
            bool grabbing = state == GGReplicaViewState.Grab;

            if (_dodgeRenderer != null) _dodgeRenderer.enabled = dodging;
            if (_dodgeHalfRenderer != null) _dodgeHalfRenderer.enabled = dodging;
            if (_shipGrabRightRenderer != null) _shipGrabRightRenderer.enabled = grabbing;
            if (_shipGrabLeftRenderer != null) _shipGrabLeftRenderer.enabled = grabbing;
        }

        private static GGReplicaViewSpritePack CopyPack(GGReplicaViewSpritePack source)
        {
            if (source == null) return null;
            return new GGReplicaViewSpritePack
            {
                State = source.State,
                FadeDuration = source.FadeDuration,
                SolidSprite = source.SolidSprite,
                LiquidSprite = source.LiquidSprite,
                HighlightSprite = source.HighlightSprite,
                SpritesOffset = source.SpritesOffset
            };
        }
    }
}
