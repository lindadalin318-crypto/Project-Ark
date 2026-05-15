namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Gates ReferenceOnly impact feedback so each dash commitment emits one readable hit cue.
    /// </summary>
    public sealed class ChargeRusherReferenceImpactGate
    {
        private bool _hasTriggeredInDash;

        public void UpdatePhase(ChargeRusherReferencePhase phase)
        {
            if (phase != ChargeRusherReferencePhase.Dashing)
                _hasTriggeredInDash = false;
        }

        public bool TryTrigger(ChargeRusherReferencePhase phase)
        {
            if (phase != ChargeRusherReferencePhase.Dashing || _hasTriggeredInDash)
                return false;

            _hasTriggeredInDash = true;
            return true;
        }

        public void Reset()
        {
            _hasTriggeredInDash = false;
        }
    }
}
