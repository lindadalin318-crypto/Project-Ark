using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Test Light Sail behavior: simplified "Doppler Effect".
    /// Damage multiplier scales linearly with ship speed.
    /// At max speed: damage × 1.5. At zero speed: damage × 1.0.
    /// </summary>
    public class SpeedDamageSail : LightSailBehavior
    {
        private const float MAX_BONUS = 0.5f; // +50% at max speed

        private float _damageMultiplier = 1f;

        public override void Tick(float deltaTime, StarChartContext context)
        {
            if (!IsActive)
            {
                _damageMultiplier = 1f;
                return;
            }

            float normalizedSpeed = context.Motor.NormalizedSpeed;
            _damageMultiplier = 1f + normalizedSpeed * MAX_BONUS;
        }

        public override void ModifyProjectileParams(ref ProjectileParams parms)
        {
            if (!IsActive || Mathf.Approximately(_damageMultiplier, 1f)) return;
            parms = parms.WithDamageMultiplied(_damageMultiplier);
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
            _damageMultiplier = 1f;
        }
    }
}
