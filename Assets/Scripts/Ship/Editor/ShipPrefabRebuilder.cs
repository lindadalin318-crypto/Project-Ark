#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Rebuilds the live Ship prefab structure and its managed visual integrations.
    ///
    /// Authority owned by this tool:
    ///   • Multi-layer ship sprite hierarchy under ShipVisual
    ///   • EngineParticles child under Ship_Sprite_Back
    ///   • Nested BoostTrailRoot prefab integration under ShipVisual
    ///   • Serialized wiring on ShipView / ShipEngineVFX / DashAfterImageSpawner
    ///
    /// Two modes:
    ///   • Rebuild (idempotent) — creates missing nodes, updates existing ones, wires all managed fields.
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
        private const string SHIP_LIQUID_BOOST_SPRITE_PATH = "Assets/_Art/Ship/Glitch/Boost_16.png";
        private const string SHIP_LIQUID_NORMAL_SPRITE_PATH = "Assets/_Art/Ship/Glitch/Movement_3.png";
        private const string SHIP_SOLID_SPRITE_PATH = "Assets/_Art/Ship/Glitch/Movement_10.png";
        private const string SHIP_HIGHLIGHT_SPRITE_PATH = "Assets/_Art/Ship/Glitch/Movement_21.png";
        private const string SHIP_JUICE_SETTINGS_PATH = "Assets/_Data/Ship/DefaultShipJuiceSettings.asset";

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

        // VFX nodes (children of Ship_Sprite_Back)
        private const string ENGINE_PARTICLES_NAME = "EngineParticles";

        // Sprite asset aliases (physical names remain frozen during MVP)
        private const string SHIP_LIQUID_BOOST_SPRITE_NAME = "Boost_16";
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

        // All managed node names under Ship_Sprite_Back (for force-delete)
        private static readonly string[] MANAGED_BACK_CHILDREN =
        {
            ENGINE_PARTICLES_NAME
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
                    $"Ship prefab not found at:\n{PREFAB_PATH}\n\nPlease run 'ProjectArk > Ship > Bootstrap > Build Ship Scene Setup' first.",
                    "OK");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
            {
                var root = scope.prefabContentsRoot;

                if (forceRebuild)
                {
                    ForceDeleteManagedNodes(root, log);
                    ForceDeleteManagedComponents(root, log);
                }

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
                Sprite boostLiquidSprite = LoadSpriteAtPath(SHIP_LIQUID_BOOST_SPRITE_PATH);
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
                if (boostLiquidSprite == null)
                {
                    todo.Add($"{SHIP_LIQUID_BOOST_SPRITE_NAME} sprite not found — import {SHIP_LIQUID_BOOST_SPRITE_PATH} first");
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

                var backTf = back != null ? back.transform : visualTf;
                var enginePs = EnsureParticleSystemChild(
                    backTf,
                    ENGINE_PARTICLES_NAME,
                    new Vector3(0f, 0.1f, 0f),
                    glowMat,
                    log);

                var shipView = EnsureComponent<ShipView>(root, log, "ShipView");
                var shipViewSO = new SerializedObject(shipView);
                WireField(shipViewSO, "_backRenderer", back, log, "ShipView._backRenderer");
                WireField(shipViewSO, "_liquidRenderer", liquid, log, "ShipView._liquidRenderer");
                WireField(shipViewSO, "_hlRenderer", hl, log, "ShipView._hlRenderer");
                WireField(shipViewSO, "_solidRenderer", solid, log, "ShipView._solidRenderer");
                WireField(shipViewSO, "_coreRenderer", core, log, "ShipView._coreRenderer");
                WireField(shipViewSO, "_boostTrailView", boostTrailView, log, "ShipView._boostTrailView");
                WireField(shipViewSO, "_normalLiquidSprite", liquidSprite, log, "ShipView._normalLiquidSprite");
                WireField(shipViewSO, "_boostLiquidSprite", boostLiquidSprite, log, "ShipView._boostLiquidSprite");
                WireField(shipViewSO, "_dodgeSprite", dodgeSr, log, "ShipView._dodgeSprite");
                if (back != null)
                {
                    var backTransformProp = shipViewSO.FindProperty("_backSpriteTransform");
                    if (backTransformProp != null && backTransformProp.objectReferenceValue != (Object)back.transform)
                    {
                        backTransformProp.objectReferenceValue = back.transform;
                        log.Add("✓ ShipView._backSpriteTransform wired");
                    }
                    else if (backTransformProp != null)
                    {
                        log.Add("✓ ShipView._backSpriteTransform (already wired)");
                    }
                }
                WireJuiceSettings(shipViewSO, "_juiceSettings", log, "ShipView._juiceSettings", todo);
                shipViewSO.ApplyModifiedProperties();

                var engineVfx = EnsureComponent<ShipEngineVFX>(root, log, "ShipEngineVFX");
                var engineVfxSO = new SerializedObject(engineVfx);
                WireField(engineVfxSO, "_engineParticles", enginePs, log, "ShipEngineVFX._engineParticles");
                WireJuiceSettings(engineVfxSO, "_juiceSettings", log, "ShipEngineVFX._juiceSettings", todo);
                engineVfxSO.ApplyModifiedProperties();

                var spawner = root.GetComponent<DashAfterImageSpawner>();
                if (spawner != null && solid != null)
                {
                    var spawnerSO = new SerializedObject(spawner);
                    WireField(spawnerSO, "_shipSpriteRenderer", solid, log, "DashAfterImageSpawner._shipSpriteRenderer → Ship_Sprite_Solid");
                    spawnerSO.ApplyModifiedProperties();
                }
                else if (spawner == null)
                {
                    log.Add("ℹ DashAfterImageSpawner not found on root — skipped");
                }
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

            var backTf = visualTf.Find(SPRITE_BACK_NAME);
            if (backTf != null)
            {
                foreach (var name in MANAGED_BACK_CHILDREN)
                {
                    var child = backTf.Find(name);
                    if (child != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                        log.Add($"✗ Deleted '{name}' (force rebuild)");
                    }
                }
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
            TryDestroyComponent<ShipEngineVFX>(root, log);
            // Note: ShipView and DashAfterImageSpawner are NOT force-deleted
            // because they may have other manually-set fields we don't manage.
        }

        private static void TryDestroyComponent<T>(GameObject go, List<string> log) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
                log.Add($"✗ Removed {typeof(T).Name} (force rebuild)");
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Ensure Helpers
        // ══════════════════════════════════════════════════════════════

        private static T EnsureComponent<T>(GameObject go, List<string> log, string label) where T : Component
        {
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

        private static ParticleSystem EnsureParticleSystemChild(
            Transform parent,
            string childName,
            Vector3 localPos,
            Material mat,
            List<string> log)
        {
            var existing = parent.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                log.Add($"✓ '{childName}' (PS) updated");
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.identity;
                log.Add($"✓ Created '{childName}' (ParticleSystem) at {localPos}");
            }

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = go.AddComponent<ParticleSystem>();
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            if (mat != null)
            {
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                if (renderer == null)
                {
                    renderer = go.AddComponent<ParticleSystemRenderer>();
                }

                renderer.sharedMaterial = mat;
                log.Add($"  → {childName}.ParticleSystemRenderer.sharedMaterial = ShipGlowMaterial");
            }

            return ps;
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
