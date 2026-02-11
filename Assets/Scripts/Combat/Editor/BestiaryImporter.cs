using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectArk.Combat.Enemy.Editor
{
    /// <summary>
    /// Editor tool that imports Enemy_Bestiary.csv and creates / updates
    /// <see cref="EnemyStatsSO"/> assets in Assets/_Data/Enemies/.
    /// Menu: ProjectArk > Import Enemy Bestiary
    /// </summary>
    public static class BestiaryImporter
    {
        private const string CSV_PATH = "Docs/DataTables/Enemy_Bestiary.csv";
        private const string SO_DIR = "Assets/_Data/Enemies";

        // Columns that exist only for designers and should never be mapped to SO fields.
        private static readonly HashSet<string> SKIP_COLUMNS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ID",
            "Rank",
            "AI_Archetype",
            "FactionID",
            "ExpReward",
            "PrefabPath",
            "DesignIntent",
            "PlayerCounter",
            // Any column ending with _Note is also skipped (handled in code).
        };

        // ════════════════════════════════════════════════════════════════
        //  MENU ENTRY
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Import Enemy Bestiary")]
        public static void Import()
        {
            var sw = Stopwatch.StartNew();

            // ── Locate CSV ──
            string csvAbsolutePath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, CSV_PATH);

            if (!File.Exists(csvAbsolutePath))
            {
                EditorUtility.DisplayDialog(
                    "Import Failed",
                    $"CSV file not found:\n{csvAbsolutePath}",
                    "OK");
                return;
            }

            // ── Ensure output directory ──
            EnsureFolder("Assets/_Data", "Enemies");

            // ── Read & parse CSV ──
            string[] allLines = File.ReadAllLines(csvAbsolutePath, Encoding.UTF8);

            if (allLines.Length < 2)
            {
                EditorUtility.DisplayDialog(
                    "Import Failed",
                    "CSV file has no data rows (only header or empty).",
                    "OK");
                return;
            }

            string[] headers = ParseCsvLine(allLines[0]);

            int createdCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;

            for (int i = 1; i < allLines.Length; i++)
            {
                string line = allLines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] values = ParseCsvLine(line);
                var row = BuildRowDict(headers, values);

                // ── Validate required fields ──
                if (!row.TryGetValue("InternalName", out string internalName) ||
                    string.IsNullOrWhiteSpace(internalName))
                {
                    Debug.LogWarning($"[BestiaryImporter] Row {i + 1}: Missing InternalName — skipped.");
                    skippedCount++;
                    continue;
                }

                if (!row.TryGetValue("ID", out string idStr) || string.IsNullOrWhiteSpace(idStr))
                {
                    Debug.LogWarning($"[BestiaryImporter] Row {i + 1} ({internalName}): Missing ID — skipped.");
                    skippedCount++;
                    continue;
                }

                // ── Find or create SO ──
                string assetPath = $"{SO_DIR}/EnemyStats_{internalName}.asset";
                var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(assetPath);
                bool isNew = stats == null;

                if (isNew)
                {
                    stats = ScriptableObject.CreateInstance<EnemyStatsSO>();
                }

                // ── Map fields ──
                MapFieldsToSO(stats, row, i + 1);

                // ── Save ──
                if (isNew)
                {
                    AssetDatabase.CreateAsset(stats, assetPath);
                    createdCount++;
                    Debug.Log($"[BestiaryImporter] Created: {assetPath}");
                }
                else
                {
                    EditorUtility.SetDirty(stats);
                    updatedCount++;
                    Debug.Log($"[BestiaryImporter] Updated: {assetPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            sw.Stop();

            // ── Summary dialog ──
            int totalProcessed = createdCount + updatedCount;
            EditorUtility.DisplayDialog(
                "Enemy Bestiary Import Complete",
                $"Processed: {totalProcessed}\n" +
                $"  • Created: {createdCount}\n" +
                $"  • Updated: {updatedCount}\n" +
                $"  • Skipped: {skippedCount}\n\n" +
                $"Time: {sw.ElapsedMilliseconds} ms\n" +
                $"Output: {SO_DIR}/",
                "OK");
        }

        // ════════════════════════════════════════════════════════════════
        //  FIELD MAPPING
        // ════════════════════════════════════════════════════════════════

        private static void MapFieldsToSO(EnemyStatsSO so, Dictionary<string, string> row, int rowNum)
        {
            // ── Identity ──
            TrySetString(row, "InternalName", ref so.EnemyID);
            TrySetString(row, "DisplayName", ref so.EnemyName);

            // ── Health ──
            TrySetFloat(row, "MaxHP", ref so.MaxHP, rowNum);
            TrySetFloat(row, "MaxPoise", ref so.MaxPoise, rowNum);

            // ── Movement ──
            TrySetFloat(row, "MoveSpeed", ref so.MoveSpeed, rowNum);
            TrySetFloat(row, "RotationSpeed", ref so.RotationSpeed, rowNum);

            // ── Attack ──
            TrySetFloat(row, "AttackDamage", ref so.AttackDamage, rowNum);
            TrySetFloat(row, "AttackRange", ref so.AttackRange, rowNum);
            TrySetFloat(row, "AttackCooldown", ref so.AttackCooldown, rowNum);
            TrySetFloat(row, "AttackKnockback", ref so.AttackKnockback, rowNum);

            // ── Attack Phases ──
            TrySetFloat(row, "TelegraphDuration", ref so.TelegraphDuration, rowNum);
            TrySetFloat(row, "AttackActiveDuration", ref so.AttackActiveDuration, rowNum);
            TrySetFloat(row, "RecoveryDuration", ref so.RecoveryDuration, rowNum);

            // ── Ranged Attack ──
            TrySetProjectilePrefab(row, so, rowNum);
            TrySetFloat(row, "ProjectileSpeed", ref so.ProjectileSpeed, rowNum);
            TrySetFloat(row, "ProjectileDamage", ref so.ProjectileDamage, rowNum);
            TrySetFloat(row, "ProjectileKnockback", ref so.ProjectileKnockback, rowNum);
            TrySetFloat(row, "ProjectileLifetime", ref so.ProjectileLifetime, rowNum);
            TrySetInt(row, "ShotsPerBurst", ref so.ShotsPerBurst, rowNum);
            TrySetFloat(row, "BurstInterval", ref so.BurstInterval, rowNum);
            TrySetFloat(row, "PreferredRange", ref so.PreferredRange, rowNum);
            TrySetFloat(row, "RetreatRange", ref so.RetreatRange, rowNum);

            // ── Perception ──
            TrySetFloat(row, "SightRange", ref so.SightRange, rowNum);
            TrySetFloat(row, "SightAngle", ref so.SightAngle, rowNum);
            TrySetFloat(row, "HearingRange", ref so.HearingRange, rowNum);

            // ── Leash & Memory ──
            TrySetFloat(row, "LeashRange", ref so.LeashRange, rowNum);
            TrySetFloat(row, "MemoryDuration", ref so.MemoryDuration, rowNum);

            // ── Resistances ──
            TrySetFloat(row, "Resist_Physical", ref so.Resist_Physical, rowNum);
            TrySetFloat(row, "Resist_Fire", ref so.Resist_Fire, rowNum);
            TrySetFloat(row, "Resist_Ice", ref so.Resist_Ice, rowNum);
            TrySetFloat(row, "Resist_Lightning", ref so.Resist_Lightning, rowNum);
            TrySetFloat(row, "Resist_Void", ref so.Resist_Void, rowNum);

            // ── Rewards & Drops ──
            TrySetString(row, "DropTableID", ref so.DropTableID);

            // ── Spawn & Metadata ──
            TrySetString(row, "PlanetID", ref so.PlanetID);
            TrySetFloat(row, "SpawnWeight", ref so.SpawnWeight, rowNum);

            // ── Visuals ──
            TrySetFloat(row, "HitFlashDuration", ref so.HitFlashDuration, rowNum);
            TrySetBaseColor(row, so, rowNum);

            // ── Behavior Tags ──
            TrySetBehaviorTags(row, so);
        }

        // ════════════════════════════════════════════════════════════════
        //  TYPE-SAFE SETTERS (skip if empty → preserve SO default)
        // ════════════════════════════════════════════════════════════════

        private static void TrySetFloat(Dictionary<string, string> row, string col, ref float field, int rowNum)
        {
            if (!row.TryGetValue(col, out string val) || string.IsNullOrWhiteSpace(val)) return;
            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                field = parsed;
            else
                Debug.LogWarning($"[BestiaryImporter] Row {rowNum}: Cannot parse '{val}' as float for column '{col}'.");
        }

        private static void TrySetInt(Dictionary<string, string> row, string col, ref int field, int rowNum)
        {
            if (!row.TryGetValue(col, out string val) || string.IsNullOrWhiteSpace(val)) return;
            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                field = parsed;
            else
                Debug.LogWarning($"[BestiaryImporter] Row {rowNum}: Cannot parse '{val}' as int for column '{col}'.");
        }

        private static void TrySetString(Dictionary<string, string> row, string col, ref string field)
        {
            if (!row.TryGetValue(col, out string val) || string.IsNullOrWhiteSpace(val)) return;
            field = val;
        }

        // ── Special: ProjectilePrefab (path string → GameObject reference) ──
        private static void TrySetProjectilePrefab(Dictionary<string, string> row, EnemyStatsSO so, int rowNum)
        {
            if (!row.TryGetValue("ProjectilePrefab", out string val) || string.IsNullOrWhiteSpace(val))
                return;

            // Try multiple path patterns to find the prefab
            string[] candidates =
            {
                $"Assets/_Prefabs/{val}.prefab",
                $"Assets/_Prefabs/Enemies/{Path.GetFileName(val)}.prefab",
                val, // raw path (if already includes extension)
            };

            foreach (string candidate in candidates)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidate);
                if (prefab != null)
                {
                    so.ProjectilePrefab = prefab;
                    return;
                }
            }

            Debug.LogWarning(
                $"[BestiaryImporter] Row {rowNum}: ProjectilePrefab path '{val}' could not be resolved. " +
                "Tried: " + string.Join(", ", candidates));
        }

        // ── Special: BaseColor from 4 RGBA columns ──
        private static void TrySetBaseColor(Dictionary<string, string> row, EnemyStatsSO so, int rowNum)
        {
            bool hasR = row.TryGetValue("BaseColor_R", out string rStr) && !string.IsNullOrWhiteSpace(rStr);
            bool hasG = row.TryGetValue("BaseColor_G", out string gStr) && !string.IsNullOrWhiteSpace(gStr);
            bool hasB = row.TryGetValue("BaseColor_B", out string bStr) && !string.IsNullOrWhiteSpace(bStr);
            bool hasA = row.TryGetValue("BaseColor_A", out string aStr) && !string.IsNullOrWhiteSpace(aStr);

            // Only set if at least one component is provided
            if (!hasR && !hasG && !hasB && !hasA) return;

            Color c = so.BaseColor; // start from current/default

            if (hasR && float.TryParse(rStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float r))
                c.r = r;
            if (hasG && float.TryParse(gStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float g))
                c.g = g;
            if (hasB && float.TryParse(bStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
                c.b = b;
            if (hasA && float.TryParse(aStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
                c.a = a;

            so.BaseColor = c;
        }

        // ── Special: BehaviorTags from semicolon-separated string ──
        private static void TrySetBehaviorTags(Dictionary<string, string> row, EnemyStatsSO so)
        {
            if (!row.TryGetValue("BehaviorTags", out string val) || string.IsNullOrWhiteSpace(val))
                return;

            so.BehaviorTags = val
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();
        }

        // ════════════════════════════════════════════════════════════════
        //  CSV PARSING
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds a column-name → value dictionary from parallel header/value arrays.
        /// </summary>
        private static Dictionary<string, string> BuildRowDict(string[] headers, string[] values)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int len = Mathf.Min(headers.Length, values.Length);
            for (int i = 0; i < len; i++)
            {
                string key = headers[i].Trim();
                if (!string.IsNullOrEmpty(key))
                    dict[key] = values[i].Trim();
            }
            return dict;
        }

        /// <summary>
        /// Parses a single CSV line, handling quoted fields that may contain commas.
        /// Supports double-quote escaping ("").
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Check for escaped quote ("")
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++; // skip next quote
                        }
                        else
                        {
                            inQuotes = false; // end of quoted field
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        fields.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            fields.Add(sb.ToString()); // last field
            return fields.ToArray();
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        private static void EnsureFolder(string parent, string folderName)
        {
            string fullPath = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
