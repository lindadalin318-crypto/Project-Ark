using System;
using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Legacy flat weapon stats. Replaced by <see cref="StarCoreSO"/> which provides
    /// the same firing parameters plus Star Chart integration (family, slot size, etc.).
    /// </summary>
    [Obsolete("Use StarCoreSO instead. Will be removed in Batch 4.")]
    [CreateAssetMenu(fileName = "NewWeaponStats", menuName = "ProjectArk/Combat/Weapon Stats")]
    public class WeaponStatsSO : ScriptableObject
    {
        [Header("Firing")]
        [Tooltip("Shots per second")]
        [SerializeField] private float _fireRate = 5f;

        [Tooltip("Random angle deviation per shot (degrees). 0 = perfectly accurate")]
        [SerializeField] private float _spread;

        [Header("Projectile")]
        [Tooltip("Damage dealt on hit")]
        [SerializeField] private float _baseDamage = 10f;

        [Tooltip("Bullet travel speed (units/second)")]
        [SerializeField] private float _projectileSpeed = 20f;

        [Tooltip("Seconds before auto-recycle. Effective range = speed * lifetime")]
        [SerializeField] private float _lifetime = 2f;

        [Tooltip("Force applied to hit target")]
        [SerializeField] private float _knockback = 1f;

        [Header("Recoil")]
        [Tooltip("Impulse applied to ship (opposite to fire direction) on each shot")]
        [SerializeField] private float _recoilForce = 0.5f;

        [Header("Prefabs")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private GameObject _impactVFXPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip _fireSound;
        [SerializeField] private AudioClip _hitSound;

        [Tooltip("Pitch randomization range (e.g. 0.1 = ±10%)")]
        [SerializeField] private float _fireSoundPitchVariance = 0.1f;

        [Header("Heat")]
        [Tooltip("Heat generated per shot. Higher = fewer shots before overheat")]
        [SerializeField] private float _heatCostPerShot = 5f;

        // --- Public read-only properties ---

        public float FireRate => _fireRate;
        /// <summary> Seconds between shots (1 / FireRate). </summary>
        public float FireInterval => _fireRate > 0f ? 1f / _fireRate : float.MaxValue;
        public float Spread => _spread;
        public float BaseDamage => _baseDamage;
        public float ProjectileSpeed => _projectileSpeed;
        public float Lifetime => _lifetime;
        public float Knockback => _knockback;
        public float RecoilForce => _recoilForce;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public GameObject MuzzleFlashPrefab => _muzzleFlashPrefab;
        public GameObject ImpactVFXPrefab => _impactVFXPrefab;
        public AudioClip FireSound => _fireSound;
        public AudioClip HitSound => _hitSound;
        public float FireSoundPitchVariance => _fireSoundPitchVariance;
        public float HeatCostPerShot => _heatCostPerShot;
    }
}
