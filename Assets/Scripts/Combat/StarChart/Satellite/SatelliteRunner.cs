using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Pure C# lifecycle manager for a single equipped Satellite.
    /// Handles prefab instantiation, internal cooldown tracking,
    /// trigger evaluation, action execution, and disposal.
    /// One runner per equipped SatelliteSO. Ticked by StarChartController.Update().
    /// </summary>
    public class SatelliteRunner
    {
        private readonly SatelliteSO _data;
        private readonly StarChartContext _context;
        private SatelliteBehavior _behavior;
        private float _cooldownTimer;

        /// <summary> The active behavior instance (null if prefab missing or invalid). </summary>
        public SatelliteBehavior ActiveBehavior => _behavior;

        /// <summary> The SO data driving this satellite. </summary>
        public SatelliteSO Data => _data;

        public SatelliteRunner(SatelliteSO data, StarChartContext context)
        {
            _data = data;
            _context = context;

            InstantiateBehavior();
        }

        /// <summary> Tick cooldown, evaluate trigger, execute if ready. </summary>
        public void Tick(float deltaTime)
        {
            if (_behavior == null) return;

            // 冷却倒计时
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= deltaTime;
                return;
            }

            // 评估触发条件
            if (_behavior.EvaluateTrigger(_context))
            {
                _behavior.Execute(_context);
                _cooldownTimer = _data.InternalCooldown;
            }
        }

        /// <summary> Clean up behavior and destroy GameObject. </summary>
        public void Dispose()
        {
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
            go.name = $"Satellite_{_data.name}";

            _behavior = go.GetComponent<SatelliteBehavior>();
            if (_behavior == null)
            {
                Debug.LogWarning($"[SatelliteRunner] BehaviorPrefab on '{_data.name}' " +
                                 "has no SatelliteBehavior component.");
                Object.Destroy(go);
                return;
            }

            _behavior.Initialize(_context);
        }
    }
}
