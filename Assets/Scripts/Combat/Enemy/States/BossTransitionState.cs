using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Boss transition state: forced during phase changes.
    /// The enemy is invulnerable, stopped, and plays a visual pulse effect.
    /// After TransitionDuration expires, ends the transition and returns to combat.
    /// </summary>
    public class BossTransitionState : IState
    {
        private readonly EnemyBrain _brain;
        private readonly BossPhaseDataSO _phaseData;
        private float _timer;

        // Visual pulse parameters
        private SpriteRenderer _spriteRenderer;
        private Color _phaseColor;
        private const float PULSE_SPEED = 6f;

        public BossTransitionState(EnemyBrain brain, BossPhaseDataSO phaseData)
        {
            _brain = brain;
            _phaseData = phaseData;
        }

        public void OnEnter()
        {
            _timer = _phaseData != null ? _phaseData.TransitionDuration : 1.5f;
            _phaseColor = _phaseData != null ? _phaseData.PhaseColor : Color.white;

            // Stop all movement
            _brain.Entity.StopMovement();

            // Cache renderer for visual effects
            _spriteRenderer = _brain.Entity.GetComponent<SpriteRenderer>();

            // HitStop: dramatic pause for phase transition
            Core.HitStopEffect.Trigger(0.1f);
        }

        public void OnUpdate(float deltaTime)
        {
            // Visual pulse: oscillate between white and phase color
            if (_spriteRenderer != null)
            {
                float t = (Mathf.Sin(Time.time * PULSE_SPEED) + 1f) * 0.5f;
                _spriteRenderer.color = Color.Lerp(Color.white, _phaseColor, t);
            }

            _timer -= deltaTime;

            if (_timer <= 0f)
            {
                // Transition complete — end invulnerability
                var bossCtrl = _brain.GetComponent<BossController>();
                if (bossCtrl != null)
                    bossCtrl.EndTransition();

                // Set final color
                if (_spriteRenderer != null)
                    _spriteRenderer.color = _phaseColor;

                // Return to combat
                if (_brain.Perception.HasTarget)
                    _brain.StateMachine.TransitionTo(_brain.ChaseState);
                else
                    _brain.StateMachine.TransitionTo(_brain.IdleState);
            }
        }

        public void OnExit()
        {
            // Ensure color is set
            if (_spriteRenderer != null)
                _spriteRenderer.color = _phaseColor;
        }
    }
}
