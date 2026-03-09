#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Rebuilds the Ship Prefab's multi-layer sprite structure, VFX nodes, and component wiring.
    ///
    /// Two modes:
    ///   • Rebuild (idempotent) — creates missing nodes, updates existing ones, wires all fields.
    ///   • Force Rebuild       — destroys ALL managed child nodes first, then rebuilds from scratch.
    ///
    /// Managed nodes (under ShipVisual):
    ///   Ship_Sprite_Back / Ship_Sprite_Liquid / Ship_Sprite_HL / Ship_Sprite_Solid / Ship_Sprite_Core
    ///   Dodge_Sprite
    ///
    /// Managed nodes (under Ship_Sprite_Back):
    ///   BoostTrailParticles  (ParticleSystem — glow layer)
    ///   BoostEmberParticles  (ParticleSystem — ember layer)
    ///   BoostTrail           (TrailRenderer)
    ///   EngineParticles      (ParticleSystem — engine exhaust)
    ///
    /// Managed components (on Ship root):
    ///   ShipView, ShipBoostTrailVFX, ShipEngineVFX, DashAfterImageSpawner
    ///
    /// Menu: ProjectArk > Ship > Rebuild Ship Prefab Sprite Layers
    ///       ProjectArk > Ship > FORCE Rebuild Ship Prefab (Delete + Recreate)
    /// </summary>
    public static class ShipPrefabRebuilder
    {
        // ── Paths ──────────────────────────────────────────────────────
        private const string PREFAB_PATH        = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string GLOW_MATERIAL_PATH = "Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat";

        // ── Node names ─────────────────────────────────────────────────
        private const string VISUAL_CHILD_NAME  = "ShipVisual";

        // Sprite layers
        private const string SPRITE_BACK_NAME   = "Ship_Sprite_Back";
        private const string SPRITE_LIQUID_NAME = "Ship_Sprite_Liquid";
        private const string SPRITE_HL_NAME     = "Ship_Sprite_HL";
        private const string SPRITE_SOLID_NAME  = "Ship_Sprite_Solid";
        private const string SPRITE_CORE_NAME   = "Ship_Sprite_Core";
        private const string DODGE_SPRITE_NAME  = "Dodge_Sprite";

        // VFX nodes (children of Ship_Sprite_Back)
        private const string BOOST_TRAIL_PS_NAME    = "BoostTrailParticles";
        private const string BOOST_EMBER_PS_NAME    = "BoostEmberParticles";
        private const string BOOST_TRAIL_TR_NAME    = "BoostTrail";
        private const string ENGINE_PARTICLES_NAME  = "EngineParticles";

        // Dodge sprite
        private const string DODGE_SPRITE_TEXTURE_NAME = "player_test_fire";
        private const string DODGE_SPRITE_SRC_PATH     = @"F:\UnityProjects\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\Texture2D\player_test_fire.png";
        private const string DODGE_SPRITE_DEST_DIR     = "Assets/_Art/Ship/Glitch/Reference";
        private const string DODGE_SPRITE_DEST_PATH    = "Assets/_Art/Ship/Glitch/Reference/player_test_fire.png";

        // All managed node names under ShipVisual (for force-delete)
        private static readonly string[] MANAGED_VISUAL_CHILDREN = new[]
        {
            SPRITE_BACK_NAME, SPRITE_LIQUID_NAME, SPRITE_HL_NAME,
            SPRITE_SOLID_NAME, SPRITE_CORE_NAME, DODGE_SPRITE_NAME
        };

        // All managed node names under Ship_Sprite_Back (for force-delete)
        private static readonly string[] MANAGED_BACK_CHILDREN = new[]
        {
            BOOST_TRAIL_PS_NAME, BOOST_EMBER_PS_NAME, BOOST_TRAIL_TR_NAME, ENGINE_PARTICLES_NAME
        };

        // ══════════════════════════════════════════════════════════════
        // Menu Items
        // ══════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Ship/Rebuild Ship Prefab Sprite Layers")]
        public static void RebuildSpriteLayers() => Run(forceRebuild: false);

        [MenuItem("ProjectArk/Ship/FORCE Rebuild Ship Prefab (Delete + Recreate)")]
        public static void ForceRebuildSpriteLayers()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Force Rebuild",
                "This will DELETE all managed child nodes and components, then recreate them from scratch.\n\nAny manual tweaks to those nodes will be lost.\n\nContinue?",
                "Yes, Force Rebuild", "Cancel");
            if (confirmed) Run(forceRebuild: true);
        }

        // ══════════════════════════════════════════════════════════════
        // Core
        // ══════════════════════════════════════════════════════════════

        private static void Run(bool forceRebuild)
        {
            var log  = new List<string>();
            var todo = new List<string>();

            // ── Load prefab ────────────────────────────────────────────
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefabAsset == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Ship prefab not found at:\n{PREFAB_PATH}\n\nPlease run 'Build Ship' first.", "OK");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
            {
                var root = scope.prefabContentsRoot;

                // ── Force-delete managed nodes ─────────────────────────
                if (forceRebuild)
                {
                    ForceDeleteManagedNodes(root, log);
                    ForceDeleteManagedComponents(root, log);
                }

                // ── Find or create ShipVisual ──────────────────────────
                var visualTf = root.transform.Find(VISUAL_CHILD_NAME)
                            ?? root.transform.Find("VisualChild");
                if (visualTf == null)
                {
                    var vGo = new GameObject(VISUAL_CHILD_NAME);
                    vGo.transform.SetParent(root.transform, false);
                    visualTf = vGo.transform;
                    log.Add($"✓ Created {VISUAL_CHILD_NAME}");
                }
                else
                {
                    log.Add($"✓ Found visual parent: {visualTf.name}");
                }

                // ── Load glow material (auto-create if missing) ────────
                var glowMat = AssetDatabase.LoadAssetAtPath<Material>(GLOW_MATERIAL_PATH);
                if (glowMat == null)
                {
                    glowMat = ShipGlowMaterialCreator.CreateOrGet();
                    if (glowMat != null) log.Add("✓ ShipGlowMaterial auto-created");
                    else                 todo.Add($"ShipGlowMaterial not found at {GLOW_MATERIAL_PATH} — run 'Create Ship Glow Material' first");
                }

                // ── Load reference sprites ─────────────────────────────
                // GG PlayerSkinDefault State 0 (Normal):
                //   solidSprite     = Movement_10.png
                //   liquidSprite    = Movement_3.png
                //   highlightSprite = Movement_21.png
                Sprite solidSprite  = FindSprite("Movement_10");
                Sprite liquidSprite = FindSprite("Movement_3");
                Sprite hlSprite     = FindSprite("Movement_21");
                if (solidSprite  == null) todo.Add("Movement_10 sprite not found — import Assets/_Art/Ship/Glitch/Movement_10.png first");
                if (liquidSprite == null) todo.Add("Movement_3 sprite not found — import Assets/_Art/Ship/Glitch/Movement_3.png first");
                if (hlSprite     == null) todo.Add("Movement_21 sprite not found — import Assets/_Art/Ship/Glitch/Movement_21.png first");

                // ── 5 Sprite Layers ────────────────────────────────────
                var back   = EnsureSpriteLayer(visualTf, SPRITE_BACK_NAME,   -3, null,        null,    log);
                var liquid = EnsureSpriteLayer(visualTf, SPRITE_LIQUID_NAME, -2, liquidSprite, glowMat, log);
                var hl     = EnsureSpriteLayer(visualTf, SPRITE_HL_NAME,     -1, hlSprite,     null,    log);
                var solid  = EnsureSpriteLayer(visualTf, SPRITE_SOLID_NAME,   0, solidSprite,  null,    log);
                var core   = EnsureSpriteLayer(visualTf, SPRITE_CORE_NAME,    1, null,         null,    log);

                if (hl != null) { var c = hl.color; c.a = 0.5f; hl.color = c; log.Add("✓ Ship_Sprite_HL alpha = 0.5"); }

                // ── Dodge_Sprite ───────────────────────────────────────
                EnsureDodgeSpriteTexture(log, todo);
                Sprite dodgeSprite = FindSprite(DODGE_SPRITE_TEXTURE_NAME);
                var dodgeSr = EnsureDodgeSpriteChild(visualTf, DODGE_SPRITE_NAME, dodgeSprite, log);
                if (dodgeSprite == null) todo.Add($"Dodge_Sprite: '{DODGE_SPRITE_TEXTURE_NAME}' not found — import player_test_fire.png then re-run");

                // ── VFX nodes under Ship_Sprite_Back ───────────────────
                var backTf = back != null ? back.transform : visualTf;

                // Glow trail PS
                var boostTrailPs = EnsureParticleSystemChild(backTf, BOOST_TRAIL_PS_NAME,
                    new Vector3(0f, -0.15f, 0f), glowMat, log);

                // Ember trail PS (new — mat_boost_ember_trail)
                var boostEmberPs = EnsureParticleSystemChild(backTf, BOOST_EMBER_PS_NAME,
                    new Vector3(0f, -0.15f, 0f), glowMat, log);

                // Trail renderer
                var boostTrailTr = EnsureTrailRendererChild(backTf, BOOST_TRAIL_TR_NAME,
                    new Vector3(0f, -0.15f, 0f), glowMat, log);

                // Engine exhaust PS (positioned at ship nose / center)
                var enginePs = EnsureParticleSystemChild(backTf, ENGINE_PARTICLES_NAME,
                    new Vector3(0f, 0.1f, 0f), glowMat, log);

                // ── ShipView ───────────────────────────────────────────
                var shipView = EnsureComponent<ShipView>(root, log, "ShipView");
                var svSO = new SerializedObject(shipView);
                WireField(svSO, "_backRenderer",        back,       log, "ShipView._backRenderer");
                WireField(svSO, "_liquidRenderer",      liquid,     log, "ShipView._liquidRenderer");
                WireField(svSO, "_hlRenderer",          hl,         log, "ShipView._hlRenderer");
                WireField(svSO, "_solidRenderer",       solid,      log, "ShipView._solidRenderer");
                WireField(svSO, "_coreRenderer",        core,       log, "ShipView._coreRenderer");
                WireField(svSO, "_boostTrail",          boostTrailTr, log, "ShipView._boostTrail");
                WireField(svSO, "_dodgeSprite",         dodgeSr,    log, "ShipView._dodgeSprite");
                if (back != null)
                {
                    var p = svSO.FindProperty("_backSpriteTransform");
                    if (p != null && p.objectReferenceValue != (Object)back.transform)
                    { p.objectReferenceValue = back.transform; log.Add("✓ ShipView._backSpriteTransform wired"); }
                    else if (p != null) log.Add("✓ ShipView._backSpriteTransform (already wired)");
                }
                WireJuiceSettings(svSO, "_juiceSettings", log, "ShipView._juiceSettings", todo);
                svSO.ApplyModifiedProperties();

                // ── ShipBoostTrailVFX ──────────────────────────────────
                var boostVfx = EnsureComponent<ShipBoostTrailVFX>(root, log, "ShipBoostTrailVFX");
                var bvSO = new SerializedObject(boostVfx);
                WireField(bvSO, "_boostTrailParticles", boostTrailPs, log, "ShipBoostTrailVFX._boostTrailParticles");
                WireField(bvSO, "_boostEmberParticles", boostEmberPs, log, "ShipBoostTrailVFX._boostEmberParticles");
                WireJuiceSettings(bvSO, "_juiceSettings", log, "ShipBoostTrailVFX._juiceSettings", todo);
                bvSO.ApplyModifiedProperties();

                // ── ShipEngineVFX ──────────────────────────────────────
                var engineVfx = EnsureComponent<ShipEngineVFX>(root, log, "ShipEngineVFX");
                var evSO = new SerializedObject(engineVfx);
                WireField(evSO, "_engineParticles", enginePs, log, "ShipEngineVFX._engineParticles");
                WireJuiceSettings(evSO, "_juiceSettings", log, "ShipEngineVFX._juiceSettings", todo);
                evSO.ApplyModifiedProperties();

                // ── DashAfterImageSpawner ──────────────────────────────
                var spawner = root.GetComponent<DashAfterImageSpawner>();
                if (spawner != null && solid != null)
                {
                    var dSO = new SerializedObject(spawner);
                    WireField(dSO, "_shipSpriteRenderer", solid, log, "DashAfterImageSpawner._shipSpriteRenderer → Ship_Sprite_Solid");
                    dSO.ApplyModifiedProperties();
                }
                else if (spawner == null)
                {
                    log.Add("ℹ DashAfterImageSpawner not found on root — skipped");
                }
            }

            // ── Summary ────────────────────────────────────────────────
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(forceRebuild ? "── FORCE REBUILD COMPLETED ─────────────────" : "── REBUILD COMPLETED ───────────────────────");
            foreach (var entry in log) sb.AppendLine(entry);
            if (todo.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("── MANUAL STEPS REQUIRED ──────────────────");
                for (int i = 0; i < todo.Count; i++) sb.AppendLine($"{i + 1}. {todo[i]}");
            }

            Debug.Log("[ShipPrefabRebuilder] Done.\n" + sb);
            EditorUtility.DisplayDialog(
                forceRebuild ? "Ship Prefab Force-Rebuilt" : "Ship Prefab Rebuilt",
                sb.ToString(), "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // Force Delete
        // ══════════════════════════════════════════════════════════════

        private static void ForceDeleteManagedNodes(GameObject root, List<string> log)
        {
            var visualTf = root.transform.Find(VISUAL_CHILD_NAME)
                        ?? root.transform.Find("VisualChild");
            if (visualTf == null) return;

            // Delete VFX nodes under Ship_Sprite_Back first
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

            // Delete sprite layer nodes
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
            // Remove and re-add managed components so all fields reset to default
            TryDestroyComponent<ShipBoostTrailVFX>(root, log);
            TryDestroyComponent<ShipEngineVFX>(root, log);
            // Note: ShipView and DashAfterImageSpawner are NOT force-deleted
            // because they may have other manually-set fields we don't manage
        }

        private static void TryDestroyComponent<T>(GameObject go, List<string> log) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null)
            {
                Object.DestroyImmediate(c);
                log.Add($"✗ Removed {typeof(T).Name} (force rebuild)");
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Ensure Helpers
        // ══════════════════════════════════════════════════════════════

        private static T EnsureComponent<T>(GameObject go, List<string> log, string label) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
                log.Add($"✓ Added {label} component");
            }
            else
            {
                log.Add($"✓ {label} already present");
            }
            return c;
        }

        private static SpriteRenderer EnsureSpriteLayer(
            Transform parent, string childName, int sortOrder,
            Sprite sprite, Material material, List<string> log)
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
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortOrder;
            if (sprite   != null) sr.sprite         = sprite;
            if (material != null) sr.sharedMaterial = material;
            return sr;
        }

        private static ParticleSystem EnsureParticleSystemChild(
            Transform parent, string childName, Vector3 localPos, Material mat, List<string> log)
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
            if (ps == null) ps = go.AddComponent<ParticleSystem>();

            // Stop by default; VFX scripts will configure and play
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 0f;

            // Assign Additive material
            if (mat != null)
            {
                var psr = go.GetComponent<ParticleSystemRenderer>();
                if (psr == null) psr = go.AddComponent<ParticleSystemRenderer>();
                psr.sharedMaterial = mat;
                log.Add($"  → {childName}.ParticleSystemRenderer.sharedMaterial = ShipGlowMaterial");
            }

            return ps;
        }

        private static TrailRenderer EnsureTrailRendererChild(
            Transform parent, string childName, Vector3 localPos, Material mat, List<string> log)
        {
            var existing = parent.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                log.Add($"✓ '{childName}' (TrailRenderer) updated");
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
                log.Add($"✓ Created '{childName}' (TrailRenderer) at {localPos}");
            }

            var tr = go.GetComponent<TrailRenderer>();
            if (tr == null) tr = go.AddComponent<TrailRenderer>();
            tr.emitting          = false;
            tr.time              = 0.25f;
            tr.startWidth        = 0.12f;
            tr.endWidth          = 0f;
            tr.minVertexDistance = 0.05f;
            if (mat != null)
            {
                tr.sharedMaterial = mat;
                log.Add($"  → {childName}.TrailRenderer.sharedMaterial = ShipGlowMaterial");
            }
            return tr;
        }

        private static SpriteRenderer EnsureDodgeSpriteChild(
            Transform parent, string childName, Sprite sprite, List<string> log)
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
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -1;
            if (sprite != null) sr.sprite = sprite;
            var c = sr.color; c.a = 0f; sr.color = c;
            go.SetActive(false);
            return sr;
        }

        // ══════════════════════════════════════════════════════════════
        // Wiring Helpers
        // ══════════════════════════════════════════════════════════════

        private static void WireField(SerializedObject so, string propertyPath, Object value,
            List<string> log, string label)
        {
            if (value == null) return;
            var prop = so.FindProperty(propertyPath);
            if (prop == null) { Debug.LogWarning($"[ShipPrefabRebuilder] Property '{propertyPath}' not found on {so.targetObject?.GetType().Name}"); return; }
            if (prop.objectReferenceValue == value) { log.Add($"✓ {label} (already wired)"); return; }
            prop.objectReferenceValue = value;
            log.Add($"✓ {label} wired");
        }

        private static void WireJuiceSettings(SerializedObject so, string propertyPath,
            List<string> log, string label, List<string> todo)
        {
            var guids = AssetDatabase.FindAssets("t:ShipJuiceSettingsSO");
            if (guids.Length == 0) { todo.Add($"{label}: No ShipJuiceSettingsSO found — assign manually"); return; }
            var asset = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
            WireField(so, propertyPath, asset, log, label);
        }

        // ══════════════════════════════════════════════════════════════
        // Dodge Sprite Texture Import
        // ══════════════════════════════════════════════════════════════

        private static void EnsureDodgeSpriteTexture(List<string> log, List<string> todo)
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(DODGE_SPRITE_DEST_PATH) != null)
            {
                log.Add($"✓ player_test_fire.png already imported");
                return;
            }
            if (!File.Exists(DODGE_SPRITE_SRC_PATH))
            {
                todo.Add($"player_test_fire.png not found at: {DODGE_SPRITE_SRC_PATH}\n  → Copy manually to {DODGE_SPRITE_DEST_PATH} and re-run.");
                log.Add("⚠ player_test_fire.png source not found — see MANUAL STEPS");
                return;
            }
            if (!AssetDatabase.IsValidFolder(DODGE_SPRITE_DEST_DIR))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "../", DODGE_SPRITE_DEST_DIR));
                AssetDatabase.Refresh();
            }
            File.Copy(DODGE_SPRITE_SRC_PATH, Path.Combine(Application.dataPath, "../", DODGE_SPRITE_DEST_PATH), overwrite: true);
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(DODGE_SPRITE_DEST_PATH) as TextureImporter;
            if (importer != null)
            {
                importer.textureType         = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 707f;
                importer.filterMode          = FilterMode.Bilinear;
                importer.textureCompression  = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                log.Add("✓ player_test_fire.png copied and imported (PPU=707, Bilinear, Uncompressed)");
            }
            else
            {
                log.Add("✓ player_test_fire.png copied (check import settings manually)");
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Sprite Finder
        // ══════════════════════════════════════════════════════════════

        private static Sprite FindSprite(string nameFilter)
        {
            var guids = AssetDatabase.FindAssets($"{nameFilter} t:Sprite");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
#endif
