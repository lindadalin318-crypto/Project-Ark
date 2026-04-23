// ⚠️ FROZEN since 2026-04-23 (Master Plan v1.1 Phase 4 §8.3). DO NOT RUN REBUILD.
//
// This window is retained for historical / reference purposes only.
// UI Prefab authority has been transferred to SpaceLifeUIPrefabBuilder:
//   ProjectArk > Space Life > Build UI Prefabs (Apply)
//   ProjectArk > Space Life > Build UI Prefabs (Audit)
//
// See Implement_rules.md §12.11 (SpaceLifeSetupWindow Freeze Declaration) for:
//   - the full list of prohibited operations
//   - the current-authority replacements for each legacy task
//   - the only legal path to unfreeze (complete Master Plan Phase 5 rewrite)
//
// Violation policy: any Rebuild/Apply executed here must be rolled back immediately
// and logged in Docs/5_ImplementationLog/ImplementationLog.md as a governance breach.

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife.Editor
{
    /// <summary>
    /// [FROZEN — 2026-04-23] Legacy editor entry point for SpaceLife scene setup.
    ///
    /// ⚠️ This window is FROZEN under Master Plan v1.1 Phase 4 §8.3 and
    /// <c>Implement_rules.md §12.11</c>. Do NOT use it to Rebuild/Apply any UI Prefab,
    /// Coordinator wiring, or scene hierarchy.
    ///
    /// Current authority entry points (use these instead):
    ///   - <c>ProjectArk > Space Life > Build UI Prefabs (Apply)</c> — UI Prefab generation
    ///   - <c>ProjectArk > Space Life > Audit UI Prefab Overrides</c> — Scene override audit
    ///   - <c>ProjectArk > Validate Dialogue Database</c> — DialogueDatabase validation
    ///
    /// Unfreeze is only possible by completing Master Plan Phase 5 (SetupWindow rewrite).
    /// </summary>
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

        private const string LogPrefix = "[SpaceLifeSetup]";
        private const string SpaceLifeCanvasName = "SpaceLifeCanvas";
        private const string SpaceLifeSceneRootName = "SpaceLifeScene";
        private const string PlayerPrefabPath = "Assets/_Prefabs/SpaceLife/Player2D_Prefab.prefab";
        private const string UIPrefabsFolder = "Assets/_Prefabs/SpaceLife/UI";
        private const string OptionButtonPrefabPath = UIPrefabsFolder + "/OptionButton_Prefab.prefab";

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

        // ------------------------------------------------------------
        // OnGUI surface
        // ------------------------------------------------------------

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawFreezeBanner();
            DrawHeader();
            DrawPhaseSelection();
            DrawOptions();
            DrawExecuteButton();
            DrawStatus();

            EditorGUILayout.EndScrollView();
        }

        private void DrawFreezeBanner()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "⚠️ FROZEN — This window is frozen since 2026-04-23.\n\n" +
                "Do NOT execute Rebuild/Apply here. UI Prefab authority has moved to:\n" +
                "  • ProjectArk > Space Life > Build UI Prefabs (Apply)\n" +
                "  • ProjectArk > Space Life > Audit UI Prefab Overrides\n" +
                "  • ProjectArk > Validate Dialogue Database\n\n" +
                "Governance reference: Implement_rules.md §12.11 — Master Plan v1.1 Phase 4.\n" +
                "Unfreeze is only possible after Phase 5 (SetupWindow rewrite) completes.",
                MessageType.Error);
            EditorGUILayout.Space(6);
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Space Life System Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "This wizard is the primary entry point for Space Life scene setup.\n" +
                "Running AllPhases guarantees a fully-formed scene (managers + player + UI + demo content)\n" +
                "ready for the Dialogue sample bootstrap and for play testing.",
                MessageType.Info);
            EditorGUILayout.Space(10);
        }

        private void DrawPhaseSelection()
        {
            EditorGUILayout.LabelField("Setup Phase", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _selectedPhase = (SetupPhase)EditorGUILayout.EnumPopup("Select Phase", _selectedPhase);

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(GetPhaseDescription(_selectedPhase), MessageType.None);
        }

        private string GetPhaseDescription(SetupPhase phase)
        {
            switch (phase)
            {
                case SetupPhase.Phase1_CoreBasics:
                    return "Phase 1 — Core & Basics\n" +
                           "• SpaceLifeManager singleton\n" +
                           "• Player2D prefab (+ asset generated if missing)\n" +
                           "• Scene root (SpaceLifeScene) with background, spawn point, camera\n" +
                           "• SpaceLifeInputHandler wired to ShipActions";

                case SetupPhase.Phase2_NPCSystem:
                    return "Phase 2 — NPC System\n" +
                           "• DialogueUIPresenter (full hierarchy with CanvasGroup)\n" +
                           "• Demo NPCs (Navigator / Engineer / Medic) — optional";

                case SetupPhase.Phase3_RelationshipGifting:
                    return "Phase 3 — Relationship & Gifting\n" +
                           "• RelationshipManager & GiftInventory singletons\n" +
                           "• GiftUIPresenter (full hierarchy)";

                case SetupPhase.Phase4_RoomsScenes:
                    return "Phase 4 — Rooms & Scenes\n" +
                           "• SpaceLifeRoomManager singleton\n" +
                           "• MinimapUI (full hierarchy)\n" +
                           "• Demo rooms (CommandCenter / MedBay / Engineering / Galley) — optional";

                case SetupPhase.Phase5_FullIntegration:
                    return "Phase 5 — Full Integration\n" +
                           "• SpaceLifeCanvas (ensure one canonical canvas)\n" +
                           "• TransitionUI (fade overlay)\n" +
                           "• Defensive reparent pass for any stray UI";

                case SetupPhase.AllPhases:
                    return "All Phases (Complete Setup)\n" +
                           "• Runs every phase in order\n" +
                           "• Result: fully-formed SpaceLife scene, ready to play";

                default:
                    return "Unknown phase";
            }
        }

        private void DrawOptions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

            _createDemoContent = EditorGUILayout.Toggle(
                new GUIContent("Create Demo Content", "Creates demo NPCs, rooms, and items."),
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

        // ------------------------------------------------------------
        // Status panel
        // ------------------------------------------------------------

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
                EditorGUILayout.HelpBox(
                    "Ship/InputHandler is MISSING! This is required for Tab toggle to work.",
                    MessageType.Error);
            }

            EditorGUI.indentLevel--;

            DrawSceneHealthCheck(hasShipInputHandler, hasInputHandler);
        }

        private void DrawSceneHealthCheck(bool hasShipInputHandler, bool hasSpaceLifeInputHandler)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("━━━ Scene Health Check ━━━", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawStatusItem("Ship Prefab Instance", hasShipInputHandler);
            if (!hasShipInputHandler)
            {
                DrawButtonRow("Add Ship to Scene", AddShipToScene, 200, 24);
            }

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
                EditorGUILayout.HelpBox(
                    "SpaceLifeManager not found in scene. Cannot check serialized references.",
                    MessageType.Warning);
            }

            DrawStatusItem("SpaceLifeInputHandler", hasSpaceLifeInputHandler);

            bool hasPlayerPrefabAsset = AssetDatabase.FindAssets("Player2D_Prefab t:Prefab").Length > 0;
            DrawStatusItem("Player2D Prefab Asset", hasPlayerPrefabAsset);

            if (manager != null)
            {
                EditorGUILayout.Space(10);
                DrawButtonRow("Auto-Wire References", () => AutoWireReferences(manager), 200, 28);
            }
        }

        private void DrawStatusItem(string name, bool exists)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  {name}:", GUILayout.Width(250));

            GUI.color = exists ? Color.green : Color.red;
            EditorGUILayout.LabelField(exists ? "✅ Present" : "❌ Missing");
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSerializedFieldStatus(SerializedObject so, string propertyName, string displayName)
        {
            var prop = so.FindProperty(propertyName);
            bool hasValue = prop != null && prop.objectReferenceValue != null;
            DrawStatusItem($"  {displayName}", hasValue);
        }

        private static void DrawButtonRow(string label, System.Action onClick, float width, float height)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if (GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(height)))
            {
                onClick?.Invoke();
            }
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------
        // Ship / Auto-Wire
        // ------------------------------------------------------------

        private void AddShipToScene()
        {
            var existingShip = Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>();
            if (existingShip != null)
            {
                EditorUtility.DisplayDialog("Ship Exists",
                    "Ship Prefab instance already exists in scene.", "OK");
                Selection.activeGameObject = existingShip.gameObject;
                return;
            }

            var guids = AssetDatabase.FindAssets("Ship t:Prefab", new[] { "Assets/_Prefabs/Ship" });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Not Found",
                    "Could not find Ship.prefab in Assets/_Prefabs/Ship/", "OK");
                return;
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to load prefab at {prefabPath}", "OK");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "Add Ship to Scene");
            Selection.activeGameObject = instance;

            Debug.Log($"{LogPrefix} Added Ship Prefab to scene from {prefabPath}");
            EditorUtility.DisplayDialog("Success", "Ship Prefab has been added to the scene.", "OK");
        }

        private void AutoWireReferences(SpaceLifeManager manager)
        {
            var so = new SerializedObject(manager);
            int wiredCount = 0;

            wiredCount += TryWireObjectReference(so, "_mainCamera", () => Camera.main);
            wiredCount += TryWireObjectReference(so, "_spaceLifeInputHandler",
                () => Object.FindFirstObjectByType<SpaceLifeInputHandler>());
            wiredCount += TryWireObjectReference(so, "_shipRoot",
                () => Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>()?.gameObject);

            var spaceLifeSceneRoot = GameObject.Find(SpaceLifeSceneRootName);
            if (spaceLifeSceneRoot != null)
            {
                wiredCount += TryWireObjectReference(so, "_spaceLifeSceneRoot", () => spaceLifeSceneRoot);
                wiredCount += TryWireObjectReference(so, "_spaceLifeSpawnPoint",
                    () => spaceLifeSceneRoot.transform.Find("SpawnPoint"));

                var camChild = spaceLifeSceneRoot.transform.Find("SpaceLifeCamera");
                if (camChild != null)
                {
                    wiredCount += TryWireObjectReference(so, "_spaceLifeCamera",
                        () => camChild.GetComponent<Camera>());
                }
            }

            wiredCount += TryWireObjectReference(so, "_spaceLifePlayerPrefab",
                () => AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath));

            so.ApplyModifiedProperties();

            string message = wiredCount > 0
                ? $"Auto-wired {wiredCount} reference(s). Check Console for details."
                : "All auto-discoverable references are already wired.";
            EditorUtility.DisplayDialog("Auto-Wire Complete", message, "OK");
            Debug.Log($"{LogPrefix} Auto-Wire completed: {wiredCount} references wired");
        }

        private int TryWireObjectReference(SerializedObject so, string propertyName,
            System.Func<Object> resolve)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || prop.objectReferenceValue != null)
            {
                return 0;
            }

            var value = resolve();
            if (value == null)
            {
                return 0;
            }

            prop.objectReferenceValue = value;
            Debug.Log($"{LogPrefix} [AutoWire] {propertyName} → {value.name}");
            return 1;
        }

        // ------------------------------------------------------------
        // Execute
        // ------------------------------------------------------------

        private void ExecuteSetup()
        {
            // ⚠️ Phase 4 freeze guard — see Implement_rules.md §12.11.
            // This gate is the runtime enforcement of the freeze declaration. Keep it wired
            // to every write-path entry in this window. Removing it requires completing
            // Master Plan Phase 5 (SetupWindow rewrite) AND updating §12.11.
            EditorUtility.DisplayDialog(
                "SpaceLifeSetupWindow is FROZEN",
                "This window has been frozen since 2026-04-23 (Master Plan v1.1 Phase 4).\n\n" +
                "Rebuild/Apply operations are prohibited. Use these current-authority menus instead:\n\n" +
                "  • ProjectArk > Space Life > Build UI Prefabs (Apply)\n" +
                "  • ProjectArk > Space Life > Audit UI Prefab Overrides\n" +
                "  • ProjectArk > Validate Dialogue Database\n\n" +
                "See Implement_rules.md §12.11 for the full freeze declaration.",
                "OK");
            Debug.LogWarning(
                "[SpaceLifeSetupWindow] Execute blocked: window is frozen since 2026-04-23. " +
                "See Implement_rules.md §12.11 for current-authority entry points.");
            return;

#pragma warning disable CS0162 // Unreachable code detected — retained for Phase 5 unfreeze reference.
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
                EditorUtility.DisplayProgressBar("Setting Up Space Life", "Creating components...", 0f);

                ExecutePhase(_selectedPhase);

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Setup Complete!",
                    "Space Life system has been set up!\n\n" +
                    "Remember to verify in SpaceLifeManager Inspector:\n" +
                    "• Player Prefab, Camera, Scene Root, Input Handler all wired\n" +
                    "(Use the Auto-Wire References button if anything is missing.)",
                    "Got it!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"{LogPrefix} Error: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Setup Failed",
                    $"An error occurred: {e.Message}\n\nCheck the Console for details.", "OK");
            }
#pragma warning restore CS0162
        }

        private void ExecutePhase(SetupPhase phase)
        {
            switch (phase)
            {
                case SetupPhase.Phase1_CoreBasics: SetupPhase1(); break;
                case SetupPhase.Phase2_NPCSystem: SetupPhase2(); break;
                case SetupPhase.Phase3_RelationshipGifting: SetupPhase3(); break;
                case SetupPhase.Phase4_RoomsScenes: SetupPhase4(); break;
                case SetupPhase.Phase5_FullIntegration: SetupPhase5(); break;
                case SetupPhase.AllPhases: SetupAllPhases(); break;
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
            GetOrCreateSpaceLifeCanvas();
            CreateTransitionUI();
            ReparentStrayUI();

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

        // ------------------------------------------------------------
        // Phase 1 — Core & Basics
        // ------------------------------------------------------------

        private void CreateSpaceLifeManager()
        {
            var existing = Object.FindFirstObjectByType<SpaceLifeManager>();
            if (existing != null)
            {
                Debug.Log($"{LogPrefix} SpaceLifeManager already exists, checking references...");
                EnsureManagerReferences(existing);
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject go = new GameObject("SpaceLifeManager");
            go.AddComponent<SpaceLifeManager>();

            Undo.RegisterCreatedObjectUndo(go, "Create SpaceLifeManager");
            Selection.activeGameObject = go;

            Debug.Log($"{LogPrefix} Created SpaceLifeManager");
        }

        private void EnsureManagerReferences(SpaceLifeManager manager)
        {
            var so = new SerializedObject(manager);
            int changed = 0;

            changed += TryWireObjectReference(so, "_spaceLifePlayerPrefab",
                () => AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath));

            var sceneRoot = GameObject.Find(SpaceLifeSceneRootName);
            if (sceneRoot != null)
            {
                changed += TryWireObjectReference(so, "_spaceLifeSceneRoot", () => sceneRoot);

                var spawn = sceneRoot.transform.Find("SpawnPoint");
                if (spawn != null)
                {
                    changed += TryWireObjectReference(so, "_spaceLifeSpawnPoint", () => spawn);
                }

                var cam = sceneRoot.transform.Find("SpaceLifeCamera");
                if (cam != null)
                {
                    changed += TryWireObjectReference(so, "_spaceLifeCamera",
                        () => cam.GetComponent<Camera>());
                }
            }

            changed += TryWireObjectReference(so, "_spaceLifeInputHandler",
                () => Object.FindFirstObjectByType<SpaceLifeInputHandler>());
            changed += TryWireObjectReference(so, "_shipRoot",
                () => Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>()?.gameObject);
            changed += TryWireObjectReference(so, "_mainCamera", () => Camera.main);

            if (changed > 0)
            {
                so.ApplyModifiedProperties();
                Debug.Log($"{LogPrefix} Updated SpaceLifeManager references ({changed} fields)");
            }
        }

        private void CreatePlayerPrefab()
        {
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existingPrefab != null)
            {
                Debug.Log($"{LogPrefix} Player2D_Prefab already exists, updating components...");
                UpdatePlayerPrefabComponents(existingPrefab);
                AssignPlayerPrefabToManager(existingPrefab);
                return;
            }

            GameObject playerGo = new GameObject("Player2D");

            var sr = playerGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpaceLifeMenuItems.CreateCapsuleSprite(Color.white);
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
                AssignInputActions(playerController, inputAsset);
                AssignInputActions(playerInteraction, inputAsset);
                Debug.Log($"{LogPrefix} Auto-assigned InputActionAsset to Player Prefab");
            }

            EnsureFolder("Assets", "_Prefabs");
            EnsureFolder("Assets/_Prefabs", "SpaceLife");

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerGo, PlayerPrefabPath);
            Undo.RegisterCreatedObjectUndo(playerGo, "Create Player2D");
            DestroyImmediate(playerGo);

            Debug.Log($"{LogPrefix} Created Player2D_Prefab at {PlayerPrefabPath}");
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
            }
            if (sr.sprite == null)
            {
                sr.sprite = SpaceLifeMenuItems.CreateCapsuleSprite(Color.white);
                sr.sortingOrder = 10;
                changed = true;
            }

            var rb = prefab.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = prefab.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.linearDamping = 5f;
                changed = true;
            }
            else if (rb.gravityScale != 0f)
            {
                rb.gravityScale = 0f;
                changed = true;
            }

            var collider = prefab.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                collider = prefab.AddComponent<CapsuleCollider2D>();
                collider.size = new Vector2(0.5f, 1f);
                changed = true;
            }

            var playerController = prefab.GetComponent<PlayerController2D>() ?? prefab.AddComponent<PlayerController2D>();
            var playerInteraction = prefab.GetComponent<PlayerInteraction>() ?? prefab.AddComponent<PlayerInteraction>();

            var inputAsset = SpaceLifeMenuItems.FindInputActionAsset();
            if (inputAsset != null)
            {
                if (AssignInputActionsIfEmpty(playerController, inputAsset)) changed = true;
                if (AssignInputActionsIfEmpty(playerInteraction, inputAsset)) changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log($"{LogPrefix} Player Prefab updated and saved");
            }
        }

        private void AssignInputActions(Component target, Object inputAsset)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty("_inputActions");
            if (prop != null)
            {
                prop.objectReferenceValue = inputAsset;
                so.ApplyModifiedProperties();
            }
        }

        private bool AssignInputActionsIfEmpty(Component target, Object inputAsset)
        {
            if (target == null) return false;
            var so = new SerializedObject(target);
            var prop = so.FindProperty("_inputActions");
            if (prop == null || prop.objectReferenceValue != null) return false;
            prop.objectReferenceValue = inputAsset;
            so.ApplyModifiedProperties();
            return true;
        }

        private void AssignPlayerPrefabToManager(GameObject prefab)
        {
            var manager = Object.FindFirstObjectByType<SpaceLifeManager>();
            if (manager == null) return;

            var so = new SerializedObject(manager);
            var prop = so.FindProperty("_spaceLifePlayerPrefab");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                Debug.Log($"{LogPrefix} Assigned Player2D_Prefab to SpaceLifeManager");
            }
        }

        private void CreateSceneStructure()
        {
            Transform sceneRoot = null;
            Transform spawnPoint = null;
            Camera spaceLifeCamera = null;

            GameObject existingRoot = GameObject.Find(SpaceLifeSceneRootName);
            if (existingRoot != null)
            {
                sceneRoot = existingRoot.transform;
                spawnPoint = existingRoot.transform.Find("SpawnPoint");
                var cam = existingRoot.transform.Find("SpaceLifeCamera");
                if (cam != null) spaceLifeCamera = cam.GetComponent<Camera>();
            }

            if (sceneRoot == null)
            {
                GameObject rootGo = new GameObject(SpaceLifeSceneRootName);
                sceneRoot = rootGo.transform;
                Undo.RegisterCreatedObjectUndo(rootGo, "Create SpaceLifeScene");
            }

            CreateBackground(sceneRoot);

            if (spawnPoint == null)
            {
                GameObject spawnGo = new GameObject("SpawnPoint");
                spawnGo.transform.SetParent(sceneRoot);
                spawnGo.transform.position = Vector3.zero;
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

            Debug.Log($"{LogPrefix} Created scene structure");
        }

        private void CreateBackground(Transform parent)
        {
            if (parent.Find("Background") != null) return;

            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(parent);
            bgGo.transform.position = new Vector3(0, 0, 10);

            var sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpaceLifeMenuItems.CreateSquareSprite(new Color(0.1f, 0.1f, 0.2f));
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(50, 30);
            sr.sortingOrder = -100;

            Undo.RegisterCreatedObjectUndo(bgGo, "Create Background");
        }

        private void CreateInputHandler()
        {
            var existing = Object.FindFirstObjectByType<SpaceLifeInputHandler>();
            if (existing != null)
            {
                EnsureInputHandlerReferences(existing);
            }
            else
            {
                GameObject go = new GameObject("SpaceLifeInputHandler");
                var handler = go.AddComponent<SpaceLifeInputHandler>();
                EnsureInputHandlerReferences(handler);
                Undo.RegisterCreatedObjectUndo(go, "Create SpaceLifeInputHandler");
                Debug.Log($"{LogPrefix} Created SpaceLifeInputHandler");
            }

            var manager = Object.FindFirstObjectByType<SpaceLifeManager>();
            if (manager != null)
            {
                EnsureManagerReferences(manager);
            }
        }

        private void EnsureInputHandlerReferences(SpaceLifeInputHandler handler)
        {
            var so = new SerializedObject(handler);
            var prop = so.FindProperty("_inputActions");

            if (prop == null || prop.objectReferenceValue != null) return;

            var inputAsset = SpaceLifeMenuItems.FindInputActionAsset();
            if (inputAsset != null)
            {
                prop.objectReferenceValue = inputAsset;
                so.ApplyModifiedProperties();
                Debug.Log($"{LogPrefix} Auto-assigned InputActionAsset to SpaceLifeInputHandler");
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} Could not find ShipActions InputActionAsset. Please assign manually.");
            }
        }

        // ------------------------------------------------------------
        // Phase 2 — NPC System
        // ------------------------------------------------------------

        private void CreateDialogueUI()
        {
            if (Object.FindFirstObjectByType<DialogueUIPresenter>() != null)
            {
                Debug.Log($"{LogPrefix} DialogueUIPresenter already exists, skipping.");
                return;
            }

            Transform canvas = GetOrCreateSpaceLifeCanvas();

            GameObject root = new GameObject("DialogueUI", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            var rootRect = (RectTransform)root.transform;
            SetStretchAll(rootRect);

            var dialogueUI = root.AddComponent<DialogueUIPresenter>();

            // Panel (bottom dialogue box)
            GameObject panel = BuildPanel(root.transform, "DialoguePanel",
                anchorMin: new Vector2(0.1f, 0.05f),
                anchorMax: new Vector2(0.9f, 0.35f),
                backgroundColor: new Color(0f, 0f, 0f, 0.75f));
            var panelCanvasGroup = panel.GetComponent<CanvasGroup>();

            // Avatar (upper-left)
            Image avatar = BuildImage(panel.transform, "Avatar",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                anchoredPosition: new Vector2(12f, -12f),
                sizeDelta: new Vector2(96f, 96f),
                color: Color.white);
            avatar.enabled = false;

            // Speaker name (top bar)
            Text speakerName = BuildText(panel.transform, "SpeakerName",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(60f, -12f),
                sizeDelta: new Vector2(-132f, 32f),
                text: string.Empty,
                fontSize: 20,
                alignment: TextAnchor.UpperLeft,
                color: new Color(1f, 0.9f, 0.6f, 1f));

            // Dialogue body text
            Text dialogueText = BuildText(panel.transform, "DialogueText",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(-140f, -120f),
                offsetMin: new Vector2(120f, 60f),
                offsetMax: new Vector2(-20f, -56f),
                text: string.Empty,
                fontSize: 18,
                alignment: TextAnchor.UpperLeft,
                color: Color.white);

            // Options container (bottom strip)
            GameObject options = new GameObject("OptionsContainer", typeof(RectTransform));
            options.transform.SetParent(panel.transform, false);
            var optRect = (RectTransform)options.transform;
            optRect.anchorMin = new Vector2(0f, 0f);
            optRect.anchorMax = new Vector2(1f, 0f);
            optRect.pivot = new Vector2(0.5f, 0f);
            optRect.offsetMin = new Vector2(120f, 8f);
            optRect.offsetMax = new Vector2(-20f, 56f);
            var layout = options.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            // Close button (top-right)
            Button closeButton = BuildButton(panel.transform, "CloseButton",
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                anchoredPosition: new Vector2(-8f, -8f),
                sizeDelta: new Vector2(60f, 28f),
                label: "×",
                fontSize: 20);

            // Option/Item button prefab (shared asset)
            GameObject optionPrefab = EnsureOptionButtonPrefab();

            // Wire fields via SerializedObject
            var so = new SerializedObject(dialogueUI);
            so.FindProperty("_dialoguePanel").objectReferenceValue = panel;
            so.FindProperty("_dialogueCanvasGroup").objectReferenceValue = panelCanvasGroup;
            so.FindProperty("_avatarImage").objectReferenceValue = avatar;
            so.FindProperty("_speakerNameText").objectReferenceValue = speakerName;
            so.FindProperty("_dialogueText").objectReferenceValue = dialogueText;
            so.FindProperty("_optionsContainer").objectReferenceValue = options.transform;
            so.FindProperty("_optionButtonPrefab").objectReferenceValue = optionPrefab;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedProperties();

            // Initialize hidden (CanvasGroup driven)
            SetPanelHidden(panelCanvasGroup);

            Undo.RegisterCreatedObjectUndo(root, "Create DialogueUIPresenter");
            Debug.Log($"{LogPrefix} Created DialogueUIPresenter (full hierarchy)");
        }

        private void CreateDemoNPCs()
        {
            Transform sceneRoot = GetOrCreateSceneRoot();

            Color[] npcColors = { Color.magenta, Color.yellow, Color.green };
            string[] npcNames = { "Navigator", "Engineer", "Medic" };

            for (int i = 0; i < npcNames.Length; i++)
            {
                string childName = $"NPC_{npcNames[i]}";
                if (sceneRoot.Find(childName) != null) continue;

                GameObject npcGo = new GameObject(childName);
                npcGo.transform.SetParent(sceneRoot);
                npcGo.transform.position = new Vector3(i * 2 - 2, 0, 0);

                var sr = npcGo.AddComponent<SpriteRenderer>();
                sr.sprite = SpaceLifeMenuItems.CreateSquareSprite(npcColors[i]);
                sr.sortingOrder = 5;

                // Interactable must be added before NPCController (RequireComponent)
                var interactable = npcGo.AddComponent<Interactable>();
                interactable.InteractionText = $"Talk to {npcNames[i]}";
                npcGo.AddComponent<NPCController>();

                var collider = npcGo.AddComponent<CapsuleCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(1f, 1.5f);

                Undo.RegisterCreatedObjectUndo(npcGo, $"Create {childName}");
            }

            Debug.Log($"{LogPrefix} Created demo NPCs");
        }

        private Transform GetOrCreateSceneRoot()
        {
            GameObject root = GameObject.Find(SpaceLifeSceneRootName);
            if (root == null)
            {
                root = new GameObject(SpaceLifeSceneRootName);
                Undo.RegisterCreatedObjectUndo(root, "Create SpaceLifeScene");
            }
            return root.transform;
        }

        // ------------------------------------------------------------
        // Phase 3 — Relationship & Gifting
        // ------------------------------------------------------------

        private void CreateRelationshipManager() => CreateSingletonGameObject<RelationshipManager>("RelationshipManager");
        private void CreateGiftInventory() => CreateSingletonGameObject<GiftInventory>("GiftInventory");

        private T CreateSingletonGameObject<T>(string name) where T : MonoBehaviour
        {
            var existing = Object.FindFirstObjectByType<T>();
            if (existing != null)
            {
                Debug.Log($"{LogPrefix} {name} already exists, skipping.");
                return existing;
            }

            GameObject go = new GameObject(name);
            var component = go.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            Debug.Log($"{LogPrefix} Created {name}");
            return component;
        }

        private void CreateGiftUI()
        {
            if (Object.FindFirstObjectByType<GiftUIPresenter>() != null)
            {
                Debug.Log($"{LogPrefix} GiftUIPresenter already exists, skipping.");
                return;
            }

            Transform canvas = GetOrCreateSpaceLifeCanvas();

            GameObject root = new GameObject("GiftUI", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            SetStretchAll((RectTransform)root.transform);

            var giftUI = root.AddComponent<GiftUIPresenter>();

            GameObject panel = BuildPanel(root.transform, "GiftPanel",
                anchorMin: new Vector2(0.25f, 0.2f),
                anchorMax: new Vector2(0.75f, 0.8f),
                backgroundColor: new Color(0.05f, 0.05f, 0.1f, 0.9f));
            var panelCanvasGroup = panel.GetComponent<CanvasGroup>();

            Text npcNameText = BuildText(panel.transform, "NpcNameText",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -16f),
                sizeDelta: new Vector2(-40f, 36f),
                text: string.Empty,
                fontSize: 22,
                alignment: TextAnchor.MiddleCenter,
                color: Color.white);

            GameObject itemsContainer = new GameObject("ItemsContainer", typeof(RectTransform));
            itemsContainer.transform.SetParent(panel.transform, false);
            var itemsRect = (RectTransform)itemsContainer.transform;
            itemsRect.anchorMin = new Vector2(0f, 0f);
            itemsRect.anchorMax = new Vector2(1f, 1f);
            itemsRect.offsetMin = new Vector2(16f, 52f);
            itemsRect.offsetMax = new Vector2(-16f, -60f);
            var grid = itemsContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(120f, 64f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperLeft;

            Button closeButton = BuildButton(panel.transform, "CloseButton",
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                anchoredPosition: new Vector2(-8f, -8f),
                sizeDelta: new Vector2(60f, 28f),
                label: "×",
                fontSize: 20);

            GameObject itemPrefab = EnsureOptionButtonPrefab();

            var so = new SerializedObject(giftUI);
            so.FindProperty("_giftPanel").objectReferenceValue = panel;
            so.FindProperty("_giftCanvasGroup").objectReferenceValue = panelCanvasGroup;
            so.FindProperty("_itemsContainer").objectReferenceValue = itemsContainer.transform;
            so.FindProperty("_itemButtonPrefab").objectReferenceValue = itemPrefab;
            so.FindProperty("_npcNameText").objectReferenceValue = npcNameText;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedProperties();

            SetPanelHidden(panelCanvasGroup);

            Undo.RegisterCreatedObjectUndo(root, "Create GiftUIPresenter");
            Debug.Log($"{LogPrefix} Created GiftUIPresenter (full hierarchy)");
        }

        private void CreateNPCInteractionUI()
        {
            // NPCInteractionUI was removed in Master Plan v1.1 Phase 2 (§6.2.1 "NPCInteractionUI.cs 删除").
            // Its responsibilities were absorbed into the DialogueUIPresenter close button and GiftUIPresenter
            // flow. This stub is retained only so legacy call sites (if any reappear) compile cleanly; it
            // intentionally performs no work.
            Debug.Log($"{LogPrefix} CreateNPCInteractionUI() is a no-op (removed in Phase 2).");
        }

        // ------------------------------------------------------------
        // Phase 4 — Rooms & Minimap
        // ------------------------------------------------------------

        private void CreateRoomManager() => CreateSingletonGameObject<SpaceLifeRoomManager>("SpaceLifeRoomManager");

        private void CreateMinimapUI()
        {
            if (Object.FindFirstObjectByType<MinimapUI>() != null)
            {
                Debug.Log($"{LogPrefix} MinimapUI already exists, skipping.");
                return;
            }

            Transform canvas = GetOrCreateSpaceLifeCanvas();

            GameObject root = new GameObject("MinimapUI", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            SetStretchAll((RectTransform)root.transform);

            var ui = root.AddComponent<MinimapUI>();

            GameObject panel = BuildPanel(root.transform, "MinimapPanel",
                anchorMin: new Vector2(0.78f, 0.62f),
                anchorMax: new Vector2(0.99f, 0.98f),
                backgroundColor: new Color(0f, 0f, 0f, 0.55f),
                addCanvasGroup: false);

            Image currentRoomIcon = BuildImage(panel.transform, "CurrentRoomIcon",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                anchoredPosition: new Vector2(8f, -8f),
                sizeDelta: new Vector2(32f, 32f),
                color: Color.white);
            currentRoomIcon.gameObject.SetActive(false);

            Text currentRoomText = BuildText(panel.transform, "CurrentRoomText",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(20f, -14f),
                sizeDelta: new Vector2(-60f, 32f),
                text: string.Empty,
                fontSize: 16,
                alignment: TextAnchor.MiddleLeft,
                color: Color.white);

            GameObject roomButtonsContainer = new GameObject("RoomButtonsContainer", typeof(RectTransform));
            roomButtonsContainer.transform.SetParent(panel.transform, false);
            var rbcRect = (RectTransform)roomButtonsContainer.transform;
            rbcRect.anchorMin = new Vector2(0f, 0f);
            rbcRect.anchorMax = new Vector2(1f, 1f);
            rbcRect.offsetMin = new Vector2(8f, 8f);
            rbcRect.offsetMax = new Vector2(-8f, -44f);
            var vLayout = roomButtonsContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 4f;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childAlignment = TextAnchor.UpperLeft;

            GameObject roomButtonPrefab = EnsureOptionButtonPrefab();

            var so = new SerializedObject(ui);
            so.FindProperty("_minimapPanel").objectReferenceValue = panel;
            so.FindProperty("_currentRoomText").objectReferenceValue = currentRoomText;
            so.FindProperty("_currentRoomIcon").objectReferenceValue = currentRoomIcon;
            so.FindProperty("_roomButtonsContainer").objectReferenceValue = roomButtonsContainer.transform;
            so.FindProperty("_roomButtonPrefab").objectReferenceValue = roomButtonPrefab;
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(root, "Create MinimapUI");
            Debug.Log($"{LogPrefix} Created MinimapUI (full hierarchy)");
        }

        private void CreateDemoRooms()
        {
            Transform sceneRoot = GetOrCreateSceneRoot();

            string[] roomNames = { "CommandCenter", "MedBay", "Engineering", "Galley" };

            for (int i = 0; i < roomNames.Length; i++)
            {
                string childName = $"Room_{roomNames[i]}";
                if (sceneRoot.Find(childName) != null) continue;

                GameObject roomGo = new GameObject(childName);
                roomGo.transform.SetParent(sceneRoot);
                roomGo.transform.position = new Vector3(i * 10, 0, 0);

                roomGo.AddComponent<SpaceLifeRoom>();

                var bounds = roomGo.AddComponent<BoxCollider2D>();
                bounds.size = new Vector2(10f, 8f);
                bounds.isTrigger = true;

                Undo.RegisterCreatedObjectUndo(roomGo, $"Create {childName}");
            }

            Debug.Log($"{LogPrefix} Created demo rooms");
        }

        // ------------------------------------------------------------
        // Phase 5 — Full Integration
        // ------------------------------------------------------------

        private void CreateTransitionUI()
        {
            if (Object.FindFirstObjectByType<TransitionUI>() != null)
            {
                Debug.Log($"{LogPrefix} TransitionUI already exists, skipping.");
                return;
            }

            Transform canvas = GetOrCreateSpaceLifeCanvas();

            GameObject root = new GameObject("TransitionUI", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            SetStretchAll((RectTransform)root.transform);

            var ui = root.AddComponent<TransitionUI>();

            // Fade overlay — full-screen black image with CanvasGroup.
            GameObject overlay = new GameObject("FadeOverlay",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(root.transform, false);
            SetStretchAll((RectTransform)overlay.transform);

            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = Color.black;
            overlayImage.raycastTarget = false;

            var overlayCanvasGroup = overlay.GetComponent<CanvasGroup>();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;

            // Place TransitionUI above the other SpaceLife UI elements so the fade covers them.
            root.transform.SetAsLastSibling();

            var so = new SerializedObject(ui);
            var fadeProp = so.FindProperty("_fadeOverlay");
            if (fadeProp != null)
            {
                fadeProp.objectReferenceValue = overlayCanvasGroup;
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError($"{LogPrefix} TransitionUI._fadeOverlay field not found — wiring failed.");
            }

            Undo.RegisterCreatedObjectUndo(root, "Create TransitionUI");
            Debug.Log($"{LogPrefix} Created TransitionUI (fade overlay)");
        }

        /// <summary>
        /// Defensive pass: any SpaceLife UI element that ended up as a scene-root child gets
        /// reparented back under SpaceLifeCanvas. Useful after manual edits.
        /// </summary>
        private void ReparentStrayUI()
        {
            Transform canvas = GetOrCreateSpaceLifeCanvas();
            int moved = 0;

            foreach (var ui in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (ui == null) continue;
                if (ui is DialogueUIPresenter || ui is GiftUIPresenter ||
                    ui is MinimapUI || ui is TransitionUI)
                {
                    var t = ui.transform;
                    if (t.parent == canvas) continue;
                    if (t.GetComponentInParent<Canvas>() == canvas.GetComponent<Canvas>()) continue;

                    t.SetParent(canvas, false);
                    var rect = t as RectTransform;
                    if (rect != null) SetStretchAll(rect);
                    moved++;
                }
            }

            if (moved > 0)
            {
                Debug.Log($"{LogPrefix} Reparented {moved} stray UI element(s) under SpaceLifeCanvas");
            }
        }

        // ------------------------------------------------------------
        // Canvas / UI hierarchy helpers
        // ------------------------------------------------------------

        private Transform GetOrCreateSpaceLifeCanvas()
        {
            GameObject existing = GameObject.Find(SpaceLifeCanvasName);
            if (existing != null)
            {
                EnsureCanvasComponents(existing);
                EnsureEventSystem();
                return existing.transform;
            }

            GameObject go = new GameObject(SpaceLifeCanvasName,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            EnsureCanvasComponents(go);
            EnsureEventSystem();

            Undo.RegisterCreatedObjectUndo(go, "Create SpaceLifeCanvas");
            Debug.Log($"{LogPrefix} Created SpaceLifeCanvas");
            return go.transform;
        }

        private void EnsureCanvasComponents(GameObject canvasGo)
        {
            var canvas = canvasGo.GetComponent<Canvas>() ?? canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>() ?? canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
            {
                canvasGo.AddComponent<GraphicRaycaster>();
            }
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

            GameObject es = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem));

            // Prefer the new Input System UI module when available; fall back to the legacy one.
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                es.AddComponent(inputModuleType);
            }
            else
            {
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            Debug.Log($"{LogPrefix} Created EventSystem");
        }

        /// <summary>
        /// Build a visible panel (Image background + CanvasGroup) under a parent rect.
        /// The returned GameObject is ready to host child UI elements.
        /// </summary>
        private GameObject BuildPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color backgroundColor, bool addCanvasGroup = true)
        {
            GameObject panel = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            var rect = (RectTransform)panel.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = true;

            if (addCanvasGroup)
            {
                var canvasGroup = panel.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            return panel;
        }

        private Text BuildText(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            string text, int fontSize, TextAnchor alignment, Color color,
            Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;

            if (offsetMin.HasValue && offsetMax.HasValue)
            {
                rect.offsetMin = offsetMin.Value;
                rect.offsetMax = offsetMax.Value;
            }
            else
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }

            var textComponent = go.GetComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.raycastTarget = false;

            return textComponent;
        }

        private Image BuildImage(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private Button BuildButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            string label, int fontSize)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.8f, 1f);
            button.colors = colors;

            // Label child (stretches to fill the button).
            GameObject labelGo = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            SetStretchAll((RectTransform)labelGo.transform);

            var textComponent = labelGo.GetComponent<Text>();
            textComponent.text = label;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.raycastTarget = false;

            return button;
        }

        /// <summary>
        /// Ensures the shared option/item button prefab exists and returns it.
        /// Used by DialogueUIPresenter (options), GiftUIPresenter (items), and MinimapUI (room buttons).
        /// </summary>
        private GameObject EnsureOptionButtonPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(OptionButtonPrefabPath);
            if (existing != null) return existing;

            EnsureFolder("Assets", "_Prefabs");
            EnsureFolder("Assets/_Prefabs", "SpaceLife");
            EnsureFolder("Assets/_Prefabs/SpaceLife", "UI");

            // Build temporary GameObject with the same structure BuildButton produces, then save
            // it as a prefab asset and destroy the scene copy.
            GameObject tempParent = new GameObject("__OptionButtonTempParent", typeof(RectTransform));
            Button button = BuildButton(tempParent.transform, "OptionButton",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(0.5f, 0f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(0f, 36f),
                label: "Option",
                fontSize: 16);

            GameObject buttonGo = button.gameObject;
            buttonGo.transform.SetParent(null, false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonGo, OptionButtonPrefabPath);

            DestroyImmediate(buttonGo);
            DestroyImmediate(tempParent);

            Debug.Log($"{LogPrefix} Created shared OptionButton prefab at {OptionButtonPrefabPath}");
            return prefab;
        }

        /// <summary>
        /// Stretch a RectTransform to fully fill its parent (offsets = 0).
        /// </summary>
        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Hide a UI panel via its CanvasGroup. Implement_rules forbids SetActive(false) on
        /// panels whose Awake() must still execute for serialized-field resolution.
        /// </summary>
        private static void SetPanelHidden(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Ensure a folder exists under AssetDatabase; creates it if missing.
        /// </summary>
        private static void EnsureFolder(string parent, string newFolder)
        {
            string path = $"{parent}/{newFolder}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, newFolder);
            }
        }
    }
}

