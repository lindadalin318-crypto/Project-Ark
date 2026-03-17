
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Bootstrap-only editor utility to build a fully configured Ship GameObject in the current scene.
    ///
    /// Menu: ProjectArk > Ship > Bootstrap > Build Ship Scene Setup
    ///
    /// This tool is not the authority for `Ship.prefab`, `BoostTrailRoot.prefab`, or scene-only BoostTrail bindings.
    /// It only bootstraps a playable scene instance and its local references.
    ///
    /// Creates (idempotent — running multiple times is safe):
    ///   - Ship root GameObject with Rigidbody2D + CircleCollider2D
    ///   - All script components in correct RequireComponent order
    ///   - `ShipVisual` root + canonical sprite layer children + `EngineParticles`
    ///   - ShipStatsSO + ShipJuiceSettingsSO assets (via ShipFeelAssetCreator if missing)
    ///   - All SerializeField references auto-wired
    ///   - WeavingStateTransition._shipTransform back-wired if UIManager exists in scene
    ///
    /// Manual steps remaining after build are listed in the summary dialog.
    /// </summary>
    public static class ShipBuilder
    {
        private const string SHIP_GO_NAME                    = "Ship";
        private const string VISUAL_CHILD_NAME               = "ShipVisual";
        private const string ENGINE_PARTICLES_NAME           = "EngineParticles";

        // 5-layer sprite structure (children of ShipVisual)
        private const string SPRITE_BACK_NAME                = "Ship_Sprite_Back";
        private const string SPRITE_LIQUID_NAME              = "Ship_Sprite_Liquid";
        private const string SPRITE_HL_NAME                  = "Ship_Sprite_HL";
        private const string SPRITE_SOLID_NAME               = "Ship_Sprite_Solid";
        private const string SPRITE_CORE_NAME                = "Ship_Sprite_Core";
        private const int SOLID_SPRITE_LAYER_INDEX           = 3;

        private const string GLOW_MATERIAL_PATH              = "Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat";
        private const string MAIN_REFERENCE_SPRITE_PATH      = "Assets/_Art/Ship/Glitch/Reference/GrabGun_Base_9.png";
        private const string HL_REFERENCE_SPRITE_PATH        = "Assets/_Art/Ship/Glitch/Reference/GrabGun_Base_8.png";
        private const string SHIP_STATS_PATH                 = "Assets/_Data/Ship/DefaultShipStats.asset";
        private const string SHIP_JUICE_SETTINGS_PATH        = "Assets/_Data/Ship/DefaultShipJuiceSettings.asset";
        private const string INPUT_ACTIONS_PATH              = "Assets/Input/ShipActions.inputactions";
        private const string DASH_AFTER_IMAGE_PREFAB_PATH    = "Assets/_Prefabs/Ship/DashAfterImage.prefab";

        [MenuItem("ProjectArk/Ship/Bootstrap/Build Ship Scene Setup")]
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
            var visualChild = FindOrCreateVisualRoot(shipGo, log);

            // ── Step 3b: 5-layer sprite children (under ShipVisual) ────
            var spriteLayerRenderers = EnsureSpriteLayers(visualChild, log);

            var engineParent = spriteLayerRenderers != null && spriteLayerRenderers.Length > 0 && spriteLayerRenderers[0] != null
                ? spriteLayerRenderers[0].gameObject
                : visualChild;
            var engineParticlesGo = EnsureChild(engineParent, ENGINE_PARTICLES_NAME, log);
            engineParticlesGo.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            var ps = GetOrAddComponent<ParticleSystem>(engineParticlesGo);
            ConfigureEngineParticles(ps, log);

            // ── Step 4: Find / create SO assets ───────────────────────
            var statsSO  = FindOrCreateShipStatsSO(log);
            var juiceSO  = FindOrCreateShipJuiceSO(log);

            // ── Step 5: Find InputActionAsset ──────────────────────────
            var inputAsset = FindInputActionAsset(log, todo);

            // ── Step 6: Wire all SerializeFields ──────────────────────
            WireReferences(shipGo, visualChild, ps,
                statsSO, juiceSO, inputAsset, spriteLayerRenderers, log, todo);

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
            GetOrAddComponent<ShipStateController>(shipGo); // state machine — must come after Motor+Aiming
            GetOrAddComponent<ShipHealth>(shipGo);
            GetOrAddComponent<ShipDash>(shipGo);
            GetOrAddComponent<ShipBoost>(shipGo);
            GetOrAddComponent<ShipVisualJuice>(shipGo);
            GetOrAddComponent<ShipView>(shipGo);
            GetOrAddComponent<ShipEngineVFX>(shipGo);
            GetOrAddComponent<DashAfterImageSpawner>(shipGo);
            log.Add("✓ All script components added (InputHandler → ShipMotor → ShipAiming → ShipStateController → ShipHealth → ShipDash → ShipBoost → ShipVisualJuice → ShipView → ShipEngineVFX → DashAfterImageSpawner)");
        }

        // ════════════════════════════════════════════════════════════════
        // Step 3 helpers — Child nodes
        // ════════════════════════════════════════════════════════════════

        private static GameObject FindOrCreateVisualRoot(GameObject shipGo, List<string> log)
        {
            var canonical = shipGo.transform.Find(VISUAL_CHILD_NAME);
            if (canonical != null)
            {
                log.Add($"✓ Visual root found: {VISUAL_CHILD_NAME}");
                return canonical.gameObject;
            }

            var child = new GameObject(VISUAL_CHILD_NAME);
            child.transform.SetParent(shipGo.transform, false);
            child.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(child, $"Create {VISUAL_CHILD_NAME}");
            log.Add($"✓ Created visual root: {VISUAL_CHILD_NAME}");
            return child;
        }

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

        // ════════════════════════════════════════════════════════════════
        // Step 3b — 5-layer sprite children
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ensures the 5 sprite layer children exist under `ShipVisual`.
        /// Returns array [back, liquid, hl, solid, core] SpriteRenderers.
        /// </summary>
        private static SpriteRenderer[] EnsureSpriteLayers(GameObject visualChild, List<string> log)
        {
            var glowMat = AssetDatabase.LoadAssetAtPath<Material>(GLOW_MATERIAL_PATH);
            if (glowMat == null)
                log.Add($"⚠ ShipGlowMaterial not found at {GLOW_MATERIAL_PATH} — assign manually to Ship_Sprite_Liquid");

            Sprite mainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MAIN_REFERENCE_SPRITE_PATH);
            if (mainSprite == null)
                log.Add($"⚠ Reference sprite missing at {MAIN_REFERENCE_SPRITE_PATH}");

            Sprite hlSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HL_REFERENCE_SPRITE_PATH);
            if (hlSprite == null)
                log.Add($"⚠ Reference sprite missing at {HL_REFERENCE_SPRITE_PATH}");

            // Ship_Sprite_Back: no reference sprite assigned — awaiting original art asset
            Sprite backSprite = null;

            SpriteRenderer back   = EnsureSpriteLayer(visualChild, SPRITE_BACK_NAME,   -3, backSprite,  null,    log);
            SpriteRenderer liquid = EnsureSpriteLayer(visualChild, SPRITE_LIQUID_NAME, -2, mainSprite,  glowMat, log);
            SpriteRenderer hl     = EnsureSpriteLayer(visualChild, SPRITE_HL_NAME,     -1, hlSprite,    null,    log);
            SpriteRenderer solid  = EnsureSpriteLayer(visualChild, SPRITE_SOLID_NAME,   0, mainSprite,  null,    log);
            SpriteRenderer core   = EnsureSpriteLayer(visualChild, SPRITE_CORE_NAME,    1, null,        null,    log);

            // HL layer default alpha = 0.5
            if (hl != null)
            {
                var c = hl.color;
                c.a = 0.5f;
                hl.color = c;
            }

            return new[] { back, liquid, hl, solid, core };
        }

        private static SpriteRenderer EnsureSpriteLayer(
            GameObject parent, string childName, int sortOrder,
            Sprite sprite, Material material, List<string> log)
        {
            var childGo = parent.transform.Find(childName)?.gameObject;
            if (childGo == null)
            {
                childGo = new GameObject(childName);
                childGo.transform.SetParent(parent.transform, false);
                childGo.transform.localPosition = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(childGo, $"Create {childName}");
                log.Add($"✓ Created sprite layer: {childName} (SortOrder={sortOrder})");
            }
            else
            {
                log.Add($"✓ Sprite layer '{childName}' already exists, updating");
            }

            var sr = GetOrAddComponent<SpriteRenderer>(childGo);
            sr.sortingOrder = sortOrder;
            if (sprite != null) sr.sprite = sprite;
            if (material != null) sr.sharedMaterial = material;

            return sr;
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
            var so = AssetDatabase.LoadAssetAtPath<ShipStatsSO>(SHIP_STATS_PATH);
            if (so != null)
            {
                log.Add($"✓ ShipStatsSO found: {SHIP_STATS_PATH}");
                return so;
            }

            ShipFeelAssetCreator.CreateOrUpdateShipStats();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            so = AssetDatabase.LoadAssetAtPath<ShipStatsSO>(SHIP_STATS_PATH);
            if (so != null)
            {
                log.Add($"✓ ShipStatsSO created via ShipFeelAssetCreator: {SHIP_STATS_PATH}");
            }
            else
            {
                Debug.LogWarning($"[ShipBuilder] Could not find or create ShipStatsSO at {SHIP_STATS_PATH}. Wire manually.");
                log.Add($"⚠ ShipStatsSO missing at {SHIP_STATS_PATH} — wire manually");
            }

            return so;
        }

        private static ShipJuiceSettingsSO FindOrCreateShipJuiceSO(List<string> log)
        {
            var so = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(SHIP_JUICE_SETTINGS_PATH);
            if (so != null)
            {
                log.Add($"✓ ShipJuiceSettingsSO found: {SHIP_JUICE_SETTINGS_PATH}");
                return so;
            }

            ShipFeelAssetCreator.CreateShipJuiceSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            so = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(SHIP_JUICE_SETTINGS_PATH);
            if (so != null)
            {
                log.Add($"✓ ShipJuiceSettingsSO created via ShipFeelAssetCreator: {SHIP_JUICE_SETTINGS_PATH}");
            }
            else
            {
                Debug.LogWarning($"[ShipBuilder] Could not find or create ShipJuiceSettingsSO at {SHIP_JUICE_SETTINGS_PATH}. Wire manually.");
                log.Add($"⚠ ShipJuiceSettingsSO missing at {SHIP_JUICE_SETTINGS_PATH} — wire manually");
            }

            return so;
        }

        // ════════════════════════════════════════════════════════════════
        // Step 5 — InputActionAsset
        // ════════════════════════════════════════════════════════════════

        private static InputActionAsset FindInputActionAsset(List<string> log, List<string> todo)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            if (asset == null)
            {
                Debug.LogWarning($"[ShipBuilder] InputActionAsset missing at {INPUT_ACTIONS_PATH}.");
                todo.Add($"InputHandler._inputActions: Missing InputActionAsset at {INPUT_ACTIONS_PATH} — assign manually");
                return null;
            }

            log.Add($"✓ InputActionAsset found: {INPUT_ACTIONS_PATH}");
            return asset;
        }

        // ════════════════════════════════════════════════════════════════
        // Step 6 — Wire all SerializeFields
        // ════════════════════════════════════════════════════════════════

        private static void WireReferences(
            GameObject shipGo,
            GameObject visualChild,
            ParticleSystem enginePS,
            ShipStatsSO statsSO,
            ShipJuiceSettingsSO juiceSO,
            InputActionAsset inputAsset,
            SpriteRenderer[] spriteLayers,
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

            // ShipStateController
            var stateController = shipGo.GetComponent<ShipStateController>();
            if (stateController != null && statsSO != null)
                WireField(stateController, "_stats", statsSO, log, "ShipStateController._stats");

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

            // ShipView — wire 5 sprite layer renderers
            var shipView = shipGo.GetComponent<ShipView>();
            if (shipView != null && spriteLayers != null && spriteLayers.Length == 5)
            {
                WireField(shipView, "_backRenderer",   spriteLayers[0], log, "ShipView._backRenderer");
                WireField(shipView, "_liquidRenderer", spriteLayers[1], log, "ShipView._liquidRenderer");
                WireField(shipView, "_hlRenderer",     spriteLayers[2], log, "ShipView._hlRenderer");
                WireField(shipView, "_solidRenderer",  spriteLayers[3], log, "ShipView._solidRenderer");
                WireField(shipView, "_coreRenderer",   spriteLayers[4], log, "ShipView._coreRenderer");
                if (juiceSO != null)
                    WireField(shipView, "_juiceSettings", juiceSO, log, "ShipView._juiceSettings");
            }

            // DashAfterImageSpawner
            var spawner = shipGo.GetComponent<DashAfterImageSpawner>();
            if (spawner != null)
            {
                if (spriteLayers != null && spriteLayers.Length > SOLID_SPRITE_LAYER_INDEX && spriteLayers[SOLID_SPRITE_LAYER_INDEX] != null)
                {
                    WireField(spawner, "_shipSpriteRenderer", spriteLayers[SOLID_SPRITE_LAYER_INDEX], log, "DashAfterImageSpawner._shipSpriteRenderer → Ship_Sprite_Solid");
                }
                else
                {
                    todo.Add("DashAfterImageSpawner._shipSpriteRenderer: Missing Ship_Sprite_Solid renderer — rebuild ShipVisual layers first");
                }

                if (statsSO != null)
                    WireField(spawner, "_stats", statsSO, log, "DashAfterImageSpawner._stats");
                if (juiceSO != null)
                    WireField(spawner, "_juiceSettings", juiceSO, log, "DashAfterImageSpawner._juiceSettings");

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DASH_AFTER_IMAGE_PREFAB_PATH);
                if (prefab != null)
                {
                    WireField(spawner, "_afterImagePrefab", prefab, log, "DashAfterImageSpawner._afterImagePrefab");
                }
                else
                {
                    todo.Add($"DashAfterImageSpawner._afterImagePrefab: Missing prefab at {DASH_AFTER_IMAGE_PREFAB_PATH}");
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
                todo.Add("WeavingStateTransition._shipTransform: UIManager not found in scene — run 'Build UI Canvas' first, then re-run 'Build Ship Scene Setup'");
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

            sb.AppendLine("• Verify ShipVisual sprite layers use the canonical ship sprites");
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
