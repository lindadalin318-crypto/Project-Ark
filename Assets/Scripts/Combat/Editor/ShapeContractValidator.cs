using UnityEditor;
using UnityEngine;
using ProjectArk.Combat;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// C4: Editor menu tool to validate the Shape Contract for all registered ItemShape values.
    /// Run from: ProjectArk > Validate Shape Contract
    ///
    /// This check should be run whenever a new ItemShape enum value is added.
    /// A PASS means every shape has cells registered in ItemShapeHelper and all cells
    /// fit within the declared bounding box.
    /// </summary>
    public static class ShapeContractValidator
    {
        [MenuItem("ProjectArk/Validate Shape Contract")]
        public static void ValidateShapeContract()
        {
            string report = ItemShapeHelper.ValidateAllShapes();

            if (string.IsNullOrEmpty(report))
            {
                Debug.Log("[ShapeContract] ✓ All shapes PASS. Shape Contract C1-C4 is intact.");
                EditorUtility.DisplayDialog(
                    "Shape Contract Validation",
                    "✓ All shapes PASS.\n\nShape Contract C1-C4 is intact.\nEvery ItemShape has valid cell layouts.",
                    "OK");
            }
            else
            {
                Debug.LogError($"[ShapeContract] ✗ Shape Contract violations found:\n{report}");
                EditorUtility.DisplayDialog(
                    "Shape Contract Validation — FAILURES",
                    $"The following issues were found:\n\n{report}\n" +
                    "Fix them in ItemShapeHelper.cs before adding new shapes.",
                    "OK");
            }
        }
    }
}
