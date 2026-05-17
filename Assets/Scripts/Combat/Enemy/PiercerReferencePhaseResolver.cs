namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Visual-only phase used by the ReferenceOnly Minishoot Piercer skin.
    /// </summary>
    public enum PiercerReferencePhase
    {
        Idle,
        Pause,
        Anticipation,
        Dashing,
        Recovery
    }

    /// <summary>
    /// Immutable ReferenceOnly phase timing snapshot used by debug harnesses and Scene labels.
    /// </summary>
    public readonly struct PiercerReferencePhaseSnapshot
    {
        public PiercerReferencePhaseSnapshot(
            PiercerReferencePhase phase,
            float phaseProgress,
            float phaseRemainingTime,
            float cycleElapsedTime,
            float cycleDuration,
            string detailedLabel)
        {
            Phase = phase;
            PhaseProgress = phaseProgress;
            PhaseRemainingTime = phaseRemainingTime;
            CycleElapsedTime = cycleElapsedTime;
            CycleDuration = cycleDuration;
            DetailedLabel = detailedLabel;
        }

        public PiercerReferencePhase Phase { get; }
        public float PhaseProgress { get; }
        public float PhaseRemainingTime { get; }
        public float CycleElapsedTime { get; }
        public float CycleDuration { get; }
        public string DetailedLabel { get; }
    }

    /// <summary>
    /// Resolves the Minishoot AICharge-style presentation window from the current Ark brain state.
    /// This is ReferenceOnly presentation logic and does not drive gameplay.
    /// </summary>
    public static class PiercerReferencePhaseResolver
    {
        private const string ArkChargeStateName = "ChargeState";
        private const string PiercerReferenceChargeStateName = "PiercerReferenceChargeState";

        public static PiercerReferencePhase Resolve(
            string currentStateName,
            float elapsedInState,
            float pauseDuration,
            float anticipationDuration,
            float dashDuration,
            float recoveryDuration)
        {
            if (!IsChargeState(currentStateName))
                return PiercerReferencePhase.Idle;

            float safeElapsed = elapsedInState < 0f ? 0f : elapsedInState;
            float safePause = pauseDuration < 0f ? 0f : pauseDuration;
            float safeAnticipation = anticipationDuration < 0f ? 0f : anticipationDuration;
            float safeDash = dashDuration < 0f ? 0f : dashDuration;
            float safeRecovery = recoveryDuration < 0f ? 0f : recoveryDuration;

            return ResolveChargeWindow(safeElapsed, safePause, safeAnticipation, safeDash, safeRecovery);
        }

        public static PiercerReferencePhase ResolveLooping(
            string currentStateName,
            float elapsedInState,
            float pauseDuration,
            float anticipationDuration,
            float dashDuration,
            float recoveryDuration,
            float idleGapDuration)
        {
            if (!IsChargeState(currentStateName))
                return PiercerReferencePhase.Idle;

            float safePause = pauseDuration < 0f ? 0f : pauseDuration;
            float safeAnticipation = anticipationDuration < 0f ? 0f : anticipationDuration;
            float safeDash = dashDuration < 0f ? 0f : dashDuration;
            float safeRecovery = recoveryDuration < 0f ? 0f : recoveryDuration;
            float safeIdleGap = idleGapDuration < 0f ? 0f : idleGapDuration;
            float chargeWindowDuration = safePause + safeAnticipation + safeDash + safeRecovery;
            float cycleDuration = chargeWindowDuration + safeIdleGap;

            if (cycleDuration <= 0f)
                return PiercerReferencePhase.Idle;

            float safeElapsed = elapsedInState < 0f ? 0f : elapsedInState;
            float cycleElapsed = safeElapsed % cycleDuration;

            if (cycleElapsed >= chargeWindowDuration)
                return PiercerReferencePhase.Idle;

            return ResolveChargeWindow(cycleElapsed, safePause, safeAnticipation, safeDash, safeRecovery);
        }

        public static string FormatDebugLabel(PiercerReferencePhase phase, float elapsedInState, bool loopPreview)
        {
            float safeElapsed = elapsedInState < 0f ? 0f : elapsedInState;
            string loopState = loopPreview ? "on" : "off";
            return $"Piercer REF | {phase} | t={safeElapsed:0.00}s | loop={loopState}";
        }

        public static PiercerReferencePhaseSnapshot ResolveSnapshot(
            string currentStateName,
            float elapsedInState,
            float pauseDuration,
            float anticipationDuration,
            float dashDuration,
            float recoveryDuration,
            float idleGapDuration,
            bool loopPreview)
        {
            PiercerReferencePhase phase = loopPreview
                ? ResolveLooping(currentStateName, elapsedInState, pauseDuration, anticipationDuration, dashDuration, recoveryDuration, idleGapDuration)
                : Resolve(currentStateName, elapsedInState, pauseDuration, anticipationDuration, dashDuration, recoveryDuration);
            float phaseProgress = ResolvePhaseProgress(currentStateName, elapsedInState, pauseDuration, anticipationDuration, dashDuration, recoveryDuration, idleGapDuration, loopPreview);
            float phaseRemainingTime = ResolvePhaseRemainingTime(currentStateName, elapsedInState, pauseDuration, anticipationDuration, dashDuration, recoveryDuration, idleGapDuration, loopPreview);
            float cycleElapsedTime = ResolveCycleElapsedTime(currentStateName, elapsedInState, pauseDuration, anticipationDuration, dashDuration, recoveryDuration, idleGapDuration, loopPreview);
            float cycleDuration = ResolveCycleDuration(pauseDuration, anticipationDuration, dashDuration, recoveryDuration, idleGapDuration);
            string detailedLabel = FormatDetailedDebugLabel(phase, elapsedInState, phaseProgress, phaseRemainingTime, cycleElapsedTime, cycleDuration, loopPreview);

            return new PiercerReferencePhaseSnapshot(phase, phaseProgress, phaseRemainingTime, cycleElapsedTime, cycleDuration, detailedLabel);
        }

        public static float ResolvePhaseProgress(
            string currentStateName,
            float elapsedInState,
            float pauseDuration,
            float anticipationDuration,
            float dashDuration,
            float recoveryDuration,
            float idleGapDuration,
            bool loopPreview)
        {
            if (!IsChargeState(currentStateName))
                return 0f;

            float safePause = pauseDuration < 0f ? 0f : pauseDuration;
            float safeAnticipation = anticipationDuration < 0f ? 0f : anticipationDuration;
            float safeDash = dashDuration < 0f ? 0f : dashDuration;
            float safeRecovery = recoveryDuration < 0f ? 0f : recoveryDuration;
            float safeIdleGap = idleGapDuration < 0f ? 0f : idleGapDuration;
            float safeElapsed = elapsedInState < 0f ? 0f : elapsedInState;

            if (loopPreview)
            {
                float chargeWindowDuration = safePause + safeAnticipation + safeDash + safeRecovery;
                float cycleDuration = chargeWindowDuration + safeIdleGap;

                if (cycleDuration <= 0f)
                    return 0f;

                safeElapsed %= cycleDuration;
            }

            return ResolveChargeWindowProgress(safeElapsed, safePause, safeAnticipation, safeDash, safeRecovery);
        }

        public static float ResolvePhaseRemainingTime(
            string currentStateName,
            float elapsedInState,
            float pauseDuration,
            float anticipationDuration,
            float dashDuration,
            float recoveryDuration,
            float idleGapDuration,
            bool loopPreview)
        {
            if (!IsChargeState(currentStateName))
                return 0f;

            float safePause = pauseDuration < 0f ? 0f : pauseDuration;
            float safeAnticipation = anticipationDuration < 0f ? 0f : anticipationDuration;
            float safeDash = dashDuration < 0f ? 0f : dashDuration;
            float safeRecovery = recoveryDuration < 0f ? 0f : recoveryDuration;
            float safeIdleGap = idleGapDuration < 0f ? 0f : idleGapDuration;
            float safeElapsed = elapsedInState < 0f ? 0f : elapsedInState;

            if (loopPreview)
            {
                float chargeWindowDuration = safePause + safeAnticipation + safeDash + safeRecovery;
                float cycleDuration = chargeWindowDuration + safeIdleGap;

                if (cycleDuration <= 0f)
                    return 0f;

                safeElapsed %= cycleDuration;
            }

            return ResolveChargeWindowRemainingTime(safeElapsed, safePause, safeAnticipation, safeDash, safeRecovery);
        }

        public static float ResolveCycleElapsedTime(
            string currentStateName,
            float elapsedInState,
            float pauseDuration,
            float anticipationDuration,
            float dashDuration,
            float recoveryDuration,
            float idleGapDuration,
            bool loopPreview)
        {
            if (!IsChargeState(currentStateName))
                return 0f;

            float safeElapsed = elapsedInState < 0f ? 0f : elapsedInState;
            if (!loopPreview)
                return safeElapsed;

            float cycleDuration = ResolveCycleDuration(pauseDuration, anticipationDuration, dashDuration, recoveryDuration, idleGapDuration);
            if (cycleDuration <= 0f)
                return 0f;

            return safeElapsed % cycleDuration;
        }

        public static float ResolveCycleDuration(
            float pauseDuration,
            float anticipationDuration,
            float dashDuration,
            float recoveryDuration,
            float idleGapDuration)
        {
            float safePause = pauseDuration < 0f ? 0f : pauseDuration;
            float safeAnticipation = anticipationDuration < 0f ? 0f : anticipationDuration;
            float safeDash = dashDuration < 0f ? 0f : dashDuration;
            float safeRecovery = recoveryDuration < 0f ? 0f : recoveryDuration;
            float safeIdleGap = idleGapDuration < 0f ? 0f : idleGapDuration;
            return safePause + safeAnticipation + safeDash + safeRecovery + safeIdleGap;
        }

        public static string FormatDetailedDebugLabel(
            PiercerReferencePhase phase,
            float elapsedInState,
            float phaseProgress,
            float phaseRemainingTime,
            float cycleElapsedTime,
            float cycleDuration,
            bool loopPreview)
        {
            float safeElapsed = elapsedInState < 0f ? 0f : elapsedInState;
            float safeProgress = phaseProgress < 0f ? 0f : phaseProgress > 1f ? 1f : phaseProgress;
            float safeRemainingTime = phaseRemainingTime < 0f ? 0f : phaseRemainingTime;
            float safeCycleElapsedTime = cycleElapsedTime < 0f ? 0f : cycleElapsedTime;
            float safeCycleDuration = cycleDuration < 0f ? 0f : cycleDuration;
            string loopState = loopPreview ? "on" : "off";
            return $"Piercer REF | {phase} | t={safeElapsed:0.00}s | p={safeProgress * 100f:0}% | left={safeRemainingTime:0.00}s | cycle={safeCycleElapsedTime:0.00}/{safeCycleDuration:0.00}s | loop={loopState}";
        }

        private static bool IsChargeState(string currentStateName)
        {
            return currentStateName == ArkChargeStateName || currentStateName == PiercerReferenceChargeStateName;
        }

        private static PiercerReferencePhase ResolveChargeWindow(
            float safeElapsed,
            float safePause,
            float safeAnticipation,
            float safeDash,
            float safeRecovery)
        {
            if (safeElapsed < safePause)
                return PiercerReferencePhase.Pause;

            if (safeElapsed < safePause + safeAnticipation)
                return PiercerReferencePhase.Anticipation;

            if (safeElapsed < safePause + safeAnticipation + safeDash)
                return PiercerReferencePhase.Dashing;

            if (safeElapsed < safePause + safeAnticipation + safeDash + safeRecovery)
                return PiercerReferencePhase.Recovery;

            return PiercerReferencePhase.Idle;
        }

        private static float ResolveChargeWindowProgress(
            float safeElapsed,
            float safePause,
            float safeAnticipation,
            float safeDash,
            float safeRecovery)
        {
            if (safeElapsed < safePause)
                return NormalizeWindowProgress(safeElapsed, safePause);

            if (safeElapsed < safePause + safeAnticipation)
                return NormalizeWindowProgress(safeElapsed - safePause, safeAnticipation);

            if (safeElapsed < safePause + safeAnticipation + safeDash)
                return NormalizeWindowProgress(safeElapsed - safePause - safeAnticipation, safeDash);

            if (safeElapsed < safePause + safeAnticipation + safeDash + safeRecovery)
                return NormalizeWindowProgress(safeElapsed - safePause - safeAnticipation - safeDash, safeRecovery);

            return 0f;
        }

        private static float ResolveChargeWindowRemainingTime(
            float safeElapsed,
            float safePause,
            float safeAnticipation,
            float safeDash,
            float safeRecovery)
        {
            if (safeElapsed < safePause)
                return ResolveWindowRemainingTime(safeElapsed, safePause);

            if (safeElapsed < safePause + safeAnticipation)
                return ResolveWindowRemainingTime(safeElapsed - safePause, safeAnticipation);

            if (safeElapsed < safePause + safeAnticipation + safeDash)
                return ResolveWindowRemainingTime(safeElapsed - safePause - safeAnticipation, safeDash);

            if (safeElapsed < safePause + safeAnticipation + safeDash + safeRecovery)
                return ResolveWindowRemainingTime(safeElapsed - safePause - safeAnticipation - safeDash, safeRecovery);

            return 0f;
        }

        private static float NormalizeWindowProgress(float elapsedInWindow, float windowDuration)
        {
            if (windowDuration <= 0f)
                return 0f;

            if (elapsedInWindow <= 0f)
                return 0f;

            if (elapsedInWindow >= windowDuration)
                return 1f;

            return elapsedInWindow / windowDuration;
        }

        private static float ResolveWindowRemainingTime(float elapsedInWindow, float windowDuration)
        {
            if (windowDuration <= 0f)
                return 0f;

            if (elapsedInWindow <= 0f)
                return windowDuration;

            if (elapsedInWindow >= windowDuration)
                return 0f;

            return windowDuration - elapsedInWindow;
        }
    }
}
