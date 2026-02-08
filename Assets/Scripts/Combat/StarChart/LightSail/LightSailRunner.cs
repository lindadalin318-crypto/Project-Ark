using UnityEngine;
using ProjectArk.Heat;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Pure C# lifecycle manager for the equipped Light Sail.
    /// Handles prefab instantiation, per-frame ticking, overheat disable/enable,
    /// and cleanup on disposal. Ticked by StarChartController.Update().
    /// </summary>
    public class LightSailRunner
    {
        private readonly LightSailSO _data;
        private readonly StarChartContext _context;
        private LightSailBehavior _behavior;
        private bool _isDisabledByOverheat;

        /// <summary> The active behavior instance (null if prefab missing or invalid). </summary>
        public LightSailBehavior ActiveBehavior => _behavior;

        /// <summary> True if sail is currently disabled by overheat penalty. </summary>
        public bool IsDisabled => _isDisabledByOverheat;

        public LightSailRunner(LightSailSO data, StarChartContext context)
        {
            _data = data;
            _context = context;

            InstantiateBehavior();
            SubscribeToHeat();
        }

        /// <summary> Tick the behavior each frame. </summary>
        public void Tick(float deltaTime)
        {
            if (_behavior == null) return;
            _behavior.Tick(deltaTime, _context);
        }

        /// <summary>
        /// Apply sail buff to projectile params. No-op if disabled or no behavior.
        /// </summary>
        public void ModifyProjectileParams(ref ProjectileParams parms)
        {
            if (_behavior == null || _isDisabledByOverheat) return;
            _behavior.ModifyProjectileParams(ref parms);
        }

        /// <summary> Clean up behavior, unsubscribe events, destroy GameObject. </summary>
        public void Dispose()
        {
            UnsubscribeFromHeat();

            if (_behavior != null)
            {
                _behavior.Cleanup();
                Object.Destroy(_behavior.gameObject);
                _behavior = null;
            }
        }

        private void InstantiateBehavior()
        {
            if (_data.BehaviorPrefab == null) return;

            var go = Object.Instantiate(_data.BehaviorPrefab, _context.ShipTransform);
            go.name = $"LightSail_{_data.name}";

            _behavior = go.GetComponent<LightSailBehavior>();
            if (_behavior == null)
            {
                Debug.LogWarning($"[LightSailRunner] BehaviorPrefab on '{_data.name}' " +
                                 "has no LightSailBehavior component.");
                Object.Destroy(go);
                return;
            }

            _behavior.Initialize(_context);
        }

        private void SubscribeToHeat()
        {
            if (_context.Heat == null) return;
            _context.Heat.OnOverheated += OnOverheated;
            _context.Heat.OnCooldownComplete += OnCooldownComplete;
        }

        private void UnsubscribeFromHeat()
        {
            if (_context.Heat == null) return;
            _context.Heat.OnOverheated -= OnOverheated;
            _context.Heat.OnCooldownComplete -= OnCooldownComplete;
        }

        private void OnOverheated()
        {
            _isDisabledByOverheat = true;
            _behavior?.OnDisabled();
        }

        private void OnCooldownComplete()
        {
            _isDisabledByOverheat = false;
            _behavior?.OnEnabled();
        }
    }
}
