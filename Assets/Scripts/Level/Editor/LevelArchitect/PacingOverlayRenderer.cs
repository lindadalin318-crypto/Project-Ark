using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Renders pacing visualization overlays on the SceneView:
    /// - Pacing Overlay: combat intensity color gradient + door lock icons
    /// - Critical Path: BFS shortest path from entry to boss
    /// - Lock-Key Graph: key pickup → locked door dependency arrows
    /// </summary>
    public static class PacingOverlayRenderer
    {
        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Draw all enabled overlays. Called from RoomBlockoutRenderer or LevelArchitectWindow.
        /// </summary>
        public static void DrawOverlays(Room[] rooms)
        {
            var window = LevelArchitectWindow.Instance;
            if (window == null) return;

            if (window.ShowPacingOverlay)
            {
                DrawPacingOverlay(rooms);
            }

            if (window.ShowCriticalPath)
            {
                DrawCriticalPath(rooms);
            }

            if (window.ShowLockKeyGraph)
            {
                DrawLockKeyGraph(rooms);
            }
        }

        // ──────────────────── Pacing Overlay ────────────────────

        private static void DrawPacingOverlay(Room[] rooms)
        {
            // Calculate max intensity for normalization
            float maxIntensity = 0f;
            var intensityMap = new Dictionary<Room, float>();

            foreach (var room in rooms)
            {
                if (room == null) continue;

                float intensity = CalculateCombatIntensity(room);
                intensityMap[room] = intensity;
                if (intensity > maxIntensity) maxIntensity = intensity;
            }

            if (maxIntensity <= 0) maxIntensity = 1f;

            foreach (var room in rooms)
            {
                if (room == null) continue;

                var box = room.GetComponent<BoxCollider2D>();
                if (box == null) continue;

                Rect worldRect = LevelArchitectWindow.GetRoomWorldRect(room, box);

                // Intensity color gradient (green → yellow → red)
                float normalizedIntensity = intensityMap.ContainsKey(room) ?
                    intensityMap[room] / maxIntensity : 0f;

                Color intensityColor = Color.Lerp(
                    new Color(0.2f, 0.8f, 0.2f, 0.3f),    // green (safe)
                    new Color(0.9f, 0.2f, 0.1f, 0.3f),     // red (intense)
                    normalizedIntensity
                );

                if (normalizedIntensity > 0.3f && normalizedIntensity < 0.7f)
                {
                    intensityColor = Color.Lerp(
                        new Color(0.2f, 0.8f, 0.2f, 0.3f),
                        new Color(0.9f, 0.9f, 0.2f, 0.3f),
                        normalizedIntensity * 2f
                    );
                }

                Vector3[] corners = new Vector3[]
                {
                    new Vector3(worldRect.xMin, worldRect.yMin, 0),
                    new Vector3(worldRect.xMax, worldRect.yMin, 0),
                    new Vector3(worldRect.xMax, worldRect.yMax, 0),
                    new Vector3(worldRect.xMin, worldRect.yMax, 0)
                };

                Handles.DrawSolidRectangleWithOutline(corners, intensityColor, Color.clear);

                // Intensity label
                if (intensityMap.ContainsKey(room) && intensityMap[room] > 0)
                {
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 10,
                        normal = { textColor = Color.white }
                    };

                    string intensityText = $"⚔ {intensityMap[room]:F0}";
                    Handles.Label(new Vector3(worldRect.center.x, worldRect.center.y - 1f, 0), intensityText, labelStyle);
                }

                // Door lock icons
                DrawDoorLockIcons(room);
            }
        }

        private static float CalculateCombatIntensity(Room room)
        {
            if (room.Data == null) return 0f;

            var encounter = room.Data.Encounter;
            if (encounter == null) return 0f;

            float totalEnemies = 0;
            foreach (var wave in encounter.Waves)
            {
                totalEnemies += wave.TotalEnemyCount;
            }

            // Weight: enemies + waves bonus
            return totalEnemies + encounter.WaveCount * 2f;
        }

        private static void DrawDoorLockIcons(Room room)
        {
            var doors = room.GetComponentsInChildren<Door>(true);
            foreach (var door in doors)
            {
                if (door == null) continue;

                Vector3 pos = door.transform.position;
                string icon = "";

                switch (door.CurrentState)
                {
                    case DoorState.Open:
                        continue; // No icon for open doors
                    case DoorState.Locked_Key:
                        icon = "🔑";
                        break;
                    case DoorState.Locked_Combat:
                        icon = "⚔";
                        break;
                    case DoorState.Locked_Ability:
                        icon = "✦";
                        break;
                    case DoorState.Locked_Schedule:
                        icon = "⏰";
                        break;
                }

                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    normal = { textColor = new Color(1f, 0.8f, 0.2f, 1f) }
                };

                Handles.Label(pos + Vector3.up * 0.5f, icon, style);
            }
        }

        // ──────────────────── Critical Path ────────────────────

        private static void DrawCriticalPath(Room[] rooms)
        {
            // Build adjacency graph
            var graph = BuildAdjacencyGraph(rooms);

            // Find entry room (from RoomManager)
            Room entryRoom = FindEntryRoom(rooms);
            Room bossRoom = FindBossRoom(rooms);

            if (entryRoom == null || bossRoom == null) return;

            // BFS from entry to boss
            var path = BFS(entryRoom, bossRoom, graph);

            if (path == null || path.Count < 2) return;

            // Draw critical path as thick line
            Handles.color = new Color(1f, 0.8f, 0.1f, 0.9f);
            for (int i = 0; i < path.Count - 1; i++)
            {
                Handles.DrawAAPolyLine(5f, path[i].transform.position, path[i + 1].transform.position);
            }

            // Draw all other connections as dotted lines
            var pathSet = new HashSet<Room>(path);
            var drawnPairs = new HashSet<string>();

            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null || door.TargetRoom == null) continue;

                    string pairKey = GetPairKey(room, door.TargetRoom);
                    if (drawnPairs.Contains(pairKey)) continue;
                    drawnPairs.Add(pairKey);

                    // Skip if both rooms are on the critical path (already drawn)
                    bool isOnPath = false;
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        if ((path[i] == room && path[i + 1] == door.TargetRoom) ||
                            (path[i] == door.TargetRoom && path[i + 1] == room))
                        {
                            isOnPath = true;
                            break;
                        }
                    }

                    if (!isOnPath)
                    {
                        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
                        Handles.DrawDottedLine(room.transform.position, door.TargetRoom.transform.position, 4f);
                    }
                }
            }

            Handles.color = Color.white;

            // Label entry and boss
            var entryStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.2f, 0.9f, 0.2f, 1f) }
            };
            Handles.Label(entryRoom.transform.position + Vector3.up * 3f, "▶ ENTRY", entryStyle);

            var bossStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.9f, 0.2f, 0.2f, 1f) }
            };
            Handles.Label(bossRoom.transform.position + Vector3.up * 3f, "☠ BOSS", bossStyle);
        }

        // ──────────────────── Lock-Key Graph ────────────────────

        private static void DrawLockKeyGraph(Room[] rooms)
        {
            // Collect all locked doors with key requirements
            var lockDoors = new List<(Door door, Room ownerRoom)>();

            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null) continue;
                    if (!string.IsNullOrEmpty(door.RequiredKeyID))
                    {
                        lockDoors.Add((door, room));
                    }
                }
            }

            if (lockDoors.Count == 0) return;

            // For each locked door, draw a colored arrow
            // Use different colors for different key IDs
            var keyColors = new Dictionary<string, Color>();
            Color[] palette = new Color[]
            {
                new Color(0.9f, 0.2f, 0.2f, 0.8f), // red
                new Color(0.2f, 0.4f, 0.9f, 0.8f), // blue
                new Color(0.9f, 0.9f, 0.2f, 0.8f), // yellow
                new Color(0.2f, 0.9f, 0.2f, 0.8f), // green
                new Color(0.9f, 0.4f, 0.9f, 0.8f), // magenta
                new Color(0.2f, 0.9f, 0.9f, 0.8f), // cyan
            };
            int colorIndex = 0;

            foreach (var (door, ownerRoom) in lockDoors)
            {
                string keyID = door.RequiredKeyID;

                if (!keyColors.ContainsKey(keyID))
                {
                    keyColors[keyID] = palette[colorIndex % palette.Length];
                    colorIndex++;
                }

                Color arrowColor = keyColors[keyID];
                Handles.color = arrowColor;

                // Draw arrow from lock position showing key requirement
                Vector3 doorPos = door.transform.position;

                // Draw the key ID label
                var keyStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 11,
                    normal = { textColor = arrowColor }
                };

                Handles.Label(doorPos + Vector3.down * 0.5f, $"🔑{keyID}", keyStyle);

                // Draw a diamond at the lock position
                float diamondSize = 0.6f;
                Vector3[] diamond = new Vector3[]
                {
                    doorPos + Vector3.up * diamondSize,
                    doorPos + Vector3.right * diamondSize,
                    doorPos + Vector3.down * diamondSize,
                    doorPos + Vector3.left * diamondSize
                };
                Handles.DrawSolidRectangleWithOutline(diamond, arrowColor * 0.5f, arrowColor);
            }

            Handles.color = Color.white;
        }

        // ──────────────────── Graph Utilities ────────────────────

        private static Dictionary<Room, List<Room>> BuildAdjacencyGraph(Room[] rooms)
        {
            var graph = new Dictionary<Room, List<Room>>();

            foreach (var room in rooms)
            {
                if (room == null) continue;
                if (!graph.ContainsKey(room)) graph[room] = new List<Room>();

                var doors = room.GetComponentsInChildren<Door>(true);
                foreach (var door in doors)
                {
                    if (door == null || door.TargetRoom == null) continue;
                    if (!graph[room].Contains(door.TargetRoom))
                    {
                        graph[room].Add(door.TargetRoom);
                    }
                }
            }

            return graph;
        }

        private static List<Room> BFS(Room start, Room target, Dictionary<Room, List<Room>> graph)
        {
            var visited = new HashSet<Room>();
            var queue = new Queue<List<Room>>();

            queue.Enqueue(new List<Room> { start });
            visited.Add(start);

            while (queue.Count > 0)
            {
                var currentPath = queue.Dequeue();
                var current = currentPath[currentPath.Count - 1];

                if (current == target)
                    return currentPath;

                if (!graph.ContainsKey(current)) continue;

                foreach (var neighbor in graph[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        var newPath = new List<Room>(currentPath) { neighbor };
                        queue.Enqueue(newPath);
                    }
                }
            }

            return null; // No path found
        }

        private static Room FindEntryRoom(Room[] rooms)
        {
            var roomManager = Object.FindAnyObjectByType<RoomManager>();
            if (roomManager != null)
            {
                var serialized = new SerializedObject(roomManager);
                var startingRoom = serialized.FindProperty("_startingRoom").objectReferenceValue as Room;
                if (startingRoom != null) return startingRoom;
            }

            // Fallback: first Safe room
            foreach (var room in rooms)
            {
                if (room != null && room.Type == RoomType.Safe) return room;
            }

            // Fallback: first room
            return rooms.Length > 0 ? rooms[0] : null;
        }

        private static Room FindBossRoom(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room != null && room.Type == RoomType.Boss) return room;
            }
            return null;
        }

        private static string GetPairKey(Room a, Room b)
        {
            string idA = a.GetInstanceID().ToString();
            string idB = b.GetInstanceID().ToString();
            return string.Compare(idA, idB) < 0 ? $"{idA}_{idB}" : $"{idB}_{idA}";
        }
    }
}
