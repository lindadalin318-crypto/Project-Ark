
using UnityEditor;
using UnityEngine;
using ProjectArk.SpaceLife.Data;

namespace ProjectArk.SpaceLife.Editor
{
    public static class SpaceLifeMenuItems
    {
        [MenuItem("Project Ark/Space Life/Setup/Phase 1 - Core & Basics", priority = 10)]
        public static void SetupPhase1()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase1_CoreBasics);
        }

        [MenuItem("Project Ark/Space Life/Setup/Phase 2 - NPC System", priority = 11)]
        public static void SetupPhase2()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase2_NPCSystem);
        }

        [MenuItem("Project Ark/Space Life/Setup/Phase 3 - Relationship & Gifting", priority = 12)]
        public static void SetupPhase3()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase3_RelationshipGifting);
        }

        [MenuItem("Project Ark/Space Life/Setup/Phase 4 - Rooms & Scenes", priority = 13)]
        public static void SetupPhase4()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase4_RoomsScenes);
        }

        [MenuItem("Project Ark/Space Life/Setup/Phase 5 - Full Integration", priority = 14)]
        public static void SetupPhase5()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.Phase5_FullIntegration);
        }

        [MenuItem("Project Ark/Space Life/Setup/All Phases - Complete Setup", priority = 15)]
        public static void SetupAllPhases()
        {
            SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase.AllPhases);
        }

        private static void SetupWizardForPhase(SpaceLifeSetupWindow.SetupPhase phase)
        {
            var window = SpaceLifeSetupWindow.ShowWindow(phase);
        }

        [MenuItem("Project Ark/Space Life/Create/SpaceLifeManager", priority = 30)]
        public static void CreateSpaceLifeManager()
        {
            CreateSingleton<SpaceLifeManager>("SpaceLifeManager");
        }

        [MenuItem("Project Ark/Space Life/Create/Player Controller", priority = 31)]
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
            go.AddComponent<Rigidbody2D>().gravityScale = 3f;
            go.AddComponent<CapsuleCollider2D>();
            go.AddComponent<PlayerController2D>();
            go.AddComponent<PlayerInteraction>();
            
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Player2D");
            Debug.Log("[SpaceLife] Created Player2D");
        }

        [MenuItem("Project Ark/Space Life/Create/RoomManager", priority = 32)]
        public static void CreateRoomManager()
        {
            CreateSingleton<RoomManager>("RoomManager");
        }

        [MenuItem("Project Ark/Space Life/Create/RelationshipManager", priority = 33)]
        public static void CreateRelationshipManager()
        {
            CreateSingleton<RelationshipManager>("RelationshipManager");
        }

        [MenuItem("Project Ark/Space Life/Create/GiftInventory", priority = 34)]
        public static void CreateGiftInventory()
        {
            CreateSingleton<GiftInventory>("GiftInventory");
        }

        [MenuItem("Project Ark/Space Life/Create/Create NPC", priority = 40)]
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

        [MenuItem("Project Ark/Space Life/Create/Create Room", priority = 41)]
        public static void CreateRoom()
        {
            GameObject roomGo = new GameObject("Room");
            roomGo.AddComponent<Room>();
            
            Selection.activeGameObject = roomGo;
            Undo.RegisterCreatedObjectUndo(roomGo, "Create Room");
            Debug.Log("[SpaceLife] Room created");
        }

        [MenuItem("Project Ark/Space Life/Create/Create Door", priority = 42)]
        public static void CreateDoor()
        {
            GameObject doorGo = new GameObject("Door");
            doorGo.AddComponent<Door>();
            doorGo.AddComponent<Interactable>();
            
            Selection.activeGameObject = doorGo;
            Undo.RegisterCreatedObjectUndo(doorGo, "Create Door");
            Debug.Log("[SpaceLife] Door created");
        }

        [MenuItem("Project Ark/Space Life/Create/Create Interactable Object", priority = 43)]
        public static void CreateInteractable()
        {
            GameObject go = new GameObject("Interactable");
            go.AddComponent<Interactable>();
            
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Interactable");
            Debug.Log("[SpaceLife] Interactable created");
        }

        [MenuItem("Project Ark/Space Life/Assets/Create NPC Data", priority = 50)]
        public static void CreateNPCData()
        {
            CreateScriptableObject<NPCDataSO>("NPCData");
        }

        [MenuItem("Project Ark/Space Life/Assets/Create Item", priority = 51)]
        public static void CreateItem()
        {
            CreateScriptableObject<ItemSO>("Item");
        }

        private static void CreateSingleton<T>(string name) where T : MonoBehaviour
        {
            if (Object.FindFirstObjectByType<T>() != null)
            {
                Debug.LogWarning($"[SpaceLife] {name} already exists!");
                Selection.activeObject = Object.FindFirstObjectByType<T>().gameObject;
                return;
            }
            else
            {
                GameObject go = new GameObject(name);
                go.AddComponent<T>();
                Selection.activeGameObject = go;
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                Debug.Log($"[SpaceLife] {name} created");
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
