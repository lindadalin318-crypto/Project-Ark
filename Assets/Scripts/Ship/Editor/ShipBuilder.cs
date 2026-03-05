
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// One-click editor utility to build a fully configured Ship GameObject in the scene.
    ///
    /// Menu: ProjectArk > Ship > Build Ship
    ///
    /// Creates (idempotent — running multiple times is safe):
    ///   - Ship root GameObject with Rigidbody2D + CircleCollider2D
    ///   - All script components in correct RequireComponent order
    ///   - VisualChild (SpriteRenderer) and EngineParticles (ParticleSystem) child nodes
    ///   - ShipStatsSO + ShipJuiceSettingsSO assets (via ShipFeelAssetCreator if missing)
    ///   - All SerializeField references auto-wired
    ///   - WeavingStateTransition._shipTransform back-wired if UIManager exists in scene
    ///
    /// Manual steps remaining after build are listed in the summary dialog.
    /// </summary>
    public static class ShipBuilder
    {
        private const string SHIP_GO_NAME  = "Ship";
        private const string VISUAL_CHILD_NAME  = "VisualChild";
        private const string ENGINE_PARTICLES_NAME = "EngineParticles";

        [MenuItem("ProjectArk/Ship/Build Ship")]
        public static void BuildShip()
        {
            var log = new List<string>();
            var todo = new List<string>();

            // ── Step 0: FindOrCreate Root ──────────────────────────────
            var shipGo = FindOrCreateShipRoot(log);

            // ── Step 1: Unity Physics Components ──────────────────────
            SetupPhysicsComponents(shipGo, log);

            // ── Step 2: Script Components ──────────────────────────────
            AddScriptComponents(shipGo, log);

            // ── Step 3: Child Nodes ────────────────────────────────────
            var visualChild   = EnsureChild(shipGo, VISUAL_CHILD_NAME, log);
            var spriteRenderer = GetOrAddComponent<SpriteRenderer>(visualChild);
            var engineParticlesGo = EnsureChild(shipGo, ENGINE_PARTICLES_NAME, log);
            engineParticlesGo.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            var ps = GetOrAddComponent<ParticleSystem>(engineParticlesGo);
            ConfigureEngineParticles(ps, log);

            // ── Step 4: Find / create SO assets ───────────────────────
            var statsSO  = FindOrCreateShipStatsSO(log);
            var juiceSO  = FindOrCreateShipJuiceSO(log);

            // ── Step 5: Find InputActionAsset ──────────────────────────
            var inputAsset = FindInputActionAsset(log, todo);

            // ── Step 6: Wire all SerializeFields ──────────────────────
            WireReferences(shipGo, visualChild, spriteRenderer, ps,
                statsSO, juiceSO, inputAsset, log, todo);

            // ── Step 7: Back-wire WeavingStateTransition ───────────────
            WireWeavingTransition(shipGo, log, todo);

            // ── Step 8: Summary Dialog ─────────────────────────────────
            ShowSummaryDialog(log, todo);

            EditorUtility.SetDirty(shipGo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                shipGo.scene);
        }

        // ════════════════════════════════════════════════════════════════
        // Step 0 — Root
        // ════════════════════════════════════════════════════════════════

        private static GameObject FindOrCreateShipRoot(List<string> log)
        {
            // 先找名字匹配且有 ShipMotor 的 GO
            var existing = Object.FindAnyObjectByType<ShipMotor>(FindObjectsInactive.Include);
            if (existing != null)
            {
                log.Add($"✓ Ship root found (existing): {existing.gameObject.name}");
                return existing.gameObject;
            }

            var shipGo = new GameObject(SHIP_GO_NAME);
            Undo.RegisterCreatedObjectUndo(shipGo, "Create Ship");

            // Tag
            try { shipGo.tag = "Player"; }
            catch { /* "Player" tag not defined — skip */ }

            // Layer
            int layer = LayerMask.NameToLayer("Ship");
            if (layer >= 0)
                shipGo.layer = layer;
            else
                Debug.LogWarning("[ShipBuilder] Layer 'Ship' not found. Please create it in Tags & Layers.");

            log.Add($"✓ Created Ship root GameObject: {shipGo.name}");
            return shipGo;
        }

        // ════════════════════════════════════════════════════════════════
        // Step 1 — Physics
        // ════════════════════════════════════════════════════════════════

        private static void SetupPhysicsComponents(GameObject shipGo, List<string> log)
        {
            var rb = GetOrAddComponent<Rigidbody2D>(shipGo);
            rb.mass = 1f;
            rb.linearDamping = 3f;
            rb.angularDamping = 0f;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.None; // 不冻结旋转，旋转由 ShipAiming 的 angularVelocity 控制
            log.Add("✓ Rigidbody2D configured (mass=1, drag=3, gravScale=0, Interpolate, Continuous)");

            bool colliderExisted = shipGo.GetComponent<Collider2D>() != null;
            if (!colliderExisted)
            {
                var col = shipGo.AddComponent<CircleCollider2D>();
                col.radius = 0.4f;
                log.Add("✓ CircleCollider2D added (radius=0.4 — adjust to match sprite)");
            }
            else
            {
                log.Add("✓ Collider2D already exists, skipping");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Step 2 — Script components (order matters for RequireComponent)
        // ════════════════════════════════════════════════════════════════

        private static void AddScriptComponents(GameObject shipGo, List<string> log)
        {
            GetOrAddComponent<InputHandler>(shipGo);
            GetOrAddComponent<ShipMotor>(shipGo);
            GetOrAddComponent<ShipAiming>(shipGo);
            GetOrAddComponent<ShipHealth>(shipGo);
            GetOrAddComponent<ShipDash>(shipGo);
            GetOrAddComponent<ShipBoost>(shipGo);
            GetOrAddComponent<ShipVisualJuice>(shipGo);
            GetOrAddComponent<ShipEngineVFX>(shipGo);
            GetOrAddComponent<DashAfterImageSpawner>(shipGo);
            log.Add("✓ All script components added (InputHandler → ShipMotor → ShipAiming → ShipHealth → ShipDash → ShipBoost → ShipVisualJuice → ShipEngineVFX → DashAfterImageSpawner)");
        }

        // ════════════════════════════════════════════════════════════════
        // Step 3 helpers — Child nodes
        // ════════════════════════════════════════════════════════════════

        private static GameObject EnsureChild(GameObject parent, string childName, List<string> log)
        {
            var existing = parent.transform.Find(childName);
            if (existing != null)
            {
                log.Add($"✓ Child '{childName}' already exists, skipping");
                return existing.gameObject;
            }

            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
            log.Add($"✓ Created child: {childName}");
            return child;
        }

        private static void ConfigureEngineParticles(ParticleSystem ps, List<string> log)
        {
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.3f;
            main.startSize = 0.1f;
            main.startSpeed = 1f;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 20f;

            log.Add("✓ EngineParticles ParticleSystem configured (loop, lifetime=0.3, size=0.1, rate=20)");
        }

        // ════════════════════════════════════════════════════════════════
        // Step 4 — SO assets
        // ════════════════════════════════════════════════════════════════

        private static ShipStatsSO FindOrCreateShipStatsSO(List<string> log)
        {
            const string assetPath = "Assets/_Data/Ship/DefaultShipStats.asset";
            var so = AssetDatabase.LoadAssetAtPath<ShipStatsSO>(assetPath);
            if (so != null)
            {
                log.Add($"✓ ShipStatsSO found: {assetPath}");
                return so;
            }

            // 委托给已有的 ShipFeelAssetCreator 创建
            ShipFeelAssetCreator.CreateOrUpdateShipStats();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            so = AssetDatabase.LoadAssetAtPath<ShipStatsSO>(assetPath);
            if (so != null)
            {
                log.Add($"✓ ShipStatsSO created via ShipFeelAssetCreator: {assetPath}");
            }
            else
            {
                // Fallback: 搜索任意 ShipStatsSO
                var guids = AssetDatabase.FindAssets("t:ShipStatsSO");
                if (guids.Length > 0)
                {
                    so = AssetDatabase.LoadAssetAtPath<ShipStatsSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    log.Add($"✓ ShipStatsSO found via search: {AssetDatabase.GUIDToAssetPath(guids[0])}");
                }
                else
                {
                    Debug.LogWarning("[ShipBuilder] Could not find or create ShipStatsSO. Wire manually.");
                    log.Add("⚠ ShipStatsSO not found — wire manually");
                }
            }
            return so;
        }

        private static ShipJuiceSettingsSO FindOrCreateShipJuiceSO(List<string> log)
        {
            const string assetPath = "Assets/_Data/Ship/DefaultShipJuiceSettings.asset";
            var so = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(assetPath);
            if (so != null)
            {
                log.Add($"✓ ShipJuiceSettingsSO found: {assetPath}");
                return so;
            }

            ShipFeelAssetCreator.CreateShipJuiceSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            so = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(assetPath);
            if (so != null)
            {
                log.Add($"✓ ShipJuiceSettingsSO created via ShipFeelAssetCreator: {assetPath}");
            }
            else
            {
                var guids = AssetDatabase.FindAssets("t:ShipJuiceSettingsSO");
                if (guids.Length > 0)
                {
                    so = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    log.Add($"✓ ShipJuiceSettingsSO found via search: {AssetDatabase.GUIDToAssetPath(guids[0])}");
                }
                else
                {
                    Debug.LogWarning("[ShipBuilder] Could not find or create ShipJuiceSettingsSO. Wire manually.");
                    log.Add("⚠ ShipJuiceSettingsSO not found — wire manually");
                }
            }
            return so;
        }

        // ════════════════════════════════════════════════════════════════
        // Step 5 — InputActionAsset
        // ════════════════════════════════════════════════════════════════

        private static InputActionAsset FindInputActionAsset(List<string> log, List<string> todo)
        {
            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[ShipBuilder] No InputActionAsset found in project.");
                todo.Add("InputHandler._inputActions: No InputActionAsset found — create one in Assets/Input/ and assign manually");
                return null;
            }

            if (guids.Length > 1)
                Debug.LogWarning($"[ShipBuilder] Multiple InputActionAssets found. Using first: {AssetDatabase.GUIDToAssetPath(guids[0])}");

            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            log.Add($"✓ InputActionAsset found: {AssetDatabase.GUIDToAssetPath(guids[0])}");
            return asset;
        }

        // ════════════════════════════════════════════════════════════════
        // Step 6 — Wire all SerializeFields
        // ════════════════════════════════════════════════════════════════

        private static void WireReferences(
            GameObject shipGo,
            GameObject visualChild,
            SpriteRenderer spriteRenderer,
            ParticleSystem enginePS,
            ShipStatsSO statsSO,
            ShipJuiceSettingsSO juiceSO,
            InputActionAsset inputAsset,
            List<string> log,
            List<string> todo)
        {
            // InputHandler
            var inputHandler = shipGo.GetComponent<InputHandler>();
            if (inputHandler != null && inputAsset != null)
                WireField(inputHandler, "_inputActions", inputAsset, log, "InputHandler._inputActions");

            // ShipMotor
            var motor = shipGo.GetComponent<ShipMotor>();
            if (motor != null && statsSO != null)
                WireField(motor, "_stats", statsSO, log, "ShipMotor._stats");

            // ShipAiming
            var aiming = shipGo.GetComponent<ShipAiming>();
            if (aiming != null && statsSO != null)
                WireField(aiming, "_stats", statsSO, log, "ShipAiming._stats");

            // ShipHealth
            var health = shipGo.GetComponent<ShipHealth>();
            if (health != null && statsSO != null)
                WireField(health, "_stats", statsSO, log, "ShipHealth._stats");

            // ShipDash
            var dash = shipGo.GetComponent<ShipDash>();
            if (dash != null && statsSO != null)
                WireField(dash, "_stats", statsSO, log, "ShipDash._stats");

            // ShipBoost
            var boost = shipGo.GetComponent<ShipBoost>();
            if (boost != null && statsSO != null)
                WireField(boost, "_stats", statsSO, log, "ShipBoost._stats");

            // ShipVisualJuice
            var juice = shipGo.GetComponent<ShipVisualJuice>();
            if (juice != null)
            {
                WireField(juice, "_visualChild", visualChild.transform, log, "ShipVisualJuice._visualChild");
                if (juiceSO != null)
                    WireField(juice, "_juiceSettings", juiceSO, log, "ShipVisualJuice._juiceSettings");
            }

            // ShipEngineVFX
            var engineVFX = shipGo.GetComponent<ShipEngineVFX>();
            if (engineVFX != null)
            {
                WireField(engineVFX, "_engineParticles", enginePS, log, "ShipEngineVFX._engineParticles");
                if (juiceSO != null)
                    WireField(engineVFX, "_juiceSettings", juiceSO, log, "ShipEngineVFX._juiceSettings");
            }

            // DashAfterImageSpawner
            var spawner = shipGo.GetComponent<DashAfterImageSpawner>();
            if (spawner != null)
            {
                WireField(spawner, "_shipSpriteRenderer", spriteRenderer, log, "DashAfterImageSpawner._shipSpriteRenderer");
                if (statsSO != null)
                    WireField(spawner, "_stats", statsSO, log, "DashAfterImageSpawner._stats");
                if (juiceSO != null)
                    WireField(spawner, "_juiceSettings", juiceSO, log, "DashAfterImageSpawner._juiceSettings");

                // After-image prefab: try to find it
                var prefabGuids = AssetDatabase.FindAssets("DashAfterImage t:Prefab");
                if (prefabGuids.Length > 0)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGuids[0]));
                    WireField(spawner, "_afterImagePrefab", prefab, log, "DashAfterImageSpawner._afterImagePrefab");
                }
                else
                {
                    todo.Add("DashAfterImageSpawner._afterImagePrefab: Create a prefab with DashAfterImage + SpriteRenderer and assign manually");
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Step 7 — Back-wire WeavingStateTransition
        // ════════════════════════════════════════════════════════════════

        private static void WireWeavingTransition(GameObject shipGo, List<string> log, List<string> todo)
        {
            // WeavingStateTransition は ProjectArk.UI アセンブリ内にあり直接参照できないため、
            // 型名で検索してリフレクションなしに SerializedObject 経由で接続する。
            // WeavingStateTransition lives in ProjectArk.UI (which depends on ProjectArk.Ship),
            // so we cannot reference it directly here to avoid circular assembly dependencies.
            // We find it via MonoBehaviour type name lookup instead.
            MonoBehaviour weavingTransition = null;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == "WeavingStateTransition")
                {
                    weavingTransition = mb;
                    break;
                }
            }

            if (weavingTransition == null)
            {
                todo.Add("WeavingStateTransition._shipTransform: UIManager not found in scene — run 'Build UI Canvas' first, then re-run Build Ship");
                return;
            }

            WireField(weavingTransition, "_shipTransform", shipGo.transform, log, "WeavingStateTransition._shipTransform");
        }

        // ════════════════════════════════════════════════════════════════
        // Step 8 — Summary Dialog
        // ════════════════════════════════════════════════════════════════

        private static void ShowSummaryDialog(List<string> log, List<string> todo)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("── COMPLETED ──────────────────────────────");
            foreach (var entry in log)
                sb.AppendLine(entry);

            if (todo.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("── MANUAL STEPS REQUIRED ──────────────────");
                for (int i = 0; i < todo.Count; i++)
                    sb.AppendLine($"{i + 1}. {todo[i]}");

                sb.AppendLine();
                sb.AppendLine("── ALWAYS CHECK ────────────────────────────");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("── ALWAYS CHECK ────────────────────────────");
            }

            sb.AppendLine("• Assign a Sprite to VisualChild/SpriteRenderer");
            sb.AppendLine("• Adjust CircleCollider2D radius to match sprite size");
            sb.AppendLine("• Verify Input Action Asset has a 'Boost' Action (Space / SouthButton)");
            sb.AppendLine("• Configure Physics 2D Layer Collision Matrix for 'Ship' layer");
            sb.AppendLine("• Set CinemachineVirtualCamera Follow/LookAt to Ship transform");

            Debug.Log("[ShipBuilder] Build complete.\n" + sb);

            EditorUtility.DisplayDialog(
                "Ship Build Complete",
                sb.ToString(),
                "OK"
            );
        }

        // ════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing != null) return existing;
            return Undo.AddComponent<T>(go);
        }

        /// <summary>
        /// Wires a single SerializeField via SerializedObject reflection.
        /// Works for any UnityEngine.Object reference (Component, SO, Asset).
        /// </summary>
        private static void WireField(
            Object target,
            string propertyPath,
            Object value,
            List<string> log,
            string label)
        {
            if (target == null || value == null) return;

            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                Debug.LogWarning($"[ShipBuilder] Property '{propertyPath}' not found on {target.GetType().Name}");
                return;
            }

            // 已经连好了就不重复写
            if (prop.objectReferenceValue == value)
            {
                log.Add($"✓ {label} (already wired)");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
            log.Add($"✓ {label} wired");
        }
    }
}
#endif
