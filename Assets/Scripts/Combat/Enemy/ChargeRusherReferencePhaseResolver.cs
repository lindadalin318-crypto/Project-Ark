namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Visual-only phase used by the ReferenceOnly Minishoot ChargeRusher skin.
    /// </summary>
    public enum ChargeRusherReferencePhase
    {
        Idle,
        Telegraph,
        Dashing,
        Recovery
    }

    /// <summary>
    /// Resolves a coarse visual phase from the enemy brain state name and elapsed state time.
    /// This does not drive gameplay; it only supports ReferenceOnly presentation.
    /// </summary>
    public static class ChargeRusherReferencePhaseResolver
    {
        public static ChargeRusherReferencePhase Resolve(
            string currentStateName,
            float elapsedInState,
            float telegraphDuration,
            float dashDuration,
            float recoveryDuration)
        {
            if (currentStateName != "ChargeState")
                return ChargeRusherReferencePhase.Idle;

            float safeElapsed = elapsedInState < 0f ? 0f : elapsedInState;
            float safeTelegraph = telegraphDuration < 0f ? 0f : telegraphDuration;
            float safeDash = dashDuration < 0f ? 0f : dashDuration;
            float safeRecovery = recoveryDuration < 0f ? 0f : recoveryDuration;

            if (safeElapsed < safeTelegraph)
                return ChargeRusherReferencePhase.Telegraph;

            if (safeElapsed < safeTelegraph + safeDash)
                return ChargeRusherReferencePhase.Dashing;

            if (safeElapsed < safeTelegraph + safeDash + safeRecovery)
                return ChargeRusherReferencePhase.Recovery;

            return ChargeRusherReferencePhase.Idle;
        }
    }
}
