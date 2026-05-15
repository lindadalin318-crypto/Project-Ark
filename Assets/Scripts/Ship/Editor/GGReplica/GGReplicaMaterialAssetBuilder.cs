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
        private const string FakeFluxyShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaFakeFluxy.shader";
        private const string EngineTrailShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaEngineTrail.shader";
        private const string DodgeParticlesShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaDodgeParticles.shader";

        public const string PlayerShipHlMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat";
        public const string TeleportSchemeMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat";
        public const string PlayerLqTrailMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat";
        public const string FakeFluxyMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaFakeFluxy.mat";
        public const string EngineTrailMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaEngineTrail.mat";
        public const string DodgeParticlesMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaDodgeParticles.mat";

        private const string MaterialFolderPath = "Assets/_Art/Ship/GGReplica/Materials";

        [MenuItem("ProjectArk/Ship/GG Replica/Build Visual Materials")]
        public static void BuildVisualMaterials()
        {
            var playerShipHlShader = LoadShader(PlayerShipHlShaderPath);
            var teleportSchemeShader = LoadShader(TeleportSchemeShaderPath);
            var playerLqTrailShader = LoadShader(PlayerLqTrailShaderPath);
            var fakeFluxyShader = LoadShader(FakeFluxyShaderPath);
            var engineTrailShader = LoadShader(EngineTrailShaderPath);
            var dodgeParticlesShader = LoadShader(DodgeParticlesShaderPath);
            if (playerShipHlShader == null || teleportSchemeShader == null || playerLqTrailShader == null || fakeFluxyShader == null || engineTrailShader == null || dodgeParticlesShader == null)
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

            var fakeFluxy = LoadOrCreateMaterial(FakeFluxyMaterialPath, fakeFluxyShader);
            fakeFluxy.shader = fakeFluxyShader;
            fakeFluxy.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
            fakeFluxy.SetColor("_GlowColor", new Color(1.1607844f, 0f, 2.9960785f, 0f));
            fakeFluxy.SetFloat("_DistortionOffset", -1f);
            fakeFluxy.SetFloat("_DepthOffset", -0.3f);
            fakeFluxy.SetFloat("_NoiseScale", 6f);
            fakeFluxy.SetFloat("_RimWidth", 0.07f);
            fakeFluxy.SetFloat("_FlowPower", 3.77f);
            fakeFluxy.SetFloat("_Alpha", 0.62f);
            EditorUtility.SetDirty(fakeFluxy);

            var engineTrail = LoadOrCreateMaterial(EngineTrailMaterialPath, engineTrailShader);
            engineTrail.shader = engineTrailShader;
            engineTrail.SetColor("_BottomColor", new Color(1f, 0f, 0.91456747f, 1f));
            engineTrail.SetColor("_TopColor", new Color(0.0990566f, 0.8460265f, 1f, 1f));
            engineTrail.SetColor("_GhostColor", new Color(0.09211465f, 0.8490566f, 0.6468398f, 0.3882353f));
            engineTrail.SetFloat("_MixEffect", 1f);
            engineTrail.SetFloat("_NoiseScale", 1.31f);
            engineTrail.SetFloat("_Spread", 2f);
            engineTrail.SetFloat("_Power", 7f);
            engineTrail.SetFloat("_WobbleSpeed", 0.4f);
            engineTrail.SetVector("_Speed1", new Vector4(-2f, 0f, 1f, 1f));
            engineTrail.SetVector("_Speed2", new Vector4(-1f, 0f, 1f, 1f));
            engineTrail.SetVector("_Speed3", new Vector4(-1.61f, 0f, 1f, 0.51f));
            EditorUtility.SetDirty(engineTrail);

            var dodgeParticles = LoadOrCreateMaterial(DodgeParticlesMaterialPath, dodgeParticlesShader);
            dodgeParticles.shader = dodgeParticlesShader;
            dodgeParticles.SetColor("_Color", new Color(1f, 0.78035855f, 0f, 1f));
            dodgeParticles.SetColor("_TintColor", new Color(1f, 1f, 1f, 1f));
            dodgeParticles.SetColor("_EmissionColor", new Color(0f, 0f, 0f, 1f));
            dodgeParticles.SetFloat("_InvFade", 3f);
            dodgeParticles.SetFloat("_SrcBlend", 1f);
            dodgeParticles.SetFloat("_DstBlend", 0f);
            dodgeParticles.SetFloat("_ZWrite", 1f);
            dodgeParticles.SetFloat("_ShellIntensity", 1f);
            EditorUtility.SetDirty(dodgeParticles);

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
