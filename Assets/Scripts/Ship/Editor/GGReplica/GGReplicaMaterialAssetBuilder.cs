#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Builds Project Ark-owned GGReplica materials from clean shaders while preserving
    /// parameter values proven from the Galactic Glitch extracted materials.
    /// </summary>
    public static class GGReplicaMaterialAssetBuilder
    {
        private const string PlayerShipHlShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader";
        private const string TeleportSchemeShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader";
        private const string PlayerLqTrailShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader";

        public const string PlayerShipHlMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat";
        public const string TeleportSchemeMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat";
        public const string PlayerLqTrailMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat";

        private const string MaterialFolderPath = "Assets/_Art/Ship/GGReplica/Materials";

        [MenuItem("ProjectArk/Ship/GG Replica/Build Visual Materials")]
        public static void BuildVisualMaterials()
        {
            var playerShipHlShader = LoadShader(PlayerShipHlShaderPath);
            var teleportSchemeShader = LoadShader(TeleportSchemeShaderPath);
            var playerLqTrailShader = LoadShader(PlayerLqTrailShaderPath);
            if (playerShipHlShader == null || teleportSchemeShader == null || playerLqTrailShader == null)
            {
                Debug.LogError("[GGReplicaMaterialAssetBuilder] Aborted; one or more GGReplica shaders are missing.");
                return;
            }

            EnsureMaterialFolder();

            var playerShipHl = LoadOrCreateMaterial(PlayerShipHlMaterialPath, playerShipHlShader);
            playerShipHl.shader = playerShipHlShader;
            playerShipHl.SetFloat("_Intensity", 8f);
            playerShipHl.SetFloat("_Smooth", 0.01f);
            playerShipHl.SetColor("_Tint", new Color(0.54509807f, 0.09019608f, 1f, 1f));
            EditorUtility.SetDirty(playerShipHl);

            var teleportScheme = LoadOrCreateMaterial(TeleportSchemeMaterialPath, teleportSchemeShader);
            teleportScheme.shader = teleportSchemeShader;
            teleportScheme.SetFloat("_Intensity", 1f);
            teleportScheme.SetFloat("_State", 0f);
            teleportScheme.SetFloat("_ScanScale", 8f);
            teleportScheme.SetFloat("_GlitchStrength", 0.3f);
            teleportScheme.SetFloat("_ScrollSpeed", 0.002f);
            EditorUtility.SetDirty(teleportScheme);

            var playerLqTrail = LoadOrCreateMaterial(PlayerLqTrailMaterialPath, playerLqTrailShader);
            playerLqTrail.shader = playerLqTrailShader;
            playerLqTrail.SetColor("_MainColor", new Color(0.12054504f, 0f, 0.18867922f, 1f));
            playerLqTrail.SetColor("_EdgeColor", new Color(0.61328405f, 0f, 0.80784315f, 0f));
            playerLqTrail.SetVector("_NoiseParams", new Vector4(1f, 1f, 0.2f, 0.7f));
            playerLqTrail.SetFloat("_ScrollSpeed", 0.2f);
            EditorUtility.SetDirty(playerLqTrail);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GGReplicaMaterialAssetBuilder] Built GGReplica visual materials.");
        }

        private static Shader LoadShader(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
            {
                Debug.LogError($"[GGReplicaMaterialAssetBuilder] Missing shader: {path}");
            }

            return shader;
        }

        private static void EnsureMaterialFolder()
        {
            if (AssetDatabase.IsValidFolder(MaterialFolderPath)) return;

            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, MaterialFolderPath));
            AssetDatabase.ImportAsset("Assets/_Art/Ship/GGReplica", ImportAssetOptions.ForceUpdate);
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
