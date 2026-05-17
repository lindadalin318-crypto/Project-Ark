using ProjectArk.Combat;
using ProjectArk.Combat.HyperWind;
using ProjectArk.Core;
using ProjectArk.Ship;
using UnityEngine;



namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Test-scene director for HyperWind slice D'. Provides pooled player/enemy projectile traffic so wind drift and ground cyclone capture can be playtested without requiring the full StarChart scene stack.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HyperWindArenaTestDirector : MonoBehaviour
    {
        [Header("Arena Readability")]
        [SerializeField] private Rect _arenaBounds = new Rect(-12f, -7f, 24f, 14f);
        [SerializeField] private Rect _cycloneLane = new Rect(-5f, -5f, 10f, 10f);
        [SerializeField] private bool _drawGizmos = true;

        [Header("Player Test Fire")]
        [SerializeField] private bool _enablePlayerTestFire = true;
        [SerializeField] private Projectile _playerProjectilePrefab;
        [SerializeField] private Transform _playerFireOrigin;
        [SerializeField] [Min(0.03f)] private float _playerFireInterval = 0.16f;
        [SerializeField] [Min(0f)] private float _playerMuzzleOffset = 0.65f;
        [SerializeField] [Min(0.1f)] private float _playerProjectileSpeed = 11f;
        [SerializeField] [Min(0f)] private float _playerProjectileDamage = 10f;
        [SerializeField] [Min(0.1f)] private float _playerProjectileLifetime = 4.5f;
        [SerializeField] [Min(0f)] private float _playerProjectileKnockback = 2f;
        [SerializeField] private Color _playerTrailColor = new Color(0.35f, 0.95f, 1f, 1f);

        [Header("Auto Smoke Fire")]
        [SerializeField] private bool _enableAutoSmokeFire = true;
        [SerializeField] [Min(0f)] private float _autoSmokeStartDelay = 1f;
        [SerializeField] [Min(0.03f)] private float _autoSmokeFireInterval = 0.12f;
        [SerializeField] [Min(0f)] private float _autoSmokeDuration = 7f;
        [SerializeField] private Vector2[] _autoSmokeOrigins =
        {
            new Vector2(-3f, -6f),
            new Vector2(0f, -6f),
            new Vector2(3f, -6f)
        };
        [SerializeField] private Vector2 _autoSmokeDirection = Vector2.up;
        [SerializeField] private bool _logSmokeSummary = true;

        [Header("Enemy Bullet Backlash")]

        [SerializeField] private bool _enableEnemyVolley = true;
        [SerializeField] private EnemyProjectile _enemyProjectilePrefab;
        [SerializeField] private Transform _enemyTarget;
        [SerializeField] private Vector2[] _enemyEmitterPositions =
        {
            new Vector2(-8f, 4.5f),
            new Vector2(8f, 4.5f),
            new Vector2(-8f, -4.5f),
            new Vector2(8f, -4.5f)
        };
        [SerializeField] [Min(0.1f)] private float _enemyFireInterval = 1.25f;
        [SerializeField] [Min(0.1f)] private float _enemyProjectileSpeed = 7.5f;
        [SerializeField] [Min(0f)] private float _enemyProjectileDamage = 8f;
        [SerializeField] [Min(0.1f)] private float _enemyProjectileLifetime = 5.5f;
        [SerializeField] [Min(0f)] private float _enemyProjectileKnockback = 1.5f;

        [Header("Pooling")]
        [SerializeField] [Min(1)] private int _playerPoolInitialSize = 40;
        [SerializeField] [Min(1)] private int _playerPoolMaxSize = 120;
        [SerializeField] [Min(1)] private int _enemyPoolInitialSize = 24;
        [SerializeField] [Min(1)] private int _enemyPoolMaxSize = 80;
        [SerializeField] private bool _createPoolManagerIfMissing = false;

        private InputHandler _input;
        private GameObjectPool _playerProjectilePool;
        private GameObjectPool _enemyProjectilePool;
        private float _playerFireTimer;
        private float _enemyFireTimer;
        private float _autoSmokeElapsed;
        private float _autoSmokeFireTimer;
        private int _nextEnemyEmitterIndex;
        private int _nextAutoSmokeOriginIndex;
        private int _playerProjectilesFired;
        private int _enemyProjectilesFired;
        private int _cycloneCapturedProjectiles;
        private int _cycloneReleasedProjectiles;
        private bool _autoSmokeSummaryLogged;
        private bool _missingPlayerSetupWarned;

        private bool _missingEnemySetupWarned;
        private bool _missingPoolWarned;

        public float AutoSmokeElapsed => _autoSmokeElapsed;
        public int PlayerProjectilesFired => _playerProjectilesFired;
        public int EnemyProjectilesFired => _enemyProjectilesFired;
        public int CycloneCapturedProjectiles => _cycloneCapturedProjectiles;
        public int CycloneReleasedProjectiles => _cycloneReleasedProjectiles;
        public bool AutoSmokeSummaryLogged => _autoSmokeSummaryLogged;

        private void OnEnable()

        {
            GroundCyclone.OnAnyProjectileCaptured += HandleCycloneProjectileCaptured;
            GroundCyclone.OnAnyProjectileReleased += HandleCycloneProjectileReleased;
        }

        private void OnDisable()
        {
            GroundCyclone.OnAnyProjectileCaptured -= HandleCycloneProjectileCaptured;
            GroundCyclone.OnAnyProjectileReleased -= HandleCycloneProjectileReleased;
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _input);
            ResolvePools();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            TickAutoSmokeFire(deltaTime);
            TickPlayerTestFire(deltaTime);
            TickEnemyVolley(deltaTime);
        }


        private void ResolvePools()
        {
            PoolManager poolManager = PoolManager.Instance;
            if (poolManager == null && _createPoolManagerIfMissing)
            {
                var poolManagerObject = new GameObject("HyperWind_TestPoolManager");
                poolManager = poolManagerObject.AddComponent<PoolManager>();
                Debug.LogWarning("[HyperWindArenaTestDirector] Created a runtime PoolManager for the test arena. Prefer a scene PoolManager before this slice graduates from lab status.", this);
            }

            if (poolManager == null)
            {
                if (!_missingPoolWarned)
                {
                    Debug.LogError("[HyperWindArenaTestDirector] PoolManager is missing. Add one to the test scene or enable Create Pool Manager If Missing.", this);
                    _missingPoolWarned = true;
                }

                return;
            }

            if (_playerProjectilePrefab != null)
            {
                _playerProjectilePool = poolManager.GetPool(_playerProjectilePrefab.gameObject, _playerPoolInitialSize, _playerPoolMaxSize);
            }

            if (_enemyProjectilePrefab != null)
            {
                _enemyProjectilePool = poolManager.GetPool(_enemyProjectilePrefab.gameObject, _enemyPoolInitialSize, _enemyPoolMaxSize);
            }
        }

        private void TickAutoSmokeFire(float deltaTime)
        {
            if (!_enableAutoSmokeFire || _autoSmokeDuration <= 0f)
            {
                return;
            }

            _autoSmokeElapsed += deltaTime;
            if (_autoSmokeElapsed < _autoSmokeStartDelay)
            {
                return;
            }

            float activeElapsed = _autoSmokeElapsed - _autoSmokeStartDelay;
            if (activeElapsed > _autoSmokeDuration)
            {
                LogAutoSmokeSummaryOnce();
                return;
            }

            _autoSmokeFireTimer -= deltaTime;
            if (_autoSmokeFireTimer > 0f)
            {
                return;
            }

            if (_playerProjectilePrefab == null || _playerProjectilePool == null || _autoSmokeOrigins == null || _autoSmokeOrigins.Length == 0)
            {
                WarnMissingPlayerSetup("Auto smoke fire cannot run because player projectile prefab, pool, or smoke origins are missing.");
                return;
            }

            Vector2 direction = _autoSmokeDirection.sqrMagnitude > 0.0001f ? _autoSmokeDirection.normalized : Vector2.up;
            Vector2 origin = _autoSmokeOrigins[_nextAutoSmokeOriginIndex % _autoSmokeOrigins.Length];
            _nextAutoSmokeOriginIndex++;
            FirePlayerProjectile(origin, direction);
            _autoSmokeFireTimer = _autoSmokeFireInterval;
        }

        private void TickPlayerTestFire(float deltaTime)

        {
            if (!_enablePlayerTestFire)
            {
                return;
            }

            _playerFireTimer -= deltaTime;
            if (_input == null && !ServiceLocator.TryGet(out _input))
            {
                WarnMissingPlayerSetup("InputHandler is unavailable, so manual test fire cannot run.");
                return;
            }

            if (!_input.IsFireHeld || _playerFireTimer > 0f)
            {
                return;
            }

            Transform origin = ResolvePlayerFireOrigin();
            if (origin == null || _playerProjectilePrefab == null || _playerProjectilePool == null)
            {
                WarnMissingPlayerSetup("Player projectile prefab, fire origin, or pool is missing.");
                return;
            }

            Vector2 direction = ResolvePlayerAimDirection();
            FirePlayerProjectile(origin.position, direction);
            _playerFireTimer = _playerFireInterval;
        }

        private void TickEnemyVolley(float deltaTime)
        {
            if (!_enableEnemyVolley)
            {
                return;
            }

            _enemyFireTimer -= deltaTime;
            if (_enemyFireTimer > 0f)
            {
                return;
            }

            if (_enemyProjectilePrefab == null || _enemyProjectilePool == null || _enemyEmitterPositions == null || _enemyEmitterPositions.Length == 0)
            {
                WarnMissingEnemySetup("Enemy projectile prefab, pool, or emitter positions are missing.");
                return;
            }

            Transform target = ResolveEnemyTarget();
            if (target == null)
            {
                WarnMissingEnemySetup("Enemy target is missing.");
                return;
            }

            Vector2 origin = _enemyEmitterPositions[_nextEnemyEmitterIndex % _enemyEmitterPositions.Length];
            _nextEnemyEmitterIndex++;
            Vector2 direction = ((Vector2)target.position - origin).normalized;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.left;
            }

            FireEnemyProjectile(origin, direction);
            _enemyFireTimer = _enemyFireInterval;
        }

        private Transform ResolvePlayerFireOrigin()
        {
            if (_playerFireOrigin != null)
            {
                return _playerFireOrigin;
            }

            return _input != null ? _input.transform : null;
        }

        private Transform ResolveEnemyTarget()
        {
            if (_enemyTarget != null)
            {
                return _enemyTarget;
            }

            return ResolvePlayerFireOrigin();
        }

        private Vector2 ResolvePlayerAimDirection()
        {
            if (_input != null && _input.AimDirection.sqrMagnitude > 0.0001f)
            {
                return _input.AimDirection.normalized;
            }

            return Vector2.up;
        }

        private void FirePlayerProjectile(Vector2 origin, Vector2 direction)
        {
            Vector2 spawnPosition = origin + direction * _playerMuzzleOffset;
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            GameObject instance = _playerProjectilePool.Get(spawnPosition, rotation);
            Projectile projectile = instance.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogError("[HyperWindArenaTestDirector] Player projectile prefab lacks Projectile component.", instance);
                _playerProjectilePool.Return(instance);
                return;
            }

            var parameters = new ProjectileParams(
                _playerProjectileDamage,
                _playerProjectileSpeed,
                _playerProjectileLifetime,
                _playerProjectileKnockback,
                impactVFXPrefab: null,
                DamageType.Physical,
                trailTime: 0.18f,
                trailWidth: 0.09f,
                trailColor: _playerTrailColor);

            projectile.Initialize(direction, parameters);
            _playerProjectilesFired++;
        }

        private void FireEnemyProjectile(Vector2 origin, Vector2 direction)

        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            GameObject instance = _enemyProjectilePool.Get(origin, rotation);
            EnemyProjectile projectile = instance.GetComponent<EnemyProjectile>();
            if (projectile == null)
            {
                Debug.LogError("[HyperWindArenaTestDirector] Enemy projectile prefab lacks EnemyProjectile component.", instance);
                _enemyProjectilePool.Return(instance);
                return;
            }

            projectile.Initialize(direction, _enemyProjectileSpeed, _enemyProjectileDamage, _enemyProjectileKnockback, _enemyProjectileLifetime);
            _enemyProjectilesFired++;
        }

        private void HandleCycloneProjectileCaptured(GroundCyclone cyclone, ICycloneCaptureTarget target)
        {
            _cycloneCapturedProjectiles++;
        }

        private void HandleCycloneProjectileReleased(GroundCyclone cyclone, ICycloneCaptureTarget target)
        {
            _cycloneReleasedProjectiles++;
        }

        private void LogAutoSmokeSummaryOnce()
        {
            if (_autoSmokeSummaryLogged || !_logSmokeSummary)
            {
                return;
            }

            _autoSmokeSummaryLogged = true;
            Debug.Log(
                $"[HyperWindArenaTestDirector] Auto smoke summary: playerFired={_playerProjectilesFired}, " +
                $"enemyFired={_enemyProjectilesFired}, cycloneCaptured={_cycloneCapturedProjectiles}, " +
                $"cycloneReleased={_cycloneReleasedProjectiles}.",
                this);
        }

        private void WarnMissingPlayerSetup(string reason)

        {
            if (_missingPlayerSetupWarned)
            {
                return;
            }

            Debug.LogWarning($"[HyperWindArenaTestDirector] {reason}", this);
            _missingPlayerSetupWarned = true;
        }

        private void WarnMissingEnemySetup(string reason)
        {
            if (_missingEnemySetupWarned)
            {
                return;
            }

            Debug.LogWarning($"[HyperWindArenaTestDirector] {reason}", this);
            _missingEnemySetupWarned = true;
        }

        private void OnValidate()
        {
            _playerFireInterval = Mathf.Max(0.03f, _playerFireInterval);
            _autoSmokeStartDelay = Mathf.Max(0f, _autoSmokeStartDelay);
            _autoSmokeFireInterval = Mathf.Max(0.03f, _autoSmokeFireInterval);
            _autoSmokeDuration = Mathf.Max(0f, _autoSmokeDuration);
            _autoSmokeDirection = _autoSmokeDirection.sqrMagnitude > 0.0001f ? _autoSmokeDirection.normalized : Vector2.up;
            _playerMuzzleOffset = Mathf.Max(0f, _playerMuzzleOffset);
            _playerProjectileSpeed = Mathf.Max(0.1f, _playerProjectileSpeed);

            _playerProjectileDamage = Mathf.Max(0f, _playerProjectileDamage);
            _playerProjectileLifetime = Mathf.Max(0.1f, _playerProjectileLifetime);
            _playerProjectileKnockback = Mathf.Max(0f, _playerProjectileKnockback);
            _enemyFireInterval = Mathf.Max(0.1f, _enemyFireInterval);
            _enemyProjectileSpeed = Mathf.Max(0.1f, _enemyProjectileSpeed);
            _enemyProjectileDamage = Mathf.Max(0f, _enemyProjectileDamage);
            _enemyProjectileLifetime = Mathf.Max(0.1f, _enemyProjectileLifetime);
            _enemyProjectileKnockback = Mathf.Max(0f, _enemyProjectileKnockback);
            _playerPoolInitialSize = Mathf.Max(1, _playerPoolInitialSize);
            _playerPoolMaxSize = Mathf.Max(_playerPoolInitialSize, _playerPoolMaxSize);
            _enemyPoolInitialSize = Mathf.Max(1, _enemyPoolInitialSize);
            _enemyPoolMaxSize = Mathf.Max(_enemyPoolInitialSize, _enemyPoolMaxSize);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos)
            {
                return;
            }

            DrawRect(_arenaBounds, new Color(0.25f, 0.7f, 1f, 0.85f));
            DrawRect(_cycloneLane, new Color(0.8f, 0.35f, 1f, 0.85f));

            if (_enemyEmitterPositions == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.9f);
            for (int i = 0; i < _enemyEmitterPositions.Length; i++)
            {
                Gizmos.DrawWireSphere(_enemyEmitterPositions[i], 0.35f);
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Vector3 center = new Vector3(rect.center.x, rect.center.y, 0f);
            Vector3 size = new Vector3(rect.width, rect.height, 0f);
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
