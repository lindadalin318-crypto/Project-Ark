using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Telegraph sub-state: wind-up phase before an attack.
    /// Enemy stops moving, plays a visual warning signal (sprite color change),
    /// then transitions to AttackSubState after TelegraphDuration expires.
    /// This is the "read" window — the player sees the signal and reacts.
    /// Reads duration and color from AttackDataSO if available, otherwise legacy EnemyStatsSO.
    /// </summary>
    public class TelegraphSubState : IState
    {
        private readonly EnemyBrain _brain;
        private readonly EngageState _engage;
        private float _timer;
        private Color _originalColor;
        private SpriteRenderer _spriteRenderer;

        public TelegraphSubState(EnemyBrain brain, EngageState engage)
        {
            _brain = brain;
            _engage = engage;
        }

        public void OnEnter()
        {
            var attack = _engage.SelectedAttack;

            // Duration: AttackDataSO > legacy EnemyStatsSO
            _timer = attack != null ? attack.TelegraphDuration : _brain.Stats.TelegraphDuration;

            // Stop all movement — committing to attack
            _brain.Entity.StopMovement();

            // Visual signal: tint the sprite to warn the player
            _spriteRenderer = _brain.Entity.GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                _spriteRenderer.color = attack != null ? attack.TelegraphColor : Color.red;
            }
        }

        public void OnUpdate(float deltaTime)
        {
            _timer -= deltaTime;

            if (_timer <= 0f)
            {
                _engage.SubStateMachine.TransitionTo(_engage.AttackState);
            }
        }

        public void OnExit()
        {
            // Restore sprite color (Attack state may set its own color)
            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;
        }
    }
}
