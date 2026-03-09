using UnityEngine;
using UnityEditor;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// One-click tool to create Boost Trail materials matching GalacticGlitch reference.
    /// GG reference: mat_boost_trail_glow (orange-yellow Additive) + mat_boost_ember_trail (magenta Additive)
    /// </summary>
    public static class CreateBoostTrailMaterials
    {
        private const string OutputFolder = "Assets/_Art/Ship/Glitch";
        private const string ParticlesUnlitShader = "Universal Render Pipeline/Particles/Unlit";

        [MenuItem("ProjectArk/VFX/Create Boost Trail Materials")]
        public static void CreateMaterials()
        {
            var shader = Shader.Find(ParticlesUnlitShader);
            if (shader == null)
            {
                Debug.LogError($"[CreateBoostTrailMaterials] Shader not found: {ParticlesUnlitShader}");
                return;
            }

            // --- mat_boost_trail_glow ---
            // GG: Color_b3dc = (1.89, 0.828, 0.426) orange-yellow HDR, Additive blend
            CreateAdditiveParticleMaterial(
                shader,
                "mat_boost_trail_glow",
                new Color(1.89f, 0.828f, 0.426f, 1f)  // HDR orange-yellow
            );

            // --- mat_boost_ember_trail ---
            // GG: Color_b3dc = (2.0, 0.0, 1.083) magenta HDR, Additive blend
            CreateAdditiveParticleMaterial(
                shader,
                "mat_boost_ember_trail",
                new Color(2.0f, 0.0f, 1.083f, 1f)     // HDR magenta
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CreateBoostTrailMaterials] ✓ Done! Created mat_boost_trail_glow and mat_boost_ember_trail in " + OutputFolder);
        }

        private static void CreateAdditiveParticleMaterial(Shader shader, string matName, Color baseColor)
        {
            string path = $"{OutputFolder}/{matName}.mat";

            // Skip if already exists
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                Debug.LogWarning($"[CreateBoostTrailMaterials] Material already exists, skipping: {path}");
                return;
            }

            var mat = new Material(shader);
            mat.name = matName;

            // Set Additive blending via URP Particles/Unlit keywords & properties
            // Surface Type = Transparent (1), Blending Mode = Additive (3)
            mat.SetFloat("_Surface", 1f);          // Transparent
            mat.SetFloat("_Blend", 3f);            // Additive
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);

            // Enable required keywords
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ADD");
            mat.DisableKeyword("_BLENDMODE_ALPHA");
            mat.DisableKeyword("_BLENDMODE_PREMULTIPLY");

            // Set HDR base color
            mat.SetColor("_BaseColor", baseColor);

            // Render queue for transparent additive
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"[CreateBoostTrailMaterials] Created: {path}");
        }
    }
}
