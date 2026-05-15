namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Throttles ReferenceOnly afterimage samples so dash visuals stay readable and cheap.
    /// </summary>
    public sealed class ChargeRusherReferenceAfterimageSampler
    {
        private readonly float _interval;
        private bool _hasSample;
        private float _lastSampleTime;

        public ChargeRusherReferenceAfterimageSampler(float interval)
        {
            _interval = interval < 0f ? 0f : interval;
            Reset();
        }

        public bool ShouldEmit(ChargeRusherReferencePhase phase, float now)
        {
            if (phase != ChargeRusherReferencePhase.Dashing)
                return false;

            if (!_hasSample || now - _lastSampleTime >= _interval)
            {
                _hasSample = true;
                _lastSampleTime = now;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _hasSample = false;
            _lastSampleTime = 0f;
        }
    }
}
