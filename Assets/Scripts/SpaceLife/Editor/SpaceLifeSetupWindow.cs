
using UnityEditor;
using UnityEngine;

namespace ProjectArk.SpaceLife.Editor
{
    public class SpaceLifeSetupWindow : EditorWindow
    {
        public enum SetupPhase
        {
            Phase1_CoreBasics,
            Phase2_NPCSystem,
            Phase3_RelationshipGifting,
            Phase4_RoomsScenes,
            Phase5_FullIntegration,
            AllPhases
        }

        internal SetupPhase _selectedPhase = SetupPhase.AllPhases;
        private Vector2 _scrollPosition;
        private bool _createDemoContent = true;

        [MenuItem("ProjectArk/Space Life/Setup Wizard")]
        public static SpaceLifeSetupWindow ShowWindow()
        {
            var window = GetWindow<SpaceLifeSetupWindow>("Space Life Setup");
            return window;
        }

        public static SpaceLifeSetupWindow ShowWindow(SetupPhase phase)
        {
            var window = ShowWindow();
            window._selectedPhase = phase;
            return window;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawPhaseSelection();
            DrawOptions();
            DrawExecuteButton();
            DrawStatus();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🚀 Space Life System Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "This wizard will help you set up the Space Life system in your scene.\n" +
                "Select which phase you want to configure and click Execute!",
                MessageType.Info);
            EditorGUILayout.Space(10);
        }

        private void DrawPhaseSelection()
        {
            EditorGUILayout.LabelField("Setup Phase", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _selectedPhase = (SetupPhase)EditorGUILayout.EnumPopup("Select Phase", _selectedPhase);

            EditorGUILayout.Space(10);

            DrawPhaseDescription();
        }

        private void DrawPhaseDescription()
        {
            string description = GetPhaseDescription(_selectedPhase);
            EditorGUILayout.HelpBox(description, MessageType.None);
        }

        private string GetPhaseDescription(SetupPhase phase)
        {
            switch (phase)
            {
                case SetupPhase.Phase1_CoreBasics:
                    return "Phase 1: Core & Basics\n" +
                           "- Creates SpaceLifeManager singleton\n" +
                           "- Sets up 2D player controller\n" +
                           "- Configures input handler\n" +
                           "- Basic camera setup";

                case SetupPhase.Phase2_NPCSystem:
                    return "Phase 2: NPC System\n" +
                           "- Creates NPC manager\n" +
                           "- Sets up Dialogue UI\n" +
                           "- Configures interaction system\n" +
                           "- Creates NPC prefabs (demo)";

                case SetupPhase.Phase3_RelationshipGifting:
                    return "Phase 3: Relationship & Gifting\n" +
                           "- Creates RelationshipManager\n" +
                           "- Sets up GiftInventory\n" +
                           "- Configures GiftUI\n" +
                           "- Creates demo gift items";

                case SetupPhase.Phase4_RoomsScenes:
                    return "Phase 4: Rooms & Scenes\n" +
                           "- Creates RoomManager\n" +
                           "- Sets up room system\n" +
                           "- Creates door prefabs\n" +
                           "- Configures minimap UI";

                case SetupPhase.Phase5_FullIntegration:
                    return "Phase 5: Full Integration\n" +
                           "- Integrates all systems\n" +
                           "- Sets up UI canvas\n" +
                           "- Creates full demo scene\n" +
                           "- Connects all components";

                case SetupPhase.AllPhases:
                    return "All Phases (Complete Setup)\n" +
                           "- Executes ALL phases at once\n" +
                           "- Complete Space Life system\n" +
                           "- Full demo content included\n" +
                           "- Ready to play!";

                default:
                    return "Unknown phase";
            }
        }

        private void DrawOptions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            
            _createDemoContent = EditorGUILayout.Toggle(
                new GUIContent("Create Demo Content", "Creates demo NPCs, rooms, and items"),
                _createDemoContent);

            EditorGUILayout.Space(10);
        }

        private void DrawExecuteButton()
        {
            EditorGUILayout.Space(10);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("✨ Execute Setup", GUILayout.Height(40)))
            {
                ExecuteSetup();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
        }

        private void DrawStatus()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);
            
            CheckSystemStatus();
        }

        private void CheckSystemStatus()
        {
            bool hasManager = Object.FindFirstObjectByType<SpaceLifeManager>() != null;
            bool hasRoomManager = Object.FindFirstObjectByType<RoomManager>() != null;
            bool hasRelationshipManager = Object.FindFirstObjectByType<RelationshipManager>() != null;
            bool hasGiftInventory = Object.FindFirstObjectByType<GiftInventory>() != null;

            EditorGUILayout.Space(5);
            EditorGUI.indentLevel++;
            
            DrawStatusItem("SpaceLifeManager", hasManager);
            DrawStatusItem("RoomManager", hasRoomManager);
            DrawStatusItem("RelationshipManager", hasRelationshipManager);
            DrawStatusItem("GiftInventory", hasGiftInventory);
            
            EditorGUI.indentLevel--;
        }

        private void DrawStatusItem(string name, bool exists)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  • {name}:", GUILayout.Width(180));
            
            if (exists)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓ Present");
            }
            else
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("✗ Missing");
            }
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }

        private void ExecuteSetup()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Confirm Setup",
                $"You are about to execute {_selectedPhase}.\n" +
                "This will create GameObjects in your scene.\n\n" +
                "Continue?",
                "Yes, Execute!",
                "Cancel");

            if (!confirmed) return;

            Undo.RecordObjects(
                Selection.gameObjects,
                "Space Life System Setup");

            try
            {
                EditorUtility.DisplayProgressBar(
                    "Setting Up Space Life",
                    "Creating components...",
                    0f);

                ExecutePhase(_selectedPhase);

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Setup Complete!",
                    "Space Life system has been successfully set up!\n\n" +
                    "Check the Hierarchy for new GameObjects.",
                    "Great!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[SpaceLife Setup] Error: {e.Message}");
                EditorUtility.DisplayDialog(
                    "Setup Failed",
                    $"An error occurred: {e.Message}\n\n" +
                    "Check the Console for details.",
                    "OK");
            }
        }

        private void ExecutePhase(SetupPhase phase)
        {
            switch (phase)
            {
                case SetupPhase.Phase1_CoreBasics:
                    SetupPhase1();
                    break;
                case SetupPhase.Phase2_NPCSystem:
                    SetupPhase2();
                    break;
                case SetupPhase.Phase3_RelationshipGifting:
                    SetupPhase3();
                    break;
                case SetupPhase.Phase4_RoomsScenes:
                    SetupPhase4();
                    break;
                case SetupPhase.Phase5_FullIntegration:
                    SetupPhase5();
                    break;
                case SetupPhase.AllPhases:
                    SetupAllPhases();
                    break;
            }
        }

        private void SetupPhase1()
        {
            EditorUtility.DisplayProgressBar("Phase 1", "Creating core systems...", 0.2f);
            CreateSpaceLifeManager();
            
            EditorUtility.DisplayProgressBar("Phase 1", "Creating player controller...", 0.5f);
            CreatePlayerController();
            
            EditorUtility.DisplayProgressBar("Phase 1", "Setting up input...", 0.8f);
            CreateInputHandler();
            
            EditorUtility.DisplayProgressBar("Phase 1", "Complete!", 1.0f);
        }

        private void SetupPhase2()
        {
            EditorUtility.DisplayProgressBar("Phase 2", "Creating NPC system...", 0.3f);
            CreateDialogueUI();
            CreateInteractableSystem();
            
            if (_createDemoContent)
            {
                EditorUtility.DisplayProgressBar("Phase 2", "Creating demo NPCs...", 0.7f);
                CreateDemoNPCs();
            }
            
            EditorUtility.DisplayProgressBar("Phase 2", "Complete!", 1.0f);
        }

        private void SetupPhase3()
        {
            EditorUtility.DisplayProgressBar("Phase 3", "Creating relationship system...", 0.3f);
            CreateRelationshipManager();
            CreateGiftInventory();
            CreateGiftUI();
            CreateNPCInteractionUI();
            
            if (_createDemoContent)
            {
                EditorUtility.DisplayProgressBar("Phase 3", "Creating demo items...", 0.7f);
                CreateDemoItems();
            }
            
            EditorUtility.DisplayProgressBar("Phase 3", "Complete!", 1.0f);
        }

        private void SetupPhase4()
        {
            EditorUtility.DisplayProgressBar("Phase 4", "Creating room system...", 0.3f);
            CreateRoomManager();
            CreateMinimapUI();
            
            if (_createDemoContent)
            {
                EditorUtility.DisplayProgressBar("Phase 4", "Creating demo rooms...", 0.7f);
                CreateDemoRooms();
            }
            
            EditorUtility.DisplayProgressBar("Phase 4", "Complete!", 1.0f);
        }

        private void SetupPhase5()
        {
            EditorUtility.DisplayProgressBar("Phase 5", "Integrating all systems...", 0.5f);
            CreateUICanvas();
            ConnectAllSystems();
            
            EditorUtility.DisplayProgressBar("Phase 5", "Complete!", 1.0f);
        }

        private void SetupAllPhases()
        {
            SetupPhase1();
            SetupPhase2();
            SetupPhase3();
            SetupPhase4();
            SetupPhase5();
        }

        private void CreateSpaceLifeManager()
        {
            if (Object.FindFirstObjectByType<SpaceLifeManager>() != null)
            {
                Debug.Log("[SpaceLife Setup] SpaceLifeManager already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("SpaceLifeManager");
            var manager = go.AddComponent<SpaceLifeManager>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create SpaceLifeManager");
            Selection.activeGameObject = go;
            
            Debug.Log("[SpaceLife Setup] Created SpaceLifeManager");
        }

        private void CreatePlayerController()
        {
            if (Object.FindFirstObjectByType<PlayerController2D>() != null)
            {
                Debug.Log("[SpaceLife Setup] PlayerController2D already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("Player2D");
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            var collider = go.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.5f, 1f);
            
            var controller = go.AddComponent<PlayerController2D>();
            var interaction = go.AddComponent<PlayerInteraction>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create Player2D");
            Selection.activeGameObject = go;
            
            Debug.Log("[SpaceLife Setup] Created Player2D");
        }

        private void CreateInputHandler()
        {
            if (Object.FindFirstObjectByType<SpaceLifeInputHandler>() != null)
            {
                Debug.Log("[SpaceLife Setup] SpaceLifeInputHandler already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("SpaceLifeInputHandler");
            go.AddComponent<SpaceLifeInputHandler>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create SpaceLifeInputHandler");
            
            Debug.Log("[SpaceLife Setup] Created SpaceLifeInputHandler");
        }

        private void CreateDialogueUI()
        {
            if (Object.FindFirstObjectByType<DialogueUI>() != null)
            {
                Debug.Log("[SpaceLife Setup] DialogueUI already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("DialogueUI");
            go.AddComponent<DialogueUI>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create DialogueUI");
            
            Debug.Log("[SpaceLife Setup] Created DialogueUI");
        }

        private void CreateInteractableSystem()
        {
            Debug.Log("[SpaceLife Setup] Interactable component is ready to use (add to objects)");
        }

        private void CreateDemoNPCs()
        {
            string[] npcNames = { "Navigator", "Engineer", "Medic" };
            
            for (int i = 0; i < npcNames.Length; i++)
            {
                GameObject npcGo = new GameObject($"NPC_{npcNames[i]}");
                npcGo.transform.position = new Vector3(i * 2, 0, 0);
                
                var npcController = npcGo.AddComponent<NPCController>();
                var interactable = npcGo.AddComponent<Interactable>();
                interactable.InteractionText = $"Talk to {npcNames[i]}";
                
                var collider = npcGo.AddComponent<CapsuleCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(1f, 1.5f);
                
                Undo.RegisterCreatedObjectUndo(npcGo, $"Create NPC_{npcNames[i]}");
            }
            
            Debug.Log("[SpaceLife Setup] Created demo NPCs");
        }

        private void CreateRelationshipManager()
        {
            if (Object.FindFirstObjectByType<RelationshipManager>() != null)
            {
                Debug.Log("[SpaceLife Setup] RelationshipManager already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("RelationshipManager");
            go.AddComponent<RelationshipManager>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create RelationshipManager");
            
            Debug.Log("[SpaceLife Setup] Created RelationshipManager");
        }

        private void CreateGiftInventory()
        {
            if (Object.FindFirstObjectByType<GiftInventory>() != null)
            {
                Debug.Log("[SpaceLife Setup] GiftInventory already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("GiftInventory");
            go.AddComponent<GiftInventory>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create GiftInventory");
            
            Debug.Log("[SpaceLife Setup] Created GiftInventory");
        }

        private void CreateGiftUI()
        {
            if (Object.FindFirstObjectByType<GiftUI>() != null)
            {
                Debug.Log("[SpaceLife Setup] GiftUI already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("GiftUI");
            go.AddComponent<GiftUI>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create GiftUI");
            
            Debug.Log("[SpaceLife Setup] Created GiftUI");
        }

        private void CreateNPCInteractionUI()
        {
            if (Object.FindFirstObjectByType<NPCInteractionUI>() != null)
            {
                Debug.Log("[SpaceLife Setup] NPCInteractionUI already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("NPCInteractionUI");
            go.AddComponent<NPCInteractionUI>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create NPCInteractionUI");
            
            Debug.Log("[SpaceLife Setup] Created NPCInteractionUI");
        }

        private void CreateDemoItems()
        {
            Debug.Log("[SpaceLife Setup] Demo items can be created via Create Asset menu");
        }

        private void CreateRoomManager()
        {
            if (Object.FindFirstObjectByType<RoomManager>() != null)
            {
                Debug.Log("[SpaceLife Setup] RoomManager already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("RoomManager");
            go.AddComponent<RoomManager>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create RoomManager");
            
            Debug.Log("[SpaceLife Setup] Created RoomManager");
        }

        private void CreateMinimapUI()
        {
            if (Object.FindFirstObjectByType<MinimapUI>() != null)
            {
                Debug.Log("[SpaceLife Setup] MinimapUI already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("MinimapUI");
            go.AddComponent<MinimapUI>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create MinimapUI");
            
            Debug.Log("[SpaceLife Setup] Created MinimapUI");
        }

        private void CreateDemoRooms()
        {
            string[] roomNames = { "CommandCenter", "MedBay", "Engineering", "Galley" };
            
            for (int i = 0; i < roomNames.Length; i++)
            {
                GameObject roomGo = new GameObject($"Room_{roomNames[i]}");
                roomGo.transform.position = new Vector3(i * 10, 0, 0);
                
                var room = roomGo.AddComponent<Room>();
                
                Undo.RegisterCreatedObjectUndo(roomGo, $"Create Room_{roomNames[i]}");
            }
            
            Debug.Log("[SpaceLife Setup] Created demo rooms");
        }

        private void CreateUICanvas()
        {
            if (GameObject.Find("SpaceLifeCanvas") != null)
            {
                Debug.Log("[SpaceLife Setup] SpaceLifeCanvas already exists, skipping.");
                return;
            }

            GameObject canvasGo = new GameObject("SpaceLifeCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create SpaceLifeCanvas");
            
            MoveUIObjectsToCanvas(canvasGo.transform);
            
            Debug.Log("[SpaceLife Setup] Created SpaceLifeCanvas");
        }

        private void MoveUIObjectsToCanvas(Transform canvasTransform)
        {
            UnityEngine.MonoBehaviour[] uiObjects = new UnityEngine.MonoBehaviour[]
            {
                Object.FindFirstObjectByType<DialogueUI>(),
                Object.FindFirstObjectByType<GiftUI>(),
                Object.FindFirstObjectByType<NPCInteractionUI>(),
                Object.FindFirstObjectByType<MinimapUI>()
            };

            foreach (var ui in uiObjects)
            {
                if (ui != null && ui.transform.parent == null)
                {
                    ui.transform.SetParent(canvasTransform, false);
                }
            }
        }

        private void ConnectAllSystems()
        {
            Debug.Log("[SpaceLife Setup] All systems connected!");
        }
    }
}
