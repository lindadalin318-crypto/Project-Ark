#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// One-click scene setup for BoostTrailView scene-only references.
    ///
    /// Authority owned by this tool:
    ///   • `BoostTrailBloomVolume` scene object
    ///   • `BoostBloomVolumeProfile.asset` assignment on the scene volume
    ///   • `BoostTrailView._boostBloomVolume` scene-only wiring
    ///
    /// It does not create prefab content and should remain scene-only.
    /// </summary>
    public static class ShipBoostTrailSceneBinder
    {
        private const string BoostBloomVolumeName = "BoostTrailBloomVolume";
        private const string BoostBloomProfilePath = "Assets/Settings/BoostBloomVolumeProfile.asset";

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

        [MenuItem("ProjectArk/Ship/VFX/Authority/Audit BoostTrail Scene Bloom References")]
        public static void RunAuditMenu()
        {
            RunAudit(logToConsole: true);
        }

        public static IReadOnlyList<AuditResult> RunAudit(bool logToConsole = true)
        {
            var results = new List<AuditResult>();
            var volumeType = ResolveVolumeType();
            if (volumeType == null)
            {
                results.Add(new AuditResult(Severity.Error, "Volume type not found: UnityEngine.Rendering.Volume"));
                LogAuditResults(results, logToConsole);
                return results;
            }

            var bloomVolume = FindBoostBloomVolume(volumeType);
            if (bloomVolume == null)
            {
                results.Add(new AuditResult(Severity.Error, $"Missing scene-only {BoostBloomVolumeName} with UnityEngine.Rendering.Volume."));
            }
            else
            {
                AuditBloomVolume(bloomVolume, results);
            }

            var views = UnityEngine.Object.FindObjectsByType<BoostTrailView>(FindObjectsInactive.Include);
            if (views.Length == 0)
            {
                results.Add(new AuditResult(Severity.Error, "Scene contains no BoostTrailView. Ensure the Ship prefab already includes BoostTrailRoot first."));
            }

            foreach (var view in views)
                AuditBoostTrailView(view, bloomVolume, results);

            if (!HasIssues(results))
                results.Add(new AuditResult(Severity.Info, "BoostTrail scene-only Bloom references are valid."));

            LogAuditResults(results, logToConsole);
            return results;
        }

        [MenuItem("ProjectArk/Ship/VFX/Authority/Bind BoostTrail Scene Bloom References")]
        public static void SetupBoostTrailSceneReferences()
        {
            var log = new List<string>();
            var todo = new List<string>();

            var bloomVolume = FindOrCreateBoostBloomVolume(log, todo);

            var views = UnityEngine.Object.FindObjectsByType<BoostTrailView>(FindObjectsInactive.Include);
            if (views.Length == 0)
            {
                todo.Add("Scene contains no BoostTrailView. Ensure the Ship prefab already includes BoostTrailRoot first.");
            }

            foreach (var view in views)
            {
                WireBoostTrailView(view, bloomVolume, log, todo);
            }

            if (bloomVolume != null)
                EditorUtility.SetDirty(bloomVolume.gameObject);

            foreach (var view in views)
                EditorUtility.SetDirty(view);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            string summary = BuildSummary(log, todo);
            Debug.Log("[ShipBoostTrailSceneBinder] Done.\n" + summary);
        }

        private static Component FindOrCreateBoostBloomVolume(List<string> log, List<string> todo)
        {
            var volumeType = ResolveVolumeType();
            if (volumeType == null)
            {
                todo.Add("Volume type not found: UnityEngine.Rendering.Volume");
                return null;
            }

            Component volume = null;
            var components = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include);
            for (int i = 0; i < components.Length; i++)
            {
                var candidate = components[i];
                if (candidate != null && candidate.GetType() == volumeType && candidate.gameObject.name == BoostBloomVolumeName)
                {
                    volume = candidate;
                    break;
                }
            }

            GameObject volumeGo;
            if (volume != null)
            {
                volumeGo = volume.gameObject;
                log.Add("✓ Reusing existing BoostTrail bloom volume");
            }
            else
            {
                volumeGo = new GameObject(BoostBloomVolumeName);
                Undo.RegisterCreatedObjectUndo(volumeGo, "Create Boost Trail Bloom Volume");
                volume = Undo.AddComponent(volumeGo, volumeType);
                log.Add("✓ Created BoostTrail bloom volume");
                log.Add("✓ Added Volume component to BoostTrail bloom volume");
            }

            if (volume == null)
            {
                volume = volumeGo.GetComponent(volumeType);
                if (volume == null)
                {
                    volume = Undo.AddComponent(volumeGo, volumeType);
                    log.Add("✓ Added Volume component to BoostTrail bloom volume");
                }
            }

            var serializedVolume = new SerializedObject(volume);
            var isGlobalProp = serializedVolume.FindProperty("m_IsGlobal");
            if (isGlobalProp != null)
                isGlobalProp.boolValue = true;

            var priorityProp = serializedVolume.FindProperty("priority");
            if (priorityProp != null)
                priorityProp.floatValue = 100f;

            var weightProp = serializedVolume.FindProperty("weight");
            if (weightProp != null)
                weightProp.floatValue = 0f;

            var blendDistanceProp = serializedVolume.FindProperty("blendDistance");
            if (blendDistanceProp != null)
                blendDistanceProp.floatValue = 0f;

            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BoostBloomProfilePath);
            if (profile != null)
            {
                var profileProp = serializedVolume.FindProperty("sharedProfile");
                if (profileProp != null)
                    profileProp.objectReferenceValue = profile;

                log.Add("✓ Wired BoostBloomVolumeProfile to bloom volume");
            }
            else
            {
                todo.Add($"Boost bloom profile missing: {BoostBloomProfilePath}");
            }

            serializedVolume.ApplyModifiedPropertiesWithoutUndo();
            return volume;
        }

        private static void WireBoostTrailView(
            BoostTrailView view,
            Component bloomVolume,
            List<string> log,
            List<string> todo)
        {
            if (view == null)
                return;

            var so = new SerializedObject(view);
            bool changed = false;

            changed |= WireObjectReference(so, "_boostBloomVolume", bloomVolume, log, $"{view.name}._boostBloomVolume");

            if (changed)
            {
                so.ApplyModifiedProperties();
                log.Add($"✓ Scene references updated on {view.name}");
            }
            else
            {
                log.Add($"✓ Scene references already wired on {view.name}");
            }

            if (bloomVolume == null)
                todo.Add($"{view.name}: bloom volume is still null.");
        }

        private static Type ResolveVolumeType()
        {
            const string volumeFullName = "UnityEngine.Rendering.Volume";
            var componentTypes = TypeCache.GetTypesDerivedFrom<Component>();
            for (int i = 0; i < componentTypes.Count; i++)
            {
                if (componentTypes[i] != null && componentTypes[i].FullName == volumeFullName)
                {
                    return componentTypes[i];
                }
            }

            return null;
        }

        private static Component FindBoostBloomVolume(Type volumeType)
        {
            if (volumeType == null)
                return null;

            var components = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include);
            for (int i = 0; i < components.Length; i++)
            {
                var candidate = components[i];
                if (candidate != null && candidate.GetType() == volumeType && candidate.gameObject.name == BoostBloomVolumeName)
                    return candidate;
            }

            return null;
        }

        private static void AuditBloomVolume(Component bloomVolume, List<AuditResult> results)
        {
            var serializedVolume = new SerializedObject(bloomVolume);
            var isGlobalProp = serializedVolume.FindProperty("m_IsGlobal");
            if (isGlobalProp != null && !isGlobalProp.boolValue)
                results.Add(new AuditResult(Severity.Error, $"{BoostBloomVolumeName}.m_IsGlobal must be true."));

            var priorityProp = serializedVolume.FindProperty("priority");
            if (priorityProp != null && !Mathf.Approximately(priorityProp.floatValue, 100f))
                results.Add(new AuditResult(Severity.Error, $"{BoostBloomVolumeName}.priority must be 100."));

            var weightProp = serializedVolume.FindProperty("weight");
            if (weightProp != null && !Mathf.Approximately(weightProp.floatValue, 0f))
                results.Add(new AuditResult(Severity.Error, $"{BoostBloomVolumeName}.weight must be 0 at edit-time baseline."));

            var blendDistanceProp = serializedVolume.FindProperty("blendDistance");
            if (blendDistanceProp != null && !Mathf.Approximately(blendDistanceProp.floatValue, 0f))
                results.Add(new AuditResult(Severity.Error, $"{BoostBloomVolumeName}.blendDistance must be 0."));

            var expectedProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BoostBloomProfilePath);
            if (expectedProfile == null)
            {
                results.Add(new AuditResult(Severity.Error, $"Boost bloom profile missing: {BoostBloomProfilePath}"));
                return;
            }

            var profileProp = serializedVolume.FindProperty("sharedProfile");
            if (profileProp != null && profileProp.objectReferenceValue != expectedProfile)
                results.Add(new AuditResult(Severity.Error, $"{BoostBloomVolumeName}.sharedProfile must reference {BoostBloomProfilePath}."));
        }

        private static void AuditBoostTrailView(Component view, Component bloomVolume, List<AuditResult> results)
        {
            if (view == null)
                return;

            var so = new SerializedObject(view);
            var bloomProp = so.FindProperty("_boostBloomVolume");
            if (bloomProp == null)
            {
                results.Add(new AuditResult(Severity.Error, $"{view.name} is missing serialized field _boostBloomVolume."));
                return;
            }

            if (bloomVolume == null)
            {
                if (bloomProp.objectReferenceValue != null)
                    results.Add(new AuditResult(Severity.Error, $"{view.name}._boostBloomVolume points to a scene object, but {BoostBloomVolumeName} is missing."));

                return;
            }

            if (bloomProp.objectReferenceValue != bloomVolume)
                results.Add(new AuditResult(Severity.Error, $"{view.name}._boostBloomVolume must point to scene-only {BoostBloomVolumeName}."));
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

            Debug.Log("[ShipBoostTrailSceneBinder] Audit completed.\n" + BuildAuditSummary(results));
        }

        private static string BuildAuditSummary(IReadOnlyList<AuditResult> results)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("── BOOST TRAIL SCENE AUDIT ──");

            foreach (var result in results)
                sb.AppendLine($"[{result.Severity}] {result.Message}");

            return sb.ToString();
        }

        private static bool WireObjectReference(
            SerializedObject so,
            string propertyPath,
            UnityEngine.Object value,
            List<string> log,
            string label)
        {
            if (value == null)
                return false;

            var prop = so.FindProperty(propertyPath);
            if (prop == null)
                return false;

            if (prop.objectReferenceValue == value)
                return false;

            prop.objectReferenceValue = value;
            log.Add($"✓ {label} wired");
            return true;
        }

        private static string BuildSummary(List<string> log, List<string> todo)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("── BOOST TRAIL SCENE SETUP COMPLETED ──");

            foreach (var entry in log)
                sb.AppendLine(entry);

            if (todo.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("── MANUAL STEPS REQUIRED ──");
                for (int i = 0; i < todo.Count; i++)
                    sb.AppendLine($"{i + 1}. {todo[i]}");
            }

            return sb.ToString();
        }
    }
}
#endif
