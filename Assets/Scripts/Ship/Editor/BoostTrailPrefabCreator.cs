using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Authority tool that creates or rebuilds the standalone BoostTrailRoot prefab.
    ///
    /// Authority owned by this tool:
    ///   • BoostTrailRoot prefab hierarchy
    ///   • AresBoostTrail child creation
    ///   • BoostTrailView serialized references inside the standalone prefab
    ///
    /// Ship.prefab integration is owned by ShipPrefabRebuilder.
    /// Scene-only bloom binding is owned by ShipBoostTrailSceneBinder.
    /// </summary>
    public static class BoostTrailPrefabCreator
    {
        private const string PREFAB_PATH = "Assets/_Prefabs/VFX/BoostTrailRoot.prefab";
        private const string SHIP_JUICE_SETTINGS_PATH = "Assets/_Data/Ship/DefaultShipJuiceSettings.asset";
        private const string AresProjectileOnlyPrefabPath = "Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles/VFX_Ares_Projectile_Only.prefab";

        public enum Severity
        {
            Info,
            Warning,
            Error
        }

        public sealed class AuditResult
        {
            public AuditResult(Severity severity, string message)
            {
                Severity = severity;
                Message = message;
            }

            public Severity Severity { get; }
            public string Message { get; }
        }

        [MenuItem("ProjectArk/Ship/VFX/Authority/Audit BoostTrailRoot Prefab")]
        public static void RunAuditMenu()
        {
            RunAudit(logToConsole: true);
        }

        public static IReadOnlyList<AuditResult> RunAudit(bool logToConsole = true)
        {
            var results = new List<AuditResult>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                results.Add(new AuditResult(Severity.Error, $"Missing BoostTrailRoot prefab: {PREFAB_PATH}"));
                LogAuditResults(results, logToConsole);
                return results;
            }

            var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                if (root == null)
                {
                    results.Add(new AuditResult(Severity.Error, $"Failed to load BoostTrailRoot prefab contents: {PREFAB_PATH}"));
                    return results;
                }

                AuditRoot(root, results);
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }

            if (!HasIssues(results))
                results.Add(new AuditResult(Severity.Info, "BoostTrailRoot prefab authority chain is valid."));

            LogAuditResults(results, logToConsole);
            return results;
        }

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

            // ── Ares sustained trail source ───────────────────────────────
            ParticleSystem[] aresSustainParticles = CreateAresSustainTrail(root.transform);

            // ── Wire BoostTrailView references ─────────────────────────
            // All tuning parameters now live in ShipJuiceSettingsSO (data-driven).
            var so = new SerializedObject(boostTrailView);
            WireParticleArray(so.FindProperty("_aresSustainParticles"), aresSustainParticles);

            // Wire ShipJuiceSettingsSO — all Boost Trail parameters are data-driven from this SO
            var juiceSettings = AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(SHIP_JUICE_SETTINGS_PATH);
            if (juiceSettings != null)
            {
                so.FindProperty("_juiceSettings").objectReferenceValue = juiceSettings;
            }
            else
            {
                Debug.LogWarning($"[BoostTrailPrefabCreator] ShipJuiceSettingsSO not found at {SHIP_JUICE_SETTINGS_PATH}. " +
                    "BoostTrailView will use fallback defaults until wired.");
            }

            // NOTE: _boostBloomVolume is a scene object and cannot be pre-wired in a Prefab.
            so.ApplyModifiedProperties();

            var debugSO = new SerializedObject(boostTrailDebugManager);
            debugSO.FindProperty("_boostTrailView").objectReferenceValue = boostTrailView;
            debugSO.FindProperty("_enableInspectorDebug").boolValue = false;
            debugSO.FindProperty("_debugMode").enumValueIndex = (int)BoostTrailDebugManager.DebugMode.ObserveRuntime;
            debugSO.FindProperty("_previewIntensity").floatValue = 1f;
            debugSO.FindProperty("_showAresTrail").boolValue = true;
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
                        "1. Run 'ProjectArk > Ship > Authority > Rebuild Ship Prefab' to integrate BoostTrailRoot into Ship.prefab.\n" +
                        "2. Verify ShipView sprite references.\n" +
                        "3. Verify scene wiring for _boostBloomVolume.\n" +
                        "4. Optional: run 'ProjectArk > Ship > VFX > Legacy > Audit Legacy BoostTrail Material References' for retained legacy material drift only.");
                }

                Selection.activeObject = prefab;
                Debug.Log($"[BoostTrailPrefabCreator] BoostTrailRoot prefab created at {PREFAB_PATH}");
                return prefab;
            }

            Debug.LogError("[BoostTrailPrefabCreator] Failed to save prefab!");
            return null;
        }

        // ══════════════════════════════════════════════════════════════
        // Audit Helpers
        // ══════════════════════════════════════════════════════════════

        private static void AuditRoot(GameObject root, List<AuditResult> results)
        {
            if (root.name != "BoostTrailRoot")
                results.Add(new AuditResult(Severity.Error, $"BoostTrailRoot prefab root name must be BoostTrailRoot, but was {root.name}."));

            var boostTrailView = root.GetComponent<BoostTrailView>();
            if (boostTrailView == null)
                results.Add(new AuditResult(Severity.Error, "BoostTrailRoot is missing BoostTrailView."));
            else
                AuditBoostTrailView(boostTrailView, results);

            var debugManager = root.GetComponent<BoostTrailDebugManager>();
            if (debugManager == null)
                results.Add(new AuditResult(Severity.Error, "BoostTrailRoot is missing BoostTrailDebugManager."));
            else
                AuditDebugManager(debugManager, boostTrailView, results);

            AuditAresBoostTrail(root.transform, results);
            AuditLegacyRemovedChildren(root.transform, results);
        }

        private static void AuditBoostTrailView(BoostTrailView boostTrailView, List<AuditResult> results)
        {
            var so = new SerializedObject(boostTrailView);
            AuditRequiredObjectReference(so, "_juiceSettings", SHIP_JUICE_SETTINGS_PATH, results, "BoostTrailView._juiceSettings");

            var bloomProp = so.FindProperty("_boostBloomVolume");
            if (bloomProp != null && bloomProp.objectReferenceValue != null)
                results.Add(new AuditResult(Severity.Error, "BoostTrailView._boostBloomVolume must remain null in BoostTrailRoot.prefab; scene-only binding is owned by ShipBoostTrailSceneBinder."));

            var particlesProp = so.FindProperty("_aresSustainParticles");
            if (particlesProp == null)
            {
                results.Add(new AuditResult(Severity.Error, "BoostTrailView is missing serialized field _aresSustainParticles."));
                return;
            }

            if (!particlesProp.isArray || particlesProp.arraySize == 0)
            {
                results.Add(new AuditResult(Severity.Error, "BoostTrailView._aresSustainParticles must contain the adapted Ares sustain particle systems."));
                return;
            }

            for (int i = 0; i < particlesProp.arraySize; i++)
            {
                var element = particlesProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                    results.Add(new AuditResult(Severity.Error, $"BoostTrailView._aresSustainParticles[{i}] is null."));
            }
        }

        private static void AuditDebugManager(BoostTrailDebugManager debugManager, BoostTrailView boostTrailView, List<AuditResult> results)
        {
            var so = new SerializedObject(debugManager);
            var viewProp = so.FindProperty("_boostTrailView");
            if (viewProp == null)
            {
                results.Add(new AuditResult(Severity.Error, "BoostTrailDebugManager is missing serialized field _boostTrailView."));
            }
            else if (viewProp.objectReferenceValue != boostTrailView)
            {
                results.Add(new AuditResult(Severity.Error, "BoostTrailDebugManager._boostTrailView must reference the local BoostTrailView."));
            }

            var enableProp = so.FindProperty("_enableInspectorDebug");
            if (enableProp != null && enableProp.boolValue)
                results.Add(new AuditResult(Severity.Error, "BoostTrailDebugManager._enableInspectorDebug must be false in prefab baseline."));

            var modeProp = so.FindProperty("_debugMode");
            if (modeProp != null && modeProp.enumValueIndex != (int)BoostTrailDebugManager.DebugMode.ObserveRuntime)
                results.Add(new AuditResult(Severity.Error, "BoostTrailDebugManager._debugMode must default to ObserveRuntime."));
        }

        private static void AuditAresBoostTrail(Transform root, List<AuditResult> results)
        {
            var ares = root.Find("AresBoostTrail");
            if (ares == null)
            {
                results.Add(new AuditResult(Severity.Error, "BoostTrailRoot is missing AresBoostTrail."));
                return;
            }

            var particles = ares.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            if (particles == null || particles.Length == 0)
            {
                results.Add(new AuditResult(Severity.Error, "AresBoostTrail must contain at least one ParticleSystem."));
                return;
            }

            foreach (var particleSystem in particles)
            {
                if (particleSystem == null)
                    continue;

                var main = particleSystem.main;
                if (main.playOnAwake)
                    results.Add(new AuditResult(Severity.Error, $"AresBoostTrail particle {particleSystem.name}.main.playOnAwake must be false."));

                if (!main.loop)
                    results.Add(new AuditResult(Severity.Error, $"AresBoostTrail particle {particleSystem.name}.main.loop must be true."));
            }
        }

        private static void AuditLegacyRemovedChildren(Transform root, List<AuditResult> results)
        {
            string[] forbiddenChildren =
            {
                "MainTrail",
                "FlameTrail_R",
                "FlameTrail_B",
                "FlameCore",
                "EmberTrail",
                "EmberSparks",
                "BoostEnergyLayer2",
                "BoostEnergyLayer3",
                "BoostActivationHalo"
            };

            foreach (var childName in forbiddenChildren)
            {
                if (root.Find(childName) != null)
                    results.Add(new AuditResult(Severity.Error, $"Legacy removed BoostTrail child must not return: {childName}."));
            }
        }

        private static void AuditRequiredObjectReference(SerializedObject so, string propertyPath, string expectedAssetPath, List<AuditResult> results, string label)
        {
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                results.Add(new AuditResult(Severity.Error, $"Missing serialized field: {label}."));
                return;
            }

            var expected = AssetDatabase.LoadAssetAtPath<Object>(expectedAssetPath);
            if (expected == null)
            {
                results.Add(new AuditResult(Severity.Error, $"Missing expected asset for {label}: {expectedAssetPath}"));
                return;
            }

            if (prop.objectReferenceValue != expected)
                results.Add(new AuditResult(Severity.Error, $"{label} must reference {expectedAssetPath}."));
        }

        private static bool HasIssues(IReadOnlyList<AuditResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Severity == Severity.Error || results[i].Severity == Severity.Warning)
                    return true;
            }

            return false;
        }

        private static void LogAuditResults(IReadOnlyList<AuditResult> results, bool logToConsole)
        {
            if (!logToConsole)
                return;

            Debug.Log("[BoostTrailPrefabCreator] Audit completed.\n" + BuildAuditSummary(results));
        }

        private static string BuildAuditSummary(IReadOnlyList<AuditResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("── BOOST TRAIL PREFAB AUDIT ──");

            foreach (var result in results)
                sb.AppendLine($"[{result.Severity}] {result.Message}");

            return sb.ToString();
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

        private static ParticleSystem[] CreateAresSustainTrail(Transform parent)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AresProjectileOnlyPrefabPath);
            if (sourcePrefab == null)
            {
                Debug.LogError($"[BoostTrailPrefabCreator] Missing QFZ Ares source prefab at {AresProjectileOnlyPrefabPath}. BoostTrailRoot will be rebuilt without adapted Ares trail layers.");
                return System.Array.Empty<ParticleSystem>();
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            instance.name = "AresBoostTrail";
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            instance.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            instance.transform.localScale = new Vector3(0.68f, 0.68f, 0.68f);

            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            foreach (ParticleSystem particleSystem in particles)
            {
                AdaptAresParticleForBoost(particleSystem);
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            return particles;
        }

        private static void AdaptAresParticleForBoost(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
                return;

            var main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetimeMultiplier *= 0.82f;
            main.startSpeedMultiplier *= 0.72f;
            main.startSizeMultiplier *= 0.78f;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverDistanceMultiplier = 0f;
            emission.rateOverTimeMultiplier = GetAresBoostEmissionRate(particleSystem.name);

            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = GetAresBoostSortingOrder(particleSystem.name);
                renderer.minParticleSize = Mathf.Min(renderer.minParticleSize, 0.02f);
                renderer.maxParticleSize = Mathf.Max(renderer.maxParticleSize, 0.75f);
            }
        }

        private static float GetAresBoostEmissionRate(string particleName)
        {
            switch (particleName)
            {
                case "AresBoostTrail":
                    return 8f;
                case "Trail":
                    return 14f;
                case "Sparks_Along":
                    return 26f;
                case "Stars_Along":
                    return 16f;
                case "Glow_Along":
                    return 28f;
                case "Smoke_Along":
                    return 10f;
                default:
                    return 12f;
            }
        }

        private static int GetAresBoostSortingOrder(string particleName)
        {
            switch (particleName)
            {
                case "Smoke_Along":
                    return 1;
                case "AresBoostTrail":
                case "Trail":
                case "Glow_Along":
                    return 4;
                case "Sparks_Along":
                case "Stars_Along":
                    return 5;
                default:
                    return 4;
            }
        }

        private static void WireParticleArray(SerializedProperty property, ParticleSystem[] particleSystems)
        {
            if (property == null)
                return;

            int count = particleSystems != null ? particleSystems.Length : 0;
            property.arraySize = count;
            for (int i = 0; i < count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = particleSystems[i];
        }
    }
}
