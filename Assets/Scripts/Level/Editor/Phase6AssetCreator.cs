using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// One-click editor tool for Phase 6 (World Clock & Dynamic Level) setup:
    /// 1. Create 4 WorldPhaseSO assets (Radiation/Calm/Storm/Silence)
    /// 2. Create scene manager GameObjects (WorldClock, WorldPhaseManager, AmbienceController)
    /// 3. Wire all references automatically
    /// 
    /// Menu: ProjectArk > Level > Phase 6: Create World Clock Assets
    /// Menu: ProjectArk > Level > Phase 6: Build Scene Managers
    /// Menu: ProjectArk > Level > Phase 6: Setup All (Assets + Scene)
    /// </summary>
    public static class Phase6AssetCreator
    {
        // ──────────────────── Paths ────────────────────

        private const string PHASE_DIR = "Assets/_Data/Level/WorldPhases";

        // ════════════════════════════════════════════════════════════════
        //  ONE-CLICK: EVERYTHING
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Level/Phase 6: Setup All (Assets + Scene)")]
        public static void SetupAll()
        {
            CreateWorldPhaseAssets();
            BuildPhase6SceneManagers();

            EditorUtility.DisplayDialog(
                "Phase 6 Setup Complete",
                "World Clock & Dynamic Level system is ready!\n\n" +
                "Created:\n" +
                "  • 4 WorldPhaseSO assets in _Data/Level/WorldPhases/\n" +
                "  • WorldClock manager in scene\n" +
                "  • WorldPhaseManager manager in scene (with 4 phases wired)\n" +
                "  • AmbienceController manager in scene\n\n" +
                "Next steps:\n" +
                "1. (Optional) Assign PhaseBGM AudioClips on each WorldPhaseSO\n" +
                "2. (Optional) Assign a URP Volume to AmbienceController._postProcessVolume\n" +
                "3. (Optional) Create ParticleSystems for each phase and assign to AmbienceController._phaseParticles\n" +
                "4. Enter Play Mode and observe phase cycling in Console logs\n" +
                "5. Adjust WorldClock._cycleDuration for faster/slower testing (e.g., 60s for 1-minute cycles)",
                "OK"
            );
        }

        // ════════════════════════════════════════════════════════════════
        //  WORLD PHASE ASSETS
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Level/Phase 6: Create World Clock Assets")]
        public static void CreateWorldPhaseAssets()
        {
            EnsureFolder("Assets/_Data", "Level");
            EnsureFolder("Assets/_Data/Level", "WorldPhases");

            // GDD 定义的 4 个阶段（归一化时间 0..1 对应一个完整周期）
            // 辐射潮 00:00~05:00 → 0.000~0.208
            // 平静期 05:00~12:00 → 0.208~0.500
            // 风暴期 12:00~18:00 → 0.500~0.750
            // 寂静时 18:00~24:00 → 0.750~1.000 (wraps to 0)

            CreateWorldPhaseSO(
                fileName: "Phase_0_RadiationTide",
                phaseName: "Radiation Tide",
                description: "辐射潮涌。某些区域出现辐射伤害，NPC 躲藏，特殊敌人出没。",
                startTime: 0.000f,
                endTime: 0.208f,
                ambientColor: new Color(1f, 0.6f, 0.6f, 1f), // 偏红
                applyLowPass: false,
                lowPassHz: 22000f,
                enemyDamageMult: 1.0f,
                enemyHealthMult: 1.0f,
                hiddenPaths: false
            );

            CreateWorldPhaseSO(
                fileName: "Phase_1_CalmPeriod",
                phaseName: "Calm Period",
                description: "平静期。最佳探索和交易窗口。NPC 出来摆摊，某些定时门打开。",
                startTime: 0.208f,
                endTime: 0.500f,
                ambientColor: new Color(1f, 1f, 0.95f, 1f), // 暖白
                applyLowPass: false,
                lowPassHz: 22000f,
                enemyDamageMult: 0.9f,
                enemyHealthMult: 0.9f,
                hiddenPaths: false
            );

            CreateWorldPhaseSO(
                fileName: "Phase_2_StormPeriod",
                phaseName: "Storm Period",
                description: "风暴期。敌人增强，特殊掉落率提升。视觉噪点加重，BGM 紧张。",
                startTime: 0.500f,
                endTime: 0.750f,
                ambientColor: new Color(0.7f, 0.7f, 0.85f, 1f), // 偏蓝灰
                applyLowPass: true,
                lowPassHz: 800f,
                enemyDamageMult: 1.3f,
                enemyHealthMult: 1.2f,
                hiddenPaths: false
            );

            CreateWorldPhaseSO(
                fileName: "Phase_3_SilentHour",
                phaseName: "Silent Hour",
                description: "寂静时。低能见度但隐藏通道出现，稀有 NPC 只在此时段出没。",
                startTime: 0.750f,
                endTime: 1.000f,
                ambientColor: new Color(0.6f, 0.55f, 0.75f, 1f), // 偏紫暗
                applyLowPass: false,
                lowPassHz: 22000f,
                enemyDamageMult: 1.0f,
                enemyHealthMult: 1.0f,
                hiddenPaths: true
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase6AssetCreator] 4 WorldPhaseSO assets created in " + PHASE_DIR);
        }

        private static void CreateWorldPhaseSO(
            string fileName, string phaseName, string description,
            float startTime, float endTime, Color ambientColor,
            bool applyLowPass, float lowPassHz,
            float enemyDamageMult, float enemyHealthMult, bool hiddenPaths)
        {
            string path = $"{PHASE_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<WorldPhaseSO>(path) != null)
            {
                Debug.Log($"[Phase6AssetCreator] Already exists: {path}");
                return;
            }

            var so = ScriptableObject.CreateInstance<WorldPhaseSO>();

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_phaseName").stringValue = phaseName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_startTime").floatValue = startTime;
            serialized.FindProperty("_endTime").floatValue = endTime;
            serialized.FindProperty("_ambientColor").colorValue = ambientColor;
            serialized.FindProperty("_applyLowPassFilter").boolValue = applyLowPass;
            serialized.FindProperty("_lowPassCutoffHz").floatValue = lowPassHz;
            serialized.FindProperty("_enemyDamageMultiplier").floatValue = enemyDamageMult;
            serialized.FindProperty("_enemyHealthMultiplier").floatValue = enemyHealthMult;
            serialized.FindProperty("_hiddenPathsVisible").boolValue = hiddenPaths;
            // _phaseBGM left null — assign manually when audio is ready
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[Phase6AssetCreator] Created: {path}");
        }

        // ════════════════════════════════════════════════════════════════
        //  SCENE MANAGERS
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Level/Phase 6: Build Scene Managers")]
        public static void BuildPhase6SceneManagers()
        {
            // Find or create a parent "Managers" GO
            var managersGO = GameObject.Find("Managers");
            if (managersGO == null)
            {
                managersGO = new GameObject("Managers");
                Undo.RegisterCreatedObjectUndo(managersGO, "Create Managers");
            }

            // ── WorldClock ──
            CreateManagerComponent<WorldClock>(managersGO, "WorldClock", (go, comp) =>
            {
                var so = new SerializedObject(comp);
                so.FindProperty("_cycleDuration").floatValue = 1200f; // 20 minutes
                so.FindProperty("_timeSpeed").floatValue = 1f;
                so.FindProperty("_startTimeNormalized").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log("[Phase6AssetCreator] WorldClock configured: 1200s cycle, speed 1x");
            });

            // ── WorldPhaseManager ──
            CreateManagerComponent<WorldPhaseManager>(managersGO, "WorldPhaseManager", (go, comp) =>
            {
                // Auto-wire the 4 WorldPhaseSO assets
                var phase0 = AssetDatabase.LoadAssetAtPath<WorldPhaseSO>($"{PHASE_DIR}/Phase_0_RadiationTide.asset");
                var phase1 = AssetDatabase.LoadAssetAtPath<WorldPhaseSO>($"{PHASE_DIR}/Phase_1_CalmPeriod.asset");
                var phase2 = AssetDatabase.LoadAssetAtPath<WorldPhaseSO>($"{PHASE_DIR}/Phase_2_StormPeriod.asset");
                var phase3 = AssetDatabase.LoadAssetAtPath<WorldPhaseSO>($"{PHASE_DIR}/Phase_3_SilentHour.asset");

                var so = new SerializedObject(comp);
                var phasesProp = so.FindProperty("_phases");
                int count = 0;
                if (phase0 != null) count++;
                if (phase1 != null) count++;
                if (phase2 != null) count++;
                if (phase3 != null) count++;

                phasesProp.arraySize = count;
                int idx = 0;
                if (phase0 != null) phasesProp.GetArrayElementAtIndex(idx++).objectReferenceValue = phase0;
                if (phase1 != null) phasesProp.GetArrayElementAtIndex(idx++).objectReferenceValue = phase1;
                if (phase2 != null) phasesProp.GetArrayElementAtIndex(idx++).objectReferenceValue = phase2;
                if (phase3 != null) phasesProp.GetArrayElementAtIndex(idx++).objectReferenceValue = phase3;

                so.ApplyModifiedPropertiesWithoutUndo();

                if (count == 4)
                    Debug.Log("[Phase6AssetCreator] WorldPhaseManager configured with 4 phases");
                else
                    Debug.LogWarning($"[Phase6AssetCreator] WorldPhaseManager: only {count}/4 phases found. Run 'Create World Clock Assets' first.");
            });

            // ── AmbienceController ──
            CreateManagerComponent<AmbienceController>(managersGO, "AmbienceController", (go, comp) =>
            {
                // Leave Volume/particles references null — user assigns in Inspector
                Debug.Log("[Phase6AssetCreator] AmbienceController added. Assign _postProcessVolume and _phaseParticles manually.");
            });

            EditorUtility.SetDirty(managersGO);

            Debug.Log("[Phase6AssetCreator] Phase 6 scene managers created on 'Managers' GameObject.");
        }

        // ──────────────────── Helpers ────────────────────

        private static void CreateManagerComponent<T>(
            GameObject parent, string childName,
            System.Action<GameObject, T> configure) where T : Component
        {
            // Check if already exists as child
            var existing = parent.transform.Find(childName);
            if (existing != null)
            {
                var existingComp = existing.GetComponent<T>();
                if (existingComp != null)
                {
                    Debug.Log($"[Phase6AssetCreator] '{childName}' already exists on '{parent.name}'. Skipping.");
                    return;
                }
            }

            // Also check if the component already exists on parent directly
            var parentComp = parent.GetComponentInChildren<T>(true);
            if (parentComp != null)
            {
                Debug.Log($"[Phase6AssetCreator] {typeof(T).Name} already exists in scene. Skipping.");
                return;
            }

            // Create as child of parent
            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(go, $"Create {childName}");

            var comp = go.AddComponent<T>();
            configure?.Invoke(go, comp);
        }

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
