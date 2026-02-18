using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Save;
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
    [RequireComponent(typeof(AudioSource))]
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

        /// <summary>
        /// Global static event broadcast when any weapon fires.
        /// Enemies subscribe to this for auditory perception.
        /// Params: (firePosition, noiseRadius).
        /// </summary>
        public static event Action<Vector2, float> OnWeaponFired;

        /// <summary> Default noise radius for weapon fire (units). </summary>
        private const float DEFAULT_NOISE_RADIUS = 15f;

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
            ServiceLocator.Register<StarChartController>(this);

            _inputHandler = GetComponent<InputHandler>();
            _shipAiming = GetComponent<ShipAiming>();
            _shipMotor = GetComponent<ShipMotor>();

            // Dedicated AudioSource for firing SFX (must be pre-attached via RequireComponent)
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound

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
            OnTrackFired = null;
            OnLightSailChanged = null;
            OnSatellitesChanged = null;
            
            ServiceLocator.Unregister<StarChartController>(this);
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
                    // Uniform fan spread when multiple projectiles
                    Vector2 fireDir;
                    if (count > 1 && !Mathf.Approximately(coreSnap.Spread, 0f))
                    {
                        // Evenly distribute across [-Spread, +Spread]
                        float t = (float)p / (count - 1); // 0..1
                        float angle = Mathf.Lerp(-coreSnap.Spread, coreSnap.Spread, t);
                        fireDir = RotateVector2(direction, angle);
                    }
                    else if (!Mathf.Approximately(coreSnap.Spread, 0f))
                    {
                        // Single projectile with spread: random offset
                        float spreadAngle = UnityEngine.Random.Range(-coreSnap.Spread, coreSnap.Spread);
                        fireDir = RotateVector2(direction, spreadAngle);
                    }
                    else
                    {
                        fireDir = direction;
                    }

                    SpawnProjectile(track, coreSnap, fireDir, spawnPos);
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

            // Broadcast weapon fire for enemy auditory perception
            OnWeaponFired?.Invoke(spawnPos, DEFAULT_NOISE_RADIUS);
            CombatEvents.RaiseWeaponFired(spawnPos, DEFAULT_NOISE_RADIUS);
        }

        /// <summary>
        /// Dispatches projectile spawning based on CoreFamily.
        /// Direction already includes spread calculation from ExecuteFire.
        /// </summary>
        private void SpawnProjectile(WeaponTrack track, CoreSnapshot coreSnap,
                                     Vector2 direction, Vector3 spawnPos)
        {
            switch (coreSnap.Family)
            {
                case CoreFamily.Matter:
                    SpawnMatterProjectile(track, coreSnap, direction, spawnPos);
                    break;
                case CoreFamily.Light:
                    SpawnLightBeam(track, coreSnap, direction, spawnPos);
                    break;
                case CoreFamily.Echo:
                    SpawnEchoWave(track, coreSnap, direction, spawnPos);
                    break;
                case CoreFamily.Anomaly:
                    SpawnAnomalyEntity(track, coreSnap, direction, spawnPos);
                    break;
                default:
                    Debug.LogWarning($"[StarChartController] Unknown CoreFamily '{coreSnap.Family}', falling back to Matter.");
                    SpawnMatterProjectile(track, coreSnap, direction, spawnPos);
                    break;
            }
        }

        /// <summary> Matter family: physical rigidbody projectile. </summary>
        private void SpawnMatterProjectile(WeaponTrack track, CoreSnapshot coreSnap,
                                            Vector2 direction, Vector3 spawnPos)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            GameObject bulletObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var projectile = bulletObj.GetComponent<Projectile>();
            if (projectile == null) return;

            // Apply ProjectileSize scaling
            if (!Mathf.Approximately(coreSnap.ProjectileSize, 1f))
                bulletObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize;

            var parms = coreSnap.ToProjectileParams();
            _lightSailRunner?.ModifyProjectileParams(ref parms);

            // Runtime-instantiate independent modifier copies for this projectile
            var modifiers = InstantiateModifiers(bulletObj, coreSnap.TintModifierPrefabs);
            projectile.Initialize(direction, parms, modifiers);
        }

        /// <summary> Light family: instant raycast laser beam. </summary>
        private void SpawnLightBeam(WeaponTrack track, CoreSnapshot coreSnap,
                                     Vector2 direction, Vector3 spawnPos)
        {
            // Get LaserBeam prefab pool (uses the same ProjectilePrefab field, but expects LaserBeam component)
            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            GameObject beamObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var laserBeam = beamObj.GetComponent<LaserBeam>();
            if (laserBeam == null)
            {
                Debug.LogWarning("[StarChartController] Light core prefab missing LaserBeam component, falling back to Matter.");
                // Return the beam object and fall back
                var poolRef = beamObj.GetComponent<PoolReference>();
                poolRef?.ReturnToPool();
                SpawnMatterProjectile(track, coreSnap, direction, spawnPos);
                return;
            }

            var parms = coreSnap.ToProjectileParams();
            _lightSailRunner?.ModifyProjectileParams(ref parms);

            // Runtime-instantiate independent modifier copies for this beam
            var modifiers = InstantiateModifiers(beamObj, coreSnap.TintModifierPrefabs);
            laserBeam.Fire(spawnPos, direction, parms, modifiers);
        }

        /// <summary> Echo family: expanding shockwave AOE. </summary>
        private void SpawnEchoWave(WeaponTrack track, CoreSnapshot coreSnap,
                                    Vector2 direction, Vector3 spawnPos)
        {
            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            GameObject waveObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var echoWave = waveObj.GetComponent<EchoWave>();
            if (echoWave == null)
            {
                Debug.LogWarning("[StarChartController] Echo core prefab missing EchoWave component, falling back to Matter.");
                var poolRef = waveObj.GetComponent<PoolReference>();
                poolRef?.ReturnToPool();
                SpawnMatterProjectile(track, coreSnap, direction, spawnPos);
                return;
            }

            // Apply ProjectileSize scaling
            if (!Mathf.Approximately(coreSnap.ProjectileSize, 1f))
                waveObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize;

            var parms = coreSnap.ToProjectileParams();
            _lightSailRunner?.ModifyProjectileParams(ref parms);

            // Runtime-instantiate independent modifier copies for this wave
            var modifiers = InstantiateModifiers(waveObj, coreSnap.TintModifierPrefabs);
            echoWave.Fire(spawnPos, direction, parms, modifiers, coreSnap.Spread);
        }

        /// <summary> Anomaly family: custom behavior entity (e.g., boomerang). </summary>
        private void SpawnAnomalyEntity(WeaponTrack track, CoreSnapshot coreSnap,
                                         Vector2 direction, Vector3 spawnPos)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            GameObject bulletObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var projectile = bulletObj.GetComponent<Projectile>();
            if (projectile == null) return;

            // Apply ProjectileSize scaling
            if (!Mathf.Approximately(coreSnap.ProjectileSize, 1f))
                bulletObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize;

            var parms = coreSnap.ToProjectileParams();
            _lightSailRunner?.ModifyProjectileParams(ref parms);

            // Runtime-instantiate Tint modifier copies
            var modifiers = InstantiateModifiers(bulletObj, coreSnap.TintModifierPrefabs);

            // Also instantiate Anomaly-specific modifier (e.g. BoomerangModifier)
            if (coreSnap.AnomalyModifierPrefab != null)
            {
                var anomalyPrefabs = new List<GameObject>(1) { coreSnap.AnomalyModifierPrefab };
                var anomalyModifiers = InstantiateModifiers(bulletObj, anomalyPrefabs);
                modifiers.AddRange(anomalyModifiers);
            }

            projectile.Initialize(direction, parms, modifiers);
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
            if (ServiceLocator.Get<PoolManager>() == null && PoolManager.Instance == null)
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

        /// <summary>
        /// Instantiates independent IProjectileModifier copies on the target GameObject
        /// from a list of modifier prefabs. Uses AddComponent + JsonUtility.FromJsonOverwrite
        /// to create a deep copy of each modifier's serialized fields.
        /// </summary>
        /// <param name="targetObj">The projectile/beam/wave GameObject to attach modifiers to.</param>
        /// <param name="prefabs">List of modifier prefab GameObjects (each expected to have IProjectileModifier).</param>
        /// <returns>List of newly instantiated modifier instances (may be empty, never null).</returns>
        private static List<IProjectileModifier> InstantiateModifiers(GameObject targetObj, List<GameObject> prefabs)
        {
            var result = new List<IProjectileModifier>();
            if (prefabs == null || prefabs.Count == 0) return result;

            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] == null) continue;

                var srcModifiers = prefabs[i].GetComponents<IProjectileModifier>();
                for (int m = 0; m < srcModifiers.Length; m++)
                {
                    var srcComponent = srcModifiers[m] as MonoBehaviour;
                    if (srcComponent == null) continue;

                    // AddComponent of the same type, then copy serialized field values
                    var newComponent = targetObj.AddComponent(srcComponent.GetType()) as IProjectileModifier;
                    if (newComponent != null)
                    {
                        var json = JsonUtility.ToJson(srcComponent);
                        JsonUtility.FromJsonOverwrite(json, newComponent as MonoBehaviour);
                        result.Add(newComponent);
                    }
                }
            }

            return result;
        }

        // ══════════════════════════════════════════════════════════════
        // Save / Load Serialization
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Export the current Star Chart loadout to a serializable data object.
        /// </summary>
        public StarChartSaveData ExportToSaveData()
        {
            var data = new StarChartSaveData();

            // Primary track
            data.PrimaryTrack = ExportTrack(_primaryTrack);
            data.SecondaryTrack = ExportTrack(_secondaryTrack);

            // Light Sail
            data.LightSailID = _equippedLightSailSO != null ? _equippedLightSailSO.DisplayName : "";

            // Satellites
            data.SatelliteIDs = new List<string>();
            for (int i = 0; i < _equippedSatelliteSOs.Count; i++)
            {
                if (_equippedSatelliteSOs[i] != null)
                    data.SatelliteIDs.Add(_equippedSatelliteSOs[i].DisplayName);
            }

            return data;
        }

        /// <summary>
        /// Import a Star Chart loadout from saved data, using a resolver to look up items by name.
        /// </summary>
        public void ImportFromSaveData(StarChartSaveData data, IStarChartItemResolver resolver)
        {
            if (data == null || resolver == null) return;

            // Clear current loadout
            _primaryTrack.ClearAll();
            _secondaryTrack.ClearAll();
            UnequipLightSail();
            for (int i = _satelliteRunners.Count - 1; i >= 0; i--)
            {
                _satelliteRunners[i].Dispose();
                _satelliteRunners.RemoveAt(i);
            }
            _equippedSatelliteSOs.Clear();

            // Import tracks
            ImportTrack(_primaryTrack, data.PrimaryTrack, resolver);
            ImportTrack(_secondaryTrack, data.SecondaryTrack, resolver);

            // Import Light Sail
            if (!string.IsNullOrEmpty(data.LightSailID))
            {
                var sail = resolver.FindLightSail(data.LightSailID);
                if (sail != null) EquipLightSail(sail);
            }

            // Import Satellites
            if (data.SatelliteIDs != null)
            {
                for (int i = 0; i < data.SatelliteIDs.Count; i++)
                {
                    var sat = resolver.FindSatellite(data.SatelliteIDs[i]);
                    if (sat != null) EquipSatellite(sat);
                }
            }

            // Re-initialize pools for new loadout
            InitializeAllPools();
        }

        private static TrackSaveData ExportTrack(WeaponTrack track)
        {
            var data = new TrackSaveData();

            var cores = track.CoreLayer.Items;
            for (int i = 0; i < cores.Count; i++)
            {
                if (cores[i] != null)
                    data.CoreIDs.Add(cores[i].DisplayName);
            }

            var prisms = track.PrismLayer.Items;
            for (int i = 0; i < prisms.Count; i++)
            {
                if (prisms[i] != null)
                    data.PrismIDs.Add(prisms[i].DisplayName);
            }

            return data;
        }

        private static void ImportTrack(WeaponTrack track, TrackSaveData data,
                                         IStarChartItemResolver resolver)
        {
            if (data == null) return;

            if (data.CoreIDs != null)
            {
                for (int i = 0; i < data.CoreIDs.Count; i++)
                {
                    var core = resolver.FindCore(data.CoreIDs[i]);
                    if (core != null) track.EquipCore(core);
                }
            }

            if (data.PrismIDs != null)
            {
                for (int i = 0; i < data.PrismIDs.Count; i++)
                {
                    var prism = resolver.FindPrism(data.PrismIDs[i]);
                    if (prism != null) track.EquipPrism(prism);
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
