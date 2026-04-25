using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using ProjectArk.Combat;
using ProjectArk.UI;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// Editor menu tool that validates global uniqueness of <see cref="StarChartItemSO.DisplayName"/>
    /// across all <see cref="StarChartInventorySO"/> assets in the project.
    ///
    /// Rationale: <see cref="StarChartController.ExportToSaveData"/> / <see cref="StarChartController.ImportFromSaveData"/>
    /// and <see cref="StarChartInventorySO.FindCore"/> / <see cref="StarChartInventorySO.FindPrism"/> /
    /// <see cref="StarChartInventorySO.FindLightSail"/> / <see cref="StarChartInventorySO.FindSatellite"/>
    /// use DisplayName as the cross-save identifier. Duplicate DisplayNames cause silent resolver
    /// mis-lookup on load. CanonicalSpec §8.2 lists uniqueness as a constraint, but no runtime check exists.
    ///
    /// Run from: ProjectArk &gt; Validate StarChart Inventory
    /// </summary>
    public static class StarChartInventoryValidator
    {
        [MenuItem("ProjectArk/Validate StarChart Inventory")]
        public static void ValidateInventory()
        {
            var inventories = FindAllInventories();
            if (inventories.Count == 0)
            {
                Debug.LogWarning("[InventoryValidator] No StarChartInventorySO asset found in the project.");
                EditorUtility.DisplayDialog(
                    "StarChart Inventory Validation",
                    "No StarChartInventorySO asset found. Nothing to validate.",
                    "OK");
                return;
            }

            var report = new StringBuilder();
            bool anyFailure = false;

            foreach (var inv in inventories)
            {
                string path = AssetDatabase.GetAssetPath(inv);
                report.AppendLine($"— Inventory: {path}");

                if (inv.OwnedItems == null || inv.OwnedItems.Count == 0)
                {
                    report.AppendLine("    (empty, skipped)");
                    continue;
                }

                int itemCount = inv.OwnedItems.Count;
                int nullCount = 0;
                int blankNameCount = 0;

                // Group items by DisplayName; allow null items / blank names to be flagged separately.
                var byName = new Dictionary<string, List<StarChartItemSO>>();
                foreach (var item in inv.OwnedItems)
                {
                    if (item == null) { nullCount++; continue; }
                    string name = item.DisplayName;
                    if (string.IsNullOrWhiteSpace(name)) { blankNameCount++; continue; }

                    if (!byName.TryGetValue(name, out var list))
                    {
                        list = new List<StarChartItemSO>();
                        byName[name] = list;
                    }
                    list.Add(item);
                }

                // Find duplicates (count > 1 under same DisplayName)
                var duplicates = byName.Where(kv => kv.Value.Count > 1).ToList();

                if (nullCount == 0 && blankNameCount == 0 && duplicates.Count == 0)
                {
                    report.AppendLine($"    ✓ All {itemCount} items have unique DisplayName.");
                    continue;
                }

                anyFailure = true;

                if (nullCount > 0)
                    report.AppendLine($"    ✗ {nullCount} null item reference(s) in _ownedItems.");

                if (blankNameCount > 0)
                    report.AppendLine($"    ✗ {blankNameCount} item(s) with blank DisplayName.");

                foreach (var (name, items) in duplicates.Select(kv => (kv.Key, kv.Value)))
                {
                    string paths = string.Join(", ",
                        items.Select(it => System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(it))));
                    report.AppendLine($"    ✗ Duplicate DisplayName \"{name}\" used by: {paths}");
                }
            }

            string full = report.ToString();
            if (anyFailure)
            {
                Debug.LogError($"[InventoryValidator] ✗ Inventory validation FAILED:\n{full}");
                EditorUtility.DisplayDialog(
                    "StarChart Inventory Validation — FAILURES",
                    "Violations were found. See Console for details.\n\n" +
                    "Duplicate DisplayNames will cause save/load resolver mis-lookup (items silently " +
                    "replaced by the wrong SO). Fix by renaming duplicates or removing null entries.",
                    "OK");
            }
            else
            {
                Debug.Log($"[InventoryValidator] ✓ Inventory validation PASSED:\n{full}");
                EditorUtility.DisplayDialog(
                    "StarChart Inventory Validation",
                    $"✓ PASS.\n\nAll StarChartInventorySO assets have unique DisplayName per item.\n\n" +
                    $"Scanned {inventories.Count} inventory asset(s).",
                    "OK");
            }
        }

        private static List<StarChartInventorySO> FindAllInventories()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(StarChartInventorySO)}");
            var result = new List<StarChartInventorySO>(guids.Length);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<StarChartInventorySO>(path);
                if (so != null) result.Add(so);
            }
            return result;
        }
    }
}
