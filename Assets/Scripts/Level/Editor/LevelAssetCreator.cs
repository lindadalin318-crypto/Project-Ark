using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// One-click editor utility to create all Level Phase 2 ScriptableObject assets:
    /// CheckpointSOs, KeyItemSOs, and WorldProgressStageSOs.
    /// 
    /// Menu: ProjectArk > Level > Create Phase 2 Assets (All)
    /// 
    /// Idempotent — skips any asset that already exists.
    /// </summary>
    public static class LevelAssetCreator
    {
        // ──────────────────── Paths ────────────────────

        private const string CHECKPOINT_DIR = "Assets/_Data/Level/Checkpoints";
        private const string KEY_DIR = "Assets/_Data/Level/Keys";
        private const string STAGE_DIR = "Assets/_Data/Level/WorldStages";

        // ════════════════════════════════════════════════════════════════
        //  ONE-CLICK: ALL PHASE 2 ASSETS
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Level/Create Phase 2 Assets (All)")]
        public static void CreateAllPhase2Assets()
        {
            CreateCheckpointAssets();
            CreateKeyItemAssets();
            CreateWorldProgressStageAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Level Phase 2 Assets Created",
                "All ScriptableObject assets have been created (or confirmed existing).\n\n" +
                "── Checkpoints ──\n" +
                $"  {CHECKPOINT_DIR}/\n" +
                "  • Checkpoint_Start.asset\n" +
                "  • Checkpoint_Corridor.asset\n" +
                "  • Checkpoint_Combat.asset\n\n" +
                "── Key Items ──\n" +
                $"  {KEY_DIR}/\n" +
                "  • Key_AccessAlpha.asset\n" +
                "  • Key_BossGate.asset\n\n" +
                "── World Stages ──\n" +
                $"  {STAGE_DIR}/\n" +
                "  • Stage_0_Initial.asset\n" +
                "  • Stage_1_PostGuardian.asset\n\n" +
                "Next steps:\n" +
                "1. Drag CheckpointSOs onto Checkpoint components in the scene\n" +
                "2. Drag KeyItemSOs onto KeyPickup and Lock components\n" +
                "3. Drag WorldProgressStageSOs into WorldProgressManager's Stages array",
                "OK"
            );
        }

        // ════════════════════════════════════════════════════════════════
        //  CHECKPOINTS
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Level/Create Checkpoint Assets")]
        public static void CreateCheckpointAssets()
        {
            EnsureFolder("Assets/_Data", "Level");
            EnsureFolder("Assets/_Data/Level", "Checkpoints");

            // ── Checkpoint: Start ──
            CreateCheckpointSO(
                fileName: "Checkpoint_Start",
                checkpointID: "checkpoint_start",
                displayName: "起始锚点",
                restoreHP: true,
                restoreHeat: true
            );

            // ── Checkpoint: Corridor ──
            CreateCheckpointSO(
                fileName: "Checkpoint_Corridor",
                checkpointID: "checkpoint_corridor",
                displayName: "走廊锚点",
                restoreHP: true,
                restoreHeat: true
            );

            // ── Checkpoint: Combat Room ──
            CreateCheckpointSO(
                fileName: "Checkpoint_Combat",
                checkpointID: "checkpoint_combat",
                displayName: "战斗区锚点",
                restoreHP: true,
                restoreHeat: false  // 战斗区不恢复热量，增加紧张感
            );

            Debug.Log("[LevelAssetCreator] Checkpoint assets done.");
        }

        private static void CreateCheckpointSO(
            string fileName, string checkpointID, string displayName,
            bool restoreHP, bool restoreHeat)
        {
            string path = $"{CHECKPOINT_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<CheckpointSO>(path) != null)
            {
                Debug.Log($"[LevelAssetCreator] Already exists: {path}");
                return;
            }

            var so = ScriptableObject.CreateInstance<CheckpointSO>();

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_checkpointID").stringValue = checkpointID;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_restoreHP").boolValue = restoreHP;
            serialized.FindProperty("_restoreHeat").boolValue = restoreHeat;
            // _activationSFX left null — assign manually when audio is ready
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[LevelAssetCreator] Created: {path}");
        }

        // ════════════════════════════════════════════════════════════════
        //  KEY ITEMS
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Level/Create Key Item Assets")]
        public static void CreateKeyItemAssets()
        {
            EnsureFolder("Assets/_Data", "Level");
            EnsureFolder("Assets/_Data/Level", "Keys");

            // ── Key: Access Alpha ──
            CreateKeyItemSO(
                fileName: "Key_AccessAlpha",
                keyID: "access_alpha",
                displayName: "Alpha 通行证",
                description: "打开通往走廊的门。表面刻着模糊的光谱标记。"
            );

            // ── Key: Boss Gate ──
            CreateKeyItemSO(
                fileName: "Key_BossGate",
                keyID: "boss_gate",
                displayName: "核心门钥",
                description: "启动 Boss 区域的能量门。散发着不稳定的辐射。"
            );

            Debug.Log("[LevelAssetCreator] Key Item assets done.");
        }

        private static void CreateKeyItemSO(
            string fileName, string keyID, string displayName, string description)
        {
            string path = $"{KEY_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<KeyItemSO>(path) != null)
            {
                Debug.Log($"[LevelAssetCreator] Already exists: {path}");
                return;
            }

            var so = ScriptableObject.CreateInstance<KeyItemSO>();

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_keyID").stringValue = keyID;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = description;
            // _icon left null — assign manually when art is ready
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[LevelAssetCreator] Created: {path}");
        }

        // ════════════════════════════════════════════════════════════════
        //  WORLD PROGRESS STAGES
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Level/Create World Progress Stage Assets")]
        public static void CreateWorldProgressStageAssets()
        {
            EnsureFolder("Assets/_Data", "Level");
            EnsureFolder("Assets/_Data/Level", "WorldStages");

            // ── Stage 0: Initial ──
            CreateWorldStageSO(
                fileName: "Stage_0_Initial",
                stageIndex: 0,
                stageName: "Initial",
                requiredBossIDs: new string[0],
                unlockDoorIDs: new string[0]
            );

            // ── Stage 1: Post-Guardian ──
            CreateWorldStageSO(
                fileName: "Stage_1_PostGuardian",
                stageIndex: 1,
                stageName: "Post-Guardian",
                requiredBossIDs: new string[] { "boss_guardian" },
                unlockDoorIDs: new string[] { "door_to_core" }
            );

            Debug.Log("[LevelAssetCreator] World Progress Stage assets done.");
        }

        private static void CreateWorldStageSO(
            string fileName, int stageIndex, string stageName,
            string[] requiredBossIDs, string[] unlockDoorIDs)
        {
            string path = $"{STAGE_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<WorldProgressStageSO>(path) != null)
            {
                Debug.Log($"[LevelAssetCreator] Already exists: {path}");
                return;
            }

            var so = ScriptableObject.CreateInstance<WorldProgressStageSO>();

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_stageIndex").intValue = stageIndex;
            serialized.FindProperty("_stageName").stringValue = stageName;

            // Set RequiredBossIDs array
            var bossArrayProp = serialized.FindProperty("_requiredBossIDs");
            bossArrayProp.arraySize = requiredBossIDs.Length;
            for (int i = 0; i < requiredBossIDs.Length; i++)
            {
                bossArrayProp.GetArrayElementAtIndex(i).stringValue = requiredBossIDs[i];
            }

            // Set UnlockDoorIDs array
            var doorArrayProp = serialized.FindProperty("_unlockDoorIDs");
            doorArrayProp.arraySize = unlockDoorIDs.Length;
            for (int i = 0; i < unlockDoorIDs.Length; i++)
            {
                doorArrayProp.GetArrayElementAtIndex(i).stringValue = unlockDoorIDs[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[LevelAssetCreator] Created: {path}");
        }

        // ──────────────────── Helpers ────────────────────

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
