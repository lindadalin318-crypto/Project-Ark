using System;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Heat
{
    /// <summary>
    /// Manages the ship's heat resource. Heat accumulates from firing weapons
    /// and dissipates passively over time. Exceeding the threshold triggers
    /// an overheat state that silences all weapons for a penalty duration.
    /// Attach to the Ship root GameObject.
    /// </summary>
    public class HeatSystem : MonoBehaviour
    {
        [SerializeField] private HeatStatsSO _stats;

        // --- Public properties ---

        /// <summary> Current absolute heat value. </summary>
        public float CurrentHeat => _currentHeat;

        /// <summary> Maximum heat capacity from stats. </summary>
        public float MaxHeat => _stats != null ? _stats.MaxHeat : 0f;

        /// <summary> Heat as a 0~1 ratio (current / max). </summary>
        public float NormalizedHeat => _stats != null && _stats.MaxHeat > 0f
            ? Mathf.Clamp01(_currentHeat / _stats.MaxHeat)
            : 0f;

        /// <summary> True while in the overheat silence state. </summary>
        public bool IsOverheated => _isOverheated;

        /// <summary> Seconds remaining in overheat penalty. 0 when normal. </summary>
        public float OverheatTimeRemaining => _overheatTimer;

        // --- Events ---

        /// <summary> Fired whenever heat value changes. Param: normalized 0~1. </summary>
        public event Action<float> OnHeatChanged;

        /// <summary> Fired once when entering overheat state. </summary>
        public event Action OnOverheated;

        /// <summary> Fired once when overheat penalty ends and normal state resumes. </summary>
        public event Action OnCooldownComplete;

        // --- Private state ---

        private float _currentHeat;
        private bool _isOverheated;
        private float _overheatTimer;

        // --- Public methods ---

        /// <summary>
        /// Quick check: can the ship fire right now?
        /// Returns false if overheated.
        /// </summary>
        public bool CanFire()
        {
            return !_isOverheated;
        }

        /// <summary>
        /// Adds heat from a weapon firing event.
        /// Triggers overheat if threshold is exceeded.
        /// </summary>
        public void AddHeat(float amount)
        {
            if (_isOverheated || amount <= 0f) return;

            _currentHeat = Mathf.Min(_currentHeat + amount, _stats.MaxHeat);
            OnHeatChanged?.Invoke(NormalizedHeat);

            // 检查是否触发过热
            if (_currentHeat >= _stats.OverheatHeatValue)
            {
                EnterOverheat();
            }
        }

        /// <summary>
        /// Forcibly reduces heat (e.g., Satellite "Scavenger" ability).
        /// </summary>
        public void ReduceHeat(float amount)
        {
            if (amount <= 0f) return;

            _currentHeat = Mathf.Max(_currentHeat - amount, 0f);
            OnHeatChanged?.Invoke(NormalizedHeat);
        }

        /// <summary>
        /// Instantly clears all heat (e.g., Satellite "Scavenger" on pickup).
        /// Does NOT exit overheat state — use this only during normal operation.
        /// </summary>
        public void ResetHeat()
        {
            _currentHeat = 0f;
            OnHeatChanged?.Invoke(0f);
        }

        // --- Lifecycle ---

        private void Awake()
        {
            ServiceLocator.Register<HeatSystem>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<HeatSystem>(this);
        }

        private void Update()
        {
            if (_stats == null) return;

            if (_isOverheated)
            {
                UpdateOverheat();
            }
            else
            {
                UpdateCooling();
            }
        }

        // --- Private methods ---

        private void UpdateCooling()
        {
            if (_currentHeat <= 0f) return;

            _currentHeat = Mathf.Max(_currentHeat - _stats.CoolingRate * Time.deltaTime, 0f);
            OnHeatChanged?.Invoke(NormalizedHeat);
        }

        private void UpdateOverheat()
        {
            _overheatTimer -= Time.deltaTime;

            if (_overheatTimer <= 0f)
            {
                ExitOverheat();
            }
        }

        private void EnterOverheat()
        {
            _isOverheated = true;
            _overheatTimer = _stats.OverheatDuration;

            Debug.Log($"[HeatSystem] OVERHEATED! Silenced for {_stats.OverheatDuration}s");
            OnOverheated?.Invoke();
        }

        private void ExitOverheat()
        {
            _isOverheated = false;
            _overheatTimer = 0f;
            _currentHeat = 0f; // 过热结束，热量清零，干净重启

            Debug.Log("[HeatSystem] Cooldown complete, heat reset to 0");
            OnCooldownComplete?.Invoke();
            OnHeatChanged?.Invoke(0f);
        }
    }
}
