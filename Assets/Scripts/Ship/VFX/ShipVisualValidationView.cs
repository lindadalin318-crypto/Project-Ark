using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Preview-only validation controller for the Canary ship visual matrix.
    /// This component must not become the runtime owner of the live Ship.prefab chain.
    /// </summary>
    public sealed class ShipVisualValidationView : MonoBehaviour
    {
        public enum ValidationState
        {
            Normal,
            LeanLeft,
            LeanRight,
            Dash,
            Boost,
            Fire,
            Hit,
            Weaving,
            Overheat
        }

        public enum ValidationBackground
        {
            Black,
            White,
            DeepBlue
        }

        [Header("Renderers")]
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _shapeRenderer;
        [SerializeField] private SpriteRenderer _outlineRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private SpriteRenderer _weaponMountRenderer;
        [SerializeField] private SpriteRenderer _dashTrailRenderer;
        [SerializeField] private SpriteRenderer _dashParticlesRenderer;
        [SerializeField] private SpriteRenderer _weavingAuraRenderer;
        [SerializeField] private SpriteRenderer _weavingCoreRenderer;
        [SerializeField] private SpriteRenderer _backgroundRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite _normalBodySprite;
        [SerializeField] private Sprite _leanLeftSprite;
        [SerializeField] private Sprite _leanRightSprite;
        [SerializeField] private Sprite _dashSprite;
        [SerializeField] private Sprite _normalShapeSprite;
        [SerializeField] private Sprite _hitMaskSprite;
        [SerializeField] private Sprite _overheatShapeSprite;
        [SerializeField] private Sprite _overheatCoreSprite;

        [Header("Optional Bloom")]
        [SerializeField] private Volume _bloomVolume;

        [Header("Initial State")]
        [SerializeField] private ValidationState _initialState = ValidationState.Normal;
        [SerializeField] private ValidationBackground _initialBackground = ValidationBackground.DeepBlue;
        [SerializeField] private bool _initialBloomEnabled = true;

        private static readonly Color NormalColor = Color.white;
        private static readonly Color FireCoreColor = new(1f, 0.72f, 0.24f, 1f);
        private static readonly Color BoostCoreColor = new(0.35f, 0.85f, 1f, 1f);
        private static readonly Color HitFlashColor = new(1f, 0.96f, 0.78f, 1f);
        private static readonly Color OverheatColor = new(1f, 0.28f, 0.08f, 1f);
        private static readonly Color WeavingColor = new(0.58f, 0.38f, 1f, 1f);
        private static readonly Color DeepBlue = new(0.015f, 0.035f, 0.11f, 1f);

        private Sprite _defaultCoreSprite;

        public ValidationState CurrentState { get; private set; }
        public ValidationBackground CurrentBackground { get; private set; }
        public bool BloomEnabled => _bloomVolume != null && _bloomVolume.enabled;

        private void Awake()
        {
            CacheDefaultSprites();
            ValidateReferences();
            ApplyInitialState();
        }

        private void OnEnable()
        {
            CacheDefaultSprites();
            ApplyInitialState();
        }

        [ContextMenu("Validation/Show Normal")]
        public void ShowNormal() => SetState(ValidationState.Normal);

        [ContextMenu("Validation/Show Lean Left")]
        public void ShowLeanLeft() => SetState(ValidationState.LeanLeft);

        [ContextMenu("Validation/Show Lean Right")]
        public void ShowLeanRight() => SetState(ValidationState.LeanRight);

        [ContextMenu("Validation/Show Dash")]
        public void ShowDash() => SetState(ValidationState.Dash);

        [ContextMenu("Validation/Show Boost")]
        public void ShowBoost() => SetState(ValidationState.Boost);

        [ContextMenu("Validation/Show Fire")]
        public void ShowFire() => SetState(ValidationState.Fire);

        [ContextMenu("Validation/Show Hit")]
        public void ShowHit() => SetState(ValidationState.Hit);

        [ContextMenu("Validation/Show Weaving")]
        public void ShowWeaving() => SetState(ValidationState.Weaving);

        [ContextMenu("Validation/Show Overheat")]
        public void ShowOverheat() => SetState(ValidationState.Overheat);

        [ContextMenu("Validation/Background Black")]
        public void ShowBlackBackground() => SetBackground(ValidationBackground.Black);

        [ContextMenu("Validation/Background White")]
        public void ShowWhiteBackground() => SetBackground(ValidationBackground.White);

        [ContextMenu("Validation/Background Deep Blue")]
        public void ShowDeepBlueBackground() => SetBackground(ValidationBackground.DeepBlue);

        [ContextMenu("Validation/Bloom On")]
        public void EnableBloom() => SetBloomEnabled(true);

        [ContextMenu("Validation/Bloom Off")]
        public void DisableBloom() => SetBloomEnabled(false);

        public void SetState(ValidationState state)
        {
            ResetVisuals();
            CurrentState = state;

            switch (state)
            {
                case ValidationState.Normal:
                    SetMainBody(_normalBodySprite, NormalColor);
                    break;
                case ValidationState.LeanLeft:
                    SetMainBody(_leanLeftSprite, NormalColor);
                    break;
                case ValidationState.LeanRight:
                    SetMainBody(_leanRightSprite, NormalColor);
                    break;
                case ValidationState.Dash:
                    SetMainBody(_dashSprite, NormalColor);
                    SetRendererEnabled(_dashTrailRenderer, true);
                    SetRendererEnabled(_dashParticlesRenderer, true);
                    break;
                case ValidationState.Boost:
                    SetMainBody(_normalBodySprite, NormalColor);
                    SetRendererColor(_coreRenderer, BoostCoreColor);
                    SetRendererEnabled(_dashTrailRenderer, true);
                    break;
                case ValidationState.Fire:
                    SetMainBody(_normalBodySprite, NormalColor);
                    SetRendererColor(_coreRenderer, FireCoreColor);
                    SetRendererColor(_weaponMountRenderer, FireCoreColor);
                    break;
                case ValidationState.Hit:
                    SetMainBody(_normalBodySprite, HitFlashColor);
                    SetRendererSprite(_shapeRenderer, _hitMaskSprite);
                    SetRendererColor(_shapeRenderer, HitFlashColor);
                    SetRendererEnabled(_shapeRenderer, true);
                    break;
                case ValidationState.Weaving:
                    SetMainBody(_normalBodySprite, NormalColor);
                    SetRendererColor(_coreRenderer, WeavingColor);
                    SetRendererEnabled(_weavingAuraRenderer, true);
                    SetRendererEnabled(_weavingCoreRenderer, true);
                    break;
                case ValidationState.Overheat:
                    SetMainBody(_normalBodySprite, OverheatColor);
                    SetRendererSprite(_shapeRenderer, _overheatShapeSprite);
                    SetRendererColor(_shapeRenderer, OverheatColor);
                    SetRendererEnabled(_shapeRenderer, true);
                    SetRendererSprite(_coreRenderer, _overheatCoreSprite != null ? _overheatCoreSprite : _coreRenderer.sprite);
                    SetRendererColor(_coreRenderer, OverheatColor);
                    break;
                default:
                    Debug.LogError($"[ShipVisualValidationView] Unsupported validation state: {state}", this);
                    break;
            }
        }

        public void SetBackground(ValidationBackground background)
        {
            CurrentBackground = background;

            Color color = background switch
            {
                ValidationBackground.Black => Color.black,
                ValidationBackground.White => Color.white,
                ValidationBackground.DeepBlue => DeepBlue,
                _ => DeepBlue
            };

            if (_backgroundRenderer == null)
            {
                Debug.LogError("[ShipVisualValidationView] Missing background renderer.", this);
                return;
            }

            _backgroundRenderer.color = color;
        }

        public void SetBloomEnabled(bool enabled)
        {
            if (_bloomVolume == null)
            {
                Debug.LogWarning("[ShipVisualValidationView] Bloom toggle requested, but no Volume is assigned. Bloom validation is unavailable for this view.", this);
                return;
            }

            _bloomVolume.enabled = enabled;
        }

        private void ResetVisuals()
        {
            SetRendererSprite(_bodyRenderer, _normalBodySprite);
            SetRendererSprite(_shapeRenderer, _normalShapeSprite);
            SetRendererSprite(_coreRenderer, _defaultCoreSprite);
            SetRendererColor(_bodyRenderer, NormalColor);
            SetRendererColor(_shapeRenderer, NormalColor);
            SetRendererColor(_outlineRenderer, NormalColor);
            SetRendererColor(_coreRenderer, NormalColor);
            SetRendererColor(_weaponMountRenderer, NormalColor);
            SetRendererEnabled(_bodyRenderer, true);
            SetRendererEnabled(_shapeRenderer, false);
            SetRendererEnabled(_outlineRenderer, true);
            SetRendererEnabled(_coreRenderer, true);
            SetRendererEnabled(_weaponMountRenderer, true);
            SetRendererEnabled(_dashTrailRenderer, false);
            SetRendererEnabled(_dashParticlesRenderer, false);
            SetRendererEnabled(_weavingAuraRenderer, false);
            SetRendererEnabled(_weavingCoreRenderer, false);
        }

        private void SetMainBody(Sprite sprite, Color color)
        {
            SetRendererSprite(_bodyRenderer, sprite != null ? sprite : _normalBodySprite);
            SetRendererColor(_bodyRenderer, color);
        }

        private void ApplyInitialState()
        {
            SetBackground(_initialBackground);
            SetBloomEnabled(_initialBloomEnabled);
            SetState(_initialState);
        }

        private void CacheDefaultSprites()
        {
            if (_defaultCoreSprite == null && _coreRenderer != null)
            {
                _defaultCoreSprite = _coreRenderer.sprite;
            }
        }

        private void ValidateReferences()
        {
            ValidateRenderer(_bodyRenderer, nameof(_bodyRenderer));
            ValidateRenderer(_outlineRenderer, nameof(_outlineRenderer));
            ValidateRenderer(_coreRenderer, nameof(_coreRenderer));
            ValidateRenderer(_weaponMountRenderer, nameof(_weaponMountRenderer));
            ValidateRenderer(_backgroundRenderer, nameof(_backgroundRenderer));
            ValidateSprite(_normalBodySprite, nameof(_normalBodySprite));
            ValidateSprite(_normalShapeSprite, nameof(_normalShapeSprite));
            ValidateSprite(_leanLeftSprite, nameof(_leanLeftSprite));
            ValidateSprite(_leanRightSprite, nameof(_leanRightSprite));
            ValidateSprite(_dashSprite, nameof(_dashSprite));
        }

        private void ValidateRenderer(SpriteRenderer renderer, string fieldName)
        {
            if (renderer == null)
            {
                Debug.LogError($"[ShipVisualValidationView] Missing required renderer: {fieldName}.", this);
            }
        }

        private void ValidateSprite(Sprite sprite, string fieldName)
        {
            if (sprite == null)
            {
                Debug.LogError($"[ShipVisualValidationView] Missing required sprite: {fieldName}.", this);
            }
        }

        private static void SetRendererEnabled(SpriteRenderer renderer, bool enabled)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        private static void SetRendererSprite(SpriteRenderer renderer, Sprite sprite)
        {
            if (renderer != null && sprite != null)
            {
                renderer.sprite = sprite;
            }
        }

        private static void SetRendererColor(SpriteRenderer renderer, Color color)
        {
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
    }
}
