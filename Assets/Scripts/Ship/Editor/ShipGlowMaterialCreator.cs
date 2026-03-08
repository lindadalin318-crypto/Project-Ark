#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Creates the ShipGlowMaterial used by the Ship_Sprite_Liquid layer.
    /// Material uses Additive blending to produce a glow/energy overlay effect
    /// on top of the solid ship sprite.
    ///
    /// Menu: ProjectArk > Ship > Create Ship Glow Material
    /// </summary>
    public static class ShipGlowMaterialCreator
    {
        private const string OUTPUT_DIR  = "Assets/_Art/Ship/Glitch";
        private const string OUTPUT_PATH = "Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat";

        [MenuItem("ProjectArk/Ship/Create Ship Glow Material")]
        public static Material CreateOrGet()
        {
            // Force refresh: delete existing material and recreate
            var existing = AssetDatabase.LoadAssetAtPath<Material>(OUTPUT_PATH);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(OUTPUT_PATH);
                AssetDatabase.Refresh();
                Debug.Log($"[ShipGlowMaterialCreator] Deleted old material at {OUTPUT_PATH}, recreating...");
            }

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(OUTPUT_DIR))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "../", OUTPUT_DIR));
                AssetDatabase.Refresh();
            }

            // Strategy 1: Try URP 2D Sprite-Lit-Default
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            bool isUrp2D = shader != null;

            if (!isUrp2D)
            {
                // Strategy 2: Fallback to Sprites/Default (Built-in)
                shader = Shader.Find("Sprites/Default");
                Debug.LogWarning("[ShipGlowMaterialCreator] URP 2D shader not found, falling back to Sprites/Default. " +
                                 "Please verify Additive blending manually in the Inspector.");
            }

            var mat = new Material(shader);
            mat.name = "ShipGlowMaterial";

            if (isUrp2D)
            {
                // URP 2D Additive blending configuration
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;

                // Enable transparent surface type keyword
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f); // 1 = Transparent

                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                // Blend operation: Add
                if (mat.HasProperty("_BlendOp"))
                    mat.SetFloat("_BlendOp", 0f); // BlendOp.Add = 0

                // Additive: SrcBlend = One (1), DstBlend = One (1)
                if (mat.HasProperty("_SrcBlend"))
                    mat.SetFloat("_SrcBlend", 1f); // BlendMode.One
                if (mat.HasProperty("_DstBlend"))
                    mat.SetFloat("_DstBlend", 1f); // BlendMode.One

                mat.SetFloat("_ZWrite", 0f);
            }
            else
            {
                // Built-in Sprites/Default Additive blending
                mat.SetFloat("_SrcBlend", 1f); // BlendMode.One
                mat.SetFloat("_DstBlend", 1f); // BlendMode.One
                mat.SetFloat("_ZWrite", 0f);
                mat.renderQueue = 3000;
                mat.SetOverrideTag("RenderType", "Transparent");
            }

            // Tint color: slightly cyan to match GG liquid layer hue
            mat.color = new Color(0.28f, 0.43f, 0.43f, 1f);

            AssetDatabase.CreateAsset(mat, OUTPUT_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ShipGlowMaterialCreator] Created ShipGlowMaterial at {OUTPUT_PATH} " +
                      $"(shader: {shader.name}, Additive blending)");
            EditorUtility.DisplayDialog(
                "Ship Glow Material Created",
                $"ShipGlowMaterial created at:\n{OUTPUT_PATH}\n\n" +
                $"Shader: {shader.name}\n" +
                "Blending: Additive (SrcBlend=One, DstBlend=One)\n" +
                "RenderQueue: 3000\n\n" +
                "NOTE: In the Inspector, verify the liquid layer shows additive glow on dark backgrounds.",
                "OK");

            return mat;
        }
    }
}
#endif
