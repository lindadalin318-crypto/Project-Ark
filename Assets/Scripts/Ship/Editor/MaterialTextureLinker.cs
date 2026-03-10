using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// One-click editor utility to auto-assign all GGrenderdoc textures to their materials.
    /// Menu: ProjectArk > VFX > Link Material Textures
    ///
    /// Assigns:
    ///   mat_boost_energy_layer2 : _Tex0~3 = boost_noise_main/distort/layer3/layer4
    ///   mat_boost_energy_layer3 : _Tex0~1 = boost_energy_noise_a / boost_energy_main
    ///   mat_boost_energy_field  : _LUTTex = boost_field_main, _UseLUT = 1
    ///   mat_trail_main_effect   : _Slot0~3 = trail_main_spritesheet / trail_second_spritesheet / trail_edge_glow / trail_color_lut
    ///   mat_flame_trail         : _BaseMap = vfx_boost_techno_flame
    ///   mat_ember_trail         : _BaseMap = vfx_ember_trail
    ///   mat_ember_sparks        : _BaseMap = vfx_ember_sparks
    ///   mat_trail_main          : _BaseMap = trail_main_spritesheet
    /// </summary>
    public static class MaterialTextureLinker
    {
        private const string MAT_DIR = "Assets/_Art/VFX/BoostTrail/Materials";
        private const string TEX_DIR = "Assets/_Art/VFX/BoostTrail/Textures";
        private const string TrailMainEffectShader = "ProjectArk/VFX/TrailMainEffect";
        private const string BoostEnergyLayer2Shader = "ProjectArk/VFX/BoostEnergyLayer2";
        private const string BoostEnergyLayer3Shader = "ProjectArk/VFX/BoostEnergyLayer3";
        private const string BoostEnergyFieldShader = "ProjectArk/VFX/BoostEnergyField";

        [MenuItem("ProjectArk/VFX/Link Material Textures")]
        public static void LinkAllMaterialTexturesMenu()
        {
            LinkAllMaterialTextures(showDialog: true);
        }

        public static void LinkAllMaterialTextures(bool showDialog)
        {
            int successCount = 0;
            int failCount    = 0;

            // ── mat_boost_energy_layer2 ───────────────────────────────────────
            var mat2 = LoadMat("mat_boost_energy_layer2");
            if (mat2 != null)
            {
                EnsureShader(mat2, BoostEnergyLayer2Shader, ref successCount, ref failCount);
                AssignTex(mat2, "_Tex0", "boost_noise_main",    ref successCount, ref failCount);
                AssignTex(mat2, "_Tex1", "boost_noise_distort", ref successCount, ref failCount);
                AssignTex(mat2, "_Tex2", "boost_noise_layer3",  ref successCount, ref failCount);
                AssignTex(mat2, "_Tex3", "boost_noise_layer4",  ref successCount, ref failCount);
                EditorUtility.SetDirty(mat2);
            }
            else { failCount++; Debug.LogWarning("[MaterialTextureLinker] mat_boost_energy_layer2 not found"); }

            // ── mat_boost_energy_layer3 ───────────────────────────────────────
            var mat3 = LoadMat("mat_boost_energy_layer3");
            if (mat3 != null)
            {
                EnsureShader(mat3, BoostEnergyLayer3Shader, ref successCount, ref failCount);
                AssignTex(mat3, "_Tex0", "boost_energy_noise_a", ref successCount, ref failCount);
                AssignTex(mat3, "_Tex1", "boost_energy_main",    ref successCount, ref failCount);
                EditorUtility.SetDirty(mat3);
            }
            else { failCount++; Debug.LogWarning("[MaterialTextureLinker] mat_boost_energy_layer3 not found"); }

            // ── mat_boost_energy_field ────────────────────────────────────────
            var matField = LoadMat("mat_boost_energy_field");
            if (matField != null)
            {
                EnsureShader(matField, BoostEnergyFieldShader, ref successCount, ref failCount);
                AssignTex(matField, "_LUTTex", "boost_field_main", ref successCount, ref failCount);
                matField.SetFloat("_UseLUT", 1f);
                EditorUtility.SetDirty(matField);
                Debug.Log("[MaterialTextureLinker] mat_boost_energy_field: _UseLUT set to 1");
            }
            else { failCount++; Debug.LogWarning("[MaterialTextureLinker] mat_boost_energy_field not found"); }

            // ── mat_trail_main_effect ─────────────────────────────────────────
            var matTrailEffect = LoadMat("mat_trail_main_effect");
            if (matTrailEffect != null)
            {
                EnsureShader(matTrailEffect, TrailMainEffectShader, ref successCount, ref failCount);
                AssignTex(matTrailEffect, "_Slot0", "trail_main_spritesheet",   ref successCount, ref failCount);
                AssignTex(matTrailEffect, "_Slot1", "trail_second_spritesheet", ref successCount, ref failCount);
                AssignTex(matTrailEffect, "_Slot2", "trail_edge_glow",          ref successCount, ref failCount);
                AssignTex(matTrailEffect, "_Slot3", "trail_color_lut",          ref successCount, ref failCount);
                EditorUtility.SetDirty(matTrailEffect);
            }
            else { failCount++; Debug.LogWarning("[MaterialTextureLinker] mat_trail_main_effect not found"); }

            // ── mat_flame_trail ───────────────────────────────────────────────
            var matFlame = LoadMat("mat_flame_trail");
            if (matFlame != null)
            {
                AssignTex(matFlame, "_BaseMap", "vfx_boost_techno_flame", ref successCount, ref failCount);
                EditorUtility.SetDirty(matFlame);
            }
            else { failCount++; Debug.LogWarning("[MaterialTextureLinker] mat_flame_trail not found"); }

            // ── mat_ember_trail ───────────────────────────────────────────────
            var matEmber = LoadMat("mat_ember_trail");
            if (matEmber != null)
            {
                AssignTex(matEmber, "_BaseMap", "vfx_ember_trail", ref successCount, ref failCount);
                EditorUtility.SetDirty(matEmber);
            }
            else { failCount++; Debug.LogWarning("[MaterialTextureLinker] mat_ember_trail not found"); }

            // ── mat_ember_sparks ──────────────────────────────────────────────
            var matSparks = LoadMat("mat_ember_sparks");
            if (matSparks != null)
            {
                AssignTex(matSparks, "_BaseMap", "vfx_ember_sparks", ref successCount, ref failCount);
                EditorUtility.SetDirty(matSparks);
            }
            else { failCount++; Debug.LogWarning("[MaterialTextureLinker] mat_ember_sparks not found"); }

            // ── mat_trail_main ────────────────────────────────────────────────
            // mat_trail_main is used by TrailRenderer (MainTrail), its _BaseMap
            // should be the trail sprite sheet, NOT the flame texture.
            var matTrailMain = LoadMat("mat_trail_main");
            if (matTrailMain != null)
            {
                AssignTex(matTrailMain, "_BaseMap", "trail_main_spritesheet", ref successCount, ref failCount);
                EditorUtility.SetDirty(matTrailMain);
            }
            else { failCount++; Debug.LogWarning("[MaterialTextureLinker] mat_trail_main not found"); }

            // ── Save & Report ─────────────────────────────────────────────────
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = $"Material Texture Linking Complete\n\n" +
                             $"✅ Success: {successCount} texture assignments\n" +
                             $"❌ Failed:  {failCount} assignments\n\n" +
                             (failCount > 0
                                 ? "Check Console for details on failed assignments.\n" +
                                   "Make sure textures are imported (run CopyGGTextures.ps1 first)."
                                 : "All textures linked successfully!");

            if (showDialog)
                EditorUtility.DisplayDialog("Link Material Textures", summary, "OK");

            Debug.Log($"[MaterialTextureLinker] Done. Success={successCount}, Fail={failCount}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Material LoadMat(string matName)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/{matName}.mat");
        }

        private static void EnsureShader(Material mat, string shaderName, ref int successCount, ref int failCount)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                failCount++;
                Debug.LogWarning($"[MaterialTextureLinker] Shader not found: {shaderName} (for {mat.name})");
                return;
            }

            if (mat.shader == shader)
                return;

            mat.shader = shader;
            successCount++;
            Debug.Log($"[MaterialTextureLinker] {mat.name}.shader = {shaderName}");
        }

        private static void AssignTex(Material mat, string propName, string texName,
                                       ref int successCount, ref int failCount)
        {
            // Search in VFX textures directory first, then ship directory
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX_DIR}/{texName}.png");
            if (tex == null)
            {
                // Fallback: search entire project
                var guids = AssetDatabase.FindAssets($"{texName} t:Texture2D");
                if (guids.Length > 0)
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (tex != null)
            {
                mat.SetTexture(propName, tex);
                successCount++;
                Debug.Log($"[MaterialTextureLinker] {mat.name}.{propName} = {texName}");
            }
            else
            {
                failCount++;
                Debug.LogWarning($"[MaterialTextureLinker] Texture not found: {texName}.png " +
                                 $"(for {mat.name}.{propName})");
            }
        }
    }
}
