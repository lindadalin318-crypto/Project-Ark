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
        private const string CANARY_BODY_MATERIAL_PATH = "Assets/_Art/Ship/Canary/Materials/mat_ship_canary_body_default.mat";
        private const string CANARY_SHAPE_MATERIAL_PATH = "Assets/_Art/Ship/Canary/Materials/mat_ship_canary_shape.mat";
        private const string CANARY_OUTLINE_MATERIAL_PATH = "Assets/_Art/Ship/Canary/Materials/mat_ship_canary_outline.mat";
        private const string HIT_SPARK_MATERIAL_PATH = "Assets/_Art/Ship/Canary/Materials/mat_ship_canary_trail.mat";
        private const string CANARY_SHAPE_SPRITE_PATH = "Assets/_Art/Ship/Canary/Sprites/Shape/spr_ship_canary_shape_normal_mask.png";
        private const string CANARY_HIT_MASK_SPRITE_PATH = "Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_hit_mask.png";
        private const string CANARY_BODY_SPRITE_PATH = "Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_body_normal_albedo.png";
        private const string CANARY_OUTLINE_SPRITE_PATH = "Assets/_Art/Ship/Canary/Sprites/Outline/spr_ship_canary_outline_normal_outline.png";
        private const string CANARY_CORE_SPRITE_PATH = "Assets/_Art/Ship/Canary/Sprites/Core/spr_ship_canary_core_normal_albedo.png";
        private const string CANARY_WEAPON_MOUNT_SPRITE_PATH = "Assets/_Art/Ship/Canary/Sprites/WeaponMount/spr_ship_canary_weapon_mount_normal_albedo.png";
        private const string SHIP_JUICE_SETTINGS_PATH = "Assets/_Data/Ship/DefaultShipJuiceSettings.asset";
        private const string SHIP_STATS_PATH = "Assets/_Data/Ship/DefaultShipStats.asset";
        private const string INPUT_ACTIONS_PATH = "Assets/Input/ShipActions.inputactions";
        private const string DASH_AFTER_IMAGE_PREFAB_PATH = "Assets/_Prefabs/Ship/DashAfterImage.prefab";

        // ── Node names ─────────────────────────────────────────────────
        private const string VISUAL_CHILD_NAME = "ShipVisual";

        // Sprite layers. Field wiring keeps legacy role names, while physical nodes use Canary art semantics.
        private const string SPRITE_BACK_NAME = "Ship_Sprite_Back";
        private const string SPRITE_LIQUID_NAME = "Ship_Sprite_Shape";
        private const string SPRITE_HL_NAME = "Ship_Sprite_Outline";
        private const string SPRITE_SOLID_NAME = "Ship_Sprite_Body";
        private const string SPRITE_CORE_NAME = "Ship_Sprite_Core";
        private const string SPRITE_WEAPON_MOUNT_NAME = "Ship_Sprite_WeaponMount";
        private const string DODGE_SPRITE_NAME = "Dodge_Sprite";
        private const string BOOST_TRAIL_ROOT_NAME = "BoostTrailRoot";
        private const string HIT_SPARK_NAME = "Ship_HitSpark";
        private const string HIT_MASK_FLASH_NAME = "Ship_HitMaskFlash";

        // Sprite asset aliases
        private const string CANARY_SHAPE_SPRITE_NAME = "spr_ship_canary_shape_normal_mask";
        private const string CANARY_HIT_MASK_SPRITE_NAME = "spr_ship_canary_shape_hit_mask";
        private const string CANARY_BODY_SPRITE_NAME = "spr_ship_canary_body_normal_albedo";
        private const string CANARY_OUTLINE_SPRITE_NAME = "spr_ship_canary_outline_normal_outline";
        private const string CANARY_CORE_SPRITE_NAME = "spr_ship_canary_core_normal_albedo";
        private const string CANARY_WEAPON_MOUNT_SPRITE_NAME = "spr_ship_canary_weapon_mount_normal_albedo";

        // Dodge ghost uses the current Canary body as a temporary readable silhouette until Batch 3 dash frames land.
        private const string DODGE_SPRITE_TEXTURE_NAME = CANARY_BODY_SPRITE_NAME;
        private const string DODGE_SPRITE_DEST_PATH = CANARY_BODY_SPRITE_PATH;

        // All managed node names under ShipVisual (for force-delete)
        private static readonly string[] MANAGED_VISUAL_CHILDREN =
        {
            SPRITE_BACK_NAME,
            "Ship_Sprite_Liquid",
            "Ship_Sprite_HL",
            "Ship_Sprite_Solid",
            SPRITE_LIQUID_NAME,
            SPRITE_HL_NAME,
            SPRITE_SOLID_NAME,
            SPRITE_CORE_NAME,
            SPRITE_WEAPON_MOUNT_NAME,
            DODGE_SPRITE_NAME,
            BOOST_TRAIL_ROOT_NAME,
            HIT_SPARK_NAME,
            HIT_MASK_FLASH_NAME
        };

        // ══════════════════════════════════════════════════════════════
        // Menu Items
        // ══════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Ship/Authority/Rebuild Ship Prefab")]
        public static void RebuildSpriteLayers() => Run(forceRebuild: false, showDialog: true);

        public static void RebuildSpriteLayersSilently() => Run(forceRebuild: false, showDialog: false);

        public static void ForceRebuildSpriteLayersSilently() => Run(forceRebuild: true, showDialog: false);

        [MenuItem("ProjectArk/Ship/Authority/FORCE Rebuild Ship Prefab")]
        public static void ForceRebuildSpriteLayers()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Force Rebuild",
                "This will DELETE all managed child nodes and components, then recreate them from scratch.\n\nAny manual tweaks to those nodes will be lost.\n\nContinue?",
                "Yes, Force Rebuild", "Cancel");
            if (confirmed)
            {
                Run(forceRebuild: true, showDialog: true);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Core
        // ══════════════════════════════════════════════════════════════

        private static void Run(bool forceRebuild, bool showDialog)
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
                else
                {
                    DeleteLegacyVisualNodes(root, log);
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

                var bodyMat = AssetDatabase.LoadAssetAtPath<Material>(CANARY_BODY_MATERIAL_PATH);
                var shapeMat = AssetDatabase.LoadAssetAtPath<Material>(CANARY_SHAPE_MATERIAL_PATH);
                var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(CANARY_OUTLINE_MATERIAL_PATH);

                if (bodyMat == null) todo.Add($"Canary body material missing at {CANARY_BODY_MATERIAL_PATH}");
                if (shapeMat == null) todo.Add($"Canary shape material missing at {CANARY_SHAPE_MATERIAL_PATH}");
                if (outlineMat == null) todo.Add($"Canary outline material missing at {CANARY_OUTLINE_MATERIAL_PATH}");

                EnsureSpriteImportSettings(CANARY_HIT_MASK_SPRITE_PATH, log);

                Sprite bodySprite = LoadSpriteAtPath(CANARY_BODY_SPRITE_PATH);
                Sprite shapeSprite = LoadSpriteAtPath(CANARY_SHAPE_SPRITE_PATH);
                Sprite hitMaskSprite = LoadSpriteAtPath(CANARY_HIT_MASK_SPRITE_PATH);
                Sprite outlineSprite = LoadSpriteAtPath(CANARY_OUTLINE_SPRITE_PATH);
                Sprite coreSprite = LoadSpriteAtPath(CANARY_CORE_SPRITE_PATH);
                Sprite weaponMountSprite = LoadSpriteAtPath(CANARY_WEAPON_MOUNT_SPRITE_PATH);
                if (bodySprite == null)
                {
                    todo.Add($"{CANARY_BODY_SPRITE_NAME} sprite not found — import {CANARY_BODY_SPRITE_PATH} first");
                }
                if (shapeSprite == null)
                {
                    todo.Add($"{CANARY_SHAPE_SPRITE_NAME} sprite not found — import {CANARY_SHAPE_SPRITE_PATH} first");
                }
                if (hitMaskSprite == null)
                {
                    todo.Add($"{CANARY_HIT_MASK_SPRITE_NAME} sprite not found — import {CANARY_HIT_MASK_SPRITE_PATH} first");
                }
                if (outlineSprite == null)
                {
                    todo.Add($"{CANARY_OUTLINE_SPRITE_NAME} sprite not found — import {CANARY_OUTLINE_SPRITE_PATH} first");
                }
                if (coreSprite == null)
                {
                    todo.Add($"{CANARY_CORE_SPRITE_NAME} sprite not found — import {CANARY_CORE_SPRITE_PATH} first");
                }
                if (weaponMountSprite == null)
                {
                    todo.Add($"{CANARY_WEAPON_MOUNT_SPRITE_NAME} sprite not found — import {CANARY_WEAPON_MOUNT_SPRITE_PATH} first");
                }

                var back = EnsureSpriteLayer(visualTf, SPRITE_BACK_NAME, -3, null, bodyMat, log);
                var liquid = EnsureSpriteLayer(visualTf, SPRITE_LIQUID_NAME, 1, shapeSprite, shapeMat, log);
                var solid = EnsureSpriteLayer(visualTf, SPRITE_SOLID_NAME, 0, bodySprite, bodyMat, log);
                var hl = EnsureSpriteLayer(visualTf, SPRITE_HL_NAME, 2, outlineSprite, outlineMat, log);
                var core = EnsureSpriteLayer(visualTf, SPRITE_CORE_NAME, 3, coreSprite, bodyMat, log);
                var weaponMount = EnsureSpriteLayer(visualTf, SPRITE_WEAPON_MOUNT_NAME, 6, weaponMountSprite, bodyMat, log);
                var hitMask = EnsureSpriteLayer(visualTf, HIT_MASK_FLASH_NAME, 7, hitMaskSprite, bodyMat, log);

                if (back != null)
                {
                    var backColor = back.color;
                    backColor.a = 0f;
                    back.color = backColor;
                    log.Add("✓ Ship_Sprite_Back alpha = 0 (thruster pulse transform holder)");
                }

                if (liquid != null)
                {
                    liquid.enabled = false;
                    log.Add("✓ Ship_Sprite_Shape disabled (reserved mask layer)");
                }

                if (hl != null)
                {
                    var highlightColor = hl.color;
                    highlightColor.a = 1f;
                    hl.color = highlightColor;
                    log.Add("✓ Ship_Sprite_Outline alpha = 1");
                }

                if (core != null)
                {
                    var coreColor = core.color;
                    coreColor.a = 1f;
                    core.color = coreColor;
                    log.Add("✓ Ship_Sprite_Core alpha = 1");
                }

                if (weaponMount != null)
                {
                    var weaponMountColor = weaponMount.color;
                    weaponMountColor.a = 1f;
                    weaponMount.color = weaponMountColor;
                    log.Add("✓ Ship_Sprite_WeaponMount alpha = 1");
                }

                if (hitMask != null)
                {
                    hitMask.color = new Color(1f, 1f, 1f, 0f);
                    hitMask.enabled = false;
                    log.Add("✓ Ship_HitMaskFlash hidden by default");
                }

                Sprite dodgeSprite = LoadSpriteAtPath(DODGE_SPRITE_DEST_PATH);
                var dodgeSr = EnsureDodgeSpriteChild(visualTf, DODGE_SPRITE_NAME, dodgeSprite, log);
                if (dodgeSprite == null)
                {
                    todo.Add($"Dodge_Sprite: '{DODGE_SPRITE_TEXTURE_NAME}' not found — import {DODGE_SPRITE_DEST_PATH} first, then re-run");
                }

                var hitSparkParticles = EnsureHitSparkParticles(visualTf, log);

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
                WireField(hitVisualsSO, "_hitSparkParticles", hitSparkParticles, log, "ShipHitVisuals._hitSparkParticles");
                WireField(hitVisualsSO, "_hitMaskRenderer", hitMask, log, "ShipHitVisuals._hitMaskRenderer");
                WireJuiceSettings(hitVisualsSO, "_juiceSettings", log, "ShipHitVisuals._juiceSettings", todo);
                hitVisualsSO.ApplyModifiedProperties();

                // ── ShipFireVisuals ──
                var fireVisuals = EnsureComponent<ShipFireVisuals>(root, log, "ShipFireVisuals");
                var fireVisualsSO = new SerializedObject(fireVisuals);
                WireField(fireVisualsSO, "_weaponMountRenderer", weaponMount, log, "ShipFireVisuals._weaponMountRenderer");
                WireField(fireVisualsSO, "_coreRenderer", core, log, "ShipFireVisuals._coreRenderer");
                WireJuiceSettings(fireVisualsSO, "_juiceSettings", log, "ShipFireVisuals._juiceSettings", todo);
                fireVisualsSO.ApplyModifiedProperties();

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
                WireField(shipViewSO, "_weaponMountRenderer", weaponMount, log, "ShipView._weaponMountRenderer");
                WireField(shipViewSO, "_boostVisuals", boostVisuals, log, "ShipView._boostVisuals");
                WireField(shipViewSO, "_hitVisuals", hitVisuals, log, "ShipView._hitVisuals");
                WireField(shipViewSO, "_dashVisuals", dashVisuals, log, "ShipView._dashVisuals");
                WireField(shipViewSO, "_fireVisuals", fireVisuals, log, "ShipView._fireVisuals");
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
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    forceRebuild ? "Ship Prefab Force-Rebuilt" : "Ship Prefab Rebuilt",
                    summary.ToString(),
                    "OK");
            }
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

        private static void DeleteLegacyVisualNodes(GameObject root, List<string> log)
        {
            var visualTf = root.transform.Find(VISUAL_CHILD_NAME);
            if (visualTf == null)
            {
                return;
            }

            DeleteChildIfPresent(visualTf, "Ship_Sprite_Liquid", log, "legacy Glitch liquid layer");
            DeleteChildIfPresent(visualTf, "Ship_Sprite_HL", log, "legacy Glitch highlight layer");
            DeleteChildIfPresent(visualTf, "Ship_Sprite_Solid", log, "legacy Glitch solid layer");
        }

        private static void DeleteChildIfPresent(Transform parent, string childName, List<string> log, string reason)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            Object.DestroyImmediate(child.gameObject);
            log.Add($"✗ Deleted '{childName}' ({reason})");
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

        private static ParticleSystem EnsureHitSparkParticles(Transform visualTf, List<string> log)
        {
            var child = visualTf.Find(HIT_SPARK_NAME);
            if (child == null)
            {
                var go = new GameObject(HIT_SPARK_NAME);
                go.transform.SetParent(visualTf, false);
                child = go.transform;
                log.Add($"✓ Created {HIT_SPARK_NAME}");
            }
            else
            {
                log.Add($"✓ Found {HIT_SPARK_NAME}");
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;

            var particles = child.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                particles = child.gameObject.AddComponent<ParticleSystem>();
                log.Add($"✓ Added ParticleSystem to {HIT_SPARK_NAME}");
            }

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.08f;
            main.startLifetime = 0.08f;
            main.startSpeed = 0.7f;
            main.startSize = 0.08f;
            main.startColor = new Color(1f, 0.92f, 0.38f, 0.95f);
            main.maxParticles = 8;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;
            shape.arc = 360f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.92f, 0.38f), 0f),
                    new GradientColorKey(new Color(1f, 1f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 8;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = EnsureHitSparkMaterial(log);

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            log.Add($"✓ Configured {HIT_SPARK_NAME} pooled local hit spark");
            return particles;
        }

        private static Material EnsureHitSparkMaterial(List<string> log)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(HIT_SPARK_MATERIAL_PATH);
            if (material == null)
            {
                Debug.LogError($"[ShipPrefabRebuilder] Missing Hit Spark material at {HIT_SPARK_MATERIAL_PATH}.");
                return null;
            }

            if (material.shader == null || !material.shader.isSupported)
            {
                Debug.LogError($"[ShipPrefabRebuilder] Hit Spark material '{material.name}' has an unsupported shader.");
                return null;
            }

            log.Add($"✓ Hit Spark material wired: {HIT_SPARK_MATERIAL_PATH}");
            return material;
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

        private static void EnsureSpriteImportSettings(string assetPath, List<string> log)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                log.Add($"✓ Imported {assetPath} as Sprite");
            }
        }

        private static Sprite LoadSpriteAtPath(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
#endif
