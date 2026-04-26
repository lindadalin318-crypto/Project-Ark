using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using ProjectArk.Combat;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// Editor menu tool that audits <see cref="StarCoreSO"/> assets for missing
    /// VFX / Audio references, per <c>Docs/3_WorkflowsAndRules/Project/ProceduralPresentation_WorkflowSpec.md</c>.
    ///
    /// Rationale: <c>StarCoreSO</c> defines a clean data-driven hand-off for
    /// <c>MuzzleFlashPrefab</c> / <c>ImpactVFXPrefab</c> / <c>FireSound</c>, but the
    /// replacement seam is "theoretical" until at least one asset actually uses it.
    /// Without this tool, checking which Cores are missing VFX requires clicking through
    /// every SO in the Inspector one by one. This audit flattens that into a single report.
    ///
    /// Also flags Cores whose <c>TrailTime</c> / <c>TrailWidth</c> / <c>TrailColor</c>
    /// are unset (&lt;=0 / alpha==0) — meaning the projectile will silently fall back to
    /// <see cref="Projectile"/>'s hardcoded defaults.
    ///
    /// Severity policy:
    ///   • MuzzleFlash / ImpactVFX / FireSound missing → WARNING (allowed, but surfaced)
    ///   • Projectile Prefab missing → ERROR (Core is unfireable)
    ///   • Trail parameters unset on Matter / Anomaly cores → INFO (fallback is intentional default)
    ///
    /// Run from: ProjectArk &gt; Audit StarCore VFX
    /// </summary>
    public static class StarCoreVFXAuditor
    {
        [MenuItem("ProjectArk/Audit StarCore VFX")]
        public static void AuditStarCoreVFX()
        {
            var cores = FindAllStarCores();
            if (cores.Count == 0)
            {
                Debug.LogWarning("[StarCoreVFXAuditor] No StarCoreSO asset found in the project.");
                EditorUtility.DisplayDialog(
                    "StarCore VFX Audit",
                    "No StarCoreSO asset found. Nothing to audit.",
                    "OK");
                return;
            }

            int missingMuzzle = 0;
            int missingImpact = 0;
            int missingFireSound = 0;
            int missingProjectilePrefab = 0;
            int missingTrailParams = 0;

            var report = new StringBuilder();
            report.AppendLine($"Scanned {cores.Count} StarCoreSO asset(s):\n");

            // Group by family for readable output
            cores.Sort((a, b) => a.Family.CompareTo(b.Family) != 0
                ? a.Family.CompareTo(b.Family)
                : a.name.CompareTo(b.name));

            CoreFamily lastFamily = (CoreFamily)(-1);
            foreach (var core in cores)
            {
                if (core.Family != lastFamily)
                {
                    report.AppendLine($"— {core.Family} family —");
                    lastFamily = core.Family;
                }

                string path = AssetDatabase.GetAssetPath(core);
                string shortPath = System.IO.Path.GetFileName(path);
                var flags = new List<string>();

                if (core.ProjectilePrefab == null)
                {
                    flags.Add("✗ NO PROJECTILE PREFAB (unfireable)");
                    missingProjectilePrefab++;
                }
                if (core.MuzzleFlashPrefab == null)
                {
                    flags.Add("⬜ no MuzzleFlash");
                    missingMuzzle++;
                }
                if (core.ImpactVFXPrefab == null)
                {
                    flags.Add("⬜ no ImpactVFX");
                    missingImpact++;
                }
                if (core.FireSound == null)
                {
                    flags.Add("⬜ no FireSound");
                    missingFireSound++;
                }

                // Trail params only meaningful for Matter / Anomaly (Light / Echo use their own prefabs)
                if (core.Family == CoreFamily.Matter || core.Family == CoreFamily.Anomaly)
                {
                    bool trailUnset = core.TrailTime <= 0f
                                      && core.TrailWidth <= 0f
                                      && core.TrailColor.a <= 0f;
                    if (trailUnset)
                    {
                        flags.Add("ⓘ trail params unset (using Projectile fallback)");
                        missingTrailParams++;
                    }
                }

                if (flags.Count == 0)
                    report.AppendLine($"    ✓ {shortPath}");
                else
                    report.AppendLine($"    {shortPath} : {string.Join(", ", flags)}");
            }

            report.AppendLine();
            report.AppendLine("── Summary ──");
            report.AppendLine($"  Missing ProjectilePrefab : {missingProjectilePrefab} / {cores.Count}  (error)");
            report.AppendLine($"  Missing MuzzleFlash      : {missingMuzzle} / {cores.Count}  (warning)");
            report.AppendLine($"  Missing ImpactVFX        : {missingImpact} / {cores.Count}  (warning)");
            report.AppendLine($"  Missing FireSound        : {missingFireSound} / {cores.Count}  (warning)");
            report.AppendLine($"  Trail params unset       : {missingTrailParams} / {cores.Count}  (info — Matter/Anomaly only)");

            string full = report.ToString();
            if (missingProjectilePrefab > 0)
            {
                Debug.LogError($"[StarCoreVFXAuditor] ✗ Audit found ERRORS:\n{full}");
                EditorUtility.DisplayDialog(
                    "StarCore VFX Audit — ERRORS",
                    $"{missingProjectilePrefab} core(s) have no ProjectilePrefab and cannot fire. " +
                    "See Console for the full report.",
                    "OK");
            }
            else if (missingMuzzle + missingImpact + missingFireSound > 0)
            {
                Debug.LogWarning($"[StarCoreVFXAuditor] ⬜ Audit found missing VFX / audio references:\n{full}");
                EditorUtility.DisplayDialog(
                    "StarCore VFX Audit — WARNINGS",
                    "Some cores are missing MuzzleFlash / ImpactVFX / FireSound. See Console for details.\n\n" +
                    "These replacement seams are intentional — assigning a prefab / clip will enable " +
                    "the effect without any code change. Blank entries mean the effect is currently silent.",
                    "OK");
            }
            else
            {
                Debug.Log($"[StarCoreVFXAuditor] ✓ All cores have VFX + audio assigned:\n{full}");
                EditorUtility.DisplayDialog(
                    "StarCore VFX Audit",
                    $"✓ PASS.\n\nAll {cores.Count} cores have MuzzleFlash / ImpactVFX / FireSound assigned.",
                    "OK");
            }
        }

        private static List<StarCoreSO> FindAllStarCores()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(StarCoreSO)}");
            var result = new List<StarCoreSO>(guids.Length);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<StarCoreSO>(path);
                if (so != null) result.Add(so);
            }
            return result;
        }
    }
}
