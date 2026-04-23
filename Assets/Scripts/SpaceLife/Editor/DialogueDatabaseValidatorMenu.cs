using System.Collections.Generic;
using ProjectArk.SpaceLife.Dialogue;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.SpaceLife.Editor
{
    /// <summary>
    /// Editor menu entry for authoring-time validation of a <see cref="DialogueDatabaseSO"/>.
    /// This is the first line of defense against authoring typos before Phase 3 content authoring begins
    /// (see Master Plan §5.2 P3 and Implement_rules.md §12.6 R8 "禁止 Silent No-Op").
    /// </summary>
    public static class DialogueDatabaseValidatorMenu
    {
        /// <summary>
        /// Menu: ProjectArk &gt; Space Life &gt; Validate Dialogue Database.
        /// Validates either the selected <see cref="DialogueDatabaseSO"/> or, when nothing relevant is selected,
        /// every database asset found in the project. Reports all errors to the Console (fail-fast disabled).
        /// </summary>
        [MenuItem("ProjectArk/Space Life/Validate Dialogue Database", priority = 80)]
        public static void ValidateSelectedOrAll()
        {
            DialogueDatabaseSO[] targets = CollectTargets();

            if (targets == null || targets.Length == 0)
            {
                Debug.LogWarning("[DialogueDatabaseValidator] No DialogueDatabaseSO asset found in the project (or in the current selection). Create one via 'Project Ark/Space Life/Dialogue Database' first.");
                return;
            }

            int totalErrors = 0;
            int totalDatabases = 0;

            for (int i = 0; i < targets.Length; i++)
            {
                DialogueDatabaseSO database = targets[i];
                if (database == null)
                {
                    continue;
                }

                totalDatabases++;
                List<string> errors = database.ValidateDatabase();
                string path = AssetDatabase.GetAssetPath(database);

                if (errors == null || errors.Count == 0)
                {
                    Debug.Log($"[DialogueDatabaseValidator] OK — '{database.name}' ({path}) passed all checks.", database);
                    continue;
                }

                totalErrors += errors.Count;
                for (int e = 0; e < errors.Count; e++)
                {
                    Debug.LogError($"[DialogueDatabaseValidator] '{database.name}': {errors[e]}", database);
                }
            }

            if (totalErrors == 0)
            {
                Debug.Log($"[DialogueDatabaseValidator] Summary: validated {totalDatabases} database(s), no errors.");
            }
            else
            {
                Debug.LogError($"[DialogueDatabaseValidator] Summary: validated {totalDatabases} database(s), total errors: {totalErrors}. See messages above.");
            }
        }

        private static DialogueDatabaseSO[] CollectTargets()
        {
            // Priority 1: explicit selection (one or more DialogueDatabaseSO assets in the Project window).
            Object[] selected = Selection.GetFiltered(typeof(DialogueDatabaseSO), SelectionMode.Assets);
            if (selected != null && selected.Length > 0)
            {
                var fromSelection = new List<DialogueDatabaseSO>(selected.Length);
                for (int i = 0; i < selected.Length; i++)
                {
                    if (selected[i] is DialogueDatabaseSO db && db != null)
                    {
                        fromSelection.Add(db);
                    }
                }

                if (fromSelection.Count > 0)
                {
                    return fromSelection.ToArray();
                }
            }

            // Priority 2: scan every DialogueDatabaseSO in the project.
            string[] guids = AssetDatabase.FindAssets("t:DialogueDatabaseSO");
            if (guids == null || guids.Length == 0)
            {
                return System.Array.Empty<DialogueDatabaseSO>();
            }

            var all = new List<DialogueDatabaseSO>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                DialogueDatabaseSO db = AssetDatabase.LoadAssetAtPath<DialogueDatabaseSO>(path);
                if (db != null)
                {
                    all.Add(db);
                }
            }

            return all.ToArray();
        }
    }
}
