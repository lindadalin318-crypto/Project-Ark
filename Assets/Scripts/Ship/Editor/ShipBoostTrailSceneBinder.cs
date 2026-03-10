#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
        private const string FlashMaterialPath = "Assets/_Art/VFX/BoostTrail/Materials/mat_ui_boost_flash.mat";
        private const string FlashMaterialFolder = "Assets/_Art/VFX/BoostTrail/Materials";
        private const string FlashShaderName = "ProjectArk/UI/BoostFlashAdditive";
        private const string VolumeTypeName = "UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime";

        [MenuItem("ProjectArk/Ship/Setup Boost Trail Scene References (GG)")]
        public static void SetupBoostTrailSceneReferences()
        {
            var log = new List<string>();
            var todo = new List<string>();

            var canvas = FindOrCreateCanvas(log);
            var flashImage = FindOrCreateFlashOverlay(canvas, log, todo);
            var bloomVolume = FindOrCreateBoostBloomVolume(log, todo);

            var views = UnityEngine.Object.FindObjectsByType<BoostTrailView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (views.Length == 0)
            {
                todo.Add("Scene contains no BoostTrailView. Run the Ship boost trail replacement tool first.");
            }

            foreach (var view in views)
            {
                WireBoostTrailView(view, flashImage, bloomVolume, log, todo);
            }

            if (canvas != null)
                EditorUtility.SetDirty(canvas.gameObject);

            if (bloomVolume != null)
                EditorUtility.SetDirty(bloomVolume.gameObject);

            foreach (var view in views)
                EditorUtility.SetDirty(view);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            string summary = BuildSummary(log, todo);
            Debug.Log("[ShipBoostTrailSceneBinder] Done.\n" + summary);
            EditorUtility.DisplayDialog("Boost Trail Scene References", summary, "OK");
        }

        private static Canvas FindOrCreateCanvas(List<string> log)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas bestCanvas = null;

            foreach (var canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;

                if (bestCanvas == null)
                {
                    bestCanvas = canvas;
                    continue;
                }

                bool isBetterBySortingOrder = canvas.sortingOrder > bestCanvas.sortingOrder;
                bool isBetterByName = canvas.sortingOrder == bestCanvas.sortingOrder
                    && canvas.name == "Canvas"
                    && bestCanvas.name != "Canvas";

                if (isBetterBySortingOrder || isBetterByName)
                    bestCanvas = canvas;
            }

            if (bestCanvas != null)
            {
                log.Add($"✓ Reusing overlay Canvas: {bestCanvas.name} (sortingOrder={bestCanvas.sortingOrder})");
                return bestCanvas;
            }

            var canvasGo = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Boost Trail Canvas");

            var newCanvas = canvasGo.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            log.Add("✓ Created fallback overlay Canvas");
            return newCanvas;
        }

        private static Image FindOrCreateFlashOverlay(Canvas canvas, List<string> log, List<string> todo)
        {
            if (canvas == null)
            {
                todo.Add("Canvas could not be created or found; flash overlay was not wired.");
                return null;
            }

            var existingGo = GameObject.Find(FlashOverlayName);
            var existing = existingGo != null ? existingGo.transform : canvas.transform.Find(FlashOverlayName);
            GameObject flashGo;
            if (existing != null)
            {
                flashGo = existing.gameObject;
                log.Add("✓ Reusing existing BoostTrail flash overlay");
            }
            else
            {
                flashGo = new GameObject(FlashOverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(flashGo, "Create Boost Trail Flash Overlay");
                log.Add("✓ Created BoostTrail flash overlay");
            }

            if (flashGo.transform.parent != canvas.transform)
            {
                Undo.SetTransformParent(flashGo.transform, canvas.transform, "Reparent Boost Trail Flash Overlay");
                log.Add($"✓ Reparented BoostTrail flash overlay under {canvas.name}");
            }

            flashGo.transform.SetAsLastSibling();

            var rect = flashGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            var image = flashGo.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            image.sprite = GetBuiltInFlashSprite();
            image.type = Image.Type.Simple;
            image.material = FindOrCreateFlashMaterial(log, todo);

            flashGo.SetActive(true);
            return image;
        }

        private static Material FindOrCreateFlashMaterial(List<string> log, List<string> todo)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
            if (material != null)
            {
                log.Add("✓ Reusing BoostTrail flash additive material");
                return material;
            }

            var shader = Shader.Find(FlashShaderName);
            if (shader == null)
            {
                todo.Add($"Flash shader not found: {FlashShaderName}");
                return null;
            }

            EnsureFolder("Assets/_Art", "VFX");
            EnsureFolder("Assets/_Art/VFX", "BoostTrail");
            EnsureFolder("Assets/_Art/VFX/BoostTrail", "Materials");

            material = new Material(shader)
            {
                name = "mat_ui_boost_flash"
            };

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);

            AssetDatabase.CreateAsset(material, FlashMaterialPath);
            AssetDatabase.SaveAssets();
            log.Add("✓ Created BoostTrail flash additive material");
            return material;
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
            Image flashImage,
            Component bloomVolume,
            List<string> log,
            List<string> todo)
        {
            if (view == null)
                return;

            var so = new SerializedObject(view);
            bool changed = false;

            changed |= WireObjectReference(so, "_flashImage", flashImage, log, $"{view.name}._flashImage");
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

            if (flashImage == null)
                todo.Add($"{view.name}: flash image is still null.");

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

        private static void EnsureFolder(string parentPath, string childName)
        {
            string targetPath = $"{parentPath}/{childName}";
            if (!AssetDatabase.IsValidFolder(targetPath))
                AssetDatabase.CreateFolder(parentPath, childName);
        }

        private static Sprite GetBuiltInFlashSprite()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite != null)
                return sprite;

            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
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
