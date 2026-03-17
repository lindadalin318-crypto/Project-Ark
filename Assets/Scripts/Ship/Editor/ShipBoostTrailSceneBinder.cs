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
        private const string VolumeTypeName = "UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime";

        [MenuItem("ProjectArk/Ship/VFX/Authority/Bind BoostTrail Scene Bloom References")]
        public static void SetupBoostTrailSceneReferences()
        {
            var log = new List<string>();
            var todo = new List<string>();

            var bloomVolume = FindOrCreateBoostBloomVolume(log, todo);

            var views = UnityEngine.Object.FindObjectsByType<BoostTrailView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
                todo.Add($"Volume type not found via VolumeProfile assembly: {VolumeTypeName}");
                return null;
            }

            Component volume = null;
            var components = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            var componentTypes = TypeCache.GetTypesDerivedFrom<Component>();
            for (int i = 0; i < componentTypes.Count; i++)
            {
                if (componentTypes[i] != null && componentTypes[i].FullName == VolumeTypeName)
                {
                    return componentTypes[i];
                }
            }

            return null;
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
