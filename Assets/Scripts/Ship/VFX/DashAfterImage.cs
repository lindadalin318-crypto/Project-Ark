using UnityEngine;
using PrimeTween;
using ProjectArk.Core;

namespace ProjectArk.Ship
{
    /// <summary>
    /// A single dash after-image ghost. Receives sprite data, fades out,
    /// then returns itself to the object pool.
    /// Attach to a pooled prefab with a SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DashAfterImage : MonoBehaviour, IPoolable
    {
        private SpriteRenderer _spriteRenderer;
        private PoolReference _poolRef;
        private Tween _fadeTween;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _poolRef = GetComponent<PoolReference>();
        }

        /// <summary>
        /// Initializes the after-image with the ship's current visual state.
        /// Call immediately after retrieving from pool.
        /// </summary>
        public void Initialize(Sprite sprite, Color baseColor, float startAlpha, float fadeDuration)
        {
            _spriteRenderer.sprite = sprite;
            Color c = baseColor;
            c.a = startAlpha;
            _spriteRenderer.color = c;

            // Fade out then return to pool
            _fadeTween = Tween.Custom(this, startAlpha, 0f, fadeDuration,
                (target, val) =>
                {
                    if (target._spriteRenderer != null)
                    {
                        Color col = target._spriteRenderer.color;
                        col.a = val;
                        target._spriteRenderer.color = col;
                    }
                },
                Ease.Linear)
                .OnComplete(this, target => target.ReturnToPool());
        }

        private void ReturnToPool()
        {
            if (_poolRef != null)
                _poolRef.ReturnToPool();
        }

        // ══════════════════════════════════════════════════════════════
        // IPoolable — Object Pool Callbacks
        // ══════════════════════════════════════════════════════════════

        public void OnGetFromPool()
        {
            // State reset happens in Initialize()
        }

        public void OnReturnToPool()
        {
            // Stop any running tween
            if (_fadeTween.isAlive)
                _fadeTween.Stop();

            // Reset visual state
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = null;
                Color c = _spriteRenderer.color;
                c.a = 0f;
                _spriteRenderer.color = c;
            }
        }
    }
}
