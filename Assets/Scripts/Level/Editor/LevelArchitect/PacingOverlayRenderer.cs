using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1]
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

            if (window.ShowConnectionTypes)
            {
                DrawConnectionTypeOverlay(rooms);
            }
        }

        // ──────────────────── Pacing Overlay ────────────────────

        private static void DrawPacingOverlay(Room[] rooms)
        {
            var intensityMap = new Dictionary<Room, float>();

            foreach (var room in rooms)
            {
                if (room == null) continue;
                intensityMap[room] = CalculateCombatIntensity(room);
            }

            foreach (var room in rooms)
            {
                if (room == null) continue;

                var box = room.GetComponent<BoxCollider2D>();
                if (box == null) continue;

                Rect worldRect = LevelArchitectWindow.GetRoomWorldRect(room, box);
                Color nodeTypeColor = LevelArchitectWindow.GetRoomNodeTypeColor(room.NodeType);
                Color fillColor = nodeTypeColor;
                fillColor.a = 0.22f;

                Vector3[] corners = new Vector3[]
                {
                    new Vector3(worldRect.xMin, worldRect.yMin, 0),
                    new Vector3(worldRect.xMax, worldRect.yMin, 0),
                    new Vector3(worldRect.xMax, worldRect.yMax, 0),
                    new Vector3(worldRect.xMin, worldRect.yMax, 0)
                };

                Handles.DrawSolidRectangleWithOutline(corners, fillColor, Color.clear);
                DrawNodeTypeLabel(room, worldRect, intensityMap.TryGetValue(room, out var intensity) ? intensity : 0f, nodeTypeColor);

                // Door lock icons
                DrawDoorLockIcons(room);
            }
        }

        private static void DrawNodeTypeLabel(Room room, Rect worldRect, float intensity, Color nodeTypeColor)
        {
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };

            string intensitySuffix = intensity > 0f ? $" · ⚔ {intensity:F0}" : string.Empty;
            string labelText = $"{room.NodeType}{intensitySuffix}";

            Handles.Label(new Vector3(worldRect.center.x, worldRect.center.y - 1f, 0), labelText, labelStyle);

            Handles.color = nodeTypeColor;
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(worldRect.xMin, worldRect.yMin, 0),
                new Vector3(worldRect.xMin, worldRect.yMax, 0),
                new Vector3(worldRect.xMax, worldRect.yMax, 0));
            Handles.color = Color.white;
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

        // ──────────────────── Connection Type Overlay ────────────────────

        /// <summary>
        /// [B4.1] Draws door connection lines colored by ConnectionType.
        /// Data source: Door.ConnectionType field (scene MonoBehaviour).
        /// Color mapping: LevelArchitectWindow.GetConnectionTypeColor().
        /// Layer transitions use thicker solid lines; normal connections use dotted lines.
        /// When this overlay is active, it replaces the default gray connections
        /// drawn by RoomBlockoutRenderer.DrawDoorConnections().
        /// </summary>
        private static void DrawConnectionTypeOverlay(Room[] rooms)
        {
            var drawnPairs = new HashSet<string>();

            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null || door.TargetRoom == null) continue;

                    // Avoid duplicate lines for bidirectional door pairs
                    string pairKey = GetPairKey(room, door.TargetRoom);
                    if (drawnPairs.Contains(pairKey)) continue;
                    drawnPairs.Add(pairKey);

                    if (!ConnectionGizmoDrawer.TryGetConnectionAnchors(room, door, out Vector3 fromPos, out Vector3 toPos, out bool isBidirectional))
                    {
                        continue;
                    }

                    bool isLayerTransition = door.Ceremony >= TransitionCeremony.Layer;

                    Color lineColor = LevelArchitectWindow.GetConnectionTypeColor(door.ConnectionType);
                    lineColor.a = isLayerTransition ? 0.95f : 0.8f;

                    ConnectionGizmoDrawer.DrawConnection(
                        fromPos,
                        toPos,
                        lineColor,
                        isBidirectional,
                        isLayerTransition
                    );

                    // Draw ConnectionType label at midpoint
                    Vector3 midPoint = (fromPos + toPos) / 2f;
                    string typeLabel = GetConnectionTypeShortLabel(door.ConnectionType);

                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9,
                        normal = { textColor = lineColor }
                    };
                    Handles.Label(midPoint + Vector3.up * 0.4f, typeLabel, labelStyle);
                }
            }

            Handles.color = Color.white;

            // Draw legend in top-right corner of SceneView
            DrawConnectionTypeLegend();
        }

        private static string GetConnectionTypeShortLabel(ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.Progression: return "PROG";
                case ConnectionType.Return:      return "RET";
                case ConnectionType.Ability:     return "ABL";
                case ConnectionType.Challenge:   return "CHAL";
                case ConnectionType.Identity:    return "IDENT";
                case ConnectionType.Scheduled:   return "SCHED";
                default:                         return "?";
            }
        }

        /// <summary>
        /// Draws a compact color legend in the SceneView for ConnectionType colors.
        /// </summary>
        private static void DrawConnectionTypeLegend()
        {
            Handles.BeginGUI();

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) { Handles.EndGUI(); return; }

            float legendWidth = 130f;
            float lineHeight = 16f;
            float padding = 6f;

            var types = new[]
            {
                (ConnectionType.Progression, "Progression"),
                (ConnectionType.Return,      "Return"),
                (ConnectionType.Ability,     "Ability"),
                (ConnectionType.Challenge,   "Challenge"),
                (ConnectionType.Identity,    "Identity"),
                (ConnectionType.Scheduled,   "Scheduled"),
            };

            float legendHeight = types.Length * lineHeight + padding * 2 + lineHeight; // +1 for header
            float legendX = sceneView.position.width - legendWidth - 12f;
            float legendY = 50f;

            // Background
            Rect bgRect = new Rect(legendX, legendY, legendWidth, legendHeight);
            EditorGUI.DrawRect(bgRect, new Color(0.12f, 0.12f, 0.12f, 0.88f));

            // Header
            var headerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(legendX, legendY + padding, legendWidth, lineHeight), "Connection Types", headerStyle);

            // Entries
            float y = legendY + padding + lineHeight;
            var entryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10
            };

            foreach (var (connType, label) in types)
            {
                Color c = LevelArchitectWindow.GetConnectionTypeColor(connType);

                // Color swatch
                Rect swatchRect = new Rect(legendX + padding, y + 3f, 10f, 10f);
                EditorGUI.DrawRect(swatchRect, c);

                // Label
                entryStyle.normal.textColor = c;
                GUI.Label(new Rect(legendX + padding + 14f, y, legendWidth - padding * 2 - 14f, lineHeight), label, entryStyle);

                y += lineHeight;
            }

            Handles.EndGUI();
        }

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

            // Fallback: first Safe node, then NodeType.Safe room
            foreach (var room in rooms)
            {
                if (room != null && room.NodeType == RoomNodeType.Safe) return room;
            }

            foreach (var room in rooms)
            {
            if (room != null && room.NodeType == RoomNodeType.Safe) return room;
            }

            // Fallback: first room
            return rooms.Length > 0 ? rooms[0] : null;
        }

        private static Room FindBossRoom(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room != null && room.NodeType == RoomNodeType.Boss) return room;
            }

            foreach (var room in rooms)
            {
            if (room != null && room.NodeType == RoomNodeType.Boss) return room;
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
