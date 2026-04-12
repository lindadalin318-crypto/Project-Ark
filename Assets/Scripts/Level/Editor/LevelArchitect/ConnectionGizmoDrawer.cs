using UnityEditor;
using UnityEngine;
using ProjectArk.Level;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Shared SceneView drawer for room connection gizmos.
    /// Renders thicker dashed/solid lines plus one-way / bidirectional arrowheads.
    /// Arrow tips prefer real door landing points so the gizmo matches traversal semantics.
    /// </summary>
    internal static class ConnectionGizmoDrawer
    {
        private const float DEFAULT_LINE_WIDTH = 4.5f;
        private const float LAYER_LINE_WIDTH = 5.5f;
        private const float DEFAULT_DASH_LENGTH = 1.8f;
        private const float DEFAULT_GAP_LENGTH = 1f;
        private const float MIN_DRAWABLE_LENGTH = 0.25f;
        private const float MIN_ARROW_LENGTH = 0.35f;
        private const float MAX_ARROW_LENGTH = 1.8f;
        private const float ARROW_HALF_ANGLE = 24f;

        /// <summary>
        /// Resolve connection anchors for a door pair.
        /// Endpoints prefer spawn points so arrow tips align with actual landing locations.
        /// </summary>
        public static bool TryGetConnectionAnchors(Room ownerRoom, Door door, out Vector3 fromPos, out Vector3 toPos, out bool isBidirectional)
        {
            fromPos = default;
            toPos = default;
            isBidirectional = false;

            if (ownerRoom == null || door == null || door.TargetRoom == null)
            {
                return false;
            }

            Door reverseDoor = DoorWiringService.FindReverseDoor(door);
            isBidirectional = reverseDoor != null;

            fromPos = ResolveStartAnchor(ownerRoom, door, reverseDoor);
            toPos = ResolveEndAnchor(door, reverseDoor);

            return Vector3.Distance(fromPos, toPos) >= MIN_DRAWABLE_LENGTH;
        }

        /// <summary>
        /// Draw a directional connection line between resolved anchors.
        /// </summary>
        public static void DrawConnection(Vector3 fromPos, Vector3 toPos, Color color, bool isBidirectional, bool isLayerTransition)
        {
            Vector3 delta = toPos - fromPos;
            delta.z = 0f;

            float rawDistance = delta.magnitude;
            if (rawDistance < MIN_DRAWABLE_LENGTH)
            {
                return;
            }

            Vector3 forward = delta / rawDistance;
            float lineWidth = isLayerTransition ? LAYER_LINE_WIDTH : DEFAULT_LINE_WIDTH;
            float arrowLength = GetArrowLength(fromPos, toPos, rawDistance, isBidirectional);

            Handles.color = color;

            if (isLayerTransition)
            {
                Handles.DrawAAPolyLine(lineWidth, fromPos, toPos);
            }
            else
            {
                DrawDashedLine(fromPos, toPos, lineWidth);
            }

            DrawArrowHead(toPos, forward, lineWidth, arrowLength);
            if (isBidirectional)
            {
                DrawArrowHead(fromPos, -forward, lineWidth, arrowLength);
            }

            Handles.color = Color.white;
        }

        private static Vector3 ResolveStartAnchor(Room ownerRoom, Door door, Door reverseDoor)
        {
            if (reverseDoor != null && reverseDoor.TargetSpawnPoint != null)
            {
                return Flatten(reverseDoor.TargetSpawnPoint.position);
            }

            if (door != null)
            {
                return Flatten(door.transform.position);
            }

            return Flatten(ownerRoom.transform.position);
        }

        private static Vector3 ResolveEndAnchor(Door door, Door reverseDoor)
        {
            if (door != null && door.TargetSpawnPoint != null)
            {
                return Flatten(door.TargetSpawnPoint.position);
            }

            if (reverseDoor != null)
            {
                return Flatten(reverseDoor.transform.position);
            }

            if (door != null && door.TargetRoom != null)
            {
                return Flatten(door.TargetRoom.transform.position);
            }

            return Vector3.zero;
        }

        private static Vector3 Flatten(Vector3 position)
        {
            position.z = 0f;
            return position;
        }

        private static float GetArrowLength(Vector3 fromPos, Vector3 toPos, float connectionLength, bool isBidirectional)
        {
            Vector3 samplePoint = (fromPos + toPos) * 0.5f;
            float handleDrivenLength = Mathf.Clamp(HandleUtility.GetHandleSize(samplePoint) * 0.22f, MIN_ARROW_LENGTH, MAX_ARROW_LENGTH);
            float maxAllowedLength = isBidirectional
                ? connectionLength * 0.22f
                : connectionLength * 0.35f;

            return Mathf.Min(handleDrivenLength, maxAllowedLength);
        }

        private static void DrawDashedLine(Vector3 start, Vector3 end, float lineWidth)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance < MIN_DRAWABLE_LENGTH)
            {
                return;
            }

            Vector3 direction = delta / distance;
            float dashLength = Mathf.Min(DEFAULT_DASH_LENGTH, distance);
            float gapLength = DEFAULT_GAP_LENGTH;
            float travelled = 0f;

            while (travelled < distance)
            {
                float segmentEnd = Mathf.Min(travelled + dashLength, distance);
                Vector3 segmentStartPos = start + direction * travelled;
                Vector3 segmentEndPos = start + direction * segmentEnd;
                Handles.DrawAAPolyLine(lineWidth, segmentStartPos, segmentEndPos);
                travelled += dashLength + gapLength;
            }
        }

        private static void DrawArrowHead(Vector3 tip, Vector3 facingDirection, float lineWidth, float arrowLength)
        {
            if (facingDirection.sqrMagnitude <= Mathf.Epsilon || arrowLength <= 0f)
            {
                return;
            }

            Vector3 normalizedFacing = facingDirection.normalized;
            Quaternion leftRotation = Quaternion.AngleAxis(180f - ARROW_HALF_ANGLE, Vector3.forward);
            Quaternion rightRotation = Quaternion.AngleAxis(180f + ARROW_HALF_ANGLE, Vector3.forward);

            Vector3 leftWing = tip + leftRotation * normalizedFacing * arrowLength;
            Vector3 rightWing = tip + rightRotation * normalizedFacing * arrowLength;

            Handles.DrawAAPolyLine(lineWidth, leftWing, tip, rightWing);
        }
    }
}
