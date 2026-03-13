using UnityEngine;
using PrimeTween;

namespace ProjectArk.Level
{
    /// <summary>
    /// Hidden area mask (Minishoot "HiddenArea" equivalent).
    /// 
    /// Covers a secret area with a visual overlay (e.g., wall tiles, foliage, darkness).
    /// When the player enters the trigger zone, the overlay fades out to reveal the hidden content.
    /// When the player exits, the overlay fades back in (unless permanently revealed).
    /// 
    /// Attach to a GameObject with:
    /// - A BoxCollider2D (trigger) defining the entry zone
    /// - A SpriteRenderer (or child SpriteRenderers) for the visual overlay
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HiddenAreaMask : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Visual")]
        [Tooltip("Sprite renderers to fade. If empty, uses all SpriteRenderers on this GameObject and children.")]
        [SerializeField] private SpriteRenderer[] _maskSprites;

        [Header("Fade")]
        [Tooltip("Duration (seconds) for the reveal/hide fade animation.")]
        [SerializeField] private float _fadeDuration = 0.4f;

        [Tooltip("Target alpha when revealed (0 = fully transparent, good for secrets).")]
        [SerializeField] private float _revealedAlpha;

        [Tooltip("Alpha when hidden (1 = fully opaque).")]
        [SerializeField] private float _hiddenAlpha = 1f;

        [Header("Behavior")]
        [Tooltip("If true, once revealed the mask stays transparent permanently (within the session).")]
        [SerializeField] private bool _permanentReveal;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        // ──────────────────── Runtime State ────────────────────

        private bool _playerInZone;
        private bool _permanentlyRevealed;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            // Auto-collect sprite renderers if not assigned
            if (_maskSprites == null || _maskSprites.Length == 0)
            {
                _maskSprites = GetComponentsInChildren<SpriteRenderer>(true);
            }

            // Validate trigger collider
            var boxCollider = GetComponent<BoxCollider2D>();
            if (!boxCollider.isTrigger)
            {
                boxCollider.isTrigger = true;
                Debug.LogWarning($"[HiddenAreaMask] {gameObject.name}: BoxCollider2D was not set as trigger. Auto-fixed.");
            }

            // Start fully hidden
            SetAlphaImmediate(_hiddenAlpha);
        }

        // ──────────────────── Player Detection ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            if (_playerInZone) return;
            if (_permanentlyRevealed) return;

            _playerInZone = true;
            FadeToAlpha(_revealedAlpha);

            if (_permanentReveal)
            {
                _permanentlyRevealed = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            if (!_playerInZone) return;

            _playerInZone = false;

            if (!_permanentlyRevealed)
            {
                FadeToAlpha(_hiddenAlpha);
            }
        }

        private bool IsPlayerLayer(GameObject obj)
        {
            return (_playerLayer.value & (1 << obj.layer)) != 0;
        }

        // ──────────────────── Fade Logic ────────────────────

        private void FadeToAlpha(float targetAlpha)
        {
            if (_maskSprites == null) return;

            foreach (var sprite in _maskSprites)
            {
                if (sprite == null) continue;

                Color current = sprite.color;
                float startAlpha = current.a;

                // Capture for closure
                var s = sprite;
                _ = Tween.Custom(startAlpha, targetAlpha, _fadeDuration,
                    onValueChange: v =>
                    {
                        if (s != null)
                        {
                            Color c = s.color;
                            c.a = v;
                            s.color = c;
                        }
                    },
                    ease: Ease.InOutSine);
            }
        }

        private void SetAlphaImmediate(float alpha)
        {
            if (_maskSprites == null) return;

            foreach (var sprite in _maskSprites)
            {
                if (sprite == null) continue;
                Color c = sprite.color;
                c.a = alpha;
                sprite.color = c;
            }
        }
    }
}
