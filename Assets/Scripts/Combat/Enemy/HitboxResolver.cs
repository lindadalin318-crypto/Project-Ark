using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Static utility for resolving melee hitbox queries based on <see cref="AttackDataSO"/> shape settings.
    /// All queries use NonAlloc variants with a shared static buffer — zero GC allocation.
    /// </summary>
    public static class HitboxResolver
    {
        // Shared buffer: 8 targets is more than enough for melee overlap detection
        private static readonly Collider2D[] _buffer = new Collider2D[8];

        /// <summary>
        /// Perform a physics overlap query matching the <see cref="AttackDataSO"/>'s hitbox shape.
        /// Returns the number of colliders found (results stored in the shared buffer).
        /// </summary>
        /// <param name="attack">Attack data defining shape, radius, length, angle, offset.</param>
        /// <param name="origin">World position of the attacker.</param>
        /// <param name="facingDirection">Normalized facing direction of the attacker.</param>
        /// <param name="layerMask">Physics layer mask to filter (e.g. Player layer).</param>
        /// <param name="results">Output span pointing into the shared buffer. Valid up to returned count.</param>
        /// <returns>Number of colliders detected.</returns>
        public static int Resolve(AttackDataSO attack, Vector2 origin, Vector2 facingDirection,
                                  int layerMask, out Collider2D[] results)
        {
            results = _buffer;

            // Calculate hitbox center (offset along facing direction)
            Vector2 center = origin + facingDirection.normalized * attack.HitboxOffset;

            switch (attack.Shape)
            {
                case HitboxShape.Circle:
                    return ResolveCircle(center, attack.HitboxRadius, layerMask);

                case HitboxShape.Box:
                    return ResolveBox(center, facingDirection, attack.HitboxRadius, attack.HitboxLength, layerMask);

                case HitboxShape.Cone:
                    return ResolveCone(center, facingDirection, attack.HitboxRadius, attack.HitboxAngle, layerMask);

                default:
                    return 0;
            }
        }

        // ──────────────────── Circle ────────────────────

        private static int ResolveCircle(Vector2 center, float radius, int layerMask)
        {
            return Physics2D.OverlapCircleNonAlloc(center, radius, _buffer, layerMask);
        }

        // ──────────────────── Box ────────────────────

        private static int ResolveBox(Vector2 center, Vector2 facingDirection,
                                      float halfWidth, float halfLength, int layerMask)
        {
            // Box center is offset forward by halfLength along facing direction
            Vector2 boxCenter = center + facingDirection.normalized * halfLength;
            Vector2 size = new Vector2(halfWidth * 2f, halfLength * 2f);

            // Calculate rotation angle from facing direction
            float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg - 90f;

            return Physics2D.OverlapBoxNonAlloc(boxCenter, size, angle, _buffer, layerMask);
        }

        // ──────────────────── Cone (Circle + Angle Filter) ────────────────────

        private static int ResolveCone(Vector2 center, Vector2 facingDirection,
                                       float radius, float halfAngle, int layerMask)
        {
            // Step 1: broad-phase circle query
            int rawCount = Physics2D.OverlapCircleNonAlloc(center, radius, _buffer, layerMask);

            if (rawCount == 0) return 0;

            // Step 2: narrow-phase angle filter
            int validCount = 0;
            Vector2 forward = facingDirection.normalized;

            for (int i = 0; i < rawCount; i++)
            {
                Vector2 toTarget = ((Vector2)_buffer[i].transform.position - center).normalized;
                float angle = Vector2.Angle(forward, toTarget);

                if (angle <= halfAngle)
                {
                    // Compact valid results to the front of the buffer
                    if (validCount != i)
                        _buffer[validCount] = _buffer[i];
                    validCount++;
                }
            }

            return validCount;
        }

#if UNITY_EDITOR
        // ──────────────────── Editor Gizmo Helpers ────────────────────

        /// <summary>
        /// Draw the hitbox shape in the Scene view. Call from OnDrawGizmosSelected().
        /// </summary>
        public static void DrawGizmo(AttackDataSO attack, Vector2 origin, Vector2 facingDirection)
        {
            if (attack == null) return;

            Vector2 center = origin + facingDirection.normalized * attack.HitboxOffset;

            switch (attack.Shape)
            {
                case HitboxShape.Circle:
                    Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
                    Gizmos.DrawWireSphere(center, attack.HitboxRadius);
                    break;

                case HitboxShape.Box:
                    Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.4f);
                    Vector2 boxCenter = center + facingDirection.normalized * attack.HitboxLength;
                    Vector3 size = new Vector3(attack.HitboxRadius * 2f, attack.HitboxLength * 2f, 0f);
                    // Note: Gizmos.DrawWireCube doesn't support rotation, approximate with a cube
                    Gizmos.DrawWireCube(boxCenter, size);
                    break;

                case HitboxShape.Cone:
                    Gizmos.color = new Color(1f, 1f, 0.3f, 0.4f);
                    Gizmos.DrawWireSphere(center, attack.HitboxRadius);
                    // Draw cone edges
                    Vector2 forward = facingDirection.normalized;
                    float rad = attack.HitboxAngle * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(rad);
                    float sin = Mathf.Sin(rad);
                    Vector2 leftEdge = new Vector2(
                        forward.x * cos - forward.y * sin,
                        forward.x * sin + forward.y * cos);
                    Vector2 rightEdge = new Vector2(
                        forward.x * cos + forward.y * sin,
                        -forward.x * sin + forward.y * cos);
                    Gizmos.DrawLine(center, center + leftEdge * attack.HitboxRadius);
                    Gizmos.DrawLine(center, center + rightEdge * attack.HitboxRadius);
                    break;
            }
        }
#endif
    }
}
