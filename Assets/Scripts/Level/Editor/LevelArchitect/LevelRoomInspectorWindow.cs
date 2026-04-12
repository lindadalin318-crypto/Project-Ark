using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Detached single-room authoring window for Level Architect.
    /// Mirrors the current Level Architect selection and exposes the full room / connection inspector.
    /// </summary>
    public sealed class LevelRoomInspectorWindow : EditorWindow
    {
        private const string WindowTitle = "Room Inspector";
        private const string MenuPath = "ProjectArk/Level/Authority/Room Inspector";

        private Vector2 _scroll;

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelRoomInspectorWindow>(WindowTitle);
            window.minSize = new Vector2(360f, 320f);
            window.Show();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(6f);

            var architectWindow = LevelArchitectWindow.Instance;
            if (architectWindow == null)
            {
                EditorGUILayout.HelpBox(
                    "未检测到 Level Architect 实例。请先打开 Level Architect，再使用 Detached Room Inspector。",
                    MessageType.Info);

                if (GUILayout.Button("Open Level Architect", GUILayout.Height(24f)))
                {
                    LevelArchitectWindow.ShowWindow();
                }

                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            architectWindow.DrawDetachedRoomInspectorWindow();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Detached Room Inspector", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "跟随 Level Architect 当前选择。这里负责单房 / 单连接细修；搜索、列表、批量维护仍留在主窗口。",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Level Architect", GUILayout.Height(20f)))
            {
                LevelArchitectWindow.ShowWindow();
            }

            var architectWindow = LevelArchitectWindow.Instance;
            using (new EditorGUI.DisabledScope(architectWindow == null || architectWindow.SelectedRoom == null))
            {
                if (GUILayout.Button("Focus Selected Room", GUILayout.Height(20f)) && architectWindow?.SelectedRoom != null)
                {
                    Selection.activeGameObject = architectWindow.SelectedRoom.gameObject;
                    SceneView.lastActiveSceneView?.FrameSelected();
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
