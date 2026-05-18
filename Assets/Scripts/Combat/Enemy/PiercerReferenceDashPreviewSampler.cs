using System.Globalization;
using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Samples ReferenceOnly dash preview offsets from Piercer phase timing.
    /// This helper is pure presentation logic and never drives gameplay collision or damage.
    /// </summary>
    public sealed class PiercerReferenceDashPreviewSampler
    {
        public Vector3 SampleOffset(PiercerReferencePhaseSnapshot snapshot, Vector3 direction, float distance)
        {
            if (snapshot.Phase != PiercerReferencePhase.Dashing)
                return Vector3.zero;

            float safeDistance = distance < 0f ? 0f : distance;
            if (safeDistance <= 0f)
                return Vector3.zero;

            Vector3 safeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
            float progress = Clamp01(snapshot.PhaseProgress);
            float easedProgress = EaseOutCubic(progress);
            return safeDirection * safeDistance * easedProgress;
        }

        public Vector3 ResolveDirection(Vector3 direction, Quaternion ownerRotation, bool useLocalDirection)
        {
            Vector3 safeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
            if (!useLocalDirection)
                return safeDirection;

            Vector3 worldDirection = ownerRotation * safeDirection;
            return worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : Vector3.right;
        }

        public string FormatReadout(PiercerReferencePhaseSnapshot snapshot, Vector3 offset, bool previewEnabled, bool useLocalDirection)
        {
            string previewState = previewEnabled ? "ON" : "OFF";
            string directionMode = useLocalDirection ? "Local" : "World";
            float progressPercent = Clamp01(snapshot.PhaseProgress) * 100f;

            return string.Format(
                CultureInfo.InvariantCulture,
                "Preview: {0}\nMode: {1}\nPhase: {2}\nProgress: {3:0}%\nOffset: ({4:0.00}, {5:0.00}, {6:0.00})",
                previewState,
                directionMode,
                snapshot.Phase,
                progressPercent,
                offset.x,
                offset.y,
                offset.z);
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }

        private static float EaseOutCubic(float value)
        {
            float inverted = 1f - value;
            return 1f - inverted * inverted * inverted;
        }
    }
}
