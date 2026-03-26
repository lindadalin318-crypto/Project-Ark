using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Custom Inspector for WorldGraphSO.
    /// Adds an "Open in Graph Editor" button and shows summary statistics.
    /// </summary>
    [CustomEditor(typeof(WorldGraphSO))]
    public class WorldGraphSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = (WorldGraphSO)target;

            // Open in Graph Editor button
            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("Open in World Graph Editor", GUILayout.Height(28)))
            {
                WorldGraphEditorWindow.OpenWithGraph(graph);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            // Summary stats
            EditorGUILayout.HelpBox(
                $"Graph: {graph.GraphName}\n" +
                $"Rooms: {graph.RoomCount}\n" +
                $"Connections: {graph.ConnectionCount}",
                MessageType.Info);

            // Validation quick check
            if (graph.RoomCount > 0)
            {
                var isolated = graph.GetIsolatedRoomIDs();
                if (isolated.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"⚠ {isolated.Count} isolated room(s) with no connections:\n" +
                        string.Join(", ", isolated),
                        MessageType.Warning);
                }
            }

            EditorGUILayout.Space(4);

            // Default inspector
            DrawDefaultInspector();
        }
    }
}
