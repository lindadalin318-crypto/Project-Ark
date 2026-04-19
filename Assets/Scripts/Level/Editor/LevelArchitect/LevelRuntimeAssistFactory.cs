using UnityEditor;
using UnityEngine;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1]
    /// Creates authoring-time starter objects for the most common runtime level elements.
    /// These helpers only build a standard scene starting point aligned with validator root rules;
    /// authors still need to assign gameplay-specific assets and values.
    /// </summary>
    public static class LevelRuntimeAssistFactory
    {
        public enum RoomAssistType
        {
            Checkpoint,
            BreakableWall,
            OpenEncounterTrigger,
            HiddenAreaMask,
            BiomeTrigger,
            ScheduledBehaviour,
            WorldEventTrigger,
            ContactHazard,
            DamageZone,
            TimedHazard
        }

        private const string ELEMENTS_ROOT_NAME = "Elements";
        private const string ENCOUNTERS_ROOT_NAME = "Encounters";
        private const string HAZARDS_ROOT_NAME = "Hazards";
        private const string TRIGGERS_ROOT_NAME = "Triggers";
        private const string SPAWN_POINTS_ROOT_NAME = "SpawnPoints";

        public static GameObject CreateRoomAssist(Room room, RoomAssistType assistType)
        {
            if (room == null)
            {
                Debug.LogWarning("[LevelRuntimeAssist] Cannot create runtime assist: room is null.");
                return null;
            }

            return assistType switch
            {
                RoomAssistType.Checkpoint => CreateCheckpoint(room),
                RoomAssistType.BreakableWall => CreateBreakableWall(room),
                RoomAssistType.OpenEncounterTrigger => CreateOpenEncounterTrigger(room),
                RoomAssistType.HiddenAreaMask => CreateHiddenAreaMask(room),
                RoomAssistType.BiomeTrigger => CreateBiomeTrigger(room),
                RoomAssistType.ScheduledBehaviour => CreateScheduledBehaviour(room),
                RoomAssistType.WorldEventTrigger => CreateWorldEventTrigger(room),
                RoomAssistType.ContactHazard => CreateContactHazard(room),
                RoomAssistType.DamageZone => CreateDamageZone(room),
                RoomAssistType.TimedHazard => CreateTimedHazard(room),
                _ => null
            };
        }


        public static GameObject CreateLockAssist(Room ownerRoom, Door targetDoor)
        {
            if (ownerRoom == null || targetDoor == null)
            {
                Debug.LogWarning("[LevelRuntimeAssist] Cannot create Lock starter: owner room or target door is null.");
                return null;
            }

            Transform root = EnsureAuthoringRoot(ownerRoom, ELEMENTS_ROOT_NAME);
            string suffix = targetDoor.TargetRoom != null ? targetDoor.TargetRoom.RoomID : targetDoor.gameObject.name;
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"Lock_{suffix}");
            GameObject lockObject = CreateChild(root, objectName, targetDoor.transform.position);

            EnsureTriggerBoxCollider(lockObject, new Vector2(2.5f, 2.5f));
            var lockComponent = Undo.AddComponent<Lock>(lockObject);
            ApplyPlayerLayer(lockComponent, "_playerLayer");
            SetObjectReference(lockComponent, "_targetDoor", targetDoor);
            SetDoorInitialState(targetDoor, DoorState.Locked_Key);

            FinalizeCreatedObject(lockObject, $"[LevelRuntimeAssist] Created Lock starter for door '{targetDoor.gameObject.name}'. Assign KeyItemSO to finish setup.");
            return lockObject;
        }

        public static string GetDisplayName(RoomAssistType assistType)
        {
            return assistType switch
            {
                RoomAssistType.Checkpoint => "Checkpoint",
                RoomAssistType.BreakableWall => "Breakable Wall",
                RoomAssistType.OpenEncounterTrigger => "Open Encounter",
                RoomAssistType.HiddenAreaMask => "Hidden Area",
                RoomAssistType.BiomeTrigger => "Biome Trigger",
                RoomAssistType.ScheduledBehaviour => "Scheduled",
                RoomAssistType.WorldEventTrigger => "World Event",
                RoomAssistType.ContactHazard => "Contact Hazard",
                RoomAssistType.DamageZone => "Damage Zone",
                RoomAssistType.TimedHazard => "Timed Hazard",
                _ => assistType.ToString()
            };
        }


        private static GameObject CreateCheckpoint(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, ELEMENTS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"Checkpoint_{room.RoomID}");
            GameObject checkpointObject = CreateChild(root, objectName, GetRoomCenter(room));

            EnsureTriggerBoxCollider(checkpointObject, new Vector2(4f, 4f));
            var checkpoint = Undo.AddComponent<Checkpoint>(checkpointObject);
            ApplyPlayerLayer(checkpoint, "_playerLayer");

            FinalizeCreatedObject(checkpointObject, $"[LevelRuntimeAssist] Created Checkpoint starter in room '{room.RoomID}'. Assign CheckpointSO to finish setup.");
            return checkpointObject;
        }

        private static GameObject CreateBreakableWall(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, ELEMENTS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"BreakableWall_{room.RoomID}");
            GameObject wallObject = CreateChild(root, objectName, GetRoomCenter(room));

            EnsureSolidBoxCollider(wallObject, new Vector2(2f, 2f));
            EnsureSpriteRenderer(wallObject);
            ApplyNamedLayer(wallObject, "Wall", "BreakableWall starter");
            Undo.AddComponent<DestroyableObject>(wallObject);
            Undo.AddComponent<BreakableWall>(wallObject);

            FinalizeCreatedObject(wallObject, $"[LevelRuntimeAssist] Created BreakableWall starter in room '{room.RoomID}'. Add subtle signal art and hidden reward setup before playtest.");
            return wallObject;
        }

        private static GameObject CreateOpenEncounterTrigger(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, ENCOUNTERS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"OpenEncounter_{room.RoomID}");
            GameObject triggerObject = CreateChild(root, objectName, GetRoomCenter(room));

            EnsureTriggerBoxCollider(triggerObject, GetSuggestedTriggerSize(room, 0.7f, 0.7f, new Vector2(8f, 6f)));
            var trigger = Undo.AddComponent<OpenEncounterTrigger>(triggerObject);
            ApplyPlayerLayer(trigger, "_playerLayer");

            GameObject spawnerObject = CreateChild(triggerObject.transform, "EnemySpawner", triggerObject.transform.position);
            var spawner = Undo.AddComponent<EnemySpawner>(spawnerObject);
            SetObjectReference(trigger, "_spawner", spawner);

            Transform spawnRoot = CreateChild(spawnerObject.transform, SPAWN_POINTS_ROOT_NAME, spawnerObject.transform.position).transform;
            Transform spawnPoint = CreateChild(spawnRoot, "SpawnPoint_01", triggerObject.transform.position).transform;
            SetObjectArray(spawner, "_spawnPoints", spawnPoint);

            FinalizeCreatedObject(triggerObject, $"[LevelRuntimeAssist] Created OpenEncounterTrigger starter in room '{room.RoomID}'. Assign EncounterSO to finish setup.");
            return triggerObject;
        }

        private static GameObject CreateHiddenAreaMask(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, TRIGGERS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"HiddenAreaMask_{room.RoomID}");
            GameObject hiddenAreaObject = CreateChild(root, objectName, GetRoomCenter(room));

            EnsureTriggerBoxCollider(hiddenAreaObject, GetSuggestedTriggerSize(room, 0.45f, 0.45f, new Vector2(6f, 4f)));
            var hiddenAreaMask = Undo.AddComponent<HiddenAreaMask>(hiddenAreaObject);
            ApplyPlayerLayer(hiddenAreaMask, "_playerLayer");

            GameObject maskVisual = CreateChild(hiddenAreaObject.transform, "Mask_Main", hiddenAreaObject.transform.position);
            EnsureSpriteRenderer(maskVisual);

            FinalizeCreatedObject(hiddenAreaObject, $"[LevelRuntimeAssist] Created HiddenAreaMask starter in room '{room.RoomID}'. Add occluder art and tune reveal footprint before playtest.");
            return hiddenAreaObject;
        }

        private static GameObject CreateBiomeTrigger(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, TRIGGERS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"BiomeTrigger_{room.RoomID}");
            GameObject biomeObject = CreateChild(root, objectName, GetRoomCenter(room));

            EnsureTriggerBoxCollider(biomeObject, GetSuggestedTriggerSize(room, 0.8f, 0.8f, new Vector2(10f, 8f)));
            var biomeTrigger = Undo.AddComponent<BiomeTrigger>(biomeObject);
            ApplyPlayerLayer(biomeTrigger, "_playerLayer");

            FinalizeCreatedObject(biomeObject, $"[LevelRuntimeAssist] Created BiomeTrigger starter in room '{room.RoomID}'. Assign RoomAmbienceSO to finish setup.");
            return biomeObject;
        }

        private static GameObject CreateScheduledBehaviour(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, TRIGGERS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"Scheduled_{room.RoomID}");
            GameObject scheduledObject = CreateChild(root, objectName, GetRoomCenter(room));

            Undo.AddComponent<ScheduledBehaviour>(scheduledObject);
            CreateChild(scheduledObject.transform, "ScheduledTarget", scheduledObject.transform.position);

            FinalizeCreatedObject(scheduledObject, $"[LevelRuntimeAssist] Created ScheduledBehaviour starter in room '{room.RoomID}'. Configure active phases to finish setup.");
            return scheduledObject;
        }

        private static GameObject CreateWorldEventTrigger(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, TRIGGERS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"WorldEvent_{room.RoomID}");
            GameObject worldEventObject = CreateChild(root, objectName, GetRoomCenter(room));

            var worldEventTrigger = Undo.AddComponent<WorldEventTrigger>(worldEventObject);
            SetIntValue(worldEventTrigger, "_requiredWorldStage", 1);

            FinalizeCreatedObject(worldEventObject, $"[LevelRuntimeAssist] Created WorldEventTrigger starter in room '{room.RoomID}'. Configure world stage and effects before playtest.");
            return worldEventObject;
        }

        private static GameObject CreateContactHazard(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, HAZARDS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"ContactHazard_{room.RoomID}");
            GameObject hazardObject = CreateChild(root, objectName, GetRoomCenter(room));

            EnsureTriggerBoxCollider(hazardObject, new Vector2(4f, 4f));
            var hazard = Undo.AddComponent<ContactHazard>(hazardObject);
            ApplyPlayerLayer(hazard, "_targetLayer");

            FinalizeCreatedObject(hazardObject, $"[LevelRuntimeAssist] Created ContactHazard starter in room '{room.RoomID}'. Tune damage, knockback and hit cooldown before playtest.");
            return hazardObject;
        }

        private static GameObject CreateDamageZone(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, HAZARDS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"DamageZone_{room.RoomID}");
            GameObject hazardObject = CreateChild(root, objectName, GetRoomCenter(room));

            EnsureTriggerBoxCollider(hazardObject, GetSuggestedTriggerSize(room, 0.45f, 0.35f, new Vector2(6f, 4f)));
            var hazard = Undo.AddComponent<DamageZone>(hazardObject);
            ApplyPlayerLayer(hazard, "_targetLayer");

            FinalizeCreatedObject(hazardObject, $"[LevelRuntimeAssist] Created DamageZone starter in room '{room.RoomID}'. Tune damage, tick interval and playable footprint before playtest.");
            return hazardObject;
        }

        private static GameObject CreateTimedHazard(Room room)
        {
            Transform root = EnsureAuthoringRoot(room, HAZARDS_ROOT_NAME);
            string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"TimedHazard_{room.RoomID}");
            GameObject hazardObject = CreateChild(root, objectName, GetRoomCenter(room));

            EnsureTriggerBoxCollider(hazardObject, new Vector2(4f, 4f));
            var hazard = Undo.AddComponent<TimedHazard>(hazardObject);
            ApplyPlayerLayer(hazard, "_targetLayer");

            FinalizeCreatedObject(hazardObject, $"[LevelRuntimeAssist] Created TimedHazard starter in room '{room.RoomID}'. Tune active/inactive durations, cooldown and visuals before playtest.");
            return hazardObject;
        }

        private static Transform EnsureAuthoringRoot(Room room, string rootName)

        {
            Transform root = room.transform.Find(rootName);
            if (root != null)
            {
                return root;
            }

            GameObject rootObject = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(rootObject, $"Create {rootName} Root");
            rootObject.transform.SetParent(room.transform, false);
            return rootObject.transform;
        }

        private static GameObject CreateChild(Transform parent, string name, Vector3 worldPosition)
        {
            GameObject gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = worldPosition;
            return gameObject;
        }

        private static void EnsureTriggerBoxCollider(GameObject gameObject, Vector2 size)
        {
            var boxCollider = gameObject.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = Undo.AddComponent<BoxCollider2D>(gameObject);
            }

            Undo.RecordObject(boxCollider, "Configure Trigger Collider");
            boxCollider.isTrigger = true;
            boxCollider.size = size;
            EditorUtility.SetDirty(boxCollider);
        }

        private static void EnsureSolidBoxCollider(GameObject gameObject, Vector2 size)
        {
            var boxCollider = gameObject.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = Undo.AddComponent<BoxCollider2D>(gameObject);
            }

            Undo.RecordObject(boxCollider, "Configure Solid Collider");
            boxCollider.isTrigger = false;
            boxCollider.size = size;
            EditorUtility.SetDirty(boxCollider);
        }

        private static void EnsureSpriteRenderer(GameObject gameObject)
        {
            var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = Undo.AddComponent<SpriteRenderer>(gameObject);
            }

            EditorUtility.SetDirty(spriteRenderer);
        }

        private static void ApplyPlayerLayer(Component component, string fieldName)
        {
            if (component == null)
            {
                return;
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0)
            {
                Debug.LogWarning($"[LevelRuntimeAssist] Player layer is missing. '{component.gameObject.name}' was created without {fieldName} setup.");
                return;
            }

            var serialized = new SerializedObject(component);
            var field = serialized.FindProperty(fieldName);
            var bits = field != null ? field.FindPropertyRelative("m_Bits") : null;
            if (bits == null)
            {
                return;
            }

            bits.intValue = 1 << playerLayer;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
        }

        private static void ApplyNamedLayer(GameObject gameObject, string layerName, string contextName)
        {
            if (gameObject == null)
            {
                return;
            }

            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[LevelRuntimeAssist] {contextName} could not assign missing layer '{layerName}' on '{gameObject.name}'.");
                return;
            }

            Undo.RecordObject(gameObject, $"Set {contextName} Layer");
            gameObject.layer = layer;
            EditorUtility.SetDirty(gameObject);
        }

        private static void SetObjectReference(Component component, string fieldName, Object value)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
        }

        private static void SetObjectArray(Component component, string fieldName, Object value)
        {
            var serialized = new SerializedObject(component);
            var array = serialized.FindProperty(fieldName);
            if (array == null || !array.isArray)
            {
                return;
            }

            array.arraySize = 1;
            array.GetArrayElementAtIndex(0).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
        }

        private static void SetIntValue(Component component, string fieldName, int value)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                return;
            }

            property.intValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
        }

        private static void SetDoorInitialState(Door door, DoorState doorState)
        {
            if (door == null)
            {
                return;
            }

            var serialized = new SerializedObject(door);
            var initialStateProperty = serialized.FindProperty("_initialState");
            if (initialStateProperty == null)
            {
                return;
            }

            initialStateProperty.enumValueIndex = (int)doorState;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(door);
        }

        private static Vector3 GetRoomCenter(Room room)
        {
            var box = room != null ? room.GetComponent<BoxCollider2D>() : null;
            if (room == null || box == null)
            {
                return room != null ? room.transform.position : Vector3.zero;
            }

            Rect rect = LevelArchitectWindow.GetRoomWorldRect(room, box);
            return new Vector3(rect.center.x, rect.center.y, room.transform.position.z);
        }

        private static Vector2 GetSuggestedTriggerSize(Room room, float widthScale, float heightScale, Vector2 minimumSize)
        {
            var box = room != null ? room.GetComponent<BoxCollider2D>() : null;
            if (box == null)
            {
                return minimumSize;
            }

            Vector2 roomSize = box.size;
            return new Vector2(
                Mathf.Max(minimumSize.x, roomSize.x * widthScale),
                Mathf.Max(minimumSize.y, roomSize.y * heightScale));
        }

        private static void FinalizeCreatedObject(GameObject gameObject, string logMessage)
        {
            if (gameObject == null)
            {
                return;
            }

            Selection.activeGameObject = gameObject;
            EditorUtility.SetDirty(gameObject);
            SceneView.RepaintAll();
            Debug.Log(logMessage);
        }
    }
}
