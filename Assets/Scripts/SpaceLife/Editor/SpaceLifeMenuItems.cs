using UnityEditor;
using UnityEngine;
using ProjectArk.SpaceLife.Data;

namespace ProjectArk.SpaceLife.Editor
{
    public static class SpaceLifeMenuItems
    {
        [MenuItem("ProjectArk/Space Life/Setup/Phase 1 - Core & Basics", priority = 10)]
        public static void SetupPhase1()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase1_CoreBasics);
        }

        [MenuItem("ProjectArk/Space Life/Setup/Phase 2 - NPC System", priority = 11)]
        public static void SetupPhase2()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase2_NPCSystem);
        }

        [MenuItem("ProjectArk/Space Life/Setup/Phase 3 - Relationship & Gifting", priority = 12)]
        public static void SetupPhase3()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase3_RelationshipGifting);
        }

        [MenuItem("ProjectArk/Space Life/Setup/Phase 4 - Rooms & Scenes", priority = 13)]
        public static void SetupPhase4()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase4_RoomsScenes);
        }

        [MenuItem("ProjectArk/Space Life/Setup/Phase 5 - Full Integration", priority = 14)]
        public static void SetupPhase5()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase5_FullIntegration);
        }

        [MenuItem("ProjectArk/Space Life/Setup/All Phases - Complete Setup", priority = 15)]
        public static void SetupAllPhases()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.AllPhases);
        }

        private static void SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase phase)
        {
            SpaceLifeSetupWindow.ShowWindow(phase);
        }

        internal static UnityEngine.Object FindInputActionAsset()
        {
            var guids = AssetDatabase.FindAssets("ShipActions t:InputActionAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset != null)
                {
                    return asset;
                }
            }
            return null;
        }

        internal static Sprite CreateSquareSprite(Color color)
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            Rect rect = new Rect(0, 0, 64, 64);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, rect, pivot, 64);
        }

        [MenuItem("ProjectArk/Space Life/Create/SpaceLifeManager", priority = 30)]
        public static void CreateSpaceLifeManager()
        {
            CreateSingleton<SpaceLifeManager>("SpaceLifeManager");
        }

        [MenuItem("ProjectArk/Space Life/Create/SpaceLifeInputHandler", priority = 31)]
        public static void CreateSpaceLifeInputHandler()
        {
            var go = CreateSingleton<SpaceLifeInputHandler>("SpaceLifeInputHandler");
            if (go != null)
            {
                var handler = go.GetComponent<SpaceLifeInputHandler>();
                var inputAsset = FindInputActionAsset();
                if (inputAsset != null && handler != null)
                {
                    var serializedObject = new SerializedObject(handler);
                    var property = serializedObject.FindProperty("_inputActions");
                    property.objectReferenceValue = inputAsset;
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log($"[SpaceLifeMenuItems] Auto-assigned InputActionAsset to SpaceLifeInputHandler");
                }
            }
        }

        [MenuItem("ProjectArk/Space Life/Create/Player Controller", priority = 32)]
        public static void CreatePlayerController()
        {
            if (Object.FindFirstObjectByType<PlayerController2D>() != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "Player Exists",
                    "A PlayerController2D already exists in the scene.\n" +
                    "Do you want to create another one?",
                    "Yes",
                    "No"))
                {
                    return;
                }
            }

            GameObject go = new GameObject("Player2D");
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CapsuleCollider2D>();
            var playerController = go.AddComponent<PlayerController2D>();
            var playerInteraction = go.AddComponent<PlayerInteraction>();
            
            var inputAsset = FindInputActionAsset();
            if (inputAsset != null)
            {
                var serializedController = new SerializedObject(playerController);
                var controllerProp = serializedController.FindProperty("_inputActions");
                controllerProp.objectReferenceValue = inputAsset;
                serializedController.ApplyModifiedProperties();
                
                var serializedInteraction = new SerializedObject(playerInteraction);
                var interactionProp = serializedInteraction.FindProperty("_inputActions");
                interactionProp.objectReferenceValue = inputAsset;
                serializedInteraction.ApplyModifiedProperties();
                
                Debug.Log($"[SpaceLifeMenuItems] Auto-assigned InputActionAsset to Player2D");
            }
            
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Player2D");
            Debug.Log("[SpaceLife] Created Player2D");
        }

        [MenuItem("ProjectArk/Space Life/Create/SpaceLifeRoomManager", priority = 33)]
        public static void CreateRoomManager()
        {
            CreateSingleton<SpaceLifeRoomManager>("SpaceLifeRoomManager");
        }

        [MenuItem("ProjectArk/Space Life/Create/RelationshipManager", priority = 34)]
        public static void CreateRelationshipManager()
        {
            CreateSingleton<RelationshipManager>("RelationshipManager");
        }

        [MenuItem("ProjectArk/Space Life/Create/GiftInventory", priority = 35)]
        public static void CreateGiftInventory()
        {
            CreateSingleton<GiftInventory>("GiftInventory");
        }

        [MenuItem("ProjectArk/Space Life/Create/Create NPC", priority = 40)]
        public static void CreateNPC()
        {
            GameObject npcGo = new GameObject("NPC");
            npcGo.AddComponent<NPCController>();
            npcGo.AddComponent<Interactable>();
            var collider = npcGo.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            
            Selection.activeGameObject = npcGo;
            Undo.RegisterCreatedObjectUndo(npcGo, "Create NPC");
            Debug.Log("[SpaceLife] NPC created");
        }

        [MenuItem("ProjectArk/Space Life/Create/Create Room", priority = 41)]
        public static void CreateRoom()
        {
            GameObject roomGo = new GameObject("Room");
            roomGo.AddComponent<SpaceLifeRoom>();
            
            Selection.activeGameObject = roomGo;
            Undo.RegisterCreatedObjectUndo(roomGo, "Create Room");
            Debug.Log("[SpaceLife] Room created");
        }

        [MenuItem("ProjectArk/Space Life/Create/Create Door", priority = 42)]
        public static void CreateDoor()
        {
            GameObject doorGo = new GameObject("Door");
            doorGo.AddComponent<SpaceLifeDoor>();
            doorGo.AddComponent<Interactable>();
            
            Selection.activeGameObject = doorGo;
            Undo.RegisterCreatedObjectUndo(doorGo, "Create Door");
            Debug.Log("[SpaceLife] Door created");
        }

        [MenuItem("ProjectArk/Space Life/Create/Create Interactable Object", priority = 43)]
        public static void CreateInteractable()
        {
            GameObject go = new GameObject("Interactable");
            go.AddComponent<Interactable>();
            
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Interactable");
            Debug.Log("[SpaceLife] Interactable created");
        }

        [MenuItem("ProjectArk/Space Life/Create/Create Dialogue UI", priority = 44)]
        public static void CreateDialogueUI()
        {
            CreateSingleton<DialogueUI>("DialogueUI");
        }

        [MenuItem("ProjectArk/Space Life/Create/Create Gift UI", priority = 45)]
        public static void CreateGiftUI()
        {
            CreateSingleton<GiftUI>("GiftUI");
        }

        [MenuItem("ProjectArk/Space Life/Create/Create Minimap UI", priority = 46)]
        public static void CreateMinimapUI()
        {
            CreateSingleton<MinimapUI>("MinimapUI");
        }

        [MenuItem("ProjectArk/Space Life/Create/Create Transition UI", priority = 47)]
        public static void CreateTransitionUI()
        {
            CreateSingleton<TransitionUI>("TransitionUI");
        }

        [MenuItem("ProjectArk/Space Life/Data/Create NPC Data", priority = 60)]
        public static void CreateNPCData()
        {
            CreateScriptableObject<NPCDataSO>("NewNPCData");
        }

        [MenuItem("ProjectArk/Space Life/Data/Create Item Data", priority = 61)]
        public static void CreateItemData()
        {
            CreateScriptableObject<ItemSO>("NewItem");
        }

        private static GameObject CreateSingleton<T>(string name) where T : MonoBehaviour
        {
            if (Object.FindFirstObjectByType<T>() != null)
            {
                Debug.LogWarning($"[SpaceLife] {name} already exists!");
                var existing = Object.FindFirstObjectByType<T>().gameObject;
                Selection.activeObject = existing;
                return existing;
            }
            else
            {
                GameObject go = new GameObject(name);
                go.AddComponent<T>();
                Selection.activeGameObject = go;
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                Debug.Log($"[SpaceLife] {name} created");
                return go;
            }
        }

        private static void CreateScriptableObject<T>(string defaultName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = EditorUtility.SaveFilePanelInProject(
                $"Save {typeof(T).Name}",
                defaultName,
                "asset",
                $"Please enter a name for the {typeof(T).Name}");

            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = asset;
            Debug.Log($"[SpaceLife] {typeof(T).Name} created at {path}");
        }
    }
}
