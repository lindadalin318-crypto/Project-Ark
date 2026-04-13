using System.Collections.Generic;
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
        private const float MIN_PICK_DISTANCE = 0.6f;
        private const float HANDLE_PICK_DISTANCE_FACTOR = 0.08f;

        internal readonly struct ConnectionVisualData
        {
            public readonly Room OwnerRoom;
            public readonly Door Door;
            public readonly Door ReverseDoor;
            public readonly Room ReverseOwnerRoom;
            public readonly Vector3 FromPos;
            public readonly Vector3 ToPos;
            public readonly bool IsBidirectional;
            public readonly bool IsLayerTransition;

            public ConnectionVisualData(
                Room ownerRoom,
                Door door,
                Door reverseDoor,
                Room reverseOwnerRoom,
                Vector3 fromPos,
                Vector3 toPos,
                bool isBidirectional,
                bool isLayerTransition)
            {
                OwnerRoom = ownerRoom;
                Door = door;
                ReverseDoor = reverseDoor;
                ReverseOwnerRoom = reverseOwnerRoom;
                FromPos = fromPos;
                ToPos = toPos;
                IsBidirectional = isBidirectional;
                IsLayerTransition = isLayerTransition;
            }
        }

        /// <summary>
        /// Resolve connection anchors for a door pair.
        /// Endpoints prefer spawn points so arrow tips align with actual landing locations.
        /// </summary>
        public static bool TryGetConnectionAnchors(Room ownerRoom, Door door, out Vector3 fromPos, out Vector3 toPos, out bool isBidirectional)
        {
            fromPos = default;
            toPos = default;
            isBidirectional = false;

            if (!TryResolveConnection(ownerRoom, door, out ConnectionVisualData connection))
            {
                return false;
            }

            fromPos = connection.FromPos;
            toPos = connection.ToPos;
            isBidirectional = connection.IsBidirectional;
            return true;
        }

        /// <summary>
        /// Resolve the full visual payload for a connection so drawing and picking share the same geometry authority.
        /// </summary>
        public static bool TryResolveConnection(Room ownerRoom, Door door, out ConnectionVisualData connection)
        {
            connection = default;
            if (ownerRoom == null || door == null || door.TargetRoom == null)
            {
                return false;
            }

            Door reverseDoor = DoorWiringService.FindReverseDoor(door);
            Room reverseOwnerRoom = reverseDoor != null ? reverseDoor.GetComponentInParent<Room>() : null;
            bool isBidirectional = reverseDoor != null;

            Vector3 fromPos = ResolveStartAnchor(ownerRoom, door, reverseDoor);
            Vector3 toPos = ResolveEndAnchor(door, reverseDoor);
            if (Vector3.Distance(fromPos, toPos) < MIN_DRAWABLE_LENGTH)
            {
                return false;
            }

            connection = new ConnectionVisualData(
                ownerRoom,
                door,
                reverseDoor,
                reverseOwnerRoom,
                fromPos,
                toPos,
                isBidirectional,
                door.Ceremony >= TransitionCeremony.Layer);
            return true;
        }

        /// <summary>
        /// Pick the nearest connection under the mouse using the same anchor geometry used for rendering.
        /// Bidirectional lines resolve to the nearer authored direction half.
        /// </summary>
        public static bool TryPickConnection(Room[] rooms, Vector2 worldPos, out Room ownerRoom, out Door door)
        {
            ownerRoom = null;
            door = null;

            if (rooms == null || rooms.Length == 0)
            {
                return false;
            }

            float bestDistance = float.MaxValue;
            var drawnPairs = new HashSet<string>();

            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var candidateDoor in doors)
                {
                    if (candidateDoor == null || candidateDoor.TargetRoom == null) continue;

                    string pairKey = GetPairKey(room, candidateDoor.TargetRoom);
                    if (!drawnPairs.Add(pairKey))
                    {
                        continue;
                    }

                    if (!TryResolveConnection(room, candidateDoor, out ConnectionVisualData connection))
                    {
                        continue;
                    }

                    float distance = DistancePointToSegment(worldPos, connection.FromPos, connection.ToPos);
                    float pickDistance = GetPickDistance(connection.FromPos, connection.ToPos);
                    if (distance > pickDistance || distance >= bestDistance)
                    {
                        continue;
                    }

                    ResolveDirectionalDoor(connection, worldPos, out Room resolvedOwnerRoom, out Door resolvedDoor);
                    if (resolvedOwnerRoom == null || resolvedDoor == null)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    ownerRoom = resolvedOwnerRoom;
                    door = resolvedDoor;
                }
            }

            return ownerRoom != null && door != null;
        }

        /// <summary>
        /// Draw a directional connection line between resolved anchors.
        /// </summary>
        public static void DrawConnection(Vector3 fromPos, Vector3 toPos, Color color, bool isBidirectional, bool isLayerTransition, float widthMultiplier = 1f)
        {
            Vector3 delta = toPos - fromPos;
            delta.z = 0f;

            float rawDistance = delta.magnitude;
            if (rawDistance < MIN_DRAWABLE_LENGTH)
            {
                return;
            }

            Vector3 forward = delta / rawDistance;
            float lineWidth = (isLayerTransition ? LAYER_LINE_WIDTH : DEFAULT_LINE_WIDTH) * Mathf.Max(0.1f, widthMultiplier);
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

        private static void ResolveDirectionalDoor(ConnectionVisualData connection, Vector2 worldPos, out Room ownerRoom, out Door door)
        {
            ownerRoom = connection.OwnerRoom;
            door = connection.Door;

            if (!connection.IsBidirectional || connection.ReverseDoor == null || connection.ReverseOwnerRoom == null)
            {
                return;
            }

            float progress = GetSegmentProgress(worldPos, connection.FromPos, connection.ToPos);
            if (progress < 0.5f)
            {
                ownerRoom = connection.ReverseOwnerRoom;
                door = connection.ReverseDoor;
            }
        }

        private static string GetPairKey(Room a, Room b)
        {
            int idA = a != null ? a.GetInstanceID() : 0;
            int idB = b != null ? b.GetInstanceID() : 0;
            return idA < idB ? $"{idA}_{idB}" : $"{idB}_{idA}";
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

        private static float GetPickDistance(Vector3 fromPos, Vector3 toPos)
        {
            Vector3 samplePoint = (fromPos + toPos) * 0.5f;
            return Mathf.Max(MIN_PICK_DISTANCE, HandleUtility.GetHandleSize(samplePoint) * HANDLE_PICK_DISTANCE_FACTOR);
        }

        private static float DistancePointToSegment(Vector2 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector2 start = segmentStart;
            Vector2 end = segmentEnd;
            Vector2 delta = end - start;
            float sqrLength = delta.sqrMagnitude;
            if (sqrLength <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, delta) / sqrLength);
            Vector2 closestPoint = start + delta * t;
            return Vector2.Distance(point, closestPoint);
        }

        private static float GetSegmentProgress(Vector2 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector2 start = segmentStart;
            Vector2 end = segmentEnd;
            Vector2 delta = end - start;
            float sqrLength = delta.sqrMagnitude;
            if (sqrLength <= Mathf.Epsilon)
            {
                return 1f;
            }

            return Mathf.Clamp01(Vector2.Dot(point - start, delta) / sqrLength);
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
