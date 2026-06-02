using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Legacy reference audit tool for the retired Ship / BoostTrail material chain.
    ///
    /// Authority owned by this tool:
    ///   • Read-only audit of retained legacy reference materials
    ///   • Exact-path-first texture checks inside Assets/_Art/VFX/BoostTrail/Textures
    ///   • TrailMainEffect / BoostEnergyLayer2 / BoostEnergyLayer3 shader drift detection
    ///
    /// This tool no longer applies or restores legacy material chains.
    /// </summary>
    public static class MaterialTextureLinker
    {
        private const string MAT_DIR = "Assets/_Art/VFX/BoostTrail/Materials";
        private const string TEX_DIR = "Assets/_Art/VFX/BoostTrail/Textures";
        private const string TrailMainEffectShader = "ProjectArk/VFX/TrailMainEffect";
        private const string BoostEnergyLayer2Shader = "ProjectArk/VFX/BoostEnergyLayer2";
        private const string BoostEnergyLayer3Shader = "ProjectArk/VFX/BoostEnergyLayer3";

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

        [MenuItem("ProjectArk/Ship/VFX/Authority/Audit Active BoostTrail Material Textures", true)]
        public static bool RunAuditMenuValidate()
        {
            return false;
        }

        [MenuItem("ProjectArk/Ship/VFX/Authority/Audit Active BoostTrail Material Textures")]
        public static void RunAuditMenu()
        {
            RunAudit(logToConsole: true);
        }

        public static IReadOnlyList<AuditResult> RunAudit(bool logToConsole = true)
        {
            var results = new List<AuditResult>();

            AuditMaterial("mat_boost_energy_layer2", BoostEnergyLayer2Shader, results,
                ("_Tex0", "boost_noise_main"),
                ("_Tex1", "boost_noise_distort"),
                ("_Tex2", "boost_noise_layer3"),
                ("_Tex3", "boost_noise_layer4"));
            AuditMaterial("mat_boost_energy_layer3", BoostEnergyLayer3Shader, results,
                ("_Tex0", "boost_energy_noise_a"),
                ("_Tex1", "boost_energy_main"));
            AuditMaterial("mat_flame_trail", null, results,
                ("_BaseMap", "vfx_boost_techno_flame"));
            AuditMaterial("mat_ember_trail", null, results,
                ("_BaseMap", "vfx_ember_trail"));
            AuditMaterial("mat_ember_sparks", null, results,
                ("_BaseMap", "vfx_ember_sparks"));
            var trailMain = AuditMaterial("mat_trail_main", TrailMainEffectShader, results,
                ("_BaseMap", "vfx_boost_techno_flame"));
            AuditTrailMainLegacySlots(trailMain, results);

            if (results.Count == 0)
                results.Add(new AuditResult(Severity.Info, "Legacy BoostTrail material reference links are valid."));

            LogAuditResults(results, logToConsole);
            return results;
        }

        [MenuItem("ProjectArk/Ship/VFX/Legacy/Audit Legacy BoostTrail Material References")]
        public static void RunLegacyAuditMenu()
        {
            RunAudit(logToConsole: true);
        }

        [MenuItem("ProjectArk/Ship/VFX/Authority/Link Active BoostTrail Material Textures", true)]
        public static bool LinkAllMaterialTexturesMenuValidate()
        {
            return false;
        }

        [MenuItem("ProjectArk/Ship/VFX/Authority/Link Active BoostTrail Material Textures")]
        public static void LinkAllMaterialTexturesMenu()
        {
            LinkAllMaterialTextures(showDialog: true);
        }

        public static void LinkAllMaterialTextures(bool showDialog)
        {
            string summary = "Legacy BoostTrail material apply is disabled. Use ProjectArk/Ship/VFX/Legacy/Audit Legacy BoostTrail Material References to report drift without repairing retained reference assets.";

            if (showDialog)
                EditorUtility.DisplayDialog("Legacy Material Apply Disabled", summary, "OK");

            Debug.Log($"[MaterialTextureLinker] {summary}");
        }

        private static void LinkLegacyMaterialTexturesForMigrationOnly(bool showDialog)
        {
            int successCount = 0;
            int failCount = 0;

            // ── mat_boost_energy_layer2 ───────────────────────────────────────
            var mat2 = LoadMat("mat_boost_energy_layer2");
            if (mat2 != null)
            {
                EnsureShader(mat2, BoostEnergyLayer2Shader, ref successCount, ref failCount);
                AssignTex(mat2, "_Tex0", "boost_noise_main", ref successCount, ref failCount);
                AssignTex(mat2, "_Tex1", "boost_noise_distort", ref successCount, ref failCount);
                AssignTex(mat2, "_Tex2", "boost_noise_layer3", ref successCount, ref failCount);
                AssignTex(mat2, "_Tex3", "boost_noise_layer4", ref successCount, ref failCount);
                EditorUtility.SetDirty(mat2);
            }
            else
            {
                failCount++;
                Debug.LogWarning("[MaterialTextureLinker] mat_boost_energy_layer2 not found");
            }

            // ── mat_boost_energy_layer3 ───────────────────────────────────────
            var mat3 = LoadMat("mat_boost_energy_layer3");
            if (mat3 != null)
            {
                EnsureShader(mat3, BoostEnergyLayer3Shader, ref successCount, ref failCount);
                AssignTex(mat3, "_Tex0", "boost_energy_noise_a", ref successCount, ref failCount);
                AssignTex(mat3, "_Tex1", "boost_energy_main", ref successCount, ref failCount);
                EditorUtility.SetDirty(mat3);
            }
            else
            {
                failCount++;
                Debug.LogWarning("[MaterialTextureLinker] mat_boost_energy_layer3 not found");
            }

            // ── mat_flame_trail ───────────────────────────────────────────────
            var matFlame = LoadMat("mat_flame_trail");
            if (matFlame != null)
            {
                AssignTex(matFlame, "_BaseMap", "vfx_boost_techno_flame", ref successCount, ref failCount);
                EditorUtility.SetDirty(matFlame);
            }
            else
            {
                failCount++;
                Debug.LogWarning("[MaterialTextureLinker] mat_flame_trail not found");
            }

            // ── mat_ember_trail ───────────────────────────────────────────────
            var matEmber = LoadMat("mat_ember_trail");
            if (matEmber != null)
            {
                AssignTex(matEmber, "_BaseMap", "vfx_ember_trail", ref successCount, ref failCount);
                EditorUtility.SetDirty(matEmber);
            }
            else
            {
                failCount++;
                Debug.LogWarning("[MaterialTextureLinker] mat_ember_trail not found");
            }

            // ── mat_ember_sparks ──────────────────────────────────────────────
            var matSparks = LoadMat("mat_ember_sparks");
            if (matSparks != null)
            {
                AssignTex(matSparks, "_BaseMap", "vfx_ember_sparks", ref successCount, ref failCount);
                EditorUtility.SetDirty(matSparks);
            }
            else
            {
                failCount++;
                Debug.LogWarning("[MaterialTextureLinker] mat_ember_sparks not found");
            }

            // ── mat_trail_main ────────────────────────────────────────────────
            // MainTrail 当前优先走更可控的火焰轮廓纹理，而不是 RenderDoc
            // 导出的整屏 trail screenshot 纹理，否则主观读感会偏离 GG。
            var matTrailMain = LoadMat("mat_trail_main");
            if (matTrailMain != null)
            {
                EnsureShader(matTrailMain, TrailMainEffectShader, ref successCount, ref failCount);
                matTrailMain.SetFloat("_UseLegacySlots", 0f);
                AssignTex(matTrailMain, "_BaseMap", "vfx_boost_techno_flame", ref successCount, ref failCount);
                EditorUtility.SetDirty(matTrailMain);
            }
            else
            {
                failCount++;
                Debug.LogWarning("[MaterialTextureLinker] mat_trail_main not found");
            }

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

        private static Material AuditMaterial(string matName, string shaderName, List<AuditResult> results,
            params (string PropertyName, string TextureName)[] textureBindings)
        {
            var mat = LoadMat(matName);
            if (mat == null)
            {
                results.Add(new AuditResult(Severity.Error, $"Missing material: {MAT_DIR}/{matName}.mat"));
                return null;
            }

            if (!string.IsNullOrEmpty(shaderName))
                AuditShader(mat, shaderName, results);

            foreach (var binding in textureBindings)
                AuditTexture(mat, binding.PropertyName, binding.TextureName, results);

            return mat;
        }

        private static void AuditShader(Material mat, string shaderName, List<AuditResult> results)
        {
            var expectedShader = Shader.Find(shaderName);
            if (expectedShader == null)
            {
                results.Add(new AuditResult(Severity.Error, $"Shader not found: {shaderName} (for {mat.name})"));
                return;
            }

            if (mat.shader != expectedShader)
                results.Add(new AuditResult(Severity.Error, $"{mat.name}.shader must be {shaderName}."));
        }

        private static void AuditTexture(Material mat, string propName, string texName, List<AuditResult> results)
        {
            var texturePath = $"{TEX_DIR}/{texName}.png";
            var expectedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (expectedTexture == null)
            {
                results.Add(new AuditResult(Severity.Error, $"Texture not found at exact path: {texturePath} (for {mat.name}.{propName})"));
                return;
            }

            if (!mat.HasProperty(propName))
            {
                results.Add(new AuditResult(Severity.Error, $"{mat.name} is missing shader property {propName}."));
                return;
            }

            var assignedTexture = mat.GetTexture(propName);
            if (assignedTexture != expectedTexture)
                results.Add(new AuditResult(Severity.Error, $"{mat.name}.{propName} must reference {texturePath}."));
        }

        private static void AuditTrailMainLegacySlots(Material mat, List<AuditResult> results)
        {
            if (mat == null || !mat.HasProperty("_UseLegacySlots"))
                return;

            if (!Mathf.Approximately(mat.GetFloat("_UseLegacySlots"), 0f))
                results.Add(new AuditResult(Severity.Error, "mat_trail_main._UseLegacySlots must be 0."));
        }

        private static void LogAuditResults(IReadOnlyList<AuditResult> results, bool logToConsole)
        {
            if (!logToConsole)
                return;

            Debug.Log("[MaterialTextureLinker] Audit completed.\n" + BuildAuditSummary(results));
        }

        private static string BuildAuditSummary(IReadOnlyList<AuditResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("── BOOST TRAIL MATERIAL AUDIT ──");
            foreach (var result in results)
                sb.AppendLine($"[{result.Severity}] {result.Message}");
            return sb.ToString();
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
            var texturePath = $"{TEX_DIR}/{texName}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            if (tex != null)
            {
                mat.SetTexture(propName, tex);
                successCount++;
                Debug.Log($"[MaterialTextureLinker] {mat.name}.{propName} = {texturePath}");
            }
            else
            {
                failCount++;
                Debug.LogWarning($"[MaterialTextureLinker] Texture not found at exact path: {texturePath} " +
                                 $"(for {mat.name}.{propName})");
            }
        }
    }
}
