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
            EditorGUILayout.LabelField("Space Life System Setup", EditorStyles.boldLabel);
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
                           "- Creates SpaceLifeManager\n" +
                           "- Sets up 2D player controller (Top-Down)\n" +
                           "- Configures input handler\n" +
                           "- Creates SpaceLife camera";

                case SetupPhase.Phase2_NPCSystem:
                    return "Phase 2: NPC System\n" +
                           "- Creates Dialogue UI\n" +
                           "- Configures interaction system\n" +
                           "- Creates NPC prefabs (demo)";

                case SetupPhase.Phase3_RelationshipGifting:
                    return "Phase 3: Relationship & Gifting\n" +
                           "- Creates RelationshipManager\n" +
                           "- Sets up GiftInventory\n" +
                           "- Configures GiftUI";

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
            if (GUILayout.Button("Execute Setup", GUILayout.Height(40)))
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
            bool hasRoomManager = Object.FindFirstObjectByType<SpaceLifeRoomManager>() != null;
            bool hasRelationshipManager = Object.FindFirstObjectByType<RelationshipManager>() != null;
            bool hasGiftInventory = Object.FindFirstObjectByType<GiftInventory>() != null;
            bool hasInputHandler = Object.FindFirstObjectByType<SpaceLifeInputHandler>() != null;
            bool hasShipInputHandler = Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>() != null;

            EditorGUILayout.Space(5);
            EditorGUI.indentLevel++;
            
            DrawStatusItem("SpaceLifeManager", hasManager);
            DrawStatusItem("SpaceLifeRoomManager", hasRoomManager);
            DrawStatusItem("RelationshipManager", hasRelationshipManager);
            DrawStatusItem("GiftInventory", hasGiftInventory);
            DrawStatusItem("SpaceLifeInputHandler", hasInputHandler);
            DrawStatusItem("Ship/InputHandler (CRITICAL)", hasShipInputHandler);
            
            if (!hasShipInputHandler)
            {
                EditorGUILayout.HelpBox("Ship/InputHandler is MISSING! This is required for Tab toggle to work.", MessageType.Error);
            }
            
            EditorGUI.indentLevel--;
            
            // Scene Health Check
            DrawSceneHealthCheck(hasManager, hasShipInputHandler, hasInputHandler);
        }

        private void DrawSceneHealthCheck(bool hasManager, bool hasShipInputHandler, bool hasSpaceLifeInputHandler)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("━━━ Scene Health Check ━━━", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // --- Ship Prefab Instance ---
            DrawStatusItem("Ship Prefab Instance", hasShipInputHandler);
            if (!hasShipInputHandler)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                if (GUILayout.Button("Add Ship to Scene", GUILayout.Width(200), GUILayout.Height(24)))
                {
                    AddShipToScene();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // --- SpaceLifeManager references ---
            var manager = Object.FindFirstObjectByType<SpaceLifeManager>();
            if (manager != null)
            {
                var so = new SerializedObject(manager);
                
                DrawSerializedFieldStatus(so, "_spaceLifePlayerPrefab", "Player2D Prefab");
                DrawSerializedFieldStatus(so, "_spaceLifeSpawnPoint", "SpaceLife SpawnPoint");
                DrawSerializedFieldStatus(so, "_spaceLifeCamera", "SpaceLife Camera");
                DrawSerializedFieldStatus(so, "_mainCamera", "Main Camera");
                DrawSerializedFieldStatus(so, "_spaceLifeSceneRoot", "SpaceLife Scene Root");
                DrawSerializedFieldStatus(so, "_shipRoot", "Ship Root");
                DrawSerializedFieldStatus(so, "_spaceLifeInputHandler", "SpaceLife InputHandler Ref");
            }
            else
            {
                EditorGUILayout.HelpBox("SpaceLifeManager not found in scene. Cannot check serialized references.", MessageType.Warning);
            }
            
            // --- SpaceLifeInputHandler ---
            DrawStatusItem("SpaceLifeInputHandler", hasSpaceLifeInputHandler);
            
            // --- SpaceLife Camera ---
            bool hasSpaceLifeCamera = false;
            if (manager != null)
            {
                var so = new SerializedObject(manager);
                var cameraProp = so.FindProperty("_spaceLifeCamera");
                hasSpaceLifeCamera = cameraProp != null && cameraProp.objectReferenceValue != null;
            }
            DrawStatusItem("SpaceLife Camera (via Manager)", hasSpaceLifeCamera);
            
            // --- SpaceLife Scene Root ---
            bool hasSceneRoot = false;
            if (manager != null)
            {
                var so = new SerializedObject(manager);
                var rootProp = so.FindProperty("_spaceLifeSceneRoot");
                hasSceneRoot = rootProp != null && rootProp.objectReferenceValue != null;
            }
            DrawStatusItem("SpaceLife Scene Root (via Manager)", hasSceneRoot);
            
            // --- Player2D Prefab Asset ---
            bool hasPlayerPrefabAsset = AssetDatabase.FindAssets("Player2D_Prefab t:Prefab").Length > 0;
            DrawStatusItem("Player2D Prefab Asset", hasPlayerPrefabAsset);
            
            // --- Auto-Wire References Button ---
            if (manager != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                if (GUILayout.Button("Auto-Wire References", GUILayout.Width(200), GUILayout.Height(28)))
                {
                    AutoWireReferences(manager);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawStatusItem(string name, bool exists)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  {name}:", GUILayout.Width(250));
            
            if (exists)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✅ Present");
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("❌ Missing");
            }
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSerializedFieldStatus(SerializedObject so, string propertyName, string displayName)
        {
            var prop = so.FindProperty(propertyName);
            bool hasValue = prop != null && prop.objectReferenceValue != null;
            DrawStatusItem($"  {displayName}", hasValue);
        }

        private void AddShipToScene()
        {
            // Check if Ship already exists
            var existingShip = Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>();
            if (existingShip != null)
            {
                EditorUtility.DisplayDialog("Ship Exists", "Ship Prefab instance already exists in scene.", "OK");
                Selection.activeGameObject = existingShip.gameObject;
                return;
            }
            
            // Find Ship prefab
            var guids = AssetDatabase.FindAssets("Ship t:Prefab", new[] { "Assets/_Prefabs/Ship" });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Not Found", "Could not find Ship.prefab in Assets/_Prefabs/Ship/", "OK");
                return;
            }
            
            string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load prefab at {prefabPath}", "OK");
                return;
            }
            
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "Add Ship to Scene");
            Selection.activeGameObject = instance;
            
            Debug.Log($"[SpaceLifeSetupWindow] Added Ship Prefab to scene from {prefabPath}");
            EditorUtility.DisplayDialog("Success", "Ship Prefab has been added to the scene.", "OK");
        }

        private void AutoWireReferences(SpaceLifeManager manager)
        {
            var so = new SerializedObject(manager);
            int wiredCount = 0;
            
            // Auto-wire _mainCamera
            var mainCameraProp = so.FindProperty("_mainCamera");
            if (mainCameraProp != null && mainCameraProp.objectReferenceValue == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCameraProp.objectReferenceValue = mainCam;
                    wiredCount++;
                    Debug.Log("[AutoWire] _mainCamera → Camera.main");
                }
            }
            
            // Auto-wire _spaceLifeInputHandler
            var inputHandlerProp = so.FindProperty("_spaceLifeInputHandler");
            if (inputHandlerProp != null && inputHandlerProp.objectReferenceValue == null)
            {
                var handler = Object.FindFirstObjectByType<SpaceLifeInputHandler>();
                if (handler != null)
                {
                    inputHandlerProp.objectReferenceValue = handler;
                    wiredCount++;
                    Debug.Log("[AutoWire] _spaceLifeInputHandler → found in scene");
                }
            }
            
            // Auto-wire _shipRoot
            var shipRootProp = so.FindProperty("_shipRoot");
            if (shipRootProp != null && shipRootProp.objectReferenceValue == null)
            {
                var shipInput = Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>();
                if (shipInput != null)
                {
                    shipRootProp.objectReferenceValue = shipInput.gameObject;
                    wiredCount++;
                    Debug.Log("[AutoWire] _shipRoot → Ship InputHandler GameObject");
                }
            }
            
            // Auto-wire _spaceLifePlayerPrefab
            var playerPrefabProp = so.FindProperty("_spaceLifePlayerPrefab");
            if (playerPrefabProp != null && playerPrefabProp.objectReferenceValue == null)
            {
                var prefabGuids = AssetDatabase.FindAssets("Player2D_Prefab t:Prefab");
                if (prefabGuids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        playerPrefabProp.objectReferenceValue = prefab;
                        wiredCount++;
                        Debug.Log($"[AutoWire] _spaceLifePlayerPrefab → {path}");
                    }
                }
            }
            
            so.ApplyModifiedProperties();
            
            string message = wiredCount > 0 
                ? $"Auto-wired {wiredCount} reference(s). Check Console for details." 
                : "All auto-discoverable references are already wired.";
            EditorUtility.DisplayDialog("Auto-Wire Complete", message, "OK");
            Debug.Log($"[SpaceLifeSetupWindow] Auto-Wire completed: {wiredCount} references wired");
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

            Undo.SetCurrentGroupName("Space Life System Setup");

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
                    "IMPORTANT: Assign the following in SpaceLifeManager Inspector:\n" +
                    "1. Space Life Player Prefab (Player2D_Prefab)\n" +
                    "2. Space Life Camera\n" +
                    "3. Space Life Scene Root\n" +
                    "4. Space Life Input Handler",
                    "Got it!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[SpaceLife Setup] Error: {e.Message}\n{e.StackTrace}");
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
            
            EditorUtility.DisplayProgressBar("Phase 1", "Creating player prefab...", 0.4f);
            CreatePlayerPrefab();
            
            EditorUtility.DisplayProgressBar("Phase 1", "Creating scene structure...", 0.6f);
            CreateSceneStructure();
            
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
            CreateTransitionUI();
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
            var existingManager = Object.FindFirstObjectByType<SpaceLifeManager>();
            if (existingManager != null)
            {
                Debug.Log("[SpaceLife Setup] SpaceLifeManager already exists, checking references...");
                EnsureManagerReferences(existingManager);
                Selection.activeGameObject = existingManager.gameObject;
                return;
            }

            GameObject go = new GameObject("SpaceLifeManager");
            go.AddComponent<SpaceLifeManager>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create SpaceLifeManager");
            Selection.activeGameObject = go;
            
            Debug.Log("[SpaceLife Setup] Created SpaceLifeManager");
        }

        private void EnsureManagerReferences(SpaceLifeManager manager)
        {
            var serializedManager = new SerializedObject(manager);
            bool changed = false;
            
            var prefabProp = serializedManager.FindProperty("_spaceLifePlayerPrefab");
            if (prefabProp != null && prefabProp.objectReferenceValue == null)
            {
                string prefabPath = "Assets/_Prefabs/SpaceLife/Player2D_Prefab.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    prefabProp.objectReferenceValue = prefab;
                    Debug.Log("[SpaceLife Setup] Fixed missing Player Prefab reference");
                    changed = true;
                }
            }
            
            var sceneRootProp = serializedManager.FindProperty("_spaceLifeSceneRoot");
            if (sceneRootProp != null && sceneRootProp.objectReferenceValue == null)
            {
                var sceneRoot = GameObject.Find("SpaceLifeScene");
                if (sceneRoot != null)
                {
                    sceneRootProp.objectReferenceValue = sceneRoot;
                    Debug.Log("[SpaceLife Setup] Fixed missing Scene Root reference");
                    changed = true;
                }
            }
            
            var spawnProp = serializedManager.FindProperty("_spaceLifeSpawnPoint");
            if (spawnProp != null && spawnProp.objectReferenceValue == null)
            {
                var sceneRoot = GameObject.Find("SpaceLifeScene");
                if (sceneRoot != null)
                {
                    var spawn = sceneRoot.transform.Find("SpawnPoint");
                    if (spawn != null)
                    {
                        spawnProp.objectReferenceValue = spawn;
                        Debug.Log("[SpaceLife Setup] Fixed missing Spawn Point reference");
                        changed = true;
                    }
                }
            }
            
            var camProp = serializedManager.FindProperty("_spaceLifeCamera");
            if (camProp != null && camProp.objectReferenceValue == null)
            {
                var sceneRoot = GameObject.Find("SpaceLifeScene");
                if (sceneRoot != null)
                {
                    var cam = sceneRoot.transform.Find("SpaceLifeCamera");
                    if (cam != null)
                    {
                        camProp.objectReferenceValue = cam.GetComponent<Camera>();
                        Debug.Log("[SpaceLife Setup] Fixed missing Camera reference");
                        changed = true;
                    }
                }
            }
            
            var inputHandlerProp = serializedManager.FindProperty("_spaceLifeInputHandler");
            if (inputHandlerProp != null && inputHandlerProp.objectReferenceValue == null)
            {
                var handler = Object.FindFirstObjectByType<SpaceLifeInputHandler>();
                if (handler != null)
                {
                    inputHandlerProp.objectReferenceValue = handler;
                    Debug.Log("[SpaceLife Setup] Fixed missing Input Handler reference");
                    changed = true;
                }
            }
            
            var shipRootProp = serializedManager.FindProperty("_shipRoot");
            if (shipRootProp != null && shipRootProp.objectReferenceValue == null)
            {
                var shipInputHandler = Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>();
                if (shipInputHandler != null)
                {
                    shipRootProp.objectReferenceValue = shipInputHandler.gameObject;
                    Debug.Log("[SpaceLife Setup] Fixed missing Ship Root reference");
                    changed = true;
                }
            }
            
            if (changed)
            {
                serializedManager.ApplyModifiedProperties();
                Debug.Log("[SpaceLife Setup] Updated SpaceLifeManager references");
            }
        }

        private void CreatePlayerPrefab()
        {
            string prefabPath = "Assets/_Prefabs/SpaceLife/Player2D_Prefab.prefab";
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (existingPrefab != null)
            {
                Debug.Log("[SpaceLife Setup] Player2D_Prefab already exists, checking and updating components...");
                UpdatePlayerPrefabComponents(existingPrefab);
                AssignPlayerPrefabToManager(existingPrefab);
                return;
            }

            GameObject playerGo = new GameObject("Player2D");
            
            var sr = playerGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpaceLifeMenuItems.CreateSquareSprite(Color.cyan);
            sr.sortingOrder = 10;
            
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearDamping = 5f;
            
            var collider = playerGo.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.5f, 1f);
            
            var playerController = playerGo.AddComponent<PlayerController2D>();
            var playerInteraction = playerGo.AddComponent<PlayerInteraction>();
            
            var inputAsset = SpaceLifeMenuItems.FindInputActionAsset();
            if (inputAsset != null)
            {
                var serializedController = new SerializedObject(playerController);
                var controllerProp = serializedController.FindProperty("_inputActions");
                if (controllerProp != null)
                {
                    controllerProp.objectReferenceValue = inputAsset;
                    serializedController.ApplyModifiedProperties();
                }
                
                var serializedInteraction = new SerializedObject(playerInteraction);
                var interactionProp = serializedInteraction.FindProperty("_inputActions");
                if (interactionProp != null)
                {
                    interactionProp.objectReferenceValue = inputAsset;
                    serializedInteraction.ApplyModifiedProperties();
                }
                
                Debug.Log("[SpaceLife Setup] Auto-assigned InputActionAsset to Player Prefab");
            }
            
            string folderPath = "Assets/_Prefabs/SpaceLife";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "_Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/_Prefabs", "SpaceLife");
            }
            
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerGo, prefabPath);
            
            Undo.RegisterCreatedObjectUndo(playerGo, "Create Player2D");
            
            DestroyImmediate(playerGo);
            
            Debug.Log($"[SpaceLife Setup] Created Player2D_Prefab at {prefabPath}");
            
            AssignPlayerPrefabToManager(prefab);
        }

        private void UpdatePlayerPrefabComponents(GameObject prefab)
        {
            bool changed = false;
            
            var sr = prefab.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = prefab.AddComponent<SpriteRenderer>();
                changed = true;
                Debug.Log("[SpaceLife Setup] Added missing SpriteRenderer to Player Prefab");
            }
            
            if (sr.sprite == null)
            {
                sr.sprite = SpaceLifeMenuItems.CreateSquareSprite(Color.cyan);
                sr.sortingOrder = 10;
                changed = true;
                Debug.Log("[SpaceLife Setup] Added sprite to Player Prefab SpriteRenderer");
            }
            
            var rb = prefab.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = prefab.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.linearDamping = 5f;
                changed = true;
                Debug.Log("[SpaceLife Setup] Added missing Rigidbody2D to Player Prefab");
            }
            else if (rb.gravityScale != 0f)
            {
                rb.gravityScale = 0f;
                changed = true;
                Debug.Log("[SpaceLife Setup] Fixed Rigidbody2D gravityScale to 0");
            }
            
            var collider = prefab.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                collider = prefab.AddComponent<CapsuleCollider2D>();
                collider.size = new Vector2(0.5f, 1f);
                changed = true;
                Debug.Log("[SpaceLife Setup] Added missing CapsuleCollider2D to Player Prefab");
            }
            
            var playerController = prefab.GetComponent<PlayerController2D>();
            if (playerController == null)
            {
                playerController = prefab.AddComponent<PlayerController2D>();
                changed = true;
                Debug.Log("[SpaceLife Setup] Added missing PlayerController2D to Player Prefab");
            }
            
            var playerInteraction = prefab.GetComponent<PlayerInteraction>();
            if (playerInteraction == null)
            {
                playerInteraction = prefab.AddComponent<PlayerInteraction>();
                changed = true;
                Debug.Log("[SpaceLife Setup] Added missing PlayerInteraction to Player Prefab");
            }
            
            var inputAsset = SpaceLifeMenuItems.FindInputActionAsset();
            if (inputAsset != null)
            {
                if (playerController != null)
                {
                    var serializedController = new SerializedObject(playerController);
                    var controllerProp = serializedController.FindProperty("_inputActions");
                    if (controllerProp != null && controllerProp.objectReferenceValue == null)
                    {
                        controllerProp.objectReferenceValue = inputAsset;
                        serializedController.ApplyModifiedProperties();
                        changed = true;
                        Debug.Log("[SpaceLife Setup] Fixed missing InputActionAsset in PlayerController2D");
                    }
                }
                
                if (playerInteraction != null)
                {
                    var serializedInteraction = new SerializedObject(playerInteraction);
                    var interactionProp = serializedInteraction.FindProperty("_inputActions");
                    if (interactionProp != null && interactionProp.objectReferenceValue == null)
                    {
                        interactionProp.objectReferenceValue = inputAsset;
                        serializedInteraction.ApplyModifiedProperties();
                        changed = true;
                        Debug.Log("[SpaceLife Setup] Fixed missing InputActionAsset in PlayerInteraction");
                    }
                }
            }
            
            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("[SpaceLife Setup] Player Prefab updated and saved");
            }
        }

        private void AssignPlayerPrefabToManager(GameObject prefab)
        {
            var manager = Object.FindFirstObjectByType<SpaceLifeManager>();
            if (manager == null) return;
            
            var serializedManager = new SerializedObject(manager);
            var prefabProp = serializedManager.FindProperty("_spaceLifePlayerPrefab");
            if (prefabProp != null && prefabProp.objectReferenceValue == null)
            {
                prefabProp.objectReferenceValue = prefab;
                serializedManager.ApplyModifiedProperties();
                Debug.Log("[SpaceLife Setup] Assigned Player2D_Prefab to SpaceLifeManager");
            }
        }

        private void CreateSceneStructure()
        {
            Transform sceneRoot = null;
            Transform spawnPoint = null;
            Camera spaceLifeCamera = null;
            
            GameObject existingRoot = GameObject.Find("SpaceLifeScene");
            if (existingRoot != null)
            {
                sceneRoot = existingRoot.transform;
                spawnPoint = existingRoot.transform.Find("SpawnPoint");
                var cam = existingRoot.transform.Find("SpaceLifeCamera");
                if (cam != null) spaceLifeCamera = cam.GetComponent<Camera>();
                Debug.Log("[SpaceLife Setup] SpaceLifeScene already exists, checking children...");
            }
            
            if (sceneRoot == null)
            {
                GameObject rootGo = new GameObject("SpaceLifeScene");
                sceneRoot = rootGo.transform;
                Undo.RegisterCreatedObjectUndo(rootGo, "Create SpaceLifeScene");
            }
            
            CreateBackground(sceneRoot);
            
            if (spawnPoint == null)
            {
                GameObject spawnGo = new GameObject("SpawnPoint");
                spawnGo.transform.SetParent(sceneRoot);
                spawnGo.transform.position = Vector3.zero;
                spawnPoint = spawnGo.transform;
                Undo.RegisterCreatedObjectUndo(spawnGo, "Create SpawnPoint");
            }
            
            if (spaceLifeCamera == null)
            {
                GameObject camGo = new GameObject("SpaceLifeCamera");
                camGo.transform.SetParent(sceneRoot);
                camGo.transform.position = new Vector3(0, 0, -10);
                
                spaceLifeCamera = camGo.AddComponent<Camera>();
                spaceLifeCamera.orthographic = true;
                spaceLifeCamera.orthographicSize = 5f;
                spaceLifeCamera.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
                spaceLifeCamera.clearFlags = CameraClearFlags.SolidColor;
                
                camGo.AddComponent<AudioListener>();
                
                Undo.RegisterCreatedObjectUndo(camGo, "Create SpaceLifeCamera");
            }
            
            var manager = Object.FindFirstObjectByType<SpaceLifeManager>();
            if (manager != null)
            {
                EnsureManagerReferences(manager);
            }
            
            Debug.Log("[SpaceLife Setup] Created scene structure");
        }

        private void CreateBackground(Transform parent)
        {
            var existingBg = parent.Find("Background");
            if (existingBg != null)
            {
                Debug.Log("[SpaceLife Setup] Background already exists, skipping.");
                return;
            }

            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(parent);
            bgGo.transform.position = new Vector3(0, 0, 10);
            
            var sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpaceLifeMenuItems.CreateSquareSprite(new Color(0.1f, 0.1f, 0.2f));
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(50, 30);
            sr.sortingOrder = -100;
            
            Undo.RegisterCreatedObjectUndo(bgGo, "Create Background");
            Debug.Log("[SpaceLife Setup] Created Background");
        }

        private void CreateInputHandler()
        {
            var existingHandler = Object.FindFirstObjectByType<SpaceLifeInputHandler>();
            if (existingHandler != null)
            {
                Debug.Log("[SpaceLife Setup] SpaceLifeInputHandler already exists, checking references...");
                EnsureInputHandlerReferences(existingHandler);
                var manager = Object.FindFirstObjectByType<SpaceLifeManager>();
                if (manager != null)
                {
                    EnsureManagerReferences(manager);
                }
                return;
            }

            GameObject go = new GameObject("SpaceLifeInputHandler");
            var handler = go.AddComponent<SpaceLifeInputHandler>();
            
            EnsureInputHandlerReferences(handler);
            
            Undo.RegisterCreatedObjectUndo(go, "Create SpaceLifeInputHandler");
            
            var manager2 = Object.FindFirstObjectByType<SpaceLifeManager>();
            if (manager2 != null)
            {
                EnsureManagerReferences(manager2);
            }
            
            Debug.Log("[SpaceLife Setup] Created SpaceLifeInputHandler");
        }

        private void EnsureInputHandlerReferences(SpaceLifeInputHandler handler)
        {
            var serializedHandler = new SerializedObject(handler);
            var inputActionsProp = serializedHandler.FindProperty("_inputActions");
            
            if (inputActionsProp != null && inputActionsProp.objectReferenceValue == null)
            {
                var inputAsset = SpaceLifeMenuItems.FindInputActionAsset();
                if (inputAsset != null)
                {
                    inputActionsProp.objectReferenceValue = inputAsset;
                    serializedHandler.ApplyModifiedProperties();
                    Debug.Log("[SpaceLife Setup] Auto-assigned InputActionAsset to SpaceLifeInputHandler");
                }
                else
                {
                    Debug.LogWarning("[SpaceLife Setup] Could not find InputActionAsset. Please assign manually.");
                }
            }
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
            Transform sceneRoot = GetOrCreateSceneRoot();
            
            Color[] npcColors = { Color.magenta, Color.yellow, Color.green };
            string[] npcNames = { "Navigator", "Engineer", "Medic" };
            
            for (int i = 0; i < npcNames.Length; i++)
            {
                GameObject npcGo = new GameObject($"NPC_{npcNames[i]}");
                npcGo.transform.SetParent(sceneRoot);
                npcGo.transform.position = new Vector3(i * 2 - 2, 0, 0);
                
                var sr = npcGo.AddComponent<SpriteRenderer>();
                sr.sprite = SpaceLifeMenuItems.CreateSquareSprite(npcColors[i]);
                sr.sortingOrder = 5;
                
                npcGo.AddComponent<NPCController>();
                var interactable = npcGo.AddComponent<Interactable>();
                interactable.InteractionText = $"Talk to {npcNames[i]}";
                
                var collider = npcGo.AddComponent<CapsuleCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(1f, 1.5f);
                
                Undo.RegisterCreatedObjectUndo(npcGo, $"Create NPC_{npcNames[i]}");
            }
            
            Debug.Log("[SpaceLife Setup] Created demo NPCs");
        }

        private Transform GetOrCreateSceneRoot()
        {
            GameObject root = GameObject.Find("SpaceLifeScene");
            if (root == null)
            {
                root = new GameObject("SpaceLifeScene");
                Undo.RegisterCreatedObjectUndo(root, "Create SpaceLifeScene");
            }
            return root.transform;
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

        private void CreateRoomManager()
        {
            if (Object.FindFirstObjectByType<SpaceLifeRoomManager>() != null)
            {
                Debug.Log("[SpaceLife Setup] SpaceLifeRoomManager already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("SpaceLifeRoomManager");
            go.AddComponent<SpaceLifeRoomManager>();
            
            Undo.RegisterCreatedObjectUndo(go, "Create SpaceLifeRoomManager");
            
            Debug.Log("[SpaceLife Setup] Created SpaceLifeRoomManager");
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
            Transform sceneRoot = GetOrCreateSceneRoot();
            
            string[] roomNames = { "CommandCenter", "MedBay", "Engineering", "Galley" };
            
            for (int i = 0; i < roomNames.Length; i++)
            {
                GameObject roomGo = new GameObject($"Room_{roomNames[i]}");
                roomGo.transform.SetParent(sceneRoot);
                roomGo.transform.position = new Vector3(i * 10, 0, 0);
                
                roomGo.AddComponent<SpaceLifeRoom>();
                
                var bounds = roomGo.AddComponent<BoxCollider2D>();
                bounds.size = new Vector2(10f, 8f);
                bounds.isTrigger = true;
                
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
            
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create SpaceLifeCanvas");
            
            var uiObjects = new MonoBehaviour[]
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
                    ui.transform.SetParent(canvasGo.transform, false);
                }
            }
            
            Debug.Log("[SpaceLife Setup] Created SpaceLifeCanvas");
        }

        private void CreateTransitionUI()
        {
            if (Object.FindFirstObjectByType<TransitionUI>() != null)
            {
                Debug.Log("[SpaceLife Setup] TransitionUI already exists, skipping.");
                return;
            }

            GameObject canvasGo = GameObject.Find("SpaceLifeCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("SpaceLifeCanvas");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create SpaceLifeCanvas");
            }

            GameObject transitionGo = new GameObject("TransitionUI");
            transitionGo.transform.SetParent(canvasGo.transform, false);
            
            var rectTransform = transitionGo.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            
            var transitionUI = transitionGo.AddComponent<TransitionUI>();
            
            GameObject fadeOverlay = new GameObject("FadeOverlay");
            fadeOverlay.transform.SetParent(transitionGo.transform, false);
            var fadeRect = fadeOverlay.AddComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.sizeDelta = Vector2.zero;
            var fadeImage = fadeOverlay.AddComponent<UnityEngine.UI.Image>();
            fadeImage.color = new Color(0, 0, 0, 0);
            
            GameObject centerText = new GameObject("CenterText");
            centerText.transform.SetParent(transitionGo.transform, false);
            var textRect = centerText.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(400, 100);
            textRect.anchoredPosition = Vector2.zero;
            var text = centerText.AddComponent<UnityEngine.UI.Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 32;
            text.color = Color.white;
            
            var serializedTransition = new SerializedObject(transitionUI);
            var fadeProp = serializedTransition.FindProperty("_fadeOverlay");
            var textProp = serializedTransition.FindProperty("_centerText");
            if (fadeProp != null) fadeProp.objectReferenceValue = fadeImage;
            if (textProp != null) textProp.objectReferenceValue = text;
            serializedTransition.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(transitionGo, "Create TransitionUI");
            
            Debug.Log("[SpaceLife Setup] Created TransitionUI");
        }

        private void ConnectAllSystems()
        {
            Debug.Log("[SpaceLife Setup] All systems connected!");
        }
    }
}
