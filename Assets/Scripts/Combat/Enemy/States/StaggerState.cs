using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Stagger state (硬直): triggered when an enemy's poise is broken.
    /// The enemy is immobilized and visually shaken — this is a premium punish window.
    /// After StaggerDuration expires, poise resets and the enemy transitions
    /// back to Chase (if target visible) or Idle.
    /// </summary>
    public class StaggerState : IState
    {
        private readonly EnemyBrain _brain;
        private float _timer;

        // Visual feedback
        private SpriteRenderer _spriteRenderer;
        private Vector3 _originalLocalPos;
        private Transform _transform;
        private Color _originalColor;

        // Shake parameters
        private const float SHAKE_INTENSITY = 0.06f;
        private const float SHAKE_FREQUENCY = 40f;

        public StaggerState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _timer = _brain.Stats.StaggerDuration;

            // Stop all movement
            _brain.Entity.StopMovement();

            // Cache for visual effects
            _transform = _brain.Entity.transform;
            _originalLocalPos = _transform.localPosition;
            _spriteRenderer = _brain.Entity.GetComponent<SpriteRenderer>();

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                // 硬直颜色：明亮的黄色，与前摇红色区分
                _spriteRenderer.color = new Color(1f, 1f, 0.3f, 1f);
            }
        }

        public void OnUpdate(float deltaTime)
        {
            // Shake effect: small oscillation around original position
            float offset = Mathf.Sin(Time.time * SHAKE_FREQUENCY) * SHAKE_INTENSITY;
            _transform.localPosition = _originalLocalPos + new Vector3(offset, 0f, 0f);

            _timer -= deltaTime;

            if (_timer <= 0f)
            {
                // Stagger over: reset poise and transition out
                _brain.Entity.ResetPoise();

                if (_brain.Perception.HasTarget)
                    _brain.StateMachine.TransitionTo(_brain.ChaseState);
                else
                    _brain.StateMachine.TransitionTo(_brain.IdleState);
            }
        }

        public void OnExit()
        {
            // Restore position and color
            if (_transform != null)
                _transform.localPosition = _originalLocalPos;

            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;
        }
    }
}
