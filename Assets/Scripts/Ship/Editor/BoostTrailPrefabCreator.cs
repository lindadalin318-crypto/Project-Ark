using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// One-click editor utility to create the BoostTrailRoot Prefab.
    /// Menu: ProjectArk > VFX > Create BoostTrailRoot Prefab
    ///
    /// Creates the full hierarchy:
    ///   BoostTrailRoot
    ///   ├── [TrailRenderer] MainTrail
    ///   ├── [PS] FlameTrail_R
    ///   ├── [PS] FlameTrail_B
    ///   ├── [PS] FlameCore
    ///   ├── [PS] EmberTrail
    ///   ├── [PS] EmberSparks
    ///   ├── [PS] EmberGlow          ← Phase 2 new
    ///   ├── [SpriteRenderer] BoostEnergyLayer2
    ///   ├── [SpriteRenderer] BoostEnergyLayer3
    ///   ├── [MeshRenderer] BoostEnergyField
    ///   └── [SpriteRenderer] BoostActivationHalo
    /// </summary>
    public static class BoostTrailPrefabCreator
    {
        private const string PREFAB_PATH = "Assets/_Prefabs/VFX/BoostTrailRoot.prefab";
        private const string MAT_DIR     = "Assets/_Art/VFX/BoostTrail/Materials";
        private const string ShipGlowMaterialPath = "Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat";
        private const string BoostSpriteName = "Boost_16";
        private const string NormalSpriteName = "Movement_3";
        private const string ActivationHaloSpriteName = "vfx_ring_glow_uneven";
        private const string ActivationHaloFallbackSpriteName = "vfx_magnetic_rings";

        [MenuItem("ProjectArk/VFX/Create BoostTrailRoot Prefab")]
        public static void CreateBoostTrailRootPrefab()
        {
            CreateOrRebuildBoostTrailRootPrefab(showDialog: false);
        }

        public static GameObject CreateOrRebuildBoostTrailRootPrefab(bool showDialog)
        {
            // Ensure output directory exists
            if (!AssetDatabase.IsValidFolder("Assets/_Prefabs/VFX"))
            {
                AssetDatabase.CreateFolder("Assets/_Prefabs", "VFX");
            }

            // ── Root ──────────────────────────────────────────────────
            var root = new GameObject("BoostTrailRoot");
            var boostTrailView = root.AddComponent<BoostTrailView>();

            // ── MainTrail ─────────────────────────────────────────────
            var mainTrailGO = new GameObject("MainTrail");
            mainTrailGO.transform.SetParent(root.transform, false);
            var trailRenderer = mainTrailGO.AddComponent<TrailRenderer>();
            ConfigureMainTrail(trailRenderer);

            // ── FlameTrail_R ──────────────────────────────────────────
            var flameTrailR = CreateParticleSystem(root.transform, "FlameTrail_R");
            ConfigureFlameTrail(flameTrailR, isRight: true);

            // ── FlameTrail_B ──────────────────────────────────────────
            var flameTrailB = CreateParticleSystem(root.transform, "FlameTrail_B");
            ConfigureFlameTrail(flameTrailB, isRight: false);

            // ── FlameCore ─────────────────────────────────────────────
            var flameCore = CreateParticleSystem(root.transform, "FlameCore");
            ConfigureFlameCore(flameCore);

            // ── EmberTrail ────────────────────────────────────────────
            var emberTrail = CreateParticleSystem(root.transform, "EmberTrail");
            ConfigureEmberTrail(emberTrail);

            // ── EmberSparks ───────────────────────────────────────────────
            var emberSparks = CreateParticleSystem(root.transform, "EmberSparks");
            ConfigureEmberSparks(emberSparks);

            // ── EmberGlow (Phase 2) ────────────────────────────────────────
            var emberGlow = CreateParticleSystem(root.transform, "EmberGlow");
            ConfigureEmberGlow(emberGlow);

            // ── BoostEnergyLayer2 (SpriteRenderer) ────────────────────────────────────────────
            var layer2GO = new GameObject("BoostEnergyLayer2");
            layer2GO.transform.SetParent(root.transform, false);
            var layer2SR = layer2GO.AddComponent<SpriteRenderer>();
            var mat2 = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_boost_energy_layer2.mat");
            if (mat2 != null) layer2SR.sharedMaterial = mat2;
            layer2SR.sprite = FindOverlaySprite();
            layer2SR.sortingOrder = 2;
            layer2SR.color = Color.white;

            // ── BoostEnergyLayer3 (SpriteRenderer) ────────────────────
            var layer3GO = new GameObject("BoostEnergyLayer3");
            layer3GO.transform.SetParent(root.transform, false);
            var layer3SR = layer3GO.AddComponent<SpriteRenderer>();
            var mat3 = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_boost_energy_layer3.mat");
            if (mat3 != null) layer3SR.sharedMaterial = mat3;
            layer3SR.sprite = FindOverlaySprite();
            layer3SR.sortingOrder = 3;
            layer3SR.color = Color.white;

            // ── BoostEnergyField (MeshRenderer + Quad) ────────────────
            var fieldGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fieldGO.name = "BoostEnergyField";
            fieldGO.transform.SetParent(root.transform, false);
            fieldGO.transform.localScale = new Vector3(5f, 5f, 1f); // World-space energy field size
            // Remove collider (not needed for VFX)
            Object.DestroyImmediate(fieldGO.GetComponent<MeshCollider>());
            var fieldMR = fieldGO.GetComponent<MeshRenderer>();
            var matField = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_boost_energy_field.mat");
            if (matField != null) fieldMR.sharedMaterial = matField;
            fieldMR.enabled = false; // Starts disabled, enabled by BoostTrailView

            // ── BoostActivationHalo (local ship-centered flash) ──────
            var activationHaloGO = new GameObject("BoostActivationHalo");
            activationHaloGO.transform.SetParent(root.transform, false);
            activationHaloGO.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
            var activationHaloSR = activationHaloGO.AddComponent<SpriteRenderer>();
            activationHaloSR.sprite = FindActivationHaloSprite();
            activationHaloSR.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShipGlowMaterialPath);
            activationHaloSR.sortingOrder = 4;
            activationHaloSR.color = new Color(3.2f, 2.2f, 1.4f, 0f);
            activationHaloSR.enabled = false;

            // ── Wire BoostTrailView references ────────────────────────
            var so = new SerializedObject(boostTrailView);
            so.FindProperty("_mainTrail").objectReferenceValue       = trailRenderer;
            so.FindProperty("_flameTrailR").objectReferenceValue     = flameTrailR;
            so.FindProperty("_flameTrailB").objectReferenceValue     = flameTrailB;
            so.FindProperty("_flameCore").objectReferenceValue       = flameCore;
            so.FindProperty("_emberTrail").objectReferenceValue      = emberTrail;
            so.FindProperty("_emberSparks").objectReferenceValue     = emberSparks;
            so.FindProperty("_emberGlow").objectReferenceValue       = emberGlow;
            so.FindProperty("_energyLayer2").objectReferenceValue    = layer2SR;
            so.FindProperty("_energyLayer3").objectReferenceValue    = layer3SR;
            so.FindProperty("_energyField").objectReferenceValue     = fieldMR;
            so.FindProperty("_activationHalo").objectReferenceValue  = activationHaloSR;
            // NOTE: _boostBloomVolume is a scene object and cannot be pre-wired in a Prefab.
            so.ApplyModifiedProperties();

            // Remind developer that the local bloom volume still needs scene-level wiring
            Debug.LogWarning(
                "[BoostTrailPrefabCreator] ⚠️ MANUAL WIRING REQUIRED:\n" +
                "  BoostTrailView._boostBloomVolume → Local Volume with BoostBloomVolumeProfile\n" +
                "This is a scene object and cannot be pre-wired in a Prefab.\n" +
                "Without it: TriggerBloomBurst() will silently no-op!");

            // ── Save as Prefab ─────────────────────────────────────────
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefab != null)
            {
                if (showDialog)
                {
                    Debug.Log(
                        "[BoostTrailPrefabCreator] Prefab saved to:\n" + PREFAB_PATH + "\n\n" +
                        "Required follow-up:\n" +
                        "1. Run 'ProjectArk > VFX > Link Material Textures'.\n" +
                        "2. Add BoostTrailRoot under the Ship visual root.\n" +
                        "3. Verify ShipView sprite references.\n" +
                        "4. Verify scene wiring for _boostBloomVolume.");
                }

                Selection.activeObject = prefab;
                Debug.Log($"[BoostTrailPrefabCreator] BoostTrailRoot prefab created at {PREFAB_PATH}");
                return prefab;
            }

            Debug.LogError("[BoostTrailPrefabCreator] Failed to save prefab!");
            return null;
        }

        // ══════════════════════════════════════════════════════════════
        // Particle System Helpers
        // ══════════════════════════════════════════════════════════════

        private static ParticleSystem CreateParticleSystem(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            // Stop immediately — all PS start in stopped state
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        /// <summary>
        /// Configures FlameTrail_R or FlameTrail_B.
        /// rateOverDistance=15, StartLifetime=0.4s, StartSize=0.4, StartSpeed=10
        /// Color HDR (5.44, 0.42, 6.06) purple
        /// </summary>
        private static void ConfigureFlameTrail(ParticleSystem ps, bool isRight)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_flame_trail.mat");

            var main = ps.main;
            main.loop            = true;
            main.startLifetime   = 0.4f;
            main.startSpeed      = 10f;
            main.startSize       = 0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor      = new Color(5.44f, 0.42f, 6.06f, 1f); // HDR purple

            var emission = ps.emission;
            emission.enabled          = true;
            emission.rateOverTime     = 0f;
            emission.rateOverDistance = 15f; // 15 particles per meter

            // Offset position slightly to left/right of ship center
            ps.transform.localPosition = isRight
                ? new Vector3(0.15f, 0f, 0f)
                : new Vector3(-0.15f, 0f, 0f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        /// <summary>
        /// Configures FlameCore burst particle system.
        /// Burst mode, StartLifetime=0.07~0.08s (very short)
        /// </summary>
        private static void ConfigureFlameCore(ParticleSystem ps)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_flame_trail.mat");

            var main = ps.main;
            main.loop          = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.07f, 0.08f);
            main.startSpeed    = 5f;
            main.startSize     = 0.6f;
            main.startColor    = new Color(5.44f, 0.42f, 6.06f, 1f); // HDR purple

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 30f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
        }

        /// <summary>
        /// Configures EmberTrail particle system.
        /// rateOverDistance=2, StartLifetime=0.35s, StartSize=0.7, StartSpeed=0
        /// Color HDR (2.0, 0, 1.08) magenta
        /// </summary>
        private static void ConfigureEmberTrail(ParticleSystem ps)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_ember_trail.mat");

            var main = ps.main;
            main.loop            = true;
            main.startLifetime   = 0.35f;
            main.startSpeed      = 0f;
            main.startSize       = 0.7f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor      = new Color(2.0f, 0f, 1.08f, 1f); // HDR magenta

            var emission = ps.emission;
            emission.enabled          = true;
            emission.rateOverTime     = 0f;
            emission.rateOverDistance = 2f; // 2 particles per meter

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        /// <summary>
        /// Configures EmberGlow burst particle system (ps_ember_glow).
        /// StartLifetime=0.12s, StartSize=1.0, StartSpeed=0, Burst mode
        /// Color HDR (2.0, 1.29, 0) orange-yellow
        /// </summary>
        private static void ConfigureEmberGlow(ParticleSystem ps)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_ember_trail.mat");

            var main = ps.main;
            main.loop          = false; // Burst mode — triggered on each Boost start
            main.startLifetime = 0.12f; // Very short, quick glow flash
            main.startSpeed    = 0f;    // No self-movement, follows ship
            main.startSize     = 1.0f;  // Larger than EmberTrail for glow halo effect
            main.startColor    = new Color(2.0f, 1.29f, 0f, 1f); // HDR orange-yellow

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 0f;
            // Burst: 15 particles at time 0 for glow halo
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        /// <summary>
        /// Configures EmberSparks one-shot burst.
        /// Loop=false, StartSpeed=50, StartSize=0.3, StartLifetime=0.2s
        /// Color HDR (3.73, 3.73, 3.73) super-bright white
        /// </summary>
        private static void ConfigureEmberSparks(ParticleSystem ps)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_ember_sparks.mat");

            var main = ps.main;
            main.loop          = false; // One-shot burst
            main.startLifetime = 0.2f;
            main.startSpeed    = new ParticleSystem.MinMaxCurve(30f, 50f);
            main.startSize     = 0.3f;
            main.startColor    = new Color(3.73f, 3.73f, 3.73f, 1f); // HDR super-bright white

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 0f;
            // Burst: 20 particles at time 0
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            // Radial velocity for spark explosion effect
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.Local;
            vel.radial  = new ParticleSystem.MinMaxCurve(-2f, 2f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        /// <summary>
        /// Configures MainTrail TrailRenderer.
        /// Time=3.5s, widthMultiplier=3.0, width curve: head 0.3 → 40% 1.0
        /// Color HDR (2.0, 1.1, 0.24) orange-yellow
        /// </summary>
        private static void ConfigureMainTrail(TrailRenderer trail)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_trail_main_effect.mat");
            if (mat == null)
                mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_trail_main.mat");

            trail.time             = 3.5f;
            trail.widthMultiplier  = 3.0f;
            trail.minVertexDistance = 0.1f;
            trail.emitting         = false;

            // Width curve: head 0.3 → 40% 1.0 → tail 0.0
            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(0.0f, 0.3f);   // Head
            widthCurve.AddKey(0.4f, 1.0f);   // Peak at 40%
            widthCurve.AddKey(1.0f, 0.0f);   // Tail
            trail.widthCurve = widthCurve;

            // Color gradient: HDR orange-yellow → transparent
            Color startColor = new Color(2.0f, 1.1f, 0.24f, 1f);
            Color endColor   = new Color(2.0f, 1.1f, 0.24f, 0f);
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;

            if (mat != null) trail.sharedMaterial = mat;
        }

        private static Sprite FindOverlaySprite()
        {
            var boostSprite = FindSprite(BoostSpriteName);
            if (boostSprite != null)
                return boostSprite;

            return FindSprite(NormalSpriteName);
        }

        private static Sprite FindActivationHaloSprite()
        {
            var haloSprite = FindSprite(ActivationHaloSpriteName);
            if (haloSprite != null)
                return haloSprite;

            haloSprite = FindSprite(ActivationHaloFallbackSpriteName);
            if (haloSprite != null)
                return haloSprite;

            return FindOverlaySprite();
        }

        private static Sprite FindSprite(string nameFilter)
        {
            var guids = AssetDatabase.FindAssets($"{nameFilter} t:Sprite");
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
