#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// One-click scene setup for BoostTrailView scene-only references.
    /// Menu: ProjectArk > Ship > Setup Boost Trail Scene References (GG)
    /// </summary>
    public static class ShipBoostTrailSceneBinder
    {
        private const string FlashOverlayName = "BoostTrailFlashOverlay";
        private const string BoostBloomVolumeName = "BoostTrailBloomVolume";
        private const string BoostBloomProfilePath = "Assets/Settings/BoostBloomVolumeProfile.asset";
        private const string VolumeTypeName = "UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime";

        [MenuItem("ProjectArk/Ship/Setup Boost Trail Scene References (GG)")]
        public static void SetupBoostTrailSceneReferences()
        {
            var log = new List<string>();
            var todo = new List<string>();

            RemoveLegacyFlashOverlay(log);
            var bloomVolume = FindOrCreateBoostBloomVolume(log, todo);

            var views = UnityEngine.Object.FindObjectsByType<BoostTrailView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (views.Length == 0)
            {
                todo.Add("Scene contains no BoostTrailView. Run the Ship boost trail replacement tool first.");
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

        private static void RemoveLegacyFlashOverlay(List<string> log)
        {
            var flashGo = GameObject.Find(FlashOverlayName);
            if (flashGo == null)
                return;

            Undo.DestroyObjectImmediate(flashGo);
            log.Add("✓ Removed legacy full-screen BoostTrail flash overlay");
        }

        private static Component FindOrCreateBoostBloomVolume(List<string> log, List<string> todo)
        {
            var volumeType = Type.GetType(VolumeTypeName);
            if (volumeType == null)
            {
                todo.Add($"Volume type not found: {VolumeTypeName}");
                return null;
            }

            var existing = GameObject.Find(BoostBloomVolumeName);
            GameObject volumeGo;
            if (existing != null)
            {
                volumeGo = existing;
                log.Add("✓ Reusing existing BoostTrail bloom volume");
            }
            else
            {
                volumeGo = new GameObject(BoostBloomVolumeName);
                Undo.RegisterCreatedObjectUndo(volumeGo, "Create Boost Trail Bloom Volume");
                log.Add("✓ Created BoostTrail bloom volume");
            }

            var volume = volumeGo.GetComponent(volumeType);
            if (volume == null)
            {
                volume = Undo.AddComponent(volumeGo, volumeType);
                log.Add("✓ Added Volume component to BoostTrail bloom volume");
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
