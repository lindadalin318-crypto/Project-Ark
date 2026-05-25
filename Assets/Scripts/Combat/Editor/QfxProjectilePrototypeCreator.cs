using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// Creates an isolated QFX projectile-only prototype for Project Ark's Anomaly family.
    /// The generated assets are experimental and are not wired into the runtime weapon pipeline.
    /// </summary>
    public static class QfxProjectilePrototypeCreator
    {
        private const string MenuPath = "ProjectArk/Combat/VFX/QFX Prototype/Create Anomaly Projectile Only Prototype";

        private const string SourcePrefabPath = "Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles/VFX_Cyber_Projectile_Only.prefab";
        private const string SourceMaterialFolder = "Assets/QFX/ProjectilesFX/VFX_Resources/Materials/Cyber";

        private const string OutputRootFolder = "Assets/_Prefabs/VFX/QFXPrototype";
        private const string OutputFamilyFolder = OutputRootFolder + "/Anomaly";
        private const string OutputMaterialFolder = OutputFamilyFolder + "/Materials";
        private const string OutputPrefabPath = OutputFamilyFolder + "/VFX_QFX_Anomaly_Projectile_Only.prefab";

        private static readonly Color AnomalyPrimary = new Color(7.5f, 0.85f, 5.4f, 1f);
        private static readonly Color AnomalySecondary = new Color(1.1f, 7.2f, 3.2f, 1f);
        private static readonly Color AnomalyDeep = new Color(2.1f, 0.35f, 6.8f, 1f);
        private static readonly Color AnomalyWhiteHot = new Color(9.5f, 7.8f, 10f, 1f);

        [MenuItem(MenuPath, priority = 40)]
        public static void CreateAnomalyProjectileOnlyPrototype()
        {
            if (!ValidateSourceAssets())
                return;

            EnsureFolder(OutputRootFolder);
            EnsureFolder(OutputFamilyFolder);
            EnsureFolder(OutputMaterialFolder);

            var materialMap = CopyAndRecolorMaterials();
            if (materialMap.Count == 0)
            {
                Debug.LogError("[QfxProjectilePrototypeCreator] No copied materials were produced. Aborting prefab copy.");
                return;
            }

            if (!CopyPrefab())
                return;

            RewirePrefabMaterials(materialMap);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[QfxProjectilePrototypeCreator] Created Anomaly QFX projectile-only prototype at {OutputPrefabPath}. Copied materials: {materialMap.Count}.");
            EditorUtility.DisplayDialog(
                "QFX Anomaly Prototype Created",
                "Created isolated Anomaly projectile-only prototype:\n" +
                OutputPrefabPath + "\n\n" +
                "This is a visual experiment only. It is not wired into StarCore runtime assets or projectile pools.",
                "OK");
        }

        private static bool ValidateSourceAssets()
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (sourcePrefab == null)
            {
                Debug.LogError($"[QfxProjectilePrototypeCreator] Missing source prefab: {SourcePrefabPath}");
                return false;
            }

            if (!AssetDatabase.IsValidFolder(SourceMaterialFolder))
            {
                Debug.LogError($"[QfxProjectilePrototypeCreator] Missing source material folder: {SourceMaterialFolder}");
                return false;
            }

            return true;
        }

        private static Dictionary<Material, Material> CopyAndRecolorMaterials()
        {
            var result = new Dictionary<Material, Material>();
            var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { SourceMaterialFolder });

            foreach (var guid in materialGuids)
            {
                var sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                if (sourceMaterial == null)
                    continue;

                var targetPath = $"{OutputMaterialFolder}/{Path.GetFileName(sourcePath)}";
                DeleteAssetIfExists(targetPath);

                if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
                {
                    Debug.LogError($"[QfxProjectilePrototypeCreator] Failed to copy material from {sourcePath} to {targetPath}");
                    continue;
                }

                var targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                if (targetMaterial == null)
                {
                    Debug.LogError($"[QfxProjectilePrototypeCreator] Copied material could not be loaded: {targetPath}");
                    continue;
                }

                ApplyAnomalyPalette(targetMaterial);
                EditorUtility.SetDirty(targetMaterial);
                result[sourceMaterial] = targetMaterial;
            }

            return result;
        }

        private static bool CopyPrefab()
        {
            DeleteAssetIfExists(OutputPrefabPath);
            if (AssetDatabase.CopyAsset(SourcePrefabPath, OutputPrefabPath))
                return true;

            Debug.LogError($"[QfxProjectilePrototypeCreator] Failed to copy prefab from {SourcePrefabPath} to {OutputPrefabPath}");
            return false;
        }

        private static void RewirePrefabMaterials(IReadOnlyDictionary<Material, Material> materialMap)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(OutputPrefabPath);
            try
            {
                var rewiredSlots = 0;
                var renderers = prefabRoot.GetComponentsInChildren<ParticleSystemRenderer>(true);
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    var changed = false;

                    for (var i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null)
                            continue;

                        if (!materialMap.TryGetValue(materials[i], out var replacement))
                            continue;

                        materials[i] = replacement;
                        changed = true;
                        rewiredSlots++;
                    }

                    if (changed)
                    {
                        renderer.sharedMaterials = materials;
                        EditorUtility.SetDirty(renderer);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, OutputPrefabPath);
                Debug.Log($"[QfxProjectilePrototypeCreator] Rewired {rewiredSlots} ParticleSystemRenderer material slots in {OutputPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void ApplyAnomalyPalette(Material material)
        {
            var color = GetColorForMaterialName(material.name);
            var emission = color * 1.8f;
            emission.a = 1f;

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emission);

            if (material.HasProperty("_TintColor"))
                material.SetColor("_TintColor", Color.white);

            if (material.HasProperty("_ColorAddSubDiff"))
                material.SetColor("_ColorAddSubDiff", new Color(0.85f, 0.05f, 0.35f, 0f));
        }

        private static Color GetColorForMaterialName(string materialName)
        {
            if (materialName.Contains("Trail") || materialName.Contains("Glow"))
                return AnomalyDeep;

            if (materialName.Contains("Spark") || materialName.Contains("Hit"))
                return AnomalySecondary;

            if (materialName.Contains("Flare") || materialName.Contains("Star"))
                return AnomalyWhiteHot;

            return AnomalyPrimary;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            {
                Debug.LogError($"[QfxProjectilePrototypeCreator] Invalid folder path: {folderPath}");
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) == null)
                return;

            if (!AssetDatabase.DeleteAsset(assetPath))
                Debug.LogError($"[QfxProjectilePrototypeCreator] Failed to delete existing asset before overwrite: {assetPath}");
        }
    }
}
