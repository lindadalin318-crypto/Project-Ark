#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Rebuilds the Ship Prefab's multi-layer sprite structure.
    /// Adds 5 SpriteRenderer child nodes under ShipVisual, wires ShipView,
    /// and adds ShipView component to the root if missing.
    ///
    /// Menu: ProjectArk > Ship > Rebuild Ship Prefab Sprite Layers
    ///
    /// Safe to run multiple times (idempotent).
    /// </summary>
    public static class ShipPrefabRebuilder
    {
        private const string PREFAB_PATH        = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string VISUAL_CHILD_NAME  = "ShipVisual";
        private const string GLOW_MATERIAL_PATH = "Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat";

        private const string SPRITE_BACK_NAME   = "Ship_Sprite_Back";
        private const string SPRITE_LIQUID_NAME = "Ship_Sprite_Liquid";
        private const string SPRITE_HL_NAME     = "Ship_Sprite_HL";
        private const string SPRITE_SOLID_NAME  = "Ship_Sprite_Solid";
        private const string SPRITE_CORE_NAME   = "Ship_Sprite_Core";

        // VFX GO names
        private const string BOOST_TRAIL_PARTICLES_NAME = "BoostTrailParticles";
        private const string BOOST_TRAIL_NAME           = "BoostTrail";
        private const string DODGE_SPRITE_NAME          = "Dodge_Sprite";
        private const string DODGE_SPRITE_TEXTURE_NAME  = "player_test_fire";

        // Dodge sprite source path (reference assets)
        private const string DODGE_SPRITE_SRC_PATH  = @"D:\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\Texture2D\player_test_fire.png";
        private const string DODGE_SPRITE_DEST_DIR  = "Assets/_Art/Ship/Glitch/Reference";
        private const string DODGE_SPRITE_DEST_PATH = "Assets/_Art/Ship/Glitch/Reference/player_test_fire.png";

        [MenuItem("ProjectArk/Ship/Rebuild Ship Prefab Sprite Layers")]
        public static void RebuildSpriteLayers()
        {
            var log  = new List<string>();
            var todo = new List<string>();

            // ── Load prefab ────────────────────────────────────────────
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefabAsset == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Ship prefab not found at:\n{PREFAB_PATH}\n\nPlease run 'Build Ship' first.",
                    "OK");
                return;
            }

            // ── Open prefab for editing ────────────────────────────────
            using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
            {
                var root = scope.prefabContentsRoot;

                // ── Find or create ShipVisual child ────────────────────
                var visualTransform = root.transform.Find(VISUAL_CHILD_NAME);
                if (visualTransform == null)
                {
                    // Fallback: look for VisualChild (old name from ShipBuilder)
                    visualTransform = root.transform.Find("VisualChild");
                }

                if (visualTransform == null)
                {
                    var visualGo = new GameObject(VISUAL_CHILD_NAME);
                    visualGo.transform.SetParent(root.transform, false);
                    visualTransform = visualGo.transform;
                    log.Add($"✓ Created {VISUAL_CHILD_NAME} child");
                }
                else
                {
                    log.Add($"✓ Found visual child: {visualTransform.name}");
                }

                // ── Load glow material ─────────────────────────────────
                var glowMat = AssetDatabase.LoadAssetAtPath<Material>(GLOW_MATERIAL_PATH);
                if (glowMat == null)
                {
                    todo.Add($"ShipGlowMaterial not found at {GLOW_MATERIAL_PATH} — run 'Create Ship Glow Material' first, then re-run this tool");
                    log.Add("⚠ ShipGlowMaterial missing — liquid layer will use default material");
                }

                // ── Load reference sprites ─────────────────────────────
                Sprite mainSprite = FindSprite("GrabGun_Base_9");
                Sprite hlSprite   = FindSprite("GrabGun_Base_8");
                // Ship_Sprite_Back: no reference sprite assigned — awaiting original art asset
                Sprite backSprite = null;

                if (mainSprite == null) todo.Add("GrabGun_Base_9 sprite not found — run Task 1 (import reference assets) first");
                if (hlSprite   == null) todo.Add("GrabGun_Base_8 sprite not found — run Task 1 (import reference assets) first");

                // ── Create / update 5 sprite layers ───────────────────
                var back   = EnsureSpriteLayer(visualTransform, SPRITE_BACK_NAME,   -3, backSprite,  null,    log);
                var liquid = EnsureSpriteLayer(visualTransform, SPRITE_LIQUID_NAME, -2, mainSprite,  glowMat, log);
                var hl     = EnsureSpriteLayer(visualTransform, SPRITE_HL_NAME,     -1, hlSprite,    null,    log);
                var solid  = EnsureSpriteLayer(visualTransform, SPRITE_SOLID_NAME,   0, mainSprite,  null,    log);
                var core   = EnsureSpriteLayer(visualTransform, SPRITE_CORE_NAME,    1, null,        null,    log);

                // HL default alpha = 0.5
                if (hl != null)
                {
                    var c = hl.color; c.a = 0.5f; hl.color = c;
                    log.Add("✓ Ship_Sprite_HL alpha set to 0.5");
                }

                // ── Add ShipView to root if missing ────────────────────
                var shipView = root.GetComponent<ShipView>();
                if (shipView == null)
                {
                    shipView = root.AddComponent<ShipView>();
                    log.Add("✓ ShipView component added to Ship root");
                }
                else
                {
                    log.Add("✓ ShipView already present on Ship root");
                }

                // ── Wire ShipView fields via SerializedObject ──────────
                var so = new SerializedObject(shipView);
                WireField(so, "_backRenderer",   back,   log, "ShipView._backRenderer");
                WireField(so, "_liquidRenderer", liquid, log, "ShipView._liquidRenderer");
                WireField(so, "_hlRenderer",     hl,     log, "ShipView._hlRenderer");
                WireField(so, "_solidRenderer",  solid,  log, "ShipView._solidRenderer");
                WireField(so, "_coreRenderer",   core,   log, "ShipView._coreRenderer");

                // Wire juiceSettings
                var juiceGuids = AssetDatabase.FindAssets("t:ShipJuiceSettingsSO");
                if (juiceGuids.Length > 0)
                {
                    var juiceSO = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(
                        AssetDatabase.GUIDToAssetPath(juiceGuids[0]));
                    WireField(so, "_juiceSettings", juiceSO, log, "ShipView._juiceSettings");
                }
                else
                {
                    todo.Add("ShipView._juiceSettings: No ShipJuiceSettingsSO found — assign manually");
                }

                so.ApplyModifiedProperties();

                // ── Create VFX child GOs under Ship_Sprite_Back ───────
                var backTransform = visualTransform.Find(SPRITE_BACK_NAME);
                if (backTransform == null)
                    backTransform = back != null ? back.transform : visualTransform;

                // Ensure glow material exists before assigning to VFX
                if (glowMat == null)
                {
                    glowMat = ShipGlowMaterialCreator.CreateOrGet();
                    if (glowMat != null)
                        log.Add("✓ ShipGlowMaterial auto-created for VFX assignment");
                }

                // BoostTrailParticles (ParticleSystem)
                var boostTrailPs = EnsureParticleSystemChild(
                    backTransform, BOOST_TRAIL_PARTICLES_NAME,
                    new Vector3(0f, -0.15f, 0f), log);

                // Assign Additive material to BoostTrailParticles renderer
                if (boostTrailPs != null && glowMat != null)
                {
                    var psRenderer = boostTrailPs.GetComponent<ParticleSystemRenderer>();
                    if (psRenderer != null)
                    {
                        psRenderer.sharedMaterial = glowMat;
                        log.Add("✓ BoostTrailParticles.ParticleSystemRenderer.sharedMaterial = ShipGlowMaterial");
                    }
                    // Ensure localRotation = identity (direction driven by velocityOverLifetime)
                    boostTrailPs.transform.localRotation = UnityEngine.Quaternion.identity;
                    log.Add("✓ BoostTrailParticles.localRotation = identity");
                }

                // BoostTrail (TrailRenderer)
                var boostTrailTr = EnsureTrailRendererChild(
                    backTransform, BOOST_TRAIL_NAME,
                    new Vector3(0f, -0.15f, 0f), log);

                // Assign Additive material to BoostTrail TrailRenderer
                if (boostTrailTr != null && glowMat != null)
                {
                    boostTrailTr.sharedMaterial = glowMat;
                    log.Add("✓ BoostTrail.TrailRenderer.sharedMaterial = ShipGlowMaterial");
                }

                // ── Ensure Dodge_Sprite texture is imported ────────────
                EnsureDodgeSpriteTexture(log, todo);

                // ── Create Dodge_Sprite under ShipVisual ──────────────
                Sprite dodgeSprite = FindSprite(DODGE_SPRITE_TEXTURE_NAME);
                var dodgeSr = EnsureDodgeSpriteChild(
                    visualTransform, DODGE_SPRITE_NAME, dodgeSprite, log);
                if (dodgeSprite == null)
                    todo.Add($"Dodge_Sprite: '{DODGE_SPRITE_TEXTURE_NAME}' not found — import player_test_fire.png then re-run");

                // ── Wire new VFX fields into ShipView ─────────────────
                WireField(so, "_boostTrail",          boostTrailTr,  log, "ShipView._boostTrail");
                WireField(so, "_dodgeSprite",         dodgeSr,       log, "ShipView._dodgeSprite");
                // _backSpriteTransform: use the Transform of Ship_Sprite_Back
                var backTf = back != null ? back.transform : null;
                if (backTf != null)
                {
                    var backTfProp = so.FindProperty("_backSpriteTransform");
                    if (backTfProp != null && backTfProp.objectReferenceValue != (Object)backTf)
                    {
                        backTfProp.objectReferenceValue = backTf;
                        log.Add("✓ ShipView._backSpriteTransform wired");
                    }
                    else if (backTfProp != null)
                    {
                        log.Add("✓ ShipView._backSpriteTransform (already wired)");
                    }
                }
                so.ApplyModifiedProperties();

                // Wire ShipBoostTrailVFX component
                var boostTrailVfx = root.GetComponent<ShipBoostTrailVFX>();
                if (boostTrailVfx == null)
                {
                    boostTrailVfx = root.AddComponent<ShipBoostTrailVFX>();
                    log.Add("✓ ShipBoostTrailVFX component added to Ship root");
                }
                if (boostTrailPs != null)
                {
                    var boostVfxSO = new SerializedObject(boostTrailVfx);
                    WireField(boostVfxSO, "_boostTrailParticles", boostTrailPs, log, "ShipBoostTrailVFX._boostTrailParticles");
                    // Wire juiceSettings to ShipBoostTrailVFX too
                    var juiceGuids2 = AssetDatabase.FindAssets("t:ShipJuiceSettingsSO");
                    if (juiceGuids2.Length > 0)
                    {
                        var juiceSO2 = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(
                            AssetDatabase.GUIDToAssetPath(juiceGuids2[0]));
                        WireField(boostVfxSO, "_juiceSettings", juiceSO2, log, "ShipBoostTrailVFX._juiceSettings");
                    }
                    boostVfxSO.ApplyModifiedProperties();
                }

                // ── Also update DashAfterImageSpawner._shipSpriteRenderer ──
                // Point it to Ship_Sprite_Solid (the main visible layer)
                var spawner = root.GetComponent<DashAfterImageSpawner>();
                if (spawner != null && solid != null)
                {
                    var spawnerSO = new SerializedObject(spawner);
                    WireField(spawnerSO, "_shipSpriteRenderer", solid, log, "DashAfterImageSpawner._shipSpriteRenderer → Ship_Sprite_Solid");
                    spawnerSO.ApplyModifiedProperties();
                }
            }

            // ── Summary ────────────────────────────────────────────────
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("── COMPLETED ──────────────────────────────");
            foreach (var entry in log) sb.AppendLine(entry);

            if (todo.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("── MANUAL STEPS REQUIRED ──────────────────");
                for (int i = 0; i < todo.Count; i++)
                    sb.AppendLine($"{i + 1}. {todo[i]}");
            }

            Debug.Log("[ShipPrefabRebuilder] Done.\n" + sb);
            EditorUtility.DisplayDialog("Ship Prefab Rebuilt", sb.ToString(), "OK");
        }

        // ════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════

        // ════════════════════════════════════════════════════════════════
        // Dodge Sprite Texture Import
        // ════════════════════════════════════════════════════════════════

        private static void EnsureDodgeSpriteTexture(List<string> log, List<string> todo)
        {
            // Already imported?
            if (AssetDatabase.LoadAssetAtPath<Sprite>(DODGE_SPRITE_DEST_PATH) != null)
            {
                log.Add($"✓ player_test_fire.png already imported at {DODGE_SPRITE_DEST_PATH}");
                return;
            }

            // Check source exists
            if (!File.Exists(DODGE_SPRITE_SRC_PATH))
            {
                todo.Add($"player_test_fire.png not found at reference path: {DODGE_SPRITE_SRC_PATH}\n" +
                         $"  → Manually copy it to {DODGE_SPRITE_DEST_PATH} and re-run this tool.");
                log.Add($"⚠ player_test_fire.png source not found — see MANUAL STEPS");
                return;
            }

            // Ensure destination directory exists
            if (!AssetDatabase.IsValidFolder(DODGE_SPRITE_DEST_DIR))
            {
                Directory.CreateDirectory(
                    Path.Combine(Application.dataPath, "../", DODGE_SPRITE_DEST_DIR));
                AssetDatabase.Refresh();
            }

            // Copy file
            File.Copy(DODGE_SPRITE_SRC_PATH, Path.Combine(Application.dataPath, "../", DODGE_SPRITE_DEST_PATH), overwrite: true);
            AssetDatabase.Refresh();

            // Configure importer
            var importer = AssetImporter.GetAtPath(DODGE_SPRITE_DEST_PATH) as TextureImporter;
            if (importer != null)
            {
                importer.textureType          = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit  = 707f;
                importer.filterMode           = FilterMode.Bilinear;
                importer.textureCompression   = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                log.Add($"✓ player_test_fire.png copied and imported (PPU=707, Bilinear, Uncompressed)");
            }
            else
            {
                log.Add($"✓ player_test_fire.png copied (importer not found — check import settings manually)");
            }
        }

        private static Sprite FindSprite(string nameFilter)
        {
            var guids = AssetDatabase.FindAssets($"{nameFilter} t:Sprite");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static SpriteRenderer EnsureSpriteLayer(
            Transform parent, string childName, int sortOrder,
            Sprite sprite, Material material, List<string> log)
        {
            var existing = parent.Find(childName);
            GameObject childGo;

            if (existing != null)
            {
                childGo = existing.gameObject;
                log.Add($"✓ Sprite layer '{childName}' already exists, updating");
            }
            else
            {
                childGo = new GameObject(childName);
                childGo.transform.SetParent(parent, false);
                childGo.transform.localPosition = Vector3.zero;
                log.Add($"✓ Created sprite layer: {childName} (SortOrder={sortOrder})");
            }

            var sr = childGo.GetComponent<SpriteRenderer>();
            if (sr == null) sr = childGo.AddComponent<SpriteRenderer>();

            sr.sortingOrder = sortOrder;
            if (sprite   != null) sr.sprite         = sprite;
            if (material != null) sr.sharedMaterial = material;

            return sr;
        }

        private static ParticleSystem EnsureParticleSystemChild(
            Transform parent, string childName, Vector3 localPos, List<string> log)
        {
            var existing = parent.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                log.Add($"✓ '{childName}' already exists, updating");
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
                log.Add($"✓ Created '{childName}' (ParticleSystem) at {localPos}");
            }

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();

            // Stop by default; ShipBoostTrailVFX will configure and play it
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            return ps;
        }

        private static TrailRenderer EnsureTrailRendererChild(
            Transform parent, string childName, Vector3 localPos, List<string> log)
        {
            var existing = parent.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                log.Add($"✓ '{childName}' already exists, updating");
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

            // Default off; ShipView will configure and enable it
            tr.emitting       = false;
            tr.time           = 0.25f;
            tr.startWidth     = 0.12f;
            tr.endWidth       = 0f;
            tr.minVertexDistance = 0.05f;

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
                log.Add($"✓ '{childName}' already exists, updating");
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

            // Start hidden and inactive
            var c = sr.color; c.a = 0f; sr.color = c;
            go.SetActive(false);

            return sr;
        }

        private static void WireField(SerializedObject so, string propertyPath, Object value,
            List<string> log, string label)
        {
            if (value == null) return;
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                Debug.LogWarning($"[ShipPrefabRebuilder] Property '{propertyPath}' not found");
                return;
            }
            if (prop.objectReferenceValue == value) { log.Add($"✓ {label} (already wired)"); return; }
            prop.objectReferenceValue = value;
            log.Add($"✓ {label} wired");
        }
    }
}
#endif
