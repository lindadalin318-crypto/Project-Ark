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

        // --- Events (forwarded from LoadoutManager for UI consumers) ---

        /// <summary> Fired after any track fires. Param: which track. </summary>
        public event Action<WeaponTrack.TrackId> OnTrackFired;

        /// <summary> Fired when the equipped Light Sail changes. </summary>
        public event Action OnLightSailChanged;

        /// <summary> Fired when the equipped Satellites list changes. </summary>
        public event Action OnSatellitesChanged;

        /// <summary> Default noise radius for weapon fire (units). </summary>
        private const float DEFAULT_NOISE_RADIUS = 15f;

        // --- Runtime components ---

        private InputHandler _inputHandler;
        private ShipAiming _shipAiming;
        private ShipMotor _shipMotor;
        private AudioSource _audioSource;

        private StarChartContext _context;

        // --- Extracted collaborators (L3-1) ---
        // Phase A: save/load serialization extracted into a pure C# helper.
        // Phase B: per-projectile spawn pipeline + CoreFamily switch + MuzzleFlash + FireSound
        //         extracted into ProjectileSpawner (single owner of CoreFamily switch).
        // Phase C: loadout state (3 slots + runner arrays + active index) + equip/unequip/
        //         switch/tick/dispose extracted into LoadoutManager (single owner of
        //         loadout state arrays). Controller now delegates all loadout API
        //         to _loadoutManager and forwards its events to public subscribers.
        private LoadoutManager _loadoutManager;
        private StarChartSaveSerializer _saveSerializer;
        private ProjectileSpawner _projectileSpawner;

        // ══════════════════════════════════════════════════════════════
        // Public API — delegated to LoadoutManager
        // ══════════════════════════════════════════════════════════════

        public WeaponTrack PrimaryTrack   => _loadoutManager.PrimaryTrack;
        public WeaponTrack SecondaryTrack => _loadoutManager.SecondaryTrack;

        /// <summary> Current active loadout index (0-based). </summary>
        public int ActiveLoadoutIndex => _loadoutManager.ActiveLoadoutIndex;

        /// <summary> The SAIL slot layer for the active loadout. </summary>
        public SlotLayer<LightSailSO> SailLayer => _loadoutManager.SailLayer;

        /// <summary> Get the currently equipped Light Sail SO, or null. </summary>
        public LightSailSO GetEquippedLightSail() => _loadoutManager.GetEquippedLightSail();

        /// <summary>
        /// Expand the SAIL layer to the given column count.
        /// Only expands (never shrinks), consistent with Core/Prism/SAT behavior.
        /// </summary>
        public void SetSailLayerCols(int cols) => _loadoutManager.SetSailLayerCols(cols);

        /// <summary> Get the currently equipped Satellite SOs for the specified track. </summary>
        public IReadOnlyList<SatelliteSO> GetEquippedSatellites(WeaponTrack.TrackId trackId)
            => _loadoutManager.GetEquippedSatellites(trackId);

        /// <summary>
        /// Equip a Light Sail at runtime. Disposes the previous one if any.
        /// Supports optional anchor position for multi-slot SAIL layer.
        /// </summary>
        public void EquipLightSail(LightSailSO sail, int anchorCol = 0, int anchorRow = 0)
            => _loadoutManager.EquipLightSail(sail, anchorCol, anchorRow);

        /// <summary> Unequip the current Light Sail. </summary>
        public void UnequipLightSail() => _loadoutManager.UnequipLightSail();

        /// <summary> Unequip a specific Light Sail from the active loadout. </summary>
        public void UnequipLightSail(LightSailSO sail) => _loadoutManager.UnequipLightSail(sail);

        /// <summary>
        /// Equip a Satellite at runtime to the specified track. Creates a new SatelliteRunner.
        /// </summary>
        public void EquipSatellite(SatelliteSO sat, WeaponTrack.TrackId trackId)
            => _loadoutManager.EquipSatellite(sat, trackId);

        /// <summary>
        /// Equip a Satellite at a specific anchor position in the SAT layer.
        /// </summary>
        public void EquipSatellite(SatelliteSO sat, WeaponTrack.TrackId trackId, int anchorCol, int anchorRow)
            => _loadoutManager.EquipSatellite(sat, trackId, anchorCol, anchorRow);

        /// <summary>
        /// Unequip a specific Satellite at runtime from the specified track.
        /// </summary>
        public void UnequipSatellite(SatelliteSO sat, WeaponTrack.TrackId trackId)
            => _loadoutManager.UnequipSatellite(sat, trackId);

        /// <summary>
        /// Switch to a different loadout slot. Disposes old slot's runners and
        /// re-initializes pools for the new slot's tracks.
        /// </summary>
        public void SwitchLoadout(int index) => _loadoutManager.SwitchLoadout(index);

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

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

            _context = new StarChartContext(
                _shipMotor, _shipAiming, _inputHandler,
                _heatSystem, this, transform);

            // Phase C: LoadoutManager owns all loadout state and equip/unequip/switch API.
            _loadoutManager = new LoadoutManager(_context, InitializeAllPools);
            _loadoutManager.OnLightSailChanged  += ForwardLightSailChanged;
            _loadoutManager.OnSatellitesChanged += ForwardSatellitesChanged;

            // Phase A: save/load serializer reads the same state arrays through the manager.
            _saveSerializer = new StarChartSaveSerializer(
                _loadoutManager.Loadouts,
                _loadoutManager.LightSailRunners,
                _loadoutManager.PrimarySatRunners,
                _loadoutManager.SecondarySatRunners,
                _context,
                _loadoutManager.DisposeSlotRunners,
                InitializeAllPools);

            // Phase B: projectile spawner resolves the active sail runner lazily through the manager.
            _projectileSpawner = new ProjectileSpawner(
                transform,
                _firePoint,
                _audioSource,
                () => _loadoutManager.ActiveLightSailRunner);
        }

        private void Start()
        {
            LoadDebugLoadout();
            InitializeAllPools();
            InitializeSailAndSatellites();
        }

        private void Update()
        {
            // Guard: loadout manager must be initialized (Awake may have failed if components missing).
            // Loud-fail policy: log once then disable to prevent per-frame silent no-op.
            if (_loadoutManager == null)
            {
                ReportMissingDependencyAndDisable(nameof(_loadoutManager));
                return;
            }

            float dt = Time.deltaTime;
            _loadoutManager.TickActive(dt);

            // Guard: inputHandler must be valid.
            // Loud-fail policy: log once then disable to prevent per-frame silent no-op.
            if (_inputHandler == null)
            {
                ReportMissingDependencyAndDisable(nameof(_inputHandler));
                return;
            }

            // 主武器：左键
            if (_inputHandler.IsFireHeld && CanFireTrack(_loadoutManager.PrimaryTrack))
                ExecuteFire(_loadoutManager.PrimaryTrack);

            // 副武器：右键
            if (_inputHandler.IsSecondaryFireHeld && CanFireTrack(_loadoutManager.SecondaryTrack))
                ExecuteFire(_loadoutManager.SecondaryTrack);
        }

        /// <summary>
        /// Loud-fail guard: emit a single LogError identifying the missing dependency,
        /// then disable this component so the error does not spam the console every frame.
        /// Per project policy (Implement_rules Loud Failure), silent Update no-op is forbidden.
        /// </summary>
        private void ReportMissingDependencyAndDisable(string dependencyName)
        {
            Debug.LogError(
                $"[StarChartController] Missing required dependency '{dependencyName}' at Update(). " +
                $"Component disabled to avoid per-frame silent no-op. " +
                $"Check Awake() initialization and RequireComponent dependencies on GameObject '{gameObject.name}'.",
                this);
            enabled = false;
        }

        private void OnDestroy()
        {
            OnTrackFired = null;
            OnLightSailChanged = null;
            OnSatellitesChanged = null;

            ServiceLocator.Unregister<StarChartController>(this);

            if (_loadoutManager != null)
            {
                _loadoutManager.OnLightSailChanged  -= ForwardLightSailChanged;
                _loadoutManager.OnSatellitesChanged -= ForwardSatellitesChanged;
                _loadoutManager.Dispose();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Fire pipeline
        // ══════════════════════════════════════════════════════════════

        private bool CanFireTrack(WeaponTrack track)
        {
            return track.CanFire && (_heatSystem == null || _heatSystem.CanFire());
        }

        private void ExecuteFire(WeaponTrack track)
        {
            var snapshot = track.TryFire();
            if (snapshot == null) return;

            Vector2 direction = _shipAiming.FacingDirection;
            Vector3 spawnPos = _projectileSpawner.GetSpawnPosition();

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

                    _projectileSpawner.SpawnProjectile(track, coreSnap, fireDir, spawnPos);
                }

                // 炮口焰（每核心一次）
                _projectileSpawner.SpawnMuzzleFlash(track, coreSnap, direction, spawnPos);

                // 音效（每核心一次）
                _projectileSpawner.PlayFireSound(coreSnap);
            }

            // 后坐力（合计）
            if (snapshot.TotalRecoilForce > 0f)
                _shipMotor.ApplyImpulse(-direction * snapshot.TotalRecoilForce);

            // 热量（合计）
            _heatSystem?.AddHeat(snapshot.TotalHeatCost);

            // 事件
            OnTrackFired?.Invoke(track.Id);

            // Broadcast weapon fire for enemy auditory perception
            CombatEvents.RaiseWeaponFired(spawnPos, DEFAULT_NOISE_RADIUS);
        }

        // ══════════════════════════════════════════════════════════════
        // Spawn pipeline
        // ══════════════════════════════════════════════════════════════
        //
        // Implementation lives in ProjectileSpawner (L3-1 Phase B).
        // That class is the single owner of the CoreFamily switch per
        // Implement_rules.md §8.3.2. ExecuteFire above only orchestrates:
        // snapshot retrieval, spread fan calculation, recoil, heat, and
        // outward events; it delegates the actual GameObject spawning,
        // modifier instantiation, MuzzleFlash, and fire sound to the spawner.

        // ══════════════════════════════════════════════════════════════
        // Loadout state / Equip / SwitchLoadout
        // ══════════════════════════════════════════════════════════════
        //
        // Implementation lives in LoadoutManager (L3-1 Phase C).
        // Public API above (PrimaryTrack, SecondaryTrack, SailLayer,
        // EquipLightSail, UnequipLightSail, EquipSatellite, UnequipSatellite,
        // SwitchLoadout, ...) are one-line delegates to _loadoutManager.

        private void LoadDebugLoadout()
        {
            // Debug loadout is always loaded into slot 0 only (requirement 2.6)
            var slot0 = _loadoutManager.Loadouts[0];

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

            // After Phase C, LoadoutManager may not yet be constructed when this
            // runs via its _initializeAllPools callback during SwitchLoadout; in
            // that path the manager is the caller so it exists. The only other
            // caller is Start(), which runs after Awake() has built the manager.
            _loadoutManager.PrimaryTrack.InitializePools();
            _loadoutManager.SecondaryTrack.InitializePools();
        }

        private void InitializeSailAndSatellites()
        {
            // Debug sail/satellites are loaded into slot 0 only
            var slot0 = _loadoutManager.Loadouts[0];

            // 光帆
            if (_debugLightSail != null)
            {
                slot0.EquippedLightSailSO = _debugLightSail;
                _loadoutManager.LightSailRunners[0] = new LightSailRunner(_debugLightSail, _context);
            }

            // 伴星（debug 默认装备到 Primary 轨道）
            if (_debugSatellites != null)
            {
                for (int i = 0; i < _debugSatellites.Length; i++)
                {
                    if (_debugSatellites[i] != null)
                    {
                        var runner = new SatelliteRunner(_debugSatellites[i], _context);
                        _loadoutManager.PrimarySatRunners[0].Add(runner);
                        slot0.PrimaryTrack.EquipSatellite(_debugSatellites[i]);
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Event forwarding (LoadoutManager → Controller public events)
        // ══════════════════════════════════════════════════════════════

        private void ForwardLightSailChanged() => OnLightSailChanged?.Invoke();
        private void ForwardSatellitesChanged() => OnSatellitesChanged?.Invoke();

        // ══════════════════════════════════════════════════════════════
        // Save / Load Serialization
        // ══════════════════════════════════════════════════════════════
        //
        // Implementation lives in StarChartSaveSerializer (L3-1 Phase A).
        // Public API signatures are preserved; external callers should see no change.

        /// <summary>
        /// Export the current Star Chart loadout to a serializable data object.
        /// Serializes all 3 loadout slots.
        /// </summary>
        public StarChartSaveData ExportToSaveData()
        {
            return _saveSerializer.Export();
        }

        /// <summary>
        /// Import a Star Chart loadout from saved data, using a resolver to look up items by name.
        /// Supports both new multi-slot format and legacy single-slot format (auto-migration).
        /// </summary>
        public void ImportFromSaveData(StarChartSaveData data, IStarChartItemResolver resolver)
        {
            _saveSerializer.Import(data, resolver);
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
