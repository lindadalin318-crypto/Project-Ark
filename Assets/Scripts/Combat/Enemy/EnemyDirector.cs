using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Global combat coordinator (导演系统).
    /// Manages attack tokens to limit how many enemies can attack simultaneously,
    /// creating "movie-feel" combat where non-attacking enemies orbit the player
    /// instead of mob-rushing.
    /// 
    /// Usage:
    ///   - Place one EnemyDirector in the scene, or let it auto-create.
    ///   - Enemies call RequestToken() before entering Engage/Shoot.
    ///   - If denied, they transition to OrbitState.
    ///   - On attack completion or death, call ReturnToken().
    ///   - If no Director exists in scene, all enemies attack freely (backward compatible).
    /// </summary>
    public class EnemyDirector : MonoBehaviour
    {
        // ──────────────────── Singleton ────────────────────
        private static EnemyDirector _instance;

        /// <summary>
        /// Global Director instance. Null if no Director exists in the scene
        /// (backward compatible — enemies attack freely without a Director).
        /// </summary>
        public static EnemyDirector Instance => _instance;

        // ──────────────────── Configuration ────────────────────
        [Header("Attack Tokens")]
        [Tooltip("Maximum number of enemies allowed to attack simultaneously.")]
        [SerializeField] [Min(1)] private int _maxAttackTokens = 2;

        [Header("Orbit")]
        [Tooltip("Distance multiplier for orbit radius relative to AttackRange.")]
        [SerializeField] [Min(1f)] private float _orbitRadiusMultiplier = 1.5f;

        [Tooltip("Angular speed of orbiting enemies (degrees per second).")]
        [SerializeField] [Min(10f)] private float _orbitSpeed = 90f;

        // ──────────────────── Runtime State ────────────────────
        private readonly HashSet<EnemyBrain> _tokenHolders = new HashSet<EnemyBrain>();

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Current number of held tokens. </summary>
        public int ActiveTokenCount => _tokenHolders.Count;

        /// <summary> Maximum simultaneous attackers. </summary>
        public int MaxAttackTokens => _maxAttackTokens;

        /// <summary> Orbit radius = AttackRange * this multiplier. </summary>
        public float OrbitRadiusMultiplier => _orbitRadiusMultiplier;

        /// <summary> Orbit angular speed in degrees/second. </summary>
        public float OrbitSpeed => _orbitSpeed;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[EnemyDirector] Duplicate Director detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            ServiceLocator.Register<EnemyDirector>(this);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                ServiceLocator.Unregister<EnemyDirector>(this);
            }
        }

        private void LateUpdate()
        {
            // Auto-cleanup: remove dead/disabled token holders to prevent token leaks
            if (_tokenHolders.Count > 0)
                CleanupStaleTokens();
        }

        // ──────────────────── Token API ────────────────────

        /// <summary>
        /// Request an attack token. Returns true if granted.
        /// Call before entering Engage/Shoot state.
        /// If the brain already holds a token, returns true immediately.
        /// </summary>
        public bool RequestToken(EnemyBrain requester)
        {
            if (requester == null) return false;

            // Already holds a token
            if (_tokenHolders.Contains(requester)) return true;

            // Pool full
            if (_tokenHolders.Count >= _maxAttackTokens) return false;

            _tokenHolders.Add(requester);
            return true;
        }

        /// <summary>
        /// Return a held attack token. Call on attack completion, death, or disable.
        /// Safe to call even if the brain doesn't hold a token.
        /// </summary>
        public void ReturnToken(EnemyBrain requester)
        {
            if (requester == null) return;
            _tokenHolders.Remove(requester);
        }

        /// <summary>
        /// Check if a specific brain currently holds an attack token.
        /// </summary>
        public bool HasToken(EnemyBrain requester)
        {
            return requester != null && _tokenHolders.Contains(requester);
        }

        // ──────────────────── Cleanup ────────────────────

        // Reusable list to avoid allocation during cleanup
        private readonly List<EnemyBrain> _staleTokens = new List<EnemyBrain>(4);

        private void CleanupStaleTokens()
        {
            _staleTokens.Clear();

            foreach (var brain in _tokenHolders)
            {
                if (brain == null || !brain.isActiveAndEnabled ||
                    brain.Entity == null || !brain.Entity.IsAlive)
                {
                    _staleTokens.Add(brain);
                }
            }

            for (int i = 0; i < _staleTokens.Count; i++)
            {
                _tokenHolders.Remove(_staleTokens[i]);
            }
        }

        // ──────────────────── Debug ────────────────────

#if UNITY_EDITOR
        private void OnGUI()
        {
            // Show token status in top-left corner
            GUI.color = Color.cyan;
            GUI.Label(new Rect(10, 10, 300, 25),
                $"[Director] Tokens: {_tokenHolders.Count}/{_maxAttackTokens}");
        }
#endif
    }
}
