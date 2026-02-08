using System;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Heat;
using ProjectArk.Ship;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Legacy single-weapon system. Replaced by <see cref="StarChartController"/>
    /// which supports multi-core tracks, prism modifiers, and dual weapon tracks.
    /// </summary>
    [Obsolete("Use StarChartController instead. Will be removed in Batch 4.")]
    [RequireComponent(typeof(InputHandler))]
    [RequireComponent(typeof(ShipAiming))]
    [RequireComponent(typeof(ShipMotor))]
    public class WeaponSystem : MonoBehaviour
    {
        [SerializeField] private WeaponStatsSO _weaponStats;
        [SerializeField] private FirePoint _firePoint;

        [Tooltip("Optional: assign to enable heat cost per shot. Leave empty for unlimited firing.")]
        [SerializeField] private HeatSystem _heatSystem;

        /// <summary> Fired when a shot is taken. Subscribe for screen shake, UI, etc. </summary>
        public event Action OnWeaponFired;

        private InputHandler _inputHandler;
        private ShipAiming _shipAiming;
        private ShipMotor _shipMotor;

        private GameObjectPool _projectilePool;
        private GameObjectPool _muzzleFlashPool;

        private float _fireCooldownTimer;
        private AudioSource _audioSource;

        private void Awake()
        {
            _inputHandler = GetComponent<InputHandler>();
            _shipAiming = GetComponent<ShipAiming>();
            _shipMotor = GetComponent<ShipMotor>();

            // 专用 AudioSource，避免干扰其他音频
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D 音效（玩家自己的飞船）
        }

        private void Start()
        {
            // 在 Start 中初始化池，确保 PoolManager 已完成 Awake
            InitializePools();
        }

        private void Update()
        {
            _fireCooldownTimer -= Time.deltaTime;

            bool canFire = _inputHandler.IsFireHeld
                        && _fireCooldownTimer <= 0f
                        && (_heatSystem == null || _heatSystem.CanFire());

            if (canFire)
            {
                Fire();
                _fireCooldownTimer = _weaponStats.FireInterval;
            }
        }

        private void Fire()
        {
            if (_projectilePool == null) return;

            Vector2 direction = _shipAiming.FacingDirection;
            Vector3 spawnPos = _firePoint != null ? _firePoint.Position : transform.position;

            // 散布：随机偏转
            if (!Mathf.Approximately(_weaponStats.Spread, 0f))
            {
                float spreadAngle = UnityEngine.Random.Range(-_weaponStats.Spread, _weaponStats.Spread);
                direction = RotateVector2(direction, spreadAngle);
            }

            // 子弹朝向角度（sprite 朝上 +Y，所以 -90°）
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            // 从池中取出子弹
            GameObject bulletObj = _projectilePool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var projectile = bulletObj.GetComponent<Projectile>();
            projectile.Initialize(direction, _weaponStats);

            // 炮口焰
            if (_muzzleFlashPool != null)
                _muzzleFlashPool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));

            // 后坐力
            if (_weaponStats.RecoilForce > 0f)
                _shipMotor.ApplyImpulse(-direction * _weaponStats.RecoilForce);

            // 热量
            _heatSystem?.AddHeat(_weaponStats.HeatCostPerShot);

            // 音效
            PlayFireSound();

            // 事件通知（屏幕震动、UI 等外部系统订阅）
            OnWeaponFired?.Invoke();
        }

        private void PlayFireSound()
        {
            if (_weaponStats.FireSound == null) return;

            float pitch = 1f + UnityEngine.Random.Range(
                -_weaponStats.FireSoundPitchVariance,
                _weaponStats.FireSoundPitchVariance);
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(_weaponStats.FireSound);
        }

        private void InitializePools()
        {
            if (PoolManager.Instance == null)
            {
                Debug.LogError("[WeaponSystem] PoolManager not found in scene. Weapon disabled.");
                enabled = false;
                return;
            }

            if (_weaponStats.ProjectilePrefab != null)
                _projectilePool = PoolManager.Instance.GetPool(_weaponStats.ProjectilePrefab, 20, 50);

            if (_weaponStats.MuzzleFlashPrefab != null)
                _muzzleFlashPool = PoolManager.Instance.GetPool(_weaponStats.MuzzleFlashPrefab, 5, 20);
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
