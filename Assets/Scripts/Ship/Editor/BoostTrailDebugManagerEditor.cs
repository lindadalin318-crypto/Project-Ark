#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Custom Inspector for BoostTrailDebugManager.
    /// This is a preview-only debug surface, not an authority entry for prefab or scene repair.
    /// Play Mode 预览只作用于当前选中的 live scene instance，不再从 prefab Inspector 代理到运行时实例。
    /// </summary>
    [CustomEditor(typeof(BoostTrailDebugManager))]
    public class BoostTrailDebugManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var inspectedTarget = (BoostTrailDebugManager)target;
            bool isLiveSceneObject = IsLiveSceneObject(inspectedTarget);

            serializedObject.Update();

            SerializedProperty boostTrailViewProp = serializedObject.FindProperty("_boostTrailView");
            SerializedProperty enableDebugProp = serializedObject.FindProperty("_enableInspectorDebug");
            SerializedProperty debugModeProp = serializedObject.FindProperty("_debugMode");
            SerializedProperty previewIntensityProp = serializedObject.FindProperty("_previewIntensity");
            SerializedProperty soloLayerProp = serializedObject.FindProperty("_soloLayer");
            SerializedProperty showMainTrailProp = serializedObject.FindProperty("_showMainTrail");
            SerializedProperty showFlameTrailProp = serializedObject.FindProperty("_showFlameTrail");
            SerializedProperty showFlameCoreProp = serializedObject.FindProperty("_showFlameCore");
            SerializedProperty showEmberTrailProp = serializedObject.FindProperty("_showEmberTrail");
            SerializedProperty showEmberSparksProp = serializedObject.FindProperty("_showEmberSparks");
            SerializedProperty showEnergyLayer2Prop = serializedObject.FindProperty("_showEnergyLayer2");
            SerializedProperty showEnergyLayer3Prop = serializedObject.FindProperty("_showEnergyLayer3");
            SerializedProperty showActivationHaloProp = serializedObject.FindProperty("_showActivationHalo");
            SerializedProperty showBloomProp = serializedObject.FindProperty("_showBloom");

            EditorGUILayout.HelpBox(
                "用途：在 Play Mode 下隔离 `MainTrail / FlameTrail / EmberTrail / EnergyLayer2 / EnergyLayer3 / Halo / Bloom`。\n" +
                "该面板现在只做显式预览，不再自动代理到 runtime，也不会被动接管 live chain。",
                MessageType.Info);

            if (Application.isPlaying && !isLiveSceneObject)
            {
                EditorGUILayout.HelpBox(
                    "当前选中的不是 live scene instance。下面字段只会修改当前对象本身；若要驱动运行时画面，请直接选中场景里的 `BoostTrailRoot`。",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(boostTrailViewProp);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Inspector Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableDebugProp);

            bool inspectorDebugEnabled = enableDebugProp != null && enableDebugProp.boolValue;
            bool forceSustainPreview = debugModeProp != null &&
                debugModeProp.enumValueIndex == (int)BoostTrailDebugManager.DebugMode.ForceSustainPreview;

            using (new EditorGUI.DisabledScope(!inspectorDebugEnabled))
            {
                EditorGUILayout.PropertyField(debugModeProp);

                if (forceSustainPreview)
                    EditorGUILayout.PropertyField(previewIntensityProp);

                EditorGUILayout.PropertyField(soloLayerProp);

                bool soloModeActive = soloLayerProp != null &&
                    soloLayerProp.enumValueIndex != (int)BoostTrailDebugManager.SoloLayer.None;

                if (soloModeActive)
                {
                    EditorGUILayout.HelpBox(
                        "当前 `Solo Layer` 已接管下面的逐层开关；若要恢复手动多选，请把 `Solo Layer` 切回 `None`。",
                        MessageType.None);
                }

                using (new EditorGUI.DisabledScope(soloModeActive))
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Layer Visibility", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(showMainTrailProp);
                    EditorGUILayout.PropertyField(showFlameTrailProp);
                    EditorGUILayout.PropertyField(showFlameCoreProp);
                    EditorGUILayout.PropertyField(showEmberTrailProp);
                    EditorGUILayout.PropertyField(showEmberSparksProp);
                    EditorGUILayout.PropertyField(showEnergyLayer2Prop);
                    EditorGUILayout.PropertyField(showEnergyLayer3Prop);
                    EditorGUILayout.PropertyField(showActivationHaloProp);
                    EditorGUILayout.PropertyField(showBloomProp);
                }
            }

            serializedObject.ApplyModifiedProperties();

            bool canDriveRuntime = Application.isPlaying && isLiveSceneObject;
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后，下面这些预览按钮才会真正驱动 BoostTrail 现役链路。", MessageType.Warning);
                return;
            }

            if (!isLiveSceneObject)
            {
                EditorGUILayout.HelpBox(
                    "当前对象不是 live scene instance，已禁止 runtime 预览按钮，避免从 prefab Inspector 间接接管运行时实例。",
                    MessageType.Warning);
            }
            else if (!inspectorDebugEnabled)
            {
                EditorGUILayout.HelpBox(
                    "如果你只改了 `Show Main Trail` 之类的勾选，但没打开 `Enable Inspector Debug`，只有显式预览按钮会驱动当前链路。",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    forceSustainPreview
                        ? "当前是 ForceSustainPreview：适合静态检查持续层。若要看真实起手 / 退场 / burst 节奏，请切到 ObserveRuntime。"
                        : "当前是 ObserveRuntime：适合看真实 Boost 起手 / 退场 / burst 节奏，同时保留 Inspector 分层遮罩。",
                    MessageType.None);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Quick Preview", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!canDriveRuntime))
            {
                if (GUILayout.Button("Apply Current Preview Mask"))
                    inspectedTarget.ApplyInspectorDebugNow();

                if (GUILayout.Button("Reset Preview"))
                    inspectedTarget.ResetPreview();

                using (new EditorGUI.DisabledScope(inspectorDebugEnabled && forceSustainPreview))
                {
                    if (GUILayout.Button("Preview Boost Start"))
                        inspectedTarget.PreviewBoostStart();

                    if (GUILayout.Button("Preview Boost End"))
                        inspectedTarget.PreviewBoostEnd();

                    if (GUILayout.Button("Preview FlameCore Burst"))
                        inspectedTarget.PreviewFlameCoreBurst();

                    if (GUILayout.Button("Preview EmberSparks Burst"))
                        inspectedTarget.PreviewEmberSparksBurst();

                    if (GUILayout.Button("Preview Activation Halo"))
                        inspectedTarget.PreviewActivationHaloBurst();

                    if (GUILayout.Button("Preview Bloom Burst"))
                        inspectedTarget.PreviewBloomBurst();
                }
            }
        }

        private static bool IsLiveSceneObject(BoostTrailDebugManager debugManager)
        {
            return debugManager != null &&
                debugManager.gameObject.scene.IsValid() &&
                debugManager.gameObject.scene.isLoaded &&
                debugManager.gameObject.activeInHierarchy;
        }
    }
}
#endif
