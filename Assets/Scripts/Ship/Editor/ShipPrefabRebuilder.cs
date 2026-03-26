#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// The sole authority for Ship.prefab structure, components, and serialized wiring.
    ///
    /// Authority owned by this tool:
    ///   • Root-level physics components (Rigidbody2D, CircleCollider2D)
    ///   • Root-level runtime script components (InputHandler, ShipMotor, ShipAiming,
    ///     ShipStateController, ShipHealth, ShipDash, ShipBoost, and all VFX workers)
    ///   • ShipStatsSO wiring on all gameplay components
    ///   • InputActionAsset wiring on InputHandler
    ///   • DashAfterImage prefab wiring on DashAfterImageSpawner
    ///   • Multi-layer ship sprite hierarchy under ShipVisual
    ///   • Nested BoostTrailRoot prefab integration under ShipVisual
    ///   • Serialized wiring on ShipView / ShipBoostVisuals / ShipHitVisuals / ShipDashVisuals
    ///   • Serialized wiring on ShipVisualJuice / DashAfterImageSpawner
    ///
    /// Two modes:
    ///   • Rebuild (idempotent) — creates missing nodes/components, updates existing ones, wires all managed fields.
    ///   • Force Rebuild       — destroys ALL managed child nodes first, then rebuilds from scratch.
    ///
    /// Scene-only references remain outside this tool and are owned by ShipBoostTrailSceneBinder.
    /// </summary>
    public static class ShipPrefabRebuilder
    {
        // ── Paths ──────────────────────────────────────────────────────
        private const string PREFAB_PATH = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string BOOST_TRAIL_PREFAB_PATH = "Assets/_Prefabs/VFX/BoostTrailRoot.prefab";
        private const string GLOW_MATERIAL_PATH = "Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat";
        private const string SHIP_LIQUID_NORMAL_SPRITE_PATH = "Assets/_Art/Ship/Glitch/Movement_3.png";
        private const string SHIP_SOLID_SPRITE_PATH = "Assets/_Art/Ship/Glitch/Movement_10.png";
        private const string SHIP_HIGHLIGHT_SPRITE_PATH = "Assets/_Art/Ship/Glitch/Movement_21.png";
        private const string SHIP_JUICE_SETTINGS_PATH = "Assets/_Data/Ship/DefaultShipJuiceSettings.asset";
        private const string SHIP_STATS_PATH = "Assets/_Data/Ship/DefaultShipStats.asset";
        private const string INPUT_ACTIONS_PATH = "Assets/Input/ShipActions.inputactions";
        private const string DASH_AFTER_IMAGE_PREFAB_PATH = "Assets/_Prefabs/Ship/DashAfterImage.prefab";

        // ── Node names ─────────────────────────────────────────────────
        private const string VISUAL_CHILD_NAME = "ShipVisual";

        // Sprite layers
        private const string SPRITE_BACK_NAME = "Ship_Sprite_Back";
        private const string SPRITE_LIQUID_NAME = "Ship_Sprite_Liquid";
        private const string SPRITE_HL_NAME = "Ship_Sprite_HL";
        private const string SPRITE_SOLID_NAME = "Ship_Sprite_Solid";
        private const string SPRITE_CORE_NAME = "Ship_Sprite_Core";
        private const string DODGE_SPRITE_NAME = "Dodge_Sprite";
        private const string BOOST_TRAIL_ROOT_NAME = "BoostTrailRoot";

        // Sprite asset aliases (physical names remain frozen during MVP)
        private const string SHIP_LIQUID_NORMAL_SPRITE_NAME = "Movement_3";
        private const string SHIP_SOLID_SPRITE_NAME = "Movement_10";
        private const string SHIP_HIGHLIGHT_SPRITE_NAME = "Movement_21";

        // Dodge sprite
        private const string DODGE_SPRITE_TEXTURE_NAME = "player_test_fire";
        private const string DODGE_SPRITE_DEST_PATH = "Assets/_Art/Ship/Glitch/Reference/player_test_fire.png";

        // All managed node names under ShipVisual (for force-delete)
        private static readonly string[] MANAGED_VISUAL_CHILDREN =
        {
            SPRITE_BACK_NAME,
            SPRITE_LIQUID_NAME,
            SPRITE_HL_NAME,
            SPRITE_SOLID_NAME,
            SPRITE_CORE_NAME,
            DODGE_SPRITE_NAME,
            BOOST_TRAIL_ROOT_NAME
        };

        // ══════════════════════════════════════════════════════════════
        // Menu Items
        // ══════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Ship/Authority/Rebuild Ship Prefab")]
        public static void RebuildSpriteLayers() => Run(forceRebuild: false);

        [MenuItem("ProjectArk/Ship/Authority/FORCE Rebuild Ship Prefab")]
        public static void ForceRebuildSpriteLayers()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Force Rebuild",
                "This will DELETE all managed child nodes and components, then recreate them from scratch.\n\nAny manual tweaks to those nodes will be lost.\n\nContinue?",
                "Yes, Force Rebuild", "Cancel");
            if (confirmed)
            {
                Run(forceRebuild: true);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Core
        // ══════════════════════════════════════════════════════════════

        private static void Run(bool forceRebuild)
        {
            var log = new List<string>();
            var todo = new List<string>();

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefabAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Ship prefab not found at:\n{PREFAB_PATH}\n\nPlease create the prefab asset first (e.g. via Unity Editor: right-click in Project > Create > Prefab).",
                    "OK");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
            {
                var root = scope.prefabContentsRoot;

                // ── Pre-pass: strip any Missing Script components ──
                // (e.g. a deleted MonoBehaviour whose serialized reference
                //  still lingers in the prefab — Unity refuses to save.)
                StripMissingScripts(root, log);

                if (forceRebuild)
                {
                    ForceDeleteManagedNodes(root, log);
                    ForceDeleteManagedComponents(root, log);
                }

                // ══════════════════════════════════════════════════════════
                // Phase 0: Root-level physics components
                // ══════════════════════════════════════════════════════════
                EnsurePhysicsComponents(root, log);

                // ══════════════════════════════════════════════════════════
                // Phase 1: Root-level runtime script components
                //   Order matters for RequireComponent dependencies.
                // ══════════════════════════════════════════════════════════
                EnsureComponent<InputHandler>(root, log, "InputHandler");
                EnsureComponent<ShipMotor>(root, log, "ShipMotor");
                EnsureComponent<ShipAiming>(root, log, "ShipAiming");
                EnsureComponent<ShipStateController>(root, log, "ShipStateController");
                EnsureComponent<ShipHealth>(root, log, "ShipHealth");
                EnsureComponent<ShipDash>(root, log, "ShipDash");
                EnsureComponent<ShipBoost>(root, log, "ShipBoost");
                // VFX workers are ensured later in Phase 3

                // ══════════════════════════════════════════════════════════
                // Phase 1b: Wire ShipStatsSO + InputActions to root components
                // ══════════════════════════════════════════════════════════
                WireRootComponentReferences(root, log, todo);

                // ══════════════════════════════════════════════════════════
                // Phase 2: Visual hierarchy
                // ══════════════════════════════════════════════════════════

                var visualTf = root.transform.Find(VISUAL_CHILD_NAME);
                if (visualTf == null)
                {
                    var visualGo = new GameObject(VISUAL_CHILD_NAME);
                    visualGo.transform.SetParent(root.transform, false);
                    visualTf = visualGo.transform;
                    log.Add($"✓ Created {VISUAL_CHILD_NAME}");
                }
                else
                {
                    log.Add($"✓ Found visual parent: {visualTf.name}");
                }

                var glowMat = AssetDatabase.LoadAssetAtPath<Material>(GLOW_MATERIAL_PATH);
                if (glowMat == null)
                {
                    glowMat = ShipGlowMaterialCreator.CreateOrGet();
                    if (glowMat != null)
                    {
                        log.Add("✓ ShipGlowMaterial auto-created");
                    }
                    else
                    {
                        todo.Add($"ShipGlowMaterial not found at {GLOW_MATERIAL_PATH} — run 'Create Ship Glow Material' first");
                    }
                }

                Sprite solidSprite = LoadSpriteAtPath(SHIP_SOLID_SPRITE_PATH);
                Sprite liquidSprite = LoadSpriteAtPath(SHIP_LIQUID_NORMAL_SPRITE_PATH);
                Sprite hlSprite = LoadSpriteAtPath(SHIP_HIGHLIGHT_SPRITE_PATH);
                if (solidSprite == null)
                {
                    todo.Add($"{SHIP_SOLID_SPRITE_NAME} sprite not found — import {SHIP_SOLID_SPRITE_PATH} first");
                }
                if (liquidSprite == null)
                {
                    todo.Add($"{SHIP_LIQUID_NORMAL_SPRITE_NAME} sprite not found — import {SHIP_LIQUID_NORMAL_SPRITE_PATH} first");
                }
                if (hlSprite == null)
                {
                    todo.Add($"{SHIP_HIGHLIGHT_SPRITE_NAME} sprite not found — import {SHIP_HIGHLIGHT_SPRITE_PATH} first");
                }

                var back = EnsureSpriteLayer(visualTf, SPRITE_BACK_NAME, -3, null, null, log);
                var liquid = EnsureSpriteLayer(visualTf, SPRITE_LIQUID_NAME, -2, liquidSprite, glowMat, log);
                var hl = EnsureSpriteLayer(visualTf, SPRITE_HL_NAME, -1, hlSprite, null, log);
                var solid = EnsureSpriteLayer(visualTf, SPRITE_SOLID_NAME, 0, solidSprite, null, log);
                var core = EnsureSpriteLayer(visualTf, SPRITE_CORE_NAME, 1, null, null, log);

                if (hl != null)
                {
                    var highlightColor = hl.color;
                    highlightColor.a = 0.5f;
                    hl.color = highlightColor;
                    log.Add("✓ Ship_Sprite_HL alpha = 0.5");
                }

                Sprite dodgeSprite = LoadSpriteAtPath(DODGE_SPRITE_DEST_PATH);
                var dodgeSr = EnsureDodgeSpriteChild(visualTf, DODGE_SPRITE_NAME, dodgeSprite, log);
                if (dodgeSprite == null)
                {
                    todo.Add($"Dodge_Sprite: '{DODGE_SPRITE_TEXTURE_NAME}' not found — import player_test_fire.png then re-run");
                }

                var boostTrailView = EnsureBoostTrailRoot(visualTf, log, todo);

                // ── ShipBoostVisuals ──
                var boostVisuals = EnsureComponent<ShipBoostVisuals>(root, log, "ShipBoostVisuals");
                var boostVisualsSO = new SerializedObject(boostVisuals);
                WireField(boostVisualsSO, "_liquidRenderer", liquid, log, "ShipBoostVisuals._liquidRenderer");
                WireField(boostVisualsSO, "_hlRenderer", hl, log, "ShipBoostVisuals._hlRenderer");
                WireField(boostVisualsSO, "_coreRenderer", core, log, "ShipBoostVisuals._coreRenderer");
                if (back != null)
                {
                    var backTransformProp = boostVisualsSO.FindProperty("_backSpriteTransform");
                    if (backTransformProp != null && backTransformProp.objectReferenceValue != (Object)back.transform)
                    {
                        backTransformProp.objectReferenceValue = back.transform;
                        log.Add("✓ ShipBoostVisuals._backSpriteTransform wired");
                    }
                    else if (backTransformProp != null)
                    {
                        log.Add("✓ ShipBoostVisuals._backSpriteTransform (already wired)");
                    }
                }
                WireField(boostVisualsSO, "_boostTrailView", boostTrailView, log, "ShipBoostVisuals._boostTrailView");
                WireJuiceSettings(boostVisualsSO, "_juiceSettings", log, "ShipBoostVisuals._juiceSettings", todo);
                boostVisualsSO.ApplyModifiedProperties();

                // ── ShipHitVisuals ──
                var hitVisuals = EnsureComponent<ShipHitVisuals>(root, log, "ShipHitVisuals");
                var hitVisualsSO = new SerializedObject(hitVisuals);
                WireField(hitVisualsSO, "_backRenderer", back, log, "ShipHitVisuals._backRenderer");
                WireField(hitVisualsSO, "_liquidRenderer", liquid, log, "ShipHitVisuals._liquidRenderer");
                WireField(hitVisualsSO, "_hlRenderer", hl, log, "ShipHitVisuals._hlRenderer");
                WireField(hitVisualsSO, "_solidRenderer", solid, log, "ShipHitVisuals._solidRenderer");
                WireField(hitVisualsSO, "_coreRenderer", core, log, "ShipHitVisuals._coreRenderer");
                WireJuiceSettings(hitVisualsSO, "_juiceSettings", log, "ShipHitVisuals._juiceSettings", todo);
                hitVisualsSO.ApplyModifiedProperties();

                // ── ShipDashVisuals ──
                var dashVisuals = EnsureComponent<ShipDashVisuals>(root, log, "ShipDashVisuals");
                var dashVisualsSO = new SerializedObject(dashVisuals);
                WireField(dashVisualsSO, "_solidRenderer", solid, log, "ShipDashVisuals._solidRenderer");
                WireField(dashVisualsSO, "_hlRenderer", hl, log, "ShipDashVisuals._hlRenderer");
                WireField(dashVisualsSO, "_coreRenderer", core, log, "ShipDashVisuals._coreRenderer");
                WireField(dashVisualsSO, "_dodgeSprite", dodgeSr, log, "ShipDashVisuals._dodgeSprite");
                WireJuiceSettings(dashVisualsSO, "_juiceSettings", log, "ShipDashVisuals._juiceSettings", todo);

                // ── DashAfterImageSpawner ──
                var afterImageSpawner = EnsureComponent<DashAfterImageSpawner>(root, log, "DashAfterImageSpawner");
                var afterImageSpawnerSO = new SerializedObject(afterImageSpawner);
                WireField(afterImageSpawnerSO, "_shipSpriteRenderer", solid, log, "DashAfterImageSpawner._shipSpriteRenderer → Ship_Sprite_Solid");
                WireJuiceSettings(afterImageSpawnerSO, "_juiceSettings", log, "DashAfterImageSpawner._juiceSettings", todo);
                WireShipStats(afterImageSpawnerSO, "_stats", log, "DashAfterImageSpawner._stats", todo);
                // Wire DashAfterImage prefab
                var afterImagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DASH_AFTER_IMAGE_PREFAB_PATH);
                if (afterImagePrefab != null)
                {
                    WireField(afterImageSpawnerSO, "_afterImagePrefab", afterImagePrefab, log, "DashAfterImageSpawner._afterImagePrefab");
                }
                else
                {
                    todo.Add($"DashAfterImageSpawner._afterImagePrefab: Missing prefab at {DASH_AFTER_IMAGE_PREFAB_PATH}");
                }
                afterImageSpawnerSO.ApplyModifiedProperties();

                // Wire DashAfterImageSpawner into ShipDashVisuals
                WireField(dashVisualsSO, "_afterImageSpawner", afterImageSpawner, log, "ShipDashVisuals._afterImageSpawner");
                dashVisualsSO.ApplyModifiedProperties();

                // ── ShipVisualJuice ──
                var juiceVisuals = EnsureComponent<ShipVisualJuice>(root, log, "ShipVisualJuice");
                var juiceVisualsSO = new SerializedObject(juiceVisuals);
                // Wire _visualChild to ShipVisual transform
                var visualChildProp = juiceVisualsSO.FindProperty("_visualChild");
                if (visualChildProp != null && visualChildProp.objectReferenceValue != (Object)visualTf)
                {
                    visualChildProp.objectReferenceValue = visualTf;
                    log.Add("✓ ShipVisualJuice._visualChild wired → ShipVisual");
                }
                else if (visualChildProp != null)
                {
                    log.Add("✓ ShipVisualJuice._visualChild (already wired)");
                }
                WireJuiceSettings(juiceVisualsSO, "_juiceSettings", log, "ShipVisualJuice._juiceSettings", todo);
                juiceVisualsSO.ApplyModifiedProperties();

                // ── ShipView (Coordinator) ──
                var shipView = EnsureComponent<ShipView>(root, log, "ShipView");
                var shipViewSO = new SerializedObject(shipView);
                WireField(shipViewSO, "_backRenderer", back, log, "ShipView._backRenderer");
                WireField(shipViewSO, "_liquidRenderer", liquid, log, "ShipView._liquidRenderer");
                WireField(shipViewSO, "_hlRenderer", hl, log, "ShipView._hlRenderer");
                WireField(shipViewSO, "_solidRenderer", solid, log, "ShipView._solidRenderer");
                WireField(shipViewSO, "_coreRenderer", core, log, "ShipView._coreRenderer");
                WireField(shipViewSO, "_boostVisuals", boostVisuals, log, "ShipView._boostVisuals");
                WireField(shipViewSO, "_hitVisuals", hitVisuals, log, "ShipView._hitVisuals");
                WireField(shipViewSO, "_dashVisuals", dashVisuals, log, "ShipView._dashVisuals");
                WireField(shipViewSO, "_juiceVisuals", juiceVisuals, log, "ShipView._juiceVisuals");
                WireField(shipViewSO, "_afterImageSpawner", afterImageSpawner, log, "ShipView._afterImageSpawner");
                WireJuiceSettings(shipViewSO, "_juiceSettings", log, "ShipView._juiceSettings", todo);
                shipViewSO.ApplyModifiedProperties();
            }

            var summary = new System.Text.StringBuilder();
            summary.AppendLine(forceRebuild
                ? "── FORCE REBUILD COMPLETED ─────────────────"
                : "── REBUILD COMPLETED ───────────────────────");
            foreach (var entry in log)
            {
                summary.AppendLine(entry);
            }

            if (todo.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("── MANUAL STEPS REQUIRED ──────────────────");
                for (int i = 0; i < todo.Count; i++)
                {
                    summary.AppendLine($"{i + 1}. {todo[i]}");
                }
            }

            Debug.Log("[ShipPrefabRebuilder] Done.\n" + summary);
            EditorUtility.DisplayDialog(
                forceRebuild ? "Ship Prefab Force-Rebuilt" : "Ship Prefab Rebuilt",
                summary.ToString(),
                "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // Force Delete
        // ══════════════════════════════════════════════════════════════

        private static void ForceDeleteManagedNodes(GameObject root, List<string> log)
        {
            var visualTf = root.transform.Find(VISUAL_CHILD_NAME);
            if (visualTf == null)
            {
                return;
            }

            foreach (var name in MANAGED_VISUAL_CHILDREN)
            {
                var child = visualTf.Find(name);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                    log.Add($"✗ Deleted '{name}' (force rebuild)");
                }
            }
        }

        private static void ForceDeleteManagedComponents(GameObject root, List<string> log)
        {
            // Note: ShipView and DashAfterImageSpawner are NOT force-deleted
            // because they may have other manually-set fields we don't manage.
        }

        // ══════════════════════════════════════════════════════════════
        // Missing Script Cleanup
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Recursively scans root and all children for components with missing scripts
        /// (e.g. a deleted MonoBehaviour still serialized in the prefab) and removes them.
        /// Must run BEFORE any EnsureComponent / wiring to avoid Unity's
        /// "saving Prefab with a missing script" error.
        /// </summary>
        private static void StripMissingScripts(GameObject root, List<string> log)
        {
            int totalRemoved = 0;
            StripMissingScriptsRecursive(root, ref totalRemoved);

            if (totalRemoved > 0)
                log.Add($"⚠ Removed {totalRemoved} missing-script component(s) from prefab");
        }

        private static void StripMissingScriptsRecursive(GameObject go, ref int totalRemoved)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            totalRemoved += removed;

            for (int i = 0; i < go.transform.childCount; i++)
                StripMissingScriptsRecursive(go.transform.GetChild(i).gameObject, ref totalRemoved);
        }

        // ══════════════════════════════════════════════════════════════
        // Ensure Helpers
        // ══════════════════════════════════════════════════════════════

        private static T EnsureComponent<T>(GameObject go, List<string> log, string label) where T : Component
        {
            // GetComponents (plural) to detect duplicates — GetComponent only returns the first.
            var all = go.GetComponents<T>();

            if (all.Length > 1)
            {
                // Keep the first, destroy extras
                for (int i = 1; i < all.Length; i++)
                {
                    Object.DestroyImmediate(all[i]);
                }
                log.Add($"⚠ {label}: removed {all.Length - 1} duplicate(s) — kept first instance");
            }

            var component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
                log.Add($"✓ Added {label} component");
            }
            else
            {
                log.Add($"✓ {label} already present");
            }

            return component;
        }

        private static SpriteRenderer EnsureSpriteLayer(
            Transform parent,
            string childName,
            int sortOrder,
            Sprite sprite,
            Material material,
            List<string> log)
        {
            var existing = parent.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                log.Add($"✓ Sprite layer '{childName}' updated (SortOrder={sortOrder})");
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
                log.Add($"✓ Created sprite layer '{childName}' (SortOrder={sortOrder})");
            }

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = go.AddComponent<SpriteRenderer>();
            }

            sr.sortingOrder = sortOrder;
            if (sprite != null)
            {
                sr.sprite = sprite;
            }
            if (material != null)
            {
                sr.sharedMaterial = material;
            }

            return sr;
        }

        private static SpriteRenderer EnsureDodgeSpriteChild(
            Transform parent,
            string childName,
            Sprite sprite,
            List<string> log)
        {
            var existing = parent.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                log.Add($"✓ '{childName}' updated");
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
                log.Add($"✓ Created '{childName}' (Dodge_Sprite SpriteRenderer)");
            }

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = go.AddComponent<SpriteRenderer>();
            }

            sr.sortingOrder = -1;
            if (sprite != null)
            {
                sr.sprite = sprite;
            }

            var color = sr.color;
            color.a = 0f;
            sr.color = color;
            go.SetActive(false);
            return sr;
        }

        // ══════════════════════════════════════════════════════════════
        // Wiring Helpers
        // ══════════════════════════════════════════════════════════════

        private static void WireField(SerializedObject so, string propertyPath, Object value, List<string> log, string label)
        {
            if (value == null)
            {
                return;
            }

            var prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                Debug.LogWarning($"[ShipPrefabRebuilder] Property '{propertyPath}' not found on {so.targetObject?.GetType().Name}");
                return;
            }

            if (prop.objectReferenceValue == value)
            {
                log.Add($"✓ {label} (already wired)");
                return;
            }

            prop.objectReferenceValue = value;
            log.Add($"✓ {label} wired");
        }

        private static void WireJuiceSettings(SerializedObject so, string propertyPath, List<string> log, string label, List<string> todo)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(SHIP_JUICE_SETTINGS_PATH);
            if (asset == null)
            {
                todo.Add($"{label}: Missing ShipJuiceSettingsSO at {SHIP_JUICE_SETTINGS_PATH}");
                return;
            }

            WireField(so, propertyPath, asset, log, label);
        }

        // ══════════════════════════════════════════════════════════════
        // Sprite Finder / Prefab Integration
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Ensures Rigidbody2D and CircleCollider2D exist on the Ship root.
        /// </summary>
        private static void EnsurePhysicsComponents(GameObject root, List<string> log)
        {
            var rb = root.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = root.AddComponent<Rigidbody2D>();
                rb.mass = 1f;
                rb.linearDamping = 3f;
                rb.angularDamping = 0f;
                rb.gravityScale = 0f;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.constraints = RigidbodyConstraints2D.None;
                log.Add("✓ Rigidbody2D added + configured");
            }
            else
            {
                log.Add("✓ Rigidbody2D already present");
            }

            var col = root.GetComponent<CircleCollider2D>();
            if (col == null)
            {
                col = root.AddComponent<CircleCollider2D>();
                col.radius = 0.4f;
                log.Add("✓ CircleCollider2D added (radius=0.4)");
            }
            else
            {
                log.Add("✓ CircleCollider2D already present");
            }
        }

        /// <summary>
        /// Wires ShipStatsSO and InputActionAsset to all root-level gameplay components.
        /// </summary>
        private static void WireRootComponentReferences(GameObject root, List<string> log, List<string> todo)
        {
            // ── ShipStatsSO ──
            var statsSO = AssetDatabase.LoadAssetAtPath<ShipStatsSO>(SHIP_STATS_PATH);
            if (statsSO == null)
            {
                todo.Add($"ShipStatsSO missing at {SHIP_STATS_PATH} — run 'ProjectArk > Ship > Create Ship Stats Asset' first");
            }
            else
            {
                // Wire _stats to all consumers
                WireStatsToComponent<ShipMotor>(root, "_stats", statsSO, log, "ShipMotor._stats");
                WireStatsToComponent<ShipAiming>(root, "_stats", statsSO, log, "ShipAiming._stats");
                WireStatsToComponent<ShipStateController>(root, "_stats", statsSO, log, "ShipStateController._stats");
                WireStatsToComponent<ShipHealth>(root, "_stats", statsSO, log, "ShipHealth._stats");
                WireStatsToComponent<ShipDash>(root, "_stats", statsSO, log, "ShipDash._stats");
                WireStatsToComponent<ShipBoost>(root, "_stats", statsSO, log, "ShipBoost._stats");
            }

            // ── InputActionAsset ──
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            if (inputAsset == null)
            {
                todo.Add($"InputActionAsset missing at {INPUT_ACTIONS_PATH} — assign manually");
            }
            else
            {
                var inputHandler = root.GetComponent<InputHandler>();
                if (inputHandler != null)
                {
                    var so = new SerializedObject(inputHandler);
                    WireField(so, "_inputActions", inputAsset, log, "InputHandler._inputActions");
                    so.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// Helper: wire a ShipStatsSO to a specific component's field via SerializedObject.
        /// </summary>
        private static void WireStatsToComponent<T>(GameObject root, string propertyPath, ShipStatsSO stats, List<string> log, string label) where T : Component
        {
            var comp = root.GetComponent<T>();
            if (comp == null) return;
            var so = new SerializedObject(comp);
            WireField(so, propertyPath, stats, log, label);
            so.ApplyModifiedProperties();
        }

        private static void WireShipStats(SerializedObject so, string propertyPath, List<string> log, string label, List<string> todo)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ShipStatsSO>(SHIP_STATS_PATH);
            if (asset == null)
            {
                todo.Add($"{label}: Missing ShipStatsSO at {SHIP_STATS_PATH}");
                return;
            }

            WireField(so, propertyPath, asset, log, label);
        }

        private static BoostTrailView EnsureBoostTrailRoot(Transform visualTf, List<string> log, List<string> todo)
        {
            var boostTrailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BOOST_TRAIL_PREFAB_PATH);
            if (boostTrailPrefab == null)
            {
                todo.Add($"BoostTrailRoot prefab missing: {BOOST_TRAIL_PREFAB_PATH} — run 'ProjectArk > Ship > VFX > Authority > Rebuild BoostTrailRoot Prefab' first");
                return null;
            }

            var existing = visualTf.Find(BOOST_TRAIL_ROOT_NAME);
            GameObject boostTrailGo;
            if (existing != null)
            {
                boostTrailGo = existing.gameObject;
                log.Add("✓ Found BoostTrailRoot under ShipVisual");
            }
            else
            {
                boostTrailGo = PrefabUtility.InstantiatePrefab(boostTrailPrefab) as GameObject;
                if (boostTrailGo == null)
                {
                    todo.Add($"Failed to instantiate nested BoostTrailRoot from {BOOST_TRAIL_PREFAB_PATH}");
                    return null;
                }

                boostTrailGo.transform.SetParent(visualTf, false);
                boostTrailGo.transform.localPosition = Vector3.zero;
                boostTrailGo.transform.localRotation = Quaternion.identity;
                boostTrailGo.transform.localScale = Vector3.one;
                log.Add("✓ Nested BoostTrailRoot prefab instantiated under ShipVisual");
            }

            var boostTrailView = boostTrailGo.GetComponent<BoostTrailView>();
            if (boostTrailView == null)
            {
                todo.Add("BoostTrailRoot is missing BoostTrailView — rebuild the BoostTrailRoot prefab first");
                return null;
            }

            log.Add("✓ BoostTrailView available on nested BoostTrailRoot");
            return boostTrailView;
        }

        private static Sprite LoadSpriteAtPath(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
#endif
