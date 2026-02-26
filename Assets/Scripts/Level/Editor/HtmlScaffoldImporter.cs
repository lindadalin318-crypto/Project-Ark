using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    // ═══════════════════════════════════════════════════════════════
    //  Intermediate deserialization data classes for HTML JSON
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Top-level container for an HTML LevelDesigner JSON export.
    /// </summary>
    internal class HtmlLevelData
    {
        public string LevelName;
        public string Description;
        public List<HtmlRoom> Rooms = new List<HtmlRoom>();
        public List<HtmlConnection> Connections = new List<HtmlConnection>();
        public List<HtmlDoorLink> DoorLinks = new List<HtmlDoorLink>();
    }

    /// <summary>
    /// A room parsed from the HTML JSON. Comment-only objects will have Id == null.
    /// </summary>
    internal class HtmlRoom
    {
        public string Id;
        public string Name;
        public string Type;
        public int Floor;
        public float[] Position; // [x, y]
        public float[] Size;     // [w, h]
        public List<HtmlElement> Elements = new List<HtmlElement>();
        public bool IsComment;   // true if this is a comment-only entry
    }

    /// <summary>
    /// A connection between two rooms parsed from the HTML JSON.
    /// </summary>
    internal class HtmlConnection
    {
        public string From;
        public string To;
        public string FromDir;
        public string ToDir;
        public bool IsComment;
    }

    /// <summary>
    /// A door link entry parsed from the HTML JSON.
    /// </summary>
    internal class HtmlDoorLink
    {
        public string RoomId;
        public string EntryDir;
        public int DoorIndex;
    }

    /// <summary>
    /// A single element inside a room parsed from the HTML JSON.
    /// </summary>
    internal class HtmlElement
    {
        public string Type;
        public float[] Position; // [x, y] relative to room top-left
    }

    // ═══════════════════════════════════════════════════════════════
    //  Editor Window: HtmlScaffoldImporterWindow
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// EditorWindow that imports HTML LevelDesigner JSON into a LevelScaffoldData asset.
    /// Menu: Window > ProjectArk > Import HTML Scaffold JSON
    /// </summary>
    public class HtmlScaffoldImporterWindow : EditorWindow
    {
        private string _jsonFilePath = "";
        private string _outputAssetPath = "Assets/_Data/Level/Scaffolds/";
        private float _gridScale = 1.0f;
        private Vector2 _scrollPos;

        [MenuItem("Window/ProjectArk/Import HTML Scaffold JSON")]
        public static void ShowWindow()
        {
            var window = GetWindow<HtmlScaffoldImporterWindow>("HTML Scaffold Importer");
            window.minSize = new Vector2(450, 260);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("HTML Scaffold JSON Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            // --- JSON File Path ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("JSON File:", GUILayout.Width(70));
            _jsonFilePath = EditorGUILayout.TextField(_jsonFilePath);
            if (GUILayout.Button("Browse…", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel(
                    "Select HTML Scaffold JSON", "", "json");
                if (!string.IsNullOrEmpty(path))
                    _jsonFilePath = path;
            }
            EditorGUILayout.EndHorizontal();

            // --- Output Asset Path ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output:", GUILayout.Width(70));
            _outputAssetPath = EditorGUILayout.TextField(_outputAssetPath);
            if (GUILayout.Button("Browse…", GUILayout.Width(80)))
            {
                string path = EditorUtility.SaveFilePanel(
                    "Save Scaffold Asset", "Assets/_Data/Level/Scaffolds", "NewScaffold", "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert absolute path to Assets-relative
                    if (path.StartsWith(Application.dataPath))
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    _outputAssetPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            // --- Grid Scale ---
            _gridScale = EditorGUILayout.FloatField("Grid Scale (1 grid = X world units):", _gridScale);
            if (_gridScale <= 0f) _gridScale = 1f;

            EditorGUILayout.Space(12);

            // --- Validation + Import Button ---
            bool fileExists = !string.IsNullOrEmpty(_jsonFilePath) && File.Exists(_jsonFilePath);
            bool outputValid = !string.IsNullOrEmpty(_outputAssetPath) && _outputAssetPath.EndsWith(".asset");

            if (!fileExists)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(_jsonFilePath)
                        ? "Please select a JSON file."
                        : $"File not found: {_jsonFilePath}",
                    MessageType.Warning);
            }
            if (!outputValid && !string.IsNullOrEmpty(_outputAssetPath) && !_outputAssetPath.EndsWith(".asset"))
            {
                EditorGUILayout.HelpBox(
                    "Output path must end with .asset (use Browse to pick a save location).",
                    MessageType.Warning);
            }

            GUI.enabled = fileExists;
            if (GUILayout.Button("Import", GUILayout.Height(30)))
            {
                ExecuteImport();
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        // ═══════════════════════════════════════════════════════════
        //  Core Import Pipeline
        // ═══════════════════════════════════════════════════════════

        private void ExecuteImport()
        {
            // ── Step 1: Parse JSON ──
            HtmlLevelData htmlData;
            try
            {
                string jsonText = File.ReadAllText(_jsonFilePath);
                htmlData = ParseHtmlJson(jsonText);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Import Error",
                    $"Failed to parse JSON:\n{ex.Message}", "OK");
                return;
            }

            if (htmlData == null || htmlData.Rooms.Count == 0)
            {
                EditorUtility.DisplayDialog("Import Error",
                    "No valid rooms found in the JSON file.", "OK");
                return;
            }

            // ── Step 2: Create ScriptableObject ──
            var scaffoldData = ScriptableObject.CreateInstance<LevelScaffoldData>();
            Undo.RegisterCreatedObjectUndo(scaffoldData, "Import HTML Scaffold");

            // Set level name via SerializedObject (private field)
            var so = new SerializedObject(scaffoldData);
            so.FindProperty("_levelName").stringValue = htmlData.LevelName ?? "Imported Level";

            // ── Step 3: Build room mappings ──
            var htmlIdToRoom = new Dictionary<string, ScaffoldRoom>();
            var htmlIdToGuid = new Dictionary<string, string>();
            var htmlIdToHtmlRoom = new Dictionary<string, HtmlRoom>();

            int totalElements = 0;
            int placeholderMappingCount = 0;

            foreach (var htmlRoom in htmlData.Rooms)
            {
                var room = new ScaffoldRoom();

                // Set fields via public setters
                room.DisplayName = htmlRoom.Name ?? htmlRoom.Id;
                room.RoomType = MapRoomType(htmlRoom.Type);
                room.Position = new Vector3(
                    htmlRoom.Position[0] * _gridScale,
                    -htmlRoom.Position[1] * _gridScale, // y-flip: HTML y-down → Unity y-up
                    0f);
                room.Size = new Vector2(
                    htmlRoom.Size[0] * _gridScale,
                    htmlRoom.Size[1] * _gridScale);

                // Map elements
                if (htmlRoom.Elements != null)
                {
                    float roomW = htmlRoom.Size[0];
                    float roomH = htmlRoom.Size[1];

                    foreach (var htmlElem in htmlRoom.Elements)
                    {
                        var elem = new ScaffoldElement();
                        var mappingResult = MapElementType(htmlElem.Type);
                        elem.ElementType = mappingResult.type;
                        if (mappingResult.isPlaceholder) placeholderMappingCount++;

                        // Position: HTML is relative to room top-left → convert to relative to room center
                        float localX = (htmlElem.Position[0] - roomW / 2f) * _gridScale;
                        float localY = -(htmlElem.Position[1] - roomH / 2f) * _gridScale; // y-flip
                        elem.LocalPosition = new Vector3(localX, localY, 0f);

                        // Door-specific setup
                        if (elem.ElementType == ScaffoldElementType.Door)
                        {
                            elem.EnsureDoorConfigExists();
                        }

                        // NPC placeholder log
                        if (htmlElem.Type == "npc")
                        {
                            Debug.Log($"[HtmlScaffoldImporter] NPC placeholder in room '{htmlRoom.Id}' mapped to Checkpoint");
                        }

                        room.AddElement(elem);
                        totalElements++;
                    }
                }

                htmlIdToRoom[htmlRoom.Id] = room;
                htmlIdToGuid[htmlRoom.Id] = room.RoomID;
                htmlIdToHtmlRoom[htmlRoom.Id] = htmlRoom;
                scaffoldData.AddRoom(room);
            }

            // ── Step 4: Convert connections ──
            int totalConnections = 0;
            int skippedConnections = 0;

            foreach (var conn in htmlData.Connections)
            {
                if (!htmlIdToRoom.TryGetValue(conn.From, out var fromRoom))
                {
                    Debug.LogWarning($"[HtmlScaffoldImporter] Connection skipped: 'from' room '{conn.From}' not found.");
                    skippedConnections++;
                    continue;
                }
                if (!htmlIdToGuid.TryGetValue(conn.To, out string targetGuid))
                {
                    Debug.LogWarning($"[HtmlScaffoldImporter] Connection skipped: 'to' room '{conn.To}' not found.");
                    skippedConnections++;
                    continue;
                }

                var doorConn = new ScaffoldDoorConnection();
                doorConn.TargetRoomID = targetGuid;
                doorConn.DoorDirection = MapDirection(conn.FromDir);
                doorConn.DoorPosition = CalculateDoorPosition(fromRoom.Size, conn.FromDir, _gridScale);

                // Check floor difference for layer transition
                if (htmlIdToHtmlRoom.TryGetValue(conn.From, out var fromHtml)
                    && htmlIdToHtmlRoom.TryGetValue(conn.To, out var toHtml))
                {
                    if (fromHtml.Floor != toHtml.Floor)
                        doorConn.IsLayerTransition = true;
                }

                fromRoom.AddConnection(doorConn);
                totalConnections++;
            }

            // ── Step 5: Process doorLinks ──
            int doorLinksProcessed = 0;
            if (htmlData.DoorLinks != null)
            {
                foreach (var link in htmlData.DoorLinks)
                {
                    if (!htmlIdToRoom.TryGetValue(link.RoomId, out var room))
                    {
                        Debug.LogWarning($"[HtmlScaffoldImporter] DoorLink skipped: room '{link.RoomId}' not found.");
                        continue;
                    }

                    if (link.DoorIndex == -1)
                    {
                        Debug.Log($"[HtmlScaffoldImporter] No door element binding for room '{link.RoomId}' (doorIndex=-1)");
                        continue;
                    }

                    // Find Door elements in this room
                    var doorElements = room.Elements
                        .Where(e => e.ElementType == ScaffoldElementType.Door)
                        .ToList();

                    if (link.DoorIndex < 0 || link.DoorIndex >= doorElements.Count)
                    {
                        Debug.LogWarning(
                            $"[HtmlScaffoldImporter] DoorLink skipped: doorIndex {link.DoorIndex} out of range " +
                            $"(room '{link.RoomId}' has {doorElements.Count} Door elements).");
                        continue;
                    }

                    var doorElem = doorElements[link.DoorIndex];

                    // Find matching connection by direction
                    var dirVec = MapDirection(link.EntryDir);
                    var matchingConn = room.Connections.FirstOrDefault(c =>
                        Mathf.Approximately(c.DoorDirection.x, dirVec.x)
                        && Mathf.Approximately(c.DoorDirection.y, dirVec.y));

                    if (matchingConn != null)
                    {
                        doorElem.BoundConnectionID = matchingConn.ConnectionID;
                        doorLinksProcessed++;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[HtmlScaffoldImporter] DoorLink: no matching connection for direction '{link.EntryDir}' " +
                            $"in room '{link.RoomId}'.");
                    }
                }
            }

            // ── Step 6: Floor level handling ──
            var floorCounts = new Dictionary<int, int>();
            foreach (var htmlRoom in htmlData.Rooms)
            {
                if (!floorCounts.ContainsKey(htmlRoom.Floor))
                    floorCounts[htmlRoom.Floor] = 0;
                floorCounts[htmlRoom.Floor]++;
            }

            int primaryFloor = floorCounts.OrderByDescending(kv => kv.Value).First().Key;
            so.FindProperty("_floorLevel").intValue = primaryFloor;

            if (floorCounts.Count > 1)
            {
                string floorSummary = string.Join(", ",
                    floorCounts.OrderBy(kv => kv.Key)
                        .Select(kv => $"F{kv.Key}: {kv.Value} rooms"));
                Debug.LogWarning(
                    $"[HtmlScaffoldImporter] Multiple floor levels detected: {floorSummary}. " +
                    $"Primary floor set to {primaryFloor}.");

                // Tag non-primary floor rooms in their display name
                foreach (var htmlRoom in htmlData.Rooms)
                {
                    if (htmlRoom.Floor != primaryFloor && htmlIdToRoom.TryGetValue(htmlRoom.Id, out var room))
                    {
                        room.DisplayName += $" [F={htmlRoom.Floor}]";
                    }
                }
            }

            // Apply serialized changes
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Step 7: Save asset ──
            string outputPath = _outputAssetPath;
            if (!outputPath.EndsWith(".asset"))
            {
                // If output path is a directory, generate a filename from the level name
                string safeName = SanitizeFileName(htmlData.LevelName ?? "ImportedScaffold");
                outputPath = outputPath.TrimEnd('/') + "/" + safeName + ".asset";
            }

            // Ensure directory exists
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(scaffoldData, outputPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the created asset
            Selection.activeObject = scaffoldData;
            EditorGUIUtility.PingObject(scaffoldData);

            // ── Step 8: Print summary ──
            string floorInfo = string.Join(", ",
                floorCounts.OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}: {kv.Value} rooms"));

            string summary =
                $"[HtmlScaffoldImporter] Import Complete:\n" +
                $"  - Rooms: {htmlData.Rooms.Count}\n" +
                $"  - Connections: {totalConnections}\n" +
                $"  - Elements: {totalElements}\n" +
                $"  - Skipped connections: {skippedConnections}\n" +
                $"  - Placeholder mappings (chest→CrateWooden, npc→Checkpoint): {placeholderMappingCount}\n" +
                $"  - Door links processed: {doorLinksProcessed}\n" +
                $"  - Floor levels: {{{floorInfo}}}\n" +
                $"  - Output: {outputPath}";

            Debug.Log(summary);
            EditorUtility.DisplayDialog("Import Success",
                $"Imported {htmlData.Rooms.Count} rooms, {totalConnections} connections, " +
                $"{totalElements} elements.\n\nAsset saved to:\n{outputPath}",
                "OK");
        }

        // ═══════════════════════════════════════════════════════════
        //  JSON Parsing (embedded MiniJSON — no third-party dependency)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Parses the HTML LevelDesigner JSON into intermediate data structures.
        /// Uses an embedded lightweight JSON parser for robust handling of comment objects
        /// without requiring System.Text.Json or Newtonsoft (not available in Unity .NET Framework profile).
        /// </summary>
        private static HtmlLevelData ParseHtmlJson(string jsonText)
        {
            var data = new HtmlLevelData();
            var root = MiniJson.Deserialize(jsonText) as Dictionary<string, object>;
            if (root == null)
                throw new Exception("JSON root is not an object.");

            // Level name
            data.LevelName = GetString(root, "levelName");
            data.Description = GetString(root, "description");

            // Rooms
            if (root.TryGetValue("rooms", out var roomsObj) && roomsObj is List<object> roomsList)
            {
                foreach (var entry in roomsList)
                {
                    if (!(entry is Dictionary<string, object> roomDict))
                        continue;

                    // Skip comment objects
                    if (roomDict.ContainsKey("comment"))
                        continue;
                    // Skip if no 'id' property
                    if (!roomDict.ContainsKey("id"))
                        continue;

                    var room = new HtmlRoom
                    {
                        Id = GetString(roomDict, "id"),
                        Name = GetString(roomDict, "name"),
                        Type = GetString(roomDict, "type") ?? "normal",
                        Floor = GetInt(roomDict, "floor", 0),
                        Position = GetFloatArray(roomDict, "position", new float[] { 0, 0 }),
                        Size = GetFloatArray(roomDict, "size", new float[] { 10, 8 })
                    };

                    // Parse elements
                    if (roomDict.TryGetValue("elements", out var elemsObj) && elemsObj is List<object> elemsList)
                    {
                        foreach (var elemEntry in elemsList)
                        {
                            if (!(elemEntry is Dictionary<string, object> elemDict))
                                continue;
                            if (!elemDict.ContainsKey("type"))
                                continue;

                            room.Elements.Add(new HtmlElement
                            {
                                Type = GetString(elemDict, "type"),
                                Position = GetFloatArray(elemDict, "position", new float[] { 0, 0 })
                            });
                        }
                    }

                    data.Rooms.Add(room);
                }
            }

            // Connections
            if (root.TryGetValue("connections", out var connsObj) && connsObj is List<object> connsList)
            {
                foreach (var entry in connsList)
                {
                    if (!(entry is Dictionary<string, object> connDict))
                        continue;
                    // Skip comment objects
                    if (connDict.ContainsKey("comment"))
                        continue;
                    if (!connDict.ContainsKey("from"))
                        continue;

                    data.Connections.Add(new HtmlConnection
                    {
                        From = GetString(connDict, "from"),
                        To = GetString(connDict, "to"),
                        FromDir = GetString(connDict, "fromDir") ?? "east",
                        ToDir = GetString(connDict, "toDir") ?? "west"
                    });
                }
            }

            // DoorLinks
            if (root.TryGetValue("doorLinks", out var dlObj) && dlObj is List<object> dlList)
            {
                foreach (var entry in dlList)
                {
                    if (!(entry is Dictionary<string, object> dlDict))
                        continue;
                    if (!dlDict.ContainsKey("roomId"))
                        continue;

                    data.DoorLinks.Add(new HtmlDoorLink
                    {
                        RoomId = GetString(dlDict, "roomId"),
                        EntryDir = GetString(dlDict, "entryDir") ?? "east",
                        DoorIndex = GetInt(dlDict, "doorIndex", -1)
                    });
                }
            }

            return data;
        }

        // ─────────── Dictionary helper accessors ───────────

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return null;
        }

        private static int GetInt(Dictionary<string, object> dict, string key, int fallback)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is long l) return (int)l;
                if (val is double d) return (int)d;
                if (int.TryParse(val.ToString(), out int parsed)) return parsed;
            }
            return fallback;
        }

        private static float[] GetFloatArray(Dictionary<string, object> dict, string key, float[] fallback)
        {
            if (!dict.TryGetValue(key, out var val) || !(val is List<object> list))
                return fallback;
            if (list.Count < 2) return fallback;

            var result = new float[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is double d) result[i] = (float)d;
                else if (list[i] is long l) result[i] = (float)l;
                else result[i] = 0f;
            }
            return result;
        }
        // ═══════════════════════════════════════════════════════════
        //  Mapping Helpers
        // ═══════════════════════════════════════════════════════════

        private static RoomType MapRoomType(string htmlType)
        {
            if (string.IsNullOrEmpty(htmlType)) return RoomType.Normal;

            switch (htmlType.ToLowerInvariant())
            {
                case "normal":  return RoomType.Normal;
                case "arena":   return RoomType.Arena;
                case "boss":    return RoomType.Boss;
                case "safe":    return RoomType.Safe;
                default:
                    Debug.LogWarning($"[HtmlScaffoldImporter] Unknown room type '{htmlType}', defaulting to Normal.");
                    return RoomType.Normal;
            }
        }

        private struct ElementMappingResult
        {
            public ScaffoldElementType type;
            public bool isPlaceholder;
        }

        private static ElementMappingResult MapElementType(string htmlType)
        {
            if (string.IsNullOrEmpty(htmlType))
            {
                Debug.LogWarning("[HtmlScaffoldImporter] Empty element type, defaulting to Hazard.");
                return new ElementMappingResult { type = ScaffoldElementType.Hazard, isPlaceholder = false };
            }

            switch (htmlType.ToLowerInvariant())
            {
                case "spawn":      return new ElementMappingResult { type = ScaffoldElementType.PlayerSpawn, isPlaceholder = false };
                case "enemy":      return new ElementMappingResult { type = ScaffoldElementType.EnemySpawn, isPlaceholder = false };
                case "checkpoint": return new ElementMappingResult { type = ScaffoldElementType.Checkpoint, isPlaceholder = false };
                case "door":       return new ElementMappingResult { type = ScaffoldElementType.Door, isPlaceholder = false };
                case "chest":      return new ElementMappingResult { type = ScaffoldElementType.CrateWooden, isPlaceholder = true };
                case "npc":        return new ElementMappingResult { type = ScaffoldElementType.Checkpoint, isPlaceholder = true };
                default:
                    Debug.LogWarning($"[HtmlScaffoldImporter] Unknown element type '{htmlType}', defaulting to Hazard.");
                    return new ElementMappingResult { type = ScaffoldElementType.Hazard, isPlaceholder = false };
            }
        }

        private static Vector2 MapDirection(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return Vector2.right;

            switch (dir.ToLowerInvariant())
            {
                case "east":  return new Vector2(1, 0);
                case "west":  return new Vector2(-1, 0);
                case "north": return new Vector2(0, 1);
                case "south": return new Vector2(0, -1);
                default:
                    Debug.LogWarning($"[HtmlScaffoldImporter] Unknown direction '{dir}', defaulting to east.");
                    return new Vector2(1, 0);
            }
        }

        /// <summary>
        /// Calculates the door position on the room edge midpoint based on direction.
        /// </summary>
        private static Vector3 CalculateDoorPosition(Vector2 roomSize, string dir, float gridScale)
        {
            // roomSize is already in world units (scaled), door is at the edge midpoint
            switch ((dir ?? "").ToLowerInvariant())
            {
                case "east":  return new Vector3(roomSize.x / 2f, 0f, 0f);
                case "west":  return new Vector3(-roomSize.x / 2f, 0f, 0f);
                case "north": return new Vector3(0f, roomSize.y / 2f, 0f);
                case "south": return new Vector3(0f, -roomSize.y / 2f, 0f);
                default:      return Vector3.zero;
            }
        }

        /// <summary>
        /// Sanitize a string to be safe for use as a file name.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
                name = name.Replace(c, '_');
            // Also replace common problematic chars
            name = name.Replace(' ', '_').Replace('·', '_').Replace('(', '_').Replace(')', '_');
            return name;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Embedded MiniJSON — lightweight recursive-descent JSON parser
    //  Returns: Dictionary<string,object> for objects, List<object> for arrays,
    //  string, double, long, bool, or null.
    //  Based on the public-domain MiniJSON pattern; zero external dependencies.
    // ═══════════════════════════════════════════════════════════════════════════

    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return new Parser(json).ParseValue();
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _pos;

            public Parser(string json)
            {
                _json = json;
                _pos = 0;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (_pos >= _json.Length) return null;

                char c = _json[_pos];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't':
                    case 'f': return ParseBool();
                    case 'n': return ParseNull();
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                            return ParseNumber();
                        throw new Exception($"Unexpected char '{c}' at position {_pos}");
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>();
                _pos++; // skip '{'
                SkipWhitespace();

                if (_pos < _json.Length && _json[_pos] == '}')
                {
                    _pos++;
                    return dict;
                }

                while (_pos < _json.Length)
                {
                    SkipWhitespace();
                    string key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    object val = ParseValue();
                    dict[key] = val;

                    SkipWhitespace();
                    if (_pos < _json.Length && _json[_pos] == ',')
                    {
                        _pos++;
                        continue;
                    }
                    break;
                }

                SkipWhitespace();
                Expect('}');
                return dict;
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _pos++; // skip '['
                SkipWhitespace();

                if (_pos < _json.Length && _json[_pos] == ']')
                {
                    _pos++;
                    return list;
                }

                while (_pos < _json.Length)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();
                    if (_pos < _json.Length && _json[_pos] == ',')
                    {
                        _pos++;
                        continue;
                    }
                    break;
                }

                SkipWhitespace();
                Expect(']');
                return list;
            }

            private string ParseString()
            {
                SkipWhitespace();
                Expect('"');

                var sb = new StringBuilder();
                while (_pos < _json.Length)
                {
                    char c = _json[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\')
                    {
                        if (_pos >= _json.Length) break;
                        char esc = _json[_pos++];
                        switch (esc)
                        {
                            case '"':  sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/':  sb.Append('/'); break;
                            case 'b':  sb.Append('\b'); break;
                            case 'f':  sb.Append('\f'); break;
                            case 'n':  sb.Append('\n'); break;
                            case 'r':  sb.Append('\r'); break;
                            case 't':  sb.Append('\t'); break;
                            case 'u':
                                if (_pos + 4 <= _json.Length)
                                {
                                    string hex = _json.Substring(_pos, 4);
                                    _pos += 4;
                                    sb.Append((char)Convert.ToInt32(hex, 16));
                                }
                                break;
                            default: sb.Append(esc); break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                throw new Exception("Unterminated string");
            }

            private object ParseNumber()
            {
                int start = _pos;
                if (_json[_pos] == '-') _pos++;
                while (_pos < _json.Length && _json[_pos] >= '0' && _json[_pos] <= '9') _pos++;

                bool isFloat = false;
                if (_pos < _json.Length && _json[_pos] == '.')
                {
                    isFloat = true;
                    _pos++;
                    while (_pos < _json.Length && _json[_pos] >= '0' && _json[_pos] <= '9') _pos++;
                }
                if (_pos < _json.Length && (_json[_pos] == 'e' || _json[_pos] == 'E'))
                {
                    isFloat = true;
                    _pos++;
                    if (_pos < _json.Length && (_json[_pos] == '+' || _json[_pos] == '-')) _pos++;
                    while (_pos < _json.Length && _json[_pos] >= '0' && _json[_pos] <= '9') _pos++;
                }

                string numStr = _json.Substring(start, _pos - start);
                if (isFloat)
                    return double.Parse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lVal))
                    return lVal;
                return double.Parse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private bool ParseBool()
            {
                if (_json.Substring(_pos, 4) == "true")  { _pos += 4; return true; }
                if (_json.Substring(_pos, 5) == "false") { _pos += 5; return false; }
                throw new Exception($"Expected bool at position {_pos}");
            }

            private object ParseNull()
            {
                if (_json.Substring(_pos, 4) == "null") { _pos += 4; return null; }
                throw new Exception($"Expected null at position {_pos}");
            }

            private void SkipWhitespace()
            {
                while (_pos < _json.Length && char.IsWhiteSpace(_json[_pos]))
                    _pos++;
            }

            private void Expect(char c)
            {
                if (_pos >= _json.Length || _json[_pos] != c)
                    throw new Exception($"Expected '{c}' at position {_pos}, got '{(_pos < _json.Length ? _json[_pos].ToString() : "EOF")}'");
                _pos++;
            }
        }
    }
}
