using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Replica-only sprite pack switcher for the isolated Galactic Glitch ship experiment.
    /// Consumes GGReplicaShipVisualProfileSO and never writes live Ship/VFX assets.
    /// </summary>
    public class GGReplicaShipViewAdapter : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private GGReplicaShipVisualProfileSO _profile;

        [Header("Sprite Layers")]
        [SerializeField] private SpriteRenderer _backRenderer;
        [SerializeField] private SpriteRenderer _liquidRenderer;
        [SerializeField] private SpriteRenderer _highlightRenderer;
        [SerializeField] private SpriteRenderer _solidRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private SpriteRenderer _dodgeGhostRenderer;

        [Header("State Sources")]
        [SerializeField] private ShipStateController _stateController;
        [SerializeField] private ShipBoost _boost;
        [SerializeField] private ShipDash _dash;

        private GGReplicaVisualState _currentState = GGReplicaVisualState.Normal;
        private bool _isFiring;

        public void SetFiring(bool isFiring)
        {
            _isFiring = isFiring;
            RefreshState();
        }

        public void ForceVisualState(GGReplicaVisualState state)
        {
            ApplyPack(state, force: true);
        }

        private void Awake()
        {
            if (_stateController == null) _stateController = GetComponent<ShipStateController>();
            if (_boost == null) _boost = GetComponent<ShipBoost>();
            if (_dash == null) _dash = GetComponent<ShipDash>();

            if (_profile == null)
            {
                Debug.LogError("[GGReplicaShipViewAdapter] Missing visual profile.", this);
            }
        }

        private void OnEnable()
        {
            if (_stateController != null) _stateController.OnStateChanged += HandleStateChanged;
            if (_dash != null) _dash.OnDashStarted += HandleDashStarted;
            if (_dash != null) _dash.OnDashEnded += HandleDashEnded;
            if (_boost != null) _boost.OnBoostStarted += HandleBoostChanged;
            if (_boost != null) _boost.OnBoostEnded += HandleBoostChanged;

            ApplyPersistentSprites();
            RefreshState();
        }

        private void OnDisable()
        {
            if (_stateController != null) _stateController.OnStateChanged -= HandleStateChanged;
            if (_dash != null) _dash.OnDashStarted -= HandleDashStarted;
            if (_dash != null) _dash.OnDashEnded -= HandleDashEnded;
            if (_boost != null) _boost.OnBoostStarted -= HandleBoostChanged;
            if (_boost != null) _boost.OnBoostEnded -= HandleBoostChanged;
        }

        private void HandleStateChanged(ShipShipState previous, ShipShipState current) => RefreshState();
        private void HandleDashStarted(Vector2 direction) => RefreshState();
        private void HandleDashEnded() => RefreshState();
        private void HandleBoostChanged() => RefreshState();

        private void ApplyPersistentSprites()
        {
            if (_profile == null) return;

            if (_backRenderer != null) _backRenderer.sprite = _profile.BackSprite;
            if (_coreRenderer != null) _coreRenderer.sprite = _profile.CoreSprite;
            if (_dodgeGhostRenderer != null)
            {
                _dodgeGhostRenderer.sprite = _profile.DodgeGhostSprite;
                _dodgeGhostRenderer.enabled = false;
            }
        }

        private void RefreshState()
        {
            if (_profile == null) return;
            ApplyPack(ResolveState(), force: false);
        }

        private GGReplicaVisualState ResolveState()
        {
            bool dashing = (_dash != null && _dash.IsDashing) ||
                           (_stateController != null && _stateController.IsInState(ShipShipState.Dash));
            bool boosting = (_boost != null && _boost.IsBoosting) ||
                            (_stateController != null && _stateController.IsInState(ShipShipState.Boost));

            if (dashing) return GGReplicaVisualState.Dodge;
            if (_isFiring && boosting) return GGReplicaVisualState.FireBoost;
            if (boosting) return GGReplicaVisualState.Boost;
            if (_isFiring) return GGReplicaVisualState.Fire;
            return GGReplicaVisualState.Normal;
        }

        private void ApplyPack(GGReplicaVisualState state, bool force)
        {
            if (!force && _currentState == state) return;
            if (_profile == null) return;
            if (!_profile.TryGetPack(state, out var pack))
            {
                Debug.LogWarning($"[GGReplicaShipViewAdapter] Missing sprite pack for {state}.", this);
                return;
            }

            _currentState = state;
            if (_solidRenderer != null) _solidRenderer.sprite = pack.SolidSprite;
            if (_liquidRenderer != null) _liquidRenderer.sprite = pack.LiquidSprite;
            if (_highlightRenderer != null) _highlightRenderer.sprite = pack.HighlightSprite;

            if (_solidRenderer != null) _solidRenderer.transform.localPosition = pack.SpritesOffset;
            if (_liquidRenderer != null) _liquidRenderer.transform.localPosition = pack.SpritesOffset;
            if (_highlightRenderer != null) _highlightRenderer.transform.localPosition = pack.SpritesOffset;

            if (_dodgeGhostRenderer != null)
            {
                _dodgeGhostRenderer.enabled = state == GGReplicaVisualState.Dodge;
            }
        }
    }
}
