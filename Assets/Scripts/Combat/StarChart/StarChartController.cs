using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Heat;
using ProjectArk.Ship;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Top-level orchestrator for the Star Chart (The Loom) system.
    /// Manages two weapon tracks (Primary/Secondary), Light Sail, Satellites,
    /// and the full firing pipeline (snapshot → spawn → recoil → heat → feedback).
    /// Attach to the Ship root GameObject.
    /// </summary>
    [RequireComponent(typeof(InputHandler))]
    [RequireComponent(typeof(ShipAiming))]
    [RequireComponent(typeof(ShipMotor))]
    public class StarChartController : MonoBehaviour
    {
        [Header("Fire Points")]
        [SerializeField] private FirePoint _firePoint;

        [Header("Heat")]
        [Tooltip("Optional: shared heat resource for both tracks. Leave empty for unlimited firing.")]
        [SerializeField] private HeatSystem _heatSystem;

        [Header("Debug / Initial Loadout — Primary Track")]
        [Tooltip("Star Cores to pre-equip in the primary track (for testing without UI)")]
        [SerializeField] private StarCoreSO[] _debugPrimaryCores;

        [Tooltip("Prisms to pre-equip in the primary track")]
        [SerializeField] private PrismSO[] _debugPrimaryPrisms;

        [Header("Debug / Initial Loadout — Secondary Track")]
        [Tooltip("Star Cores to pre-equip in the secondary track")]
        [SerializeField] private StarCoreSO[] _debugSecondaryCores;

        [Tooltip("Prisms to pre-equip in the secondary track")]
        [SerializeField] private PrismSO[] _debugSecondaryPrisms;

        [Header("Debug / Initial Loadout — Light Sail")]
        [Tooltip("Light Sail to pre-equip (for testing without UI)")]
        [SerializeField] private LightSailSO _debugLightSail;

        [Header("Debug / Initial Loadout — Satellites")]
        [Tooltip("Satellites to pre-equip (for testing without UI)")]
        [SerializeField] private SatelliteSO[] _debugSatellites;

        // --- Events ---

        /// <summary> Fired after any track fires. Param: which track. </summary>
        public event Action<WeaponTrack.TrackId> OnTrackFired;

        /// <summary> Fired when the equipped Light Sail changes. </summary>
        public event Action OnLightSailChanged;

        /// <summary> Fired when the equipped Satellites list changes. </summary>
        public event Action OnSatellitesChanged;

        // --- Runtime state ---

        private WeaponTrack _primaryTrack;
        private WeaponTrack _secondaryTrack;

        private InputHandler _inputHandler;
        private ShipAiming _shipAiming;
        private ShipMotor _shipMotor;
        private AudioSource _audioSource;

        private StarChartContext _context;
        private LightSailRunner _lightSailRunner;
        private readonly List<SatelliteRunner> _satelliteRunners = new();

        // --- Public access ---

        public WeaponTrack PrimaryTrack => _primaryTrack;
        public WeaponTrack SecondaryTrack => _secondaryTrack;

        // 运行时装备状态（供 UI 读取）
        private LightSailSO _equippedLightSailSO;
        private readonly List<SatelliteSO> _equippedSatelliteSOs = new();

        // --- Runtime Equip API (for UI) ---

        /// <summary> Get the currently equipped Light Sail SO, or null. </summary>
        public LightSailSO GetEquippedLightSail() => _equippedLightSailSO;

        /// <summary> Get the currently equipped Satellite SOs. </summary>
        public IReadOnlyList<SatelliteSO> GetEquippedSatellites() => _equippedSatelliteSOs;

        /// <summary>
        /// Equip a Light Sail at runtime. Disposes the previous one if any.
        /// </summary>
        public void EquipLightSail(LightSailSO sail)
        {
            if (sail == null) return;

            // 卸载旧光帆
            UnequipLightSail();

            _equippedLightSailSO = sail;
            _lightSailRunner = new LightSailRunner(sail, _context);
            OnLightSailChanged?.Invoke();
        }

        /// <summary> Unequip the current Light Sail. </summary>
        public void UnequipLightSail()
        {
            if (_lightSailRunner != null)
            {
                _lightSailRunner.Dispose();
                _lightSailRunner = null;
            }
            _equippedLightSailSO = null;
            OnLightSailChanged?.Invoke();
        }

        /// <summary>
        /// Equip a Satellite at runtime. Creates a new SatelliteRunner.
        /// </summary>
        public void EquipSatellite(SatelliteSO sat)
        {
            if (sat == null) return;

            var runner = new SatelliteRunner(sat, _context);
            _satelliteRunners.Add(runner);
            _equippedSatelliteSOs.Add(sat);
            OnSatellitesChanged?.Invoke();
        }

        /// <summary>
        /// Unequip a specific Satellite at runtime.
        /// </summary>
        public void UnequipSatellite(SatelliteSO sat)
        {
            if (sat == null) return;

            for (int i = _satelliteRunners.Count - 1; i >= 0; i--)
            {
                if (_satelliteRunners[i].Data == sat)
                {
                    _satelliteRunners[i].Dispose();
                    _satelliteRunners.RemoveAt(i);
                    break;
                }
            }

            _equippedSatelliteSOs.Remove(sat);
            OnSatellitesChanged?.Invoke();
        }

        private void Awake()
        {
            _inputHandler = GetComponent<InputHandler>();
            _shipAiming = GetComponent<ShipAiming>();
            _shipMotor = GetComponent<ShipMotor>();

            // 专用 AudioSource，避免干扰其他音频
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D 音效

            _primaryTrack = new WeaponTrack(WeaponTrack.TrackId.Primary);
            _secondaryTrack = new WeaponTrack(WeaponTrack.TrackId.Secondary);

            _context = new StarChartContext(
                _shipMotor, _shipAiming, _inputHandler,
                _heatSystem, this, transform);
        }

        private void Start()
        {
            LoadDebugLoadout();
            InitializeAllPools();
            InitializeSailAndSatellites();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _primaryTrack.Tick(dt);
            _secondaryTrack.Tick(dt);

            // 光帆和伴星 tick（在武器 Tick 之后、开火检查之前）
            _lightSailRunner?.Tick(dt);
            for (int i = 0; i < _satelliteRunners.Count; i++)
                _satelliteRunners[i].Tick(dt);

            // 主武器：左键
            if (_inputHandler.IsFireHeld && CanFireTrack(_primaryTrack))
                ExecuteFire(_primaryTrack);

            // 副武器：右键
            if (_inputHandler.IsSecondaryFireHeld && CanFireTrack(_secondaryTrack))
                ExecuteFire(_secondaryTrack);
        }

        private void OnDestroy()
        {
            _lightSailRunner?.Dispose();
            for (int i = 0; i < _satelliteRunners.Count; i++)
                _satelliteRunners[i].Dispose();
            _satelliteRunners.Clear();
        }

        private bool CanFireTrack(WeaponTrack track)
        {
            return track.CanFire && (_heatSystem == null || _heatSystem.CanFire());
        }

        private void ExecuteFire(WeaponTrack track)
        {
            var snapshot = track.TryFire();
            if (snapshot == null) return;

            Vector2 direction = _shipAiming.FacingDirection;
            Vector3 spawnPos = _firePoint != null ? _firePoint.Position : transform.position;

            // 对每个核心快照，生成对应的投射物
            for (int i = 0; i < snapshot.CoreSnapshots.Count; i++)
            {
                var coreSnap = snapshot.CoreSnapshots[i];
                int count = coreSnap.ProjectileCount;

                for (int p = 0; p < count; p++)
                {
                    SpawnProjectile(track, coreSnap, direction, spawnPos);
                }

                // 炮口焰（每核心一次）
                SpawnMuzzleFlash(track, coreSnap, direction, spawnPos);

                // 音效（每核心一次）
                PlayFireSound(coreSnap);
            }

            // 后坐力（合计）
            if (snapshot.TotalRecoilForce > 0f)
                _shipMotor.ApplyImpulse(-direction * snapshot.TotalRecoilForce);

            // 热量（合计）
            _heatSystem?.AddHeat(snapshot.TotalHeatCost);

            // 事件
            OnTrackFired?.Invoke(track.Id);
        }

        private void SpawnProjectile(WeaponTrack track, CoreSnapshot coreSnap,
                                     Vector2 baseDirection, Vector3 spawnPos)
        {
            Vector2 dir = baseDirection;

            // 散布
            if (!Mathf.Approximately(coreSnap.Spread, 0f))
            {
                float spreadAngle = UnityEngine.Random.Range(-coreSnap.Spread, coreSnap.Spread);
                dir = RotateVector2(dir, spreadAngle);
            }

            // 子弹朝向角度（sprite 朝上 +Y，所以 -90°）
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            GameObject bulletObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var projectile = bulletObj.GetComponent<Projectile>();
            if (projectile == null) return;

            var parms = coreSnap.ToProjectileParams();

            // 光帆 buff 修正（零分配，ref 传递 readonly struct）
            _lightSailRunner?.ModifyProjectileParams(ref parms);

            projectile.Initialize(dir, parms, coreSnap.Modifiers);
        }

        private void SpawnMuzzleFlash(WeaponTrack track, CoreSnapshot coreSnap,
                                      Vector2 direction, Vector3 spawnPos)
        {
            if (coreSnap.MuzzleFlashPrefab == null) return;

            var pool = track.GetMuzzleFlashPool(coreSnap.MuzzleFlashPrefab);
            if (pool == null) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
        }

        private void PlayFireSound(CoreSnapshot coreSnap)
        {
            if (coreSnap.FireSound == null) return;

            float pitch = 1f + UnityEngine.Random.Range(
                -coreSnap.FireSoundPitchVariance,
                coreSnap.FireSoundPitchVariance);
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(coreSnap.FireSound);
        }

        private void LoadDebugLoadout()
        {
            if (_debugPrimaryCores != null)
            {
                for (int i = 0; i < _debugPrimaryCores.Length; i++)
                {
                    if (_debugPrimaryCores[i] != null)
                        _primaryTrack.EquipCore(_debugPrimaryCores[i]);
                }
            }

            if (_debugPrimaryPrisms != null)
            {
                for (int i = 0; i < _debugPrimaryPrisms.Length; i++)
                {
                    if (_debugPrimaryPrisms[i] != null)
                        _primaryTrack.EquipPrism(_debugPrimaryPrisms[i]);
                }
            }

            if (_debugSecondaryCores != null)
            {
                for (int i = 0; i < _debugSecondaryCores.Length; i++)
                {
                    if (_debugSecondaryCores[i] != null)
                        _secondaryTrack.EquipCore(_debugSecondaryCores[i]);
                }
            }

            if (_debugSecondaryPrisms != null)
            {
                for (int i = 0; i < _debugSecondaryPrisms.Length; i++)
                {
                    if (_debugSecondaryPrisms[i] != null)
                        _secondaryTrack.EquipPrism(_debugSecondaryPrisms[i]);
                }
            }
        }

        private void InitializeAllPools()
        {
            if (PoolManager.Instance == null)
            {
                Debug.LogError("[StarChartController] PoolManager not found. Weapons disabled.");
                enabled = false;
                return;
            }

            _primaryTrack.InitializePools();
            _secondaryTrack.InitializePools();
        }

        private void InitializeSailAndSatellites()
        {
            // 光帆
            if (_debugLightSail != null)
            {
                _equippedLightSailSO = _debugLightSail;
                _lightSailRunner = new LightSailRunner(_debugLightSail, _context);
            }

            // 伴星
            if (_debugSatellites != null)
            {
                for (int i = 0; i < _debugSatellites.Length; i++)
                {
                    if (_debugSatellites[i] != null)
                    {
                        var runner = new SatelliteRunner(_debugSatellites[i], _context);
                        _satelliteRunners.Add(runner);
                        _equippedSatelliteSOs.Add(_debugSatellites[i]);
                    }
                }
            }
        }

        private static Vector2 RotateVector2(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
