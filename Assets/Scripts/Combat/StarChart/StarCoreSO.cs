using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Star Core — the firing source in the Star Chart system.
    /// Defines what projectile is spawned and its base stats.
    /// Each Core belongs to a family that determines its fundamental behavior
    /// (physical bullet, laser, shockwave, or anomalous entity).
    /// </summary>
    [CreateAssetMenu(fileName = "NewStarCore", menuName = "ProjectArk/StarChart/Star Core")]
    public class StarCoreSO : StarChartItemSO
    {
        public override StarChartItemType ItemType => StarChartItemType.Core;

        [Header("Core Identity")]
        [SerializeField] private CoreFamily _family;

        [Header("Projectile")]
        [SerializeField] private GameObject _projectilePrefab;

        [Header("Firing Stats")]
        [Tooltip("Shots per second")]
        [SerializeField] private float _fireRate = 5f;

        [Tooltip("Base damage per hit")]
        [SerializeField] private float _baseDamage = 10f;

        [Tooltip("Projectile travel speed (units/second)")]
        [SerializeField] private float _projectileSpeed = 20f;

        [Tooltip("Seconds before auto-recycle. Range = speed * lifetime")]
        [SerializeField] private float _lifetime = 2f;

        [Tooltip("Random angle deviation per shot (degrees)")]
        [SerializeField] private float _spread;

        [Tooltip("Force applied to hit target")]
        [SerializeField] private float _knockback = 1f;

        [Tooltip("Impulse applied to ship on each shot (opposite to fire direction)")]
        [SerializeField] private float _recoilForce = 0.5f;

        [Header("VFX")]
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private GameObject _impactVFXPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip _fireSound;

        [Tooltip("Pitch randomization (e.g. 0.1 = ±10%)")]
        [SerializeField] private float _fireSoundPitchVariance = 0.1f;

        // --- Public read-only properties ---

        public CoreFamily Family => _family;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float FireRate => _fireRate;
        public float FireInterval => _fireRate > 0f ? 1f / _fireRate : float.MaxValue;
        public float BaseDamage => _baseDamage;
        public float ProjectileSpeed => _projectileSpeed;
        public float Lifetime => _lifetime;
        public float Spread => _spread;
        public float Knockback => _knockback;
        public float RecoilForce => _recoilForce;
        public GameObject MuzzleFlashPrefab => _muzzleFlashPrefab;
        public GameObject ImpactVFXPrefab => _impactVFXPrefab;
        public AudioClip FireSound => _fireSound;
        public float FireSoundPitchVariance => _fireSoundPitchVariance;
    }
}
