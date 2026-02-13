using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Periodic on/off hazard (e.g., rotating drill, intermittent laser, piston trap).
    /// Cycles between active and inactive states, toggling the Collider2D.
    /// While active, behaves as a ContactHazard (damage on enter + cooldown).
    /// 
    /// Visual sync: optionally fades SpriteRenderer alpha to indicate state.
    /// </summary>
    public class TimedHazard : EnvironmentHazard
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Timing")]
        [Tooltip("Duration (seconds) the hazard stays active (dealing damage).")]
        [SerializeField] private float _activeDuration = 2f;

        [Tooltip("Duration (seconds) the hazard stays inactive (safe to cross).")]
        [SerializeField] private float _inactiveDuration = 2f;

        [Tooltip("Startup delay before the first cycle begins.")]
        [SerializeField] private float _startDelay = 0f;

        [Header("Contact")]
        [Tooltip("Cooldown (seconds) before damaging the same target again during active phase.")]
        [SerializeField] private float _hitCooldown = 0.5f;

        [Header("Visuals")]
        [Tooltip("Optional SpriteRenderer for visual state sync.")]
        [SerializeField] private SpriteRenderer _visual;

        [Tooltip("Alpha when active.")]
        [SerializeField] [Range(0f, 1f)] private float _activeAlpha = 1f;

        [Tooltip("Alpha when inactive.")]
        [SerializeField] [Range(0f, 1f)] private float _inactiveAlpha = 0.2f;

        // ──────────────────── Runtime State ────────────────────

        private float _cycleTimer;
        private bool _isActive;
        private readonly Dictionary<int, float> _cooldownExpiry = new();

        // ──────────────────── Lifecycle ────────────────────

        protected override void Awake()
        {
            base.Awake();

            // Auto-find visual if not assigned
            if (_visual == null)
            {
                _visual = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            _cycleTimer = -_startDelay; // 负值表示启动延迟
            SetActive(false); // 启动时先关闭
        }

        private void Update()
        {
            _cycleTimer += Time.deltaTime;

            if (_cycleTimer < 0f) return; // 仍在启动延迟中

            float cycleDuration = _activeDuration + _inactiveDuration;
            float phase = _cycleTimer % cycleDuration;

            bool shouldBeActive = phase < _activeDuration;

            if (shouldBeActive != _isActive)
            {
                SetActive(shouldBeActive);
            }
        }

        // ──────────────────── State Toggle ────────────────────

        private void SetActive(bool active)
        {
            _isActive = active;
            _collider.enabled = active;

            // 切换为非激活时清理冷却追踪
            if (!active)
            {
                _cooldownExpiry.Clear();
            }

            // 视觉同步
            if (_visual != null)
            {
                Color c = _visual.color;
                c.a = active ? _activeAlpha : _inactiveAlpha;
                _visual.color = c;
            }
        }

        // ──────────────────── Trigger (active phase only) ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider2D other)
        {
            if (!_isActive) return;
            if (!IsValidTarget(other.gameObject)) return;

            int id = other.gameObject.GetInstanceID();
            float now = Time.time;

            // 每目标冷却
            if (_cooldownExpiry.TryGetValue(id, out float expiry) && now < expiry)
                return;

            ApplyDamage(other.gameObject);
            _cooldownExpiry[id] = now + _hitCooldown;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValidTarget(other.gameObject)) return;
            _cooldownExpiry.Remove(other.gameObject.GetInstanceID());
        }

        private void OnDisable()
        {
            _cooldownExpiry.Clear();
        }
    }
}
