using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Handles all Dash-state visual feedback on the ship's sprite layers:
    ///   - i-frame flicker on Solid + HL + Core layers
    ///   - Dodge_Sprite static ghost at dash origin (detach → fade → re-parent)
    ///
    /// Driven by ShipView via OnDashStarted() / OnDashEnded().
    /// Does NOT subscribe to events directly — ShipView routes state changes.
    /// </summary>
    public class ShipDashVisuals : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // Serialized References (wired by ShipPrefabRebuilder)
        // ══════════════════════════════════════════════════════════════

        [Header("Sprite Layers")]
        [SerializeField] private SpriteRenderer _solidRenderer;
        [SerializeField] private SpriteRenderer _hlRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;

        [Header("Dodge Ghost")]
        [SerializeField] private SpriteRenderer _dodgeSprite;

        [Header("After-Image Trail")]
        [Tooltip("DashAfterImageSpawner — spawns pooled after-image ghosts along the dash path.")]
        [SerializeField] private DashAfterImageSpawner _afterImageSpawner;

        [Header("Settings")]
        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        [Header("Enable Toggles")]
        [Tooltip("Master switch — when OFF, all dash visuals are silently skipped.")]
        [SerializeField] private bool _enableAll = true;

        [Tooltip("i-frame flicker on Solid + HL + Core layers during dash.")]
        [SerializeField] private bool _enableIFrameFlicker = true;

        [Tooltip("Dodge_Sprite static ghost at dash origin (detach → fade → re-parent).")]
        [SerializeField] private bool _enableDodgeGhost = true;

        [Tooltip("Pooled after-image ghosts along the dash path.")]
        [SerializeField] private bool _enableAfterImages = true;

        // ══════════════════════════════════════════════════════════════
        // Runtime State
        // ══════════════════════════════════════════════════════════════

        private Color _solidBaseColor;
        private Color _hlBaseColor;
        private Color _coreBaseColor;

        private Transform _dodgeSpriteOriginalParent;

        // i-frame flicker
        private CancellationTokenSource _iFrameCts;

        // Dodge ghost fade
        private Tween _dodgeFadeTween;

        // ══════════════════════════════════════════════════════════════
        // Initialization
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called by ShipView after Awake to pass baseline colors.
        /// </summary>
        public void Initialize(Color solidBase, Color hlBase, Color coreBase)
        {
            _solidBaseColor = solidBase;
            _hlBaseColor = hlBase;
            _coreBaseColor = coreBase;

            // Cache Dodge_Sprite parent for re-attachment
            if (_dodgeSprite != null)
            {
                _dodgeSpriteOriginalParent = _dodgeSprite.transform.parent;
                var dc = _dodgeSprite.color;
                dc.a = 0f;
                _dodgeSprite.color = dc;
                _dodgeSprite.gameObject.SetActive(false);

                if (_dodgeSprite.sprite == null)
                {
                    Debug.LogError("[ShipDashVisuals] Dodge_Sprite has no sprite assigned. Fix the prefab wiring.", _dodgeSprite);
                }
            }
        }

        /// <summary>
        /// Sync baseline colors when another system modifies them.
        /// </summary>
        public void SyncBaselineColors(Color solidBase, Color hlBase, Color coreBase)
        {
            _solidBaseColor = solidBase;
            _hlBaseColor = hlBase;
            _coreBaseColor = coreBase;
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — called by ShipView
        // ══════════════════════════════════════════════════════════════

        public void OnDashStarted(Vector2 direction)
        {
            if (!_enableAll || _juiceSettings == null) return;

            // 1. i-frame flicker on Solid + HL + Core layers
            if (_enableIFrameFlicker)
            {
                CancelIFrameFlicker();
                _iFrameCts = new CancellationTokenSource();
                RunIFrameFlickerAsync(_iFrameCts.Token).Forget();
            }

            // 2. Dodge_Sprite static ghost at dash origin
            if (_enableDodgeGhost)
                SpawnDodgeSprite();

            // 3. Pooled after-image trail along dash path
            if (_enableAfterImages && _afterImageSpawner != null)
                _afterImageSpawner.TriggerSpawn();
        }

        public void OnDashEnded()
        {
            // Stop i-frame flicker and restore all affected layers
            CancelIFrameFlicker();
            if (_solidRenderer != null) _solidRenderer.color = _solidBaseColor;
            if (_hlRenderer != null) _hlRenderer.color = _hlBaseColor;
            if (_coreRenderer != null) _coreRenderer.color = _coreBaseColor;
        }

        public void ResetState()
        {
            CancelIFrameFlicker();

            _dodgeFadeTween.Stop();

            if (_solidRenderer != null) _solidRenderer.color = _solidBaseColor;
            if (_hlRenderer != null) _hlRenderer.color = _hlBaseColor;
            if (_coreRenderer != null) _coreRenderer.color = _coreBaseColor;

            ResetDodgeSprite();

            // Cancel any in-progress after-image spawn sequence
            if (_afterImageSpawner != null)
                _afterImageSpawner.CancelSpawning();
        }

        // ══════════════════════════════════════════════════════════════
        // Dodge_Sprite — static ghost at dash origin
        // ══════════════════════════════════════════════════════════════

        private void SpawnDodgeSprite()
        {
            if (_dodgeSprite == null) return;

            if (_dodgeSprite.sprite == null)
            {
                Debug.LogError("[ShipDashVisuals] Dodge_Sprite sprite is missing during dash. Fix the prefab wiring.", _dodgeSprite);
                return;
            }

            _dodgeSprite.transform.SetParent(null, worldPositionStays: true);
            _dodgeSprite.transform.position = transform.position;
            _dodgeSprite.transform.rotation = transform.rotation;

            Color teal = _juiceSettings != null
                ? _juiceSettings.AfterImageColor
                : new Color(0.28f, 0.43f, 0.43f, 1f);
            teal.a = 1f;
            _dodgeSprite.color = teal;
            _dodgeSprite.gameObject.SetActive(true);

            _dodgeFadeTween.Stop();
            float fadeDuration = _juiceSettings != null ? _juiceSettings.AfterImageFadeDuration : 0.15f;
            _dodgeFadeTween = Tween.Alpha(
                _dodgeSprite,
                endValue: 0f,
                duration: fadeDuration,
                ease: Ease.Linear)
                .OnComplete(ResetDodgeSprite);
        }

        private void ResetDodgeSprite()
        {
            if (_dodgeSprite == null) return;

            _dodgeFadeTween.Stop();

            if (_dodgeSpriteOriginalParent != null)
                _dodgeSprite.transform.SetParent(_dodgeSpriteOriginalParent, worldPositionStays: false);

            var c = _dodgeSprite.color;
            c.a = 0f;
            _dodgeSprite.color = c;
            _dodgeSprite.gameObject.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════
        // i-frame Flicker
        // ══════════════════════════════════════════════════════════════

        private async UniTaskVoid RunIFrameFlickerAsync(CancellationToken ct)
        {
            if (_juiceSettings == null) return;

            int intervalMs = Mathf.Max(1, Mathf.RoundToInt(_juiceSettings.IFrameFlashInterval * 1000f));
            float dimAlpha = _juiceSettings.IFrameFlashAlpha;
            bool bright = false;

            while (!ct.IsCancellationRequested)
            {
                if (_solidRenderer != null)
                {
                    Color c = _solidBaseColor;
                    c.a = bright ? _solidBaseColor.a : dimAlpha;
                    _solidRenderer.color = c;
                }

                if (_hlRenderer != null)
                {
                    Color c = _hlBaseColor;
                    c.a = bright ? _hlBaseColor.a : dimAlpha * _hlBaseColor.a;
                    _hlRenderer.color = c;
                }

                if (_coreRenderer != null)
                {
                    Color c = _coreBaseColor;
                    c.a = bright ? _coreBaseColor.a : dimAlpha * _coreBaseColor.a;
                    _coreRenderer.color = c;
                }

                bright = !bright;

                await UniTask.Delay(intervalMs, cancellationToken: ct).SuppressCancellationThrow();
                if (ct.IsCancellationRequested) break;
            }
        }

        private void CancelIFrameFlicker()
        {
            if (_iFrameCts == null) return;
            _iFrameCts.Cancel();
            _iFrameCts.Dispose();
            _iFrameCts = null;
        }
    }
}
