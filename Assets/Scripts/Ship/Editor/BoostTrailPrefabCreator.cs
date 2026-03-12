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
            main.playOnAwake     = false;
            main.maxParticles    = 256;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.26f, 0.4f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(9.5f, 12.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor      = new Color(5.44f, 0.42f, 6.06f, 1f); // HDR purple

            var emission = ps.emission;
            emission.enabled          = true;
            emission.rateOverTime     = 0f;
            emission.rateOverDistance = 15f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(0.025f, 0.08f, 0.01f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient flameGradient = new Gradient();
            flameGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(6.4f, 2.1f, 6.1f), 0f),
                    new GradientColorKey(new Color(5.44f, 0.42f, 6.06f), 0.32f),
                    new GradientColorKey(new Color(0.25f, 0.91f, 1.0f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.72f, 0.22f),
                    new GradientAlphaKey(0.42f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(flameGradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve flameSizeCurve = new AnimationCurve();
            flameSizeCurve.AddKey(0f, 0.3f);
            flameSizeCurve.AddKey(0.18f, 0.9f);
            flameSizeCurve.AddKey(0.5f, 1f);
            flameSizeCurve.AddKey(1f, 0.04f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, flameSizeCurve);

            var noise = ps.noise;
            noise.enabled     = true;
            noise.separateAxes = false;
            noise.strength    = 0.08f;
            noise.frequency   = 0.9f;
            noise.scrollSpeed = 0.8f;
            noise.damping     = true;

            ps.transform.localPosition = isRight
                ? new Vector3(0.18f, -0.09f, 0f)
                : new Vector3(-0.18f, -0.09f, 0f);
            ps.transform.localEulerAngles = isRight
                ? new Vector3(0f, 0f, 12f)
                : new Vector3(0f, 0f, -12f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode    = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.42f;
            renderer.lengthScale   = 2.6f;
        }

        /// <summary>
        /// Configures FlameCore burst particle system.
        /// Burst mode, StartLifetime=0.07~0.08s (very short)
        /// </summary>
        private static void ConfigureFlameCore(ParticleSystem ps)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_flame_trail.mat");

            var main = ps.main;
            main.loop            = true;
            main.playOnAwake     = false;
            main.maxParticles    = 96;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.26f, 0.48f);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor      = new Color(5.44f, 0.42f, 6.06f, 1f); // HDR purple

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 42f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 0.035f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient coreGradient = new Gradient();
            coreGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(8f, 4.2f, 1.4f), 0f),
                    new GradientColorKey(new Color(5.44f, 0.42f, 6.06f), 0.35f),
                    new GradientColorKey(new Color(0.25f, 0.91f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.7f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(coreGradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve coreSizeCurve = new AnimationCurve();
            coreSizeCurve.AddKey(0f, 0.4f);
            coreSizeCurve.AddKey(0.18f, 1f);
            coreSizeCurve.AddKey(1f, 0.05f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, coreSizeCurve);

            ps.transform.localPosition = new Vector3(0f, -0.06f, 0f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
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
            main.playOnAwake     = false;
            main.maxParticles    = 160;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
            main.startSpeed      = 0f;
            main.startSize       = new ParticleSystem.MinMaxCurve(0.38f, 0.56f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor      = new Color(2.0f, 0f, 1.08f, 1f); // HDR magenta

            var emission = ps.emission;
            emission.enabled          = true;
            emission.rateOverTime     = 0f;
            emission.rateOverDistance = 3.5f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(0.12f, 0.06f, 0.01f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient emberGradient = new Gradient();
            emberGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(2.0f, 0f, 1.08f), 0f),
                    new GradientColorKey(new Color(2.3f, 0.5f, 0.65f), 0.55f),
                    new GradientColorKey(new Color(1.6f, 0.9f, 0.15f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.6f, 0f),
                    new GradientAlphaKey(0.35f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(emberGradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve emberSizeCurve = new AnimationCurve();
            emberSizeCurve.AddKey(0f, 0.85f);
            emberSizeCurve.AddKey(1f, 0.15f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, emberSizeCurve);

            var noise = ps.noise;
            noise.enabled      = true;
            noise.separateAxes = false;
            noise.strength     = 0.06f;
            noise.frequency    = 0.55f;
            noise.damping      = true;

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
            main.playOnAwake   = false;
            main.startLifetime = 0.14f;
            main.startSpeed    = 0f;    // No self-movement, follows ship
            main.startSize     = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor    = new Color(2.0f, 1.29f, 0f, 1f); // HDR orange-yellow

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 0.05f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient glowGradient = new Gradient();
            glowGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(4.5f, 2.8f, 0.8f), 0f),
                    new GradientColorKey(new Color(2.0f, 1.29f, 0f), 0.45f),
                    new GradientColorKey(new Color(1.4f, 0.35f, 0.25f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0.45f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(glowGradient);

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
            main.playOnAwake   = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(34f, 52f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor    = new Color(3.73f, 3.73f, 3.73f, 1f); // HDR super-bright white

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 0.04f;

            // Radial velocity for spark explosion effect
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.Local;
            vel.radial  = new ParticleSystem.MinMaxCurve(-1f, 2.5f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient sparkGradient = new Gradient();
            sparkGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(4.6f, 4.6f, 4.6f), 0f),
                    new GradientColorKey(new Color(3.2f, 1.8f, 0.6f), 0.35f),
                    new GradientColorKey(new Color(1.2f, 0.35f, 0.15f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.5f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(sparkGradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sparkSizeCurve = new AnimationCurve();
            sparkSizeCurve.AddKey(0f, 0.9f);
            sparkSizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sparkSizeCurve);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode    = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale   = 1.4f;
        }

        /// <summary>
        /// Configures MainTrail TrailRenderer.
        /// Time=3.5s, widthMultiplier=3.0, width curve: head 0.3 → 40% 1.0
        /// Color HDR (2.0, 1.1, 0.24) orange-yellow
        /// </summary>
        private static void ConfigureMainTrail(TrailRenderer trail)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_trail_main.mat");
            if (mat == null)
                mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_trail_main_effect.mat");

            trail.time              = 2.2f;
            trail.widthMultiplier   = 2.75f;
            trail.minVertexDistance = 0.03f;
            trail.textureMode       = LineTextureMode.Tile;
            trail.numCapVertices    = 8;
            trail.alignment         = LineAlignment.View;
            trail.textureScale      = new Vector2(1.9f, 1f);
            trail.emitting          = false;

            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(0.0f, 0.12f);
            widthCurve.AddKey(0.16f, 0.55f);
            widthCurve.AddKey(0.62f, 1.0f);
            widthCurve.AddKey(1.0f, 0.05f);
            trail.widthCurve = widthCurve;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f),
                    new GradientColorKey(new Color(1f, 0.75f, 0.35f), 0.45f),
                    new GradientColorKey(new Color(0.75f, 0.22f, 0.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.2f),
                    new GradientAlphaKey(0.4f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
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
