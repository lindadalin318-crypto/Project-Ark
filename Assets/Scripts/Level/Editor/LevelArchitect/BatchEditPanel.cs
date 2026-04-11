using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1 — sub-panel of LevelArchitectWindow]
    /// Handles batch editing of multiple selected rooms and context menu operations.
    /// Provides bulk property modification, room configuration copy/paste, and
    /// context menu actions.
    /// </summary>
    public static class BatchEditPanel
    {
        // ──────────────────── Batch Edit State ────────────────────

        private static RoomNodeType _batchNodeType = RoomNodeType.Transit;
        private static int _batchFloorLevel;
        private static RoomSO _batchRoomSO;
        private static Vector2 _batchSize = new Vector2(20, 15);

        // ──────────────────── Copy/Paste State ────────────────────

        private static RoomNodeType _copiedNodeType;
        private static int _copiedFloorLevel;
        private static Vector2 _copiedSize;
        private static bool _hasCopiedConfig;

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Draw the batch edit panel in the side panel when multiple rooms are selected.
        /// </summary>
        public static void DrawBatchEditPanel(List<Room> selectedRooms)
        {
            if (selectedRooms == null || selectedRooms.Count < 2) return;

            GUILayout.Label($"Batch Edit ({selectedRooms.Count} rooms)", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("HelpBox");

            _batchNodeType = (RoomNodeType)EditorGUILayout.EnumPopup("Node Type", _batchNodeType);
            _batchFloorLevel = EditorGUILayout.IntField("Floor Level", _batchFloorLevel);
            _batchSize = EditorGUILayout.Vector2Field("Size", _batchSize);
            _batchRoomSO = (RoomSO)EditorGUILayout.ObjectField("RoomSO", _batchRoomSO, typeof(RoomSO), false);

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Node", GUILayout.Height(22)))
            {
                ApplyBatchNodeType(selectedRooms, _batchNodeType);
            }

            if (GUILayout.Button("Apply Floor", GUILayout.Height(22)))
            {
                ApplyBatchFloorLevel(selectedRooms, _batchFloorLevel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Size", GUILayout.Height(22)))
            {
                ApplyBatchSize(selectedRooms, _batchSize);
            }

            if (GUILayout.Button("Apply All", GUILayout.Height(22)))
            {
                ApplyBatchNodeType(selectedRooms, _batchNodeType);
                ApplyBatchFloorLevel(selectedRooms, _batchFloorLevel);
                ApplyBatchSize(selectedRooms, _batchSize);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Show context menu for selected rooms.
        /// </summary>
        public static void ShowContextMenu(List<Room> selectedRooms)
        {
            if (selectedRooms == null || selectedRooms.Count == 0) return;

            var menu = new GenericMenu();

            // Single room options
            if (selectedRooms.Count == 1)
            {
                var room = selectedRooms[0];
                menu.AddItem(new GUIContent("Set as Entry Room"), false, () => SetAsEntryRoom(room));
                menu.AddItem(new GUIContent("Set as Boss Room"), false, () => SetNodeType(room, RoomNodeType.Boss));
                menu.AddItem(new GUIContent("Set as Arena Room"), false, () => SetNodeType(room, RoomNodeType.Arena));
                menu.AddItem(new GUIContent("Set as Combat Room"), false, () => SetNodeType(room, RoomNodeType.Combat));
                menu.AddItem(new GUIContent("Set as Reward Room"), false, () => SetNodeType(room, RoomNodeType.Reward));
                menu.AddItem(new GUIContent("Set as Safe Room"), false, () => SetNodeType(room, RoomNodeType.Safe));
                menu.AddItem(new GUIContent("Set as Transit Room"), false, () => SetNodeType(room, RoomNodeType.Transit));
                menu.AddSeparator("");

                menu.AddItem(new GUIContent("Assign EncounterSO..."), false, () => ShowEncounterPicker(room));
                menu.AddItem(new GUIContent("Save as Preset..."), false, () => SaveAsPreset(room));
                menu.AddSeparator("");
            }

            // Multi room options
            menu.AddItem(new GUIContent("Copy Room Config"), false, () => CopyConfig(selectedRooms[0]));

            if (_hasCopiedConfig)
            {
                menu.AddItem(new GUIContent("Paste Room Config"), false, () => PasteConfig(selectedRooms));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste Room Config"));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Resize to Default (Transit)"), false,
                () => ApplyBatchSize(selectedRooms, new Vector2(20, 15)));
            menu.AddItem(new GUIContent("Resize to Default (Arena)"), false,
                () => ApplyBatchSize(selectedRooms, new Vector2(25, 20)));
            menu.AddItem(new GUIContent("Resize to Default (Boss)"), false,
                () => ApplyBatchSize(selectedRooms, new Vector2(35, 25)));

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Auto-Connect Adjacent"), false,
                () => AutoConnectSelected(selectedRooms));

            menu.ShowAsContext();
        }

        // ──────────────────── Batch Apply ────────────────────

        private static void ApplyBatchNodeType(List<Room> rooms, RoomNodeType nodeType)
        {
            var objects = CollectRoomDataObjects(rooms);
            if (objects.Count == 0) return;

            Undo.RecordObjects(objects.ToArray(), "Batch Set Room NodeType");

            foreach (var room in rooms)
            {
                ApplyRoomNodeType(room, nodeType);
            }

            Debug.Log($"[BatchEdit] Set {objects.Count} room(s) to NodeType '{nodeType}'.");
            SceneView.RepaintAll();
        }

        private static void ApplyBatchFloorLevel(List<Room> rooms, int floorLevel)
        {
            var objects = CollectRoomDataObjects(rooms);
            if (objects.Count == 0) return;

            Undo.RecordObjects(objects.ToArray(), "Batch Set Floor Level");

            foreach (var room in rooms)
            {
                ApplyRoomFloorLevel(room, floorLevel);
            }

            SynchronizeDoorAuthoring(rooms);

            Debug.Log($"[BatchEdit] Set {objects.Count} room(s) to floor level {floorLevel}.");
            SceneView.RepaintAll();
        }

        private static void ApplyBatchSize(List<Room> rooms, Vector2 size)
        {
            var objects = new List<Object>();
            foreach (var room in rooms)
            {
                if (room == null) continue;

                var box = room.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    objects.Add(box);
                }
            }

            if (objects.Count == 0) return;

            Undo.RecordObjects(objects.ToArray(), "Batch Resize Rooms");

            foreach (var room in rooms)
            {
                if (room == null) continue;

                var box = room.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.size = size;
                    EditorUtility.SetDirty(box);
                }

                var confiner = room.transform.Find("CameraConfiner");
                if (confiner == null) continue;

                var poly = confiner.GetComponent<PolygonCollider2D>();
                if (poly == null) continue;

                Undo.RecordObject(poly, "Resize Confiner");
                float hw = size.x / 2f - 0.1f;
                float hh = size.y / 2f - 0.1f;
                poly.points = new Vector2[]
                {
                    new Vector2(-hw, -hh), new Vector2(hw, -hh),
                    new Vector2(hw, hh), new Vector2(-hw, hh)
                };
                EditorUtility.SetDirty(poly);
            }

            SynchronizeDoorAuthoring(rooms);

            Debug.Log($"[BatchEdit] Resized {objects.Count} room(s) to {size}.");
            SceneView.RepaintAll();
        }

        private static List<Object> CollectRoomDataObjects(List<Room> rooms)
        {
            var objects = new List<Object>();
            foreach (var room in rooms)
            {
                if (room != null && room.Data != null)
                {
                    objects.Add(room.Data);
                }
            }

            return objects;
        }

        private static void ApplyRoomNodeType(Room room, RoomNodeType nodeType)
        {
            if (room == null || room.Data == null) return;

            var serialized = new SerializedObject(room.Data);
            serialized.FindProperty("_nodeType").enumValueIndex = (int)nodeType;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(room.Data);

            EnsureNodeTypeSceneState(room, nodeType);
        }

        private static void ApplyRoomFloorLevel(Room room, int floorLevel)
        {
            if (room == null || room.Data == null) return;

            var serialized = new SerializedObject(room.Data);
            serialized.FindProperty("_floorLevel").intValue = floorLevel;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(room.Data);
        }

        private static void SynchronizeDoorAuthoring(List<Room> rooms)
        {
            foreach (var room in rooms)
            {
                if (room == null) continue;
                DoorWiringService.SynchronizeRoomConnections(room);
            }
        }

        private static void EnsureNodeTypeSceneState(Room room, RoomNodeType nodeType)
        {
            if (room == null) return;
            if (nodeType != RoomNodeType.Arena && nodeType != RoomNodeType.Boss) return;
            if (room.GetComponent<ArenaController>() != null) return;

            var arenaController = Undo.AddComponent<ArenaController>(room.gameObject);
            if (arenaController != null)
            {
                EditorUtility.SetDirty(arenaController);
            }
        }

        // ──────────────────── Context Actions ────────────────────

        private static void SetNodeType(Room room, RoomNodeType nodeType)
        {
            if (room == null || room.Data == null) return;

            Undo.RecordObject(room.Data, "Set Room NodeType");
            ApplyRoomNodeType(room, nodeType);
            SceneView.RepaintAll();
        }

        private static void SetAsEntryRoom(Room room)
        {
            var roomManager = Object.FindAnyObjectByType<RoomManager>();
            if (roomManager == null)
            {
                Debug.LogWarning("[BatchEdit] No RoomManager found in scene.");
                return;
            }

            Undo.RecordObject(roomManager, "Set Entry Room");
            var serialized = new SerializedObject(roomManager);
            serialized.FindProperty("_startingRoom").objectReferenceValue = room;
            serialized.ApplyModifiedProperties();

            Debug.Log($"[BatchEdit] Set '{room.RoomID}' as entry room.");
        }

        private static void ShowEncounterPicker(Room room)
        {
            if (room.Data == null)
            {
                Debug.LogWarning("[BatchEdit] Room has no RoomSO — cannot assign encounter.");
                return;
            }

            EditorGUIUtility.ShowObjectPicker<EncounterSO>(room.Data.Encounter, false, "", 0);

            EditorApplication.update += OnEncounterPickerUpdate;
            _encounterPickerTargetRoom = room;
        }

        private static Room _encounterPickerTargetRoom;

        private static void OnEncounterPickerUpdate()
        {
            if (Event.current == null) return;

            string commandName = Event.current.commandName;
            if (commandName == "ObjectSelectorClosed" || commandName == "ObjectSelectorUpdated")
            {
                if (_encounterPickerTargetRoom != null && _encounterPickerTargetRoom.Data != null)
                {
                    var selected = EditorGUIUtility.GetObjectPickerObject() as EncounterSO;
                    if (selected != null)
                    {
                        Undo.RecordObject(_encounterPickerTargetRoom.Data, "Assign Encounter");
                        var serialized = new SerializedObject(_encounterPickerTargetRoom.Data);
                        serialized.FindProperty("_encounter").objectReferenceValue = selected;
                        serialized.ApplyModifiedProperties();
                        EditorUtility.SetDirty(_encounterPickerTargetRoom.Data);
                    }
                }

                if (commandName == "ObjectSelectorClosed")
                {
                    EditorApplication.update -= OnEncounterPickerUpdate;
                    _encounterPickerTargetRoom = null;
                }
            }
        }

        private static void SaveAsPreset(Room room)
        {
            string presetName = room.RoomID;
            var preset = RoomFactory.SaveRoomAsPreset(room, presetName);
            if (preset != null)
            {
                EditorGUIUtility.PingObject(preset);
            }
        }

        private static void CopyConfig(Room room)
        {
            if (room == null) return;

            _copiedNodeType = room.Data != null ? room.Data.NodeType : RoomNodeType.Transit;
            _copiedFloorLevel = room.Data != null ? room.Data.FloorLevel : 0;

            var box = room.GetComponent<BoxCollider2D>();
            _copiedSize = box != null ? box.size : new Vector2(20, 15);

            _hasCopiedConfig = true;
            Debug.Log($"[BatchEdit] Copied config from '{room.RoomID}'.");
        }

        private static void PasteConfig(List<Room> rooms)
        {
            if (!_hasCopiedConfig) return;

            ApplyBatchNodeType(rooms, _copiedNodeType);
            ApplyBatchFloorLevel(rooms, _copiedFloorLevel);
            ApplyBatchSize(rooms, _copiedSize);

            Debug.Log($"[BatchEdit] Pasted config to {rooms.Count} room(s).");
        }

        private static void AutoConnectSelected(List<Room> rooms)
        {
            int total = 0;
            foreach (var room in rooms)
            {
                if (room == null) continue;
                total += DoorWiringService.AutoConnectAllAdjacent(room);
            }
            Debug.Log($"[BatchEdit] Auto-connected {total} adjacent pair(s).");
        }
    }
}
