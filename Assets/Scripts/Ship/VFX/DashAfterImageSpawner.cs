using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Spawns dash after-image ghosts during a dash.
    /// Driven by ShipDashVisuals (Worker pattern, second-level delegation).
    ///
    /// Does NOT subscribe to events directly — ShipDashVisuals calls TriggerSpawn()
    /// when ShipView routes a Dash-started signal.
    /// </summary>
    public class DashAfterImageSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Prefab with DashAfterImage + SpriteRenderer. Will be pooled.")]
        [SerializeField] private GameObject _afterImagePrefab;

        [Tooltip("The ship's main SpriteRenderer to copy sprite from.")]
        [SerializeField] private SpriteRenderer _shipSpriteRenderer;

        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;
        [SerializeField] private ShipStatsSO _stats;

        // ══════════════════════════════════════════════════════════════
        // Cached
        // ══════════════════════════════════════════════════════════════

        private ShipDash _dash;
        private GameObjectPool _pool;
        private CancellationTokenSource _spawnCts;

        private const int POOL_INITIAL_SIZE = 5;
        private const int POOL_MAX_SIZE = 20;

        // ══════════════════════════════════════════════════════════════
        // Initialization (called by ShipDashVisuals or ShipView)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called during setup to inject the ShipDash reference needed for IsDashing check.
        /// </summary>
        public void Initialize(ShipDash dash)
        {
            _dash = dash;
        }

        private void Start()
        {
            if (_afterImagePrefab == null)
            {
                Debug.LogError("[DashAfterImageSpawner] No after-image prefab assigned. After-images will not work.", this);
                return;
            }

            // Pre-warm the pool
            var poolManager = PoolManager.Instance;
            if (poolManager != null)
                _pool = poolManager.GetPool(_afterImagePrefab, POOL_INITIAL_SIZE, POOL_MAX_SIZE);
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — called by ShipDashVisuals
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Triggers the after-image spawn sequence. Call when Dash starts.
        /// </summary>
        public void TriggerSpawn()
        {
            if (_pool == null || _juiceSettings == null || _stats == null) return;
            if (_shipSpriteRenderer == null) return;

            CancelSpawning();
            _spawnCts = new CancellationTokenSource();
            SpawnAfterImagesAsync(_spawnCts.Token).Forget();
        }

        /// <summary>
        /// Cancels any in-progress spawn sequence.
        /// Called by ShipDashVisuals.ResetState() for clean pool return.
        /// </summary>
        public void CancelSpawning()
        {
            if (_spawnCts != null)
            {
                _spawnCts.Cancel();
                _spawnCts.Dispose();
                _spawnCts = null;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Spawn Sequence
        // ══════════════════════════════════════════════════════════════

        private async UniTaskVoid SpawnAfterImagesAsync(CancellationToken ct)
        {
            int count = _juiceSettings.DashAfterImageCount;
            if (count <= 0) return;

            float dashDuration = _stats.DashDuration;
            float interval = dashDuration / count;
            int intervalMs = Mathf.Max(1, Mathf.RoundToInt(interval * 1000f));

            Sprite currentSprite = _shipSpriteRenderer.sprite;
            // Use the configured tint color (defaults to GG cyan-green rgba(0.28,0.43,0.43))
            // rather than sampling the live sprite color, so the ghost always has the correct hue.
            Color baseColor = _juiceSettings.AfterImageColor;

            for (int i = 0; i < count; i++)
            {
                if (ct.IsCancellationRequested) break;
                if (_dash != null && !_dash.IsDashing) break;

                // Spawn after-image at current position
                var instance = _pool.Get(transform.position, transform.rotation);
                var afterImage = instance.GetComponent<DashAfterImage>();

                if (afterImage != null)
                {
                    afterImage.Initialize(
                        currentSprite,
                        baseColor,
                        _juiceSettings.AfterImageAlpha,
                        _juiceSettings.AfterImageFadeDuration);
                }

                // Wait before spawning next
                if (i < count - 1)
                {
                    await UniTask.Delay(intervalMs, cancellationToken: ct).SuppressCancellationThrow();
                    if (ct.IsCancellationRequested) break;
                }
            }
        }
    }
}
