using Cysharp.Threading.Tasks;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Spawns dash after-image ghosts during a dash.
    /// Listens to ShipDash events and spawns from the object pool.
    /// </summary>
    [RequireComponent(typeof(ShipDash))]
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

        private const int POOL_INITIAL_SIZE = 5;
        private const int POOL_MAX_SIZE = 20;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _dash = GetComponent<ShipDash>();

            if (_afterImagePrefab == null)
            {
                Debug.LogWarning("[DashAfterImageSpawner] No after-image prefab assigned. Disabling.");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            // Pre-warm the pool
            var poolManager = PoolManager.Instance;
            if (poolManager != null)
                _pool = poolManager.GetPool(_afterImagePrefab, POOL_INITIAL_SIZE, POOL_MAX_SIZE);
        }

        private void OnEnable()
        {
            if (_dash != null) _dash.OnDashStarted += OnDashStarted;
        }

        private void OnDisable()
        {
            if (_dash != null) _dash.OnDashStarted -= OnDashStarted;
        }

        // ══════════════════════════════════════════════════════════════
        // Dash → Spawn After-Images
        // ══════════════════════════════════════════════════════════════

        private void OnDashStarted(Vector2 direction)
        {
            if (_pool == null || _juiceSettings == null || _stats == null) return;
            if (_shipSpriteRenderer == null) return;

            SpawnAfterImagesAsync().Forget();
        }

        private async UniTaskVoid SpawnAfterImagesAsync()
        {
            int count = _juiceSettings.DashAfterImageCount;
            if (count <= 0) return;

            float dashDuration = _stats.DashDuration;
            float interval = dashDuration / count;
            int intervalMs = Mathf.Max(1, Mathf.RoundToInt(interval * 1000f));

            Sprite currentSprite = _shipSpriteRenderer.sprite;
            Color baseColor = _shipSpriteRenderer.color;

            for (int i = 0; i < count; i++)
            {
                if (!_dash.IsDashing) break;

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
                    await UniTask.Delay(intervalMs, cancellationToken: destroyCancellationToken);
                }
            }
        }
    }
}
