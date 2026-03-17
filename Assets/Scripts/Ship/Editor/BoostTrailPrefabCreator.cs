using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Authority tool that creates or rebuilds the standalone BoostTrailRoot prefab.
    ///
    /// Authority owned by this tool:
    ///   • BoostTrailRoot prefab hierarchy
    ///   • MainTrail / flame / ember / energy layer / halo child creation
    ///   • BoostTrailView serialized references inside the standalone prefab
    ///
    /// Ship.prefab integration is owned by ShipPrefabRebuilder.
    /// Scene-only bloom binding is owned by ShipBoostTrailSceneBinder.
    /// </summary>
    public static class BoostTrailPrefabCreator
    {
        private const string PREFAB_PATH = "Assets/_Prefabs/VFX/BoostTrailRoot.prefab";
        private const string MAT_DIR     = "Assets/_Art/VFX/BoostTrail/Materials";
        private const string ShipGlowMaterialPath = "Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat";
        private const string BoostLiquidSpritePath = "Assets/_Art/Ship/Glitch/Boost_16.png";
        private const string NormalLiquidSpritePath = "Assets/_Art/Ship/Glitch/Movement_3.png";
        private const string ActivationHaloPrimarySpritePath = "Assets/_Art/VFX/BoostTrail/Textures/vfx_ring_glow_uneven.png";
        private const string ActivationHaloFallbackSpritePath = "Assets/_Art/VFX/BoostTrail/Textures/vfx_magnetic_rings.png";
        private const string BoostLiquidSpriteName = "Boost_16";
        private const string NormalLiquidSpriteName = "Movement_3";
        private const string ActivationHaloPrimarySpriteName = "vfx_ring_glow_uneven";
        private const string ActivationHaloFallbackSpriteName = "vfx_magnetic_rings";

        [MenuItem("ProjectArk/Ship/VFX/Authority/Rebuild BoostTrailRoot Prefab")]
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
            var boostTrailDebugManager = root.AddComponent<BoostTrailDebugManager>();

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

            // ── BoostActivationHalo (local ship-centered flash) ──────
            var activationHaloGO = new GameObject("BoostActivationHalo");
            activationHaloGO.transform.SetParent(root.transform, false);
            activationHaloGO.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            activationHaloGO.transform.localScale = new Vector3(0.78f, 0.56f, 1f);
            var activationHaloSR = activationHaloGO.AddComponent<SpriteRenderer>();
            activationHaloSR.sprite = FindActivationHaloSprite();
            activationHaloSR.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShipGlowMaterialPath);
            activationHaloSR.sortingOrder = 4;
            activationHaloSR.color = new Color(5.3f, 3.1f, 1.28f, 0f);
            activationHaloSR.enabled = false;

            // ── Wire BoostTrailView references + default tuning ───────
            var so = new SerializedObject(boostTrailView);
            so.FindProperty("_mainTrail").objectReferenceValue       = trailRenderer;
            so.FindProperty("_flameTrailR").objectReferenceValue     = flameTrailR;
            so.FindProperty("_flameTrailB").objectReferenceValue     = flameTrailB;
            so.FindProperty("_flameCore").objectReferenceValue       = flameCore;
            so.FindProperty("_emberTrail").objectReferenceValue      = emberTrail;
            so.FindProperty("_emberSparks").objectReferenceValue     = emberSparks;
            so.FindProperty("_energyLayer2").objectReferenceValue    = layer2SR;
            so.FindProperty("_energyLayer3").objectReferenceValue    = layer3SR;
            so.FindProperty("_activationHalo").objectReferenceValue  = activationHaloSR;
            so.FindProperty("_intensityRampUpDuration").floatValue   = 0.22f;
            so.FindProperty("_intensityRampDownDuration").floatValue = 0.42f;
            so.FindProperty("_sustainFlameStartDelay").floatValue    = 0.045f;
            so.FindProperty("_emberTrailStartDelay").floatValue      = 0.07f;
            so.FindProperty("_emberSparksBurstDelay").floatValue     = 0.018f;
            so.FindProperty("_flameTrailBlendInThreshold").floatValue = 0.18f;
            so.FindProperty("_flameTrailMaxIntensity").floatValue     = 0.78f;
            so.FindProperty("_emberTrailBlendInThreshold").floatValue = 0.42f;
            so.FindProperty("_emberTrailMaxIntensity").floatValue     = 0.32f;
            so.FindProperty("_energyLayer2BlendInThreshold").floatValue = 0.16f;
            so.FindProperty("_energyLayer2MaxIntensity").floatValue     = 0.62f;
            so.FindProperty("_energyLayer3BlendInThreshold").floatValue = 0.38f;
            so.FindProperty("_energyLayer3MaxIntensity").floatValue     = 0.34f;
            so.FindProperty("_activationHaloPeakAlpha").floatValue   = 1.4f;
            so.FindProperty("_activationHaloDuration").floatValue    = 0.12f;
            so.FindProperty("_activationHaloStartScale").floatValue  = 0.56f;
            so.FindProperty("_activationHaloPeakScale").floatValue   = 0.98f;
            so.FindProperty("_activationHaloEndScale").floatValue    = 0.82f;
            so.FindProperty("_bloomBurstIntensity").floatValue       = 2.15f;
            so.FindProperty("_bloomPeakWeight").floatValue           = 0.72f;
            so.FindProperty("_bloomAttackDuration").floatValue       = 0.05f;
            so.FindProperty("_bloomReleaseDuration").floatValue      = 0.16f;
            // NOTE: _boostBloomVolume is a scene object and cannot be pre-wired in a Prefab.
            so.ApplyModifiedProperties();

            var debugSO = new SerializedObject(boostTrailDebugManager);
            debugSO.FindProperty("_boostTrailView").objectReferenceValue = boostTrailView;
            debugSO.FindProperty("_enableInspectorDebug").boolValue = false;
            debugSO.FindProperty("_debugMode").enumValueIndex = (int)BoostTrailDebugManager.DebugMode.ObserveRuntime;
            debugSO.FindProperty("_previewIntensity").floatValue = 1f;
            debugSO.FindProperty("_soloLayer").enumValueIndex = (int)BoostTrailDebugManager.SoloLayer.None;
            debugSO.FindProperty("_showMainTrail").boolValue = true;
            debugSO.FindProperty("_showFlameTrail").boolValue = true;
            debugSO.FindProperty("_showFlameCore").boolValue = true;
            debugSO.FindProperty("_showEmberTrail").boolValue = true;
            debugSO.FindProperty("_showEmberSparks").boolValue = true;
            debugSO.FindProperty("_showEnergyLayer2").boolValue = true;
            debugSO.FindProperty("_showEnergyLayer3").boolValue = true;
            debugSO.FindProperty("_showActivationHalo").boolValue = true;
            debugSO.FindProperty("_showBloom").boolValue = true;
            debugSO.ApplyModifiedProperties();

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
                        "1. Run 'ProjectArk > Ship > VFX > Authority > Link Active BoostTrail Material Textures'.\n" +
                        "2. Run 'ProjectArk > Ship > Authority > Rebuild Ship Prefab' to integrate BoostTrailRoot into Ship.prefab.\n" +
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
        /// Local-space sustained thruster flame that supports MainTrail instead of drawing a second world tail.
        /// </summary>
        private static void ConfigureFlameTrail(ParticleSystem ps, bool isRight)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_flame_trail.mat");

            var main = ps.main;
            main.loop            = true;
            main.playOnAwake     = false;
            main.maxParticles    = 196;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(4.8f, 7.2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor      = new Color(6.8f, 2.1f, 5.4f, 1f);

            var emission = ps.emission;
            emission.enabled          = true;
            emission.rateOverTime     = 54f;
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(0.018f, 0.05f, 0.01f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient flameGradient = new Gradient();
            flameGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(7.8f, 4.6f, 2.2f), 0f),
                    new GradientColorKey(new Color(6.6f, 1.5f, 5.2f), 0.26f),
                    new GradientColorKey(new Color(0.32f, 0.95f, 1.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.7f, 0.18f),
                    new GradientAlphaKey(0.32f, 0.52f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(flameGradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve flameSizeCurve = new AnimationCurve();
            flameSizeCurve.AddKey(0f, 0.22f);
            flameSizeCurve.AddKey(0.14f, 0.9f);
            flameSizeCurve.AddKey(0.44f, 1f);
            flameSizeCurve.AddKey(1f, 0.06f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, flameSizeCurve);

            var noise = ps.noise;
            noise.enabled      = true;
            noise.separateAxes = false;
            noise.strength     = 0.045f;
            noise.frequency    = 0.75f;
            noise.scrollSpeed  = 0.45f;
            noise.damping      = true;

            ps.transform.localPosition = isRight
                ? new Vector3(0.16f, -0.095f, 0f)
                : new Vector3(-0.16f, -0.095f, 0f);
            ps.transform.localEulerAngles = isRight
                ? new Vector3(0f, 0f, 8f)
                : new Vector3(0f, 0f, -8f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode    = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.2f;
            renderer.lengthScale   = 2.1f;
        }

        /// <summary>
        /// Configures FlameCore burst particle system.
        /// Start-only ignition burst: dense, short-lived, and visually anchored at the thruster root.
        /// </summary>
        private static void ConfigureFlameCore(ParticleSystem ps)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_flame_trail.mat");

            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.maxParticles    = 48;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor      = new Color(8.2f, 3.8f, 1.45f, 1f); // HDR hot core

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 16)
            });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(0.08f, 0.028f, 0.01f);

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled       = true;
            velocityOverLifetime.space         = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x             = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);
            velocityOverLifetime.y             = new ParticleSystem.MinMaxCurve(-4.6f, -2.4f);
            velocityOverLifetime.z             = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocityOverLifetime.orbitalX      = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocityOverLifetime.orbitalY      = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocityOverLifetime.orbitalZ      = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocityOverLifetime.radial        = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocityOverLifetime.speedModifier = new ParticleSystem.MinMaxCurve(1f, 1f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient coreGradient = new Gradient();
            coreGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(9f, 4.4f, 1.5f), 0f),
                    new GradientColorKey(new Color(5.44f, 0.42f, 6.06f), 0.28f),
                    new GradientColorKey(new Color(0.25f, 0.91f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.98f, 0f),
                    new GradientAlphaKey(0.82f, 0.18f),
                    new GradientAlphaKey(0.24f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(coreGradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve coreSizeCurve = new AnimationCurve();
            coreSizeCurve.AddKey(0f, 0.18f);
            coreSizeCurve.AddKey(0.16f, 1f);
            coreSizeCurve.AddKey(0.52f, 0.68f);
            coreSizeCurve.AddKey(1f, 0.04f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, coreSizeCurve);

            ps.transform.localPosition = new Vector3(0f, -0.07f, 0f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode    = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.22f;
            renderer.lengthScale   = 1.1f;
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
            main.maxParticles    = 96;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor      = new Color(4.1f, 1.05f, 0.28f, 1f);

            var emission = ps.emission;
            emission.enabled          = true;
            emission.rateOverTime     = 0f;
            emission.rateOverDistance = 2.2f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(0.08f, 0.035f, 0.01f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient emberGradient = new Gradient();
            emberGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(4.8f, 1.7f, 0.48f), 0f),
                    new GradientColorKey(new Color(2.5f, 0.55f, 0.26f), 0.48f),
                    new GradientColorKey(new Color(0.95f, 0.08f, 0.08f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.45f, 0f),
                    new GradientAlphaKey(0.22f, 0.42f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(emberGradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve emberSizeCurve = new AnimationCurve();
            emberSizeCurve.AddKey(0f, 0.7f);
            emberSizeCurve.AddKey(0.45f, 0.32f);
            emberSizeCurve.AddKey(1f, 0.08f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, emberSizeCurve);

            var noise = ps.noise;
            noise.enabled      = true;
            noise.separateAxes = false;
            noise.strength     = 0.03f;
            noise.frequency    = 0.42f;
            noise.damping      = true;

            ps.transform.localPosition = new Vector3(0f, -0.12f, 0f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode    = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale   = 0.9f;
        }

        /// <summary>
        /// Configures EmberSparks one-shot burst.
        /// Acts as a short directional edge accent after FlameCore, not a radial fireworks explosion.
        /// </summary>
        private static void ConfigureEmberSparks(ParticleSystem ps)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_ember_sparks.mat");

            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.maxParticles    = 24;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(8f, 18f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor      = new Color(5.1f, 4.3f, 2.5f, 1f);

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(0.1f, 0.025f, 0.01f);

            var vel = ps.velocityOverLifetime;
            vel.enabled        = true;
            vel.space          = ParticleSystemSimulationSpace.Local;
            vel.x              = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            vel.y              = new ParticleSystem.MinMaxCurve(-15f, -8f);
            vel.z              = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalX       = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalY       = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalZ       = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.radial         = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.speedModifier  = new ParticleSystem.MinMaxCurve(1f, 1f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient sparkGradient = new Gradient();
            sparkGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(6f, 5f, 3.2f), 0f),
                    new GradientColorKey(new Color(3.6f, 1.75f, 0.55f), 0.32f),
                    new GradientColorKey(new Color(1.4f, 0.25f, 0.08f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.42f, 0.28f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(sparkGradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sparkSizeCurve = new AnimationCurve();
            sparkSizeCurve.AddKey(0f, 1f);
            sparkSizeCurve.AddKey(0.45f, 0.42f);
            sparkSizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sparkSizeCurve);

            ps.transform.localPosition = new Vector3(0f, -0.075f, 0f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.renderMode    = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.24f;
            renderer.lengthScale   = 2f;
        }

        /// <summary>
        /// Configures MainTrail TrailRenderer.
        /// Time=3.5s, widthMultiplier=3.0, width curve: head 0.3 → 40% 1.0
        /// Color HDR (2.0, 1.1, 0.24) orange-yellow
        /// </summary>
        private static void ConfigureMainTrail(TrailRenderer trail)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/mat_trail_main.mat");

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
            var boostSprite = LoadSpriteAtPath(BoostLiquidSpritePath);
            if (boostSprite != null)
                return boostSprite;

            var normalSprite = LoadSpriteAtPath(NormalLiquidSpritePath);
            if (normalSprite == null)
            {
                Debug.LogWarning($"[BoostTrailPrefabCreator] Overlay sprite missing at both '{BoostLiquidSpritePath}' and '{NormalLiquidSpritePath}'.");
            }

            return normalSprite;
        }

        private static Sprite FindActivationHaloSprite()
        {
            var haloSprite = LoadSpriteAtPath(ActivationHaloPrimarySpritePath);
            if (haloSprite != null)
                return haloSprite;

            haloSprite = LoadSpriteAtPath(ActivationHaloFallbackSpritePath);
            if (haloSprite != null)
                return haloSprite;

            Debug.LogWarning($"[BoostTrailPrefabCreator] Activation halo sprite missing at '{ActivationHaloPrimarySpritePath}' and '{ActivationHaloFallbackSpritePath}'. Falling back to overlay sprite.");
            return FindOverlaySprite();
        }

        private static Sprite LoadSpriteAtPath(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
