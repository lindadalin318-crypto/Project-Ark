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

        // Multi-Loadout: 3 independent slots, each with its own tracks + sail + satellites
        private LoadoutSlot[] _loadouts;
        private int _activeLoadoutIndex;

        // Per-slot runtime runners (parallel arrays, indexed by loadout slot)
        private LightSailRunner[] _lightSailRunners;
        private List<SatelliteRunner>[] _satelliteRunners;

        private InputHandler _inputHandler;
        private ShipAiming _shipAiming;
        private ShipMotor _shipMotor;
        private AudioSource _audioSource;

        private StarChartContext _context;

        // --- Convenience accessors for active slot ---

        private LoadoutSlot ActiveSlot => _loadouts[_activeLoadoutIndex];

        public WeaponTrack PrimaryTrack   => ActiveSlot.PrimaryTrack;
        public WeaponTrack SecondaryTrack => ActiveSlot.SecondaryTrack;

        /// <summary> Current active loadout index (0-based). </summary>
        public int ActiveLoadoutIndex => _activeLoadoutIndex;

        // --- Runtime Equip API (for UI) ---

        /// <summary> Get the currently equipped Light Sail SO, or null. </summary>
        public LightSailSO GetEquippedLightSail() => ActiveSlot.EquippedLightSailSO;

        /// <summary> Get the currently equipped Satellite SOs. </summary>
        public IReadOnlyList<SatelliteSO> GetEquippedSatellites() => ActiveSlot.EquippedSatelliteSOs;

        /// <summary>
        /// Equip a Light Sail at runtime. Disposes the previous one if any.
        /// </summary>
        public void EquipLightSail(LightSailSO sail)
        {
            if (sail == null) return;

            // 卸载旧光帆
            UnequipLightSail();

            ActiveSlot.EquippedLightSailSO = sail;
            _lightSailRunners[_activeLoadoutIndex] = new LightSailRunner(sail, _context);
            OnLightSailChanged?.Invoke();
        }

        /// <summary> Unequip the current Light Sail. </summary>
        public void UnequipLightSail()
        {
            ref var runner = ref _lightSailRunners[_activeLoadoutIndex];
            if (runner != null)
            {
                runner.Dispose();
                runner = null;
            }
            ActiveSlot.EquippedLightSailSO = null;
            OnLightSailChanged?.Invoke();
        }

        /// <summary>
        /// Equip a Satellite at runtime. Creates a new SatelliteRunner.
        /// </summary>
        public void EquipSatellite(SatelliteSO sat)
        {
            if (sat == null) return;

            var runner = new SatelliteRunner(sat, _context);
            _satelliteRunners[_activeLoadoutIndex].Add(runner);
            ActiveSlot.EquippedSatelliteSOs.Add(sat);
            OnSatellitesChanged?.Invoke();
        }

        /// <summary>
        /// Unequip a specific Satellite at runtime.
        /// </summary>
        public void UnequipSatellite(SatelliteSO sat)
        {
            if (sat == null) return;

            var runners = _satelliteRunners[_activeLoadoutIndex];
            for (int i = runners.Count - 1; i >= 0; i--)
            {
                if (runners[i].Data == sat)
                {
                    runners[i].Dispose();
                    runners.RemoveAt(i);
                    break;
                }
            }

            ActiveSlot.EquippedSatelliteSOs.Remove(sat);
            OnSatellitesChanged?.Invoke();
        }

        /// <summary>
        /// Switch to a different loadout slot. Disposes old slot's runners and
        /// re-initializes pools for the new slot's tracks.
        /// </summary>
        public void SwitchLoadout(int index)
        {
            if (index < 0 || index >= _loadouts.Length) return;
            if (index == _activeLoadoutIndex) return;

            // Dispose runners of the current slot
            DisposeSlotRunners(_activeLoadoutIndex);

            _activeLoadoutIndex = index;

            // Re-create runners for the new slot
            RebuildSlotRunners(_activeLoadoutIndex);

            // Ensure object pools are ready for the new slot's tracks
            InitializeAllPools();

            OnLightSailChanged?.Invoke();
            OnSatellitesChanged?.Invoke();
        }

        /// <summary> Dispose all runners for a given slot index. </summary>
        private void DisposeSlotRunners(int slotIndex)
        {
            ref var sailRunner = ref _lightSailRunners[slotIndex];
            if (sailRunner != null)
            {
                sailRunner.Dispose();
                sailRunner = null;
            }

            var satRunners = _satelliteRunners[slotIndex];
            for (int i = satRunners.Count - 1; i >= 0; i--)
                satRunners[i].Dispose();
            satRunners.Clear();
        }

        /// <summary>
        /// Re-create LightSailRunner and SatelliteRunners for a slot
        /// based on its current equipped SO data.
        /// </summary>
        private void RebuildSlotRunners(int slotIndex)
        {
            var slot = _loadouts[slotIndex];

            // Light Sail
            if (slot.EquippedLightSailSO != null)
                _lightSailRunners[slotIndex] = new LightSailRunner(slot.EquippedLightSailSO, _context);

            // Satellites
            var satRunners = _satelliteRunners[slotIndex];
            for (int i = 0; i < slot.EquippedSatelliteSOs.Count; i++)
            {
                if (slot.EquippedSatelliteSOs[i] != null)
                    satRunners.Add(new SatelliteRunner(slot.EquippedSatelliteSOs[i], _context));
            }
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

            // Initialize 3 independent loadout slots
            const int SlotCount = 3;
            _loadouts = new LoadoutSlot[SlotCount];
            _lightSailRunners = new LightSailRunner[SlotCount];
            _satelliteRunners = new List<SatelliteRunner>[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                _loadouts[i] = new LoadoutSlot();
                _satelliteRunners[i] = new List<SatelliteRunner>();
            }
            _activeLoadoutIndex = 0;

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
            // Guard: tracks must be initialized (Awake may have failed if components missing)
            if (_loadouts == null) return;

            float dt = Time.deltaTime;
            ActiveSlot.PrimaryTrack.Tick(dt);
            ActiveSlot.SecondaryTrack.Tick(dt);

            // 光帆和伴星 tick（在武器 Tick 之后、开火检查之前）
            _lightSailRunners[_activeLoadoutIndex]?.Tick(dt);
            var activeSatRunners = _satelliteRunners[_activeLoadoutIndex];
            for (int i = 0; i < activeSatRunners.Count; i++)
                activeSatRunners[i]?.Tick(dt);

            // Guard: inputHandler must be valid
            if (_inputHandler == null) return;

            // 主武器：左键
            if (_inputHandler.IsFireHeld && CanFireTrack(ActiveSlot.PrimaryTrack))
                ExecuteFire(ActiveSlot.PrimaryTrack);

            // 副武器：右键
            if (_inputHandler.IsSecondaryFireHeld && CanFireTrack(ActiveSlot.SecondaryTrack))
                ExecuteFire(ActiveSlot.SecondaryTrack);
        }

        private void OnDestroy()
        {
            OnTrackFired = null;
            OnLightSailChanged = null;
            OnSatellitesChanged = null;

            ServiceLocator.Unregister<StarChartController>(this);

            // Dispose all runners across all slots
            if (_lightSailRunners != null)
            {
                for (int i = 0; i < _lightSailRunners.Length; i++)
                    _lightSailRunners[i]?.Dispose();
            }
            if (_satelliteRunners != null)
            {
                for (int i = 0; i < _satelliteRunners.Length; i++)
                {
                    if (_satelliteRunners[i] == null) continue;
                    for (int j = 0; j < _satelliteRunners[i].Count; j++)
                        _satelliteRunners[i][j].Dispose();
                    _satelliteRunners[i].Clear();
                }
            }
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
            _lightSailRunners[_activeLoadoutIndex]?.ModifyProjectileParams(ref parms);

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
            _lightSailRunners[_activeLoadoutIndex]?.ModifyProjectileParams(ref parms);

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
            _lightSailRunners[_activeLoadoutIndex]?.ModifyProjectileParams(ref parms);

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
            _lightSailRunners[_activeLoadoutIndex]?.ModifyProjectileParams(ref parms);

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
            // Debug loadout is always loaded into slot 0 only (requirement 2.6)
            var slot0 = _loadouts[0];

            if (_debugPrimaryCores != null)
            {
                for (int i = 0; i < _debugPrimaryCores.Length; i++)
                {
                    if (_debugPrimaryCores[i] != null)
                        slot0.PrimaryTrack.EquipCore(_debugPrimaryCores[i]);
                }
            }

            if (_debugPrimaryPrisms != null)
            {
                for (int i = 0; i < _debugPrimaryPrisms.Length; i++)
                {
                    if (_debugPrimaryPrisms[i] != null)
                        slot0.PrimaryTrack.EquipPrism(_debugPrimaryPrisms[i]);
                }
            }

            if (_debugSecondaryCores != null)
            {
                for (int i = 0; i < _debugSecondaryCores.Length; i++)
                {
                    if (_debugSecondaryCores[i] != null)
                        slot0.SecondaryTrack.EquipCore(_debugSecondaryCores[i]);
                }
            }

            if (_debugSecondaryPrisms != null)
            {
                for (int i = 0; i < _debugSecondaryPrisms.Length; i++)
                {
                    if (_debugSecondaryPrisms[i] != null)
                        slot0.SecondaryTrack.EquipPrism(_debugSecondaryPrisms[i]);
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

            ActiveSlot.PrimaryTrack.InitializePools();
            ActiveSlot.SecondaryTrack.InitializePools();
        }

        private void InitializeSailAndSatellites()
        {
            // Debug sail/satellites are loaded into slot 0 only
            var slot0 = _loadouts[0];

            // 光帆
            if (_debugLightSail != null)
            {
                slot0.EquippedLightSailSO = _debugLightSail;
                _lightSailRunners[0] = new LightSailRunner(_debugLightSail, _context);
            }

            // 伴星
            if (_debugSatellites != null)
            {
                for (int i = 0; i < _debugSatellites.Length; i++)
                {
                    if (_debugSatellites[i] != null)
                    {
                        var runner = new SatelliteRunner(_debugSatellites[i], _context);
                        _satelliteRunners[0].Add(runner);
                        slot0.EquippedSatelliteSOs.Add(_debugSatellites[i]);
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
        /// Serializes all 3 loadout slots.
        /// </summary>
        public StarChartSaveData ExportToSaveData()
        {
            var data = new StarChartSaveData();
            data.Loadouts = new List<LoadoutSlotSaveData>();

            for (int i = 0; i < _loadouts.Length; i++)
            {
                var slot = _loadouts[i];
                var slotData = new LoadoutSlotSaveData();
                slotData.PrimaryTrack   = ExportTrack(slot.PrimaryTrack);
                slotData.SecondaryTrack = ExportTrack(slot.SecondaryTrack);
                slotData.LightSailID    = slot.EquippedLightSailSO != null ? slot.EquippedLightSailSO.DisplayName : "";
                slotData.SatelliteIDs   = new List<string>();
                for (int j = 0; j < slot.EquippedSatelliteSOs.Count; j++)
                {
                    if (slot.EquippedSatelliteSOs[j] != null)
                        slotData.SatelliteIDs.Add(slot.EquippedSatelliteSOs[j].DisplayName);
                }
                data.Loadouts.Add(slotData);
            }

            return data;
        }

        /// <summary>
        /// Import a Star Chart loadout from saved data, using a resolver to look up items by name.
        /// Supports both new multi-slot format and legacy single-slot format (auto-migration).
        /// </summary>
        public void ImportFromSaveData(StarChartSaveData data, IStarChartItemResolver resolver)
        {
            if (data == null || resolver == null) return;

            // Dispose all existing runners across all slots
            for (int i = 0; i < _loadouts.Length; i++)
                DisposeSlotRunners(i);

            // Clear all slot data
            for (int i = 0; i < _loadouts.Length; i++)
                _loadouts[i].Clear();

            List<LoadoutSlotSaveData> slotDataList;

#pragma warning disable CS0618 // Obsolete field access for migration
            if (data.Loadouts != null && data.Loadouts.Count > 0)
            {
                // New format: use Loadouts list directly
                slotDataList = data.Loadouts;
            }
            else
            {
                // Legacy format: migrate single-slot data to slot 0
                slotDataList = new List<LoadoutSlotSaveData>
                {
                    new LoadoutSlotSaveData
                    {
                        PrimaryTrack   = data.PrimaryTrack,
                        SecondaryTrack = data.SecondaryTrack,
                        LightSailID    = data.LightSailID,
                        SatelliteIDs   = data.SatelliteIDs ?? new List<string>()
                    }
                };
            }
#pragma warning restore CS0618

            // Import each slot (pad with empty slots if list is shorter than 3)
            for (int i = 0; i < _loadouts.Length; i++)
            {
                if (i >= slotDataList.Count) break; // remaining slots stay empty

                var slotData = slotDataList[i];
                if (slotData == null) continue;

                var slot = _loadouts[i];
                ImportTrack(slot.PrimaryTrack,   slotData.PrimaryTrack,   resolver);
                ImportTrack(slot.SecondaryTrack, slotData.SecondaryTrack, resolver);

                if (!string.IsNullOrEmpty(slotData.LightSailID))
                {
                    var sail = resolver.FindLightSail(slotData.LightSailID);
                    if (sail != null)
                    {
                        slot.EquippedLightSailSO = sail;
                        _lightSailRunners[i] = new LightSailRunner(sail, _context);
                    }
                }

                if (slotData.SatelliteIDs != null)
                {
                    for (int j = 0; j < slotData.SatelliteIDs.Count; j++)
                    {
                        var sat = resolver.FindSatellite(slotData.SatelliteIDs[j]);
                        if (sat != null)
                        {
                            slot.EquippedSatelliteSOs.Add(sat);
                            _satelliteRunners[i].Add(new SatelliteRunner(sat, _context));
                        }
                    }
                }
            }

            // Re-initialize pools for the active loadout
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

            // Persist unlocked column counts for progressive capacity system
            data.CoreLayerCols  = track.CoreLayer.Cols;
            data.PrismLayerCols = track.PrismLayer.Cols;

            return data;
        }

        private static void ImportTrack(WeaponTrack track, TrackSaveData data,
                                         IStarChartItemResolver resolver)
        {
            if (data == null) return;

            // Restore unlocked column counts (clamp to ≥1 for old saves where field defaults to 0)
            int coreCols  = Mathf.Max(1, data.CoreLayerCols);
            int prismCols = Mathf.Max(1, data.PrismLayerCols);
            track.SetLayerCols(coreCols, prismCols);

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
