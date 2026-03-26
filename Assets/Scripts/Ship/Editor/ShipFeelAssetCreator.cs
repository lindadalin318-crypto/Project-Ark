using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// One-click editor utility to create Ship Feel Enhancement SO assets:
    /// ShipStatsSO (with full movement/dash/hit-feedback params) and ShipJuiceSettingsSO.
    ///
    /// Menu: ProjectArk > Ship > Create Ship Feel Assets (All)
    ///
    /// Idempotent — skips any asset that already exists,
    /// or offers to update the existing ShipStatsSO with new default fields.
    /// </summary>
    public static class ShipFeelAssetCreator
    {
        // ──────────────────── Paths ────────────────────

        private const string SHIP_DATA_DIR = "Assets/_Data/Ship";
        private const string STATS_ASSET_NAME = "DefaultShipStats";
        private const string JUICE_ASSET_NAME = "DefaultShipJuiceSettings";

        // ════════════════════════════════════════════════════════════════
        //  ONE-CLICK: ALL SHIP FEEL ASSETS
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Ship/Create Ship Feel Assets (All)")]
        public static void CreateAllShipFeelAssets()
        {
            CreateOrUpdateShipStats();
            CreateShipJuiceSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Ship Feel Assets Created",
                "All Ship Feel Enhancement SO assets have been created (or confirmed existing).\n\n" +
                $"── Ship Stats ──\n" +
                $"  {SHIP_DATA_DIR}/{STATS_ASSET_NAME}.asset\n" +
                $"  (Movement + Curves + Dash + Hit Feedback)\n\n" +
                $"── Juice Settings ──\n" +
                $"  {SHIP_DATA_DIR}/{JUICE_ASSET_NAME}.asset\n" +
                $"  (Tilt + Squash/Stretch + After-Image)\n\n" +
                "Next steps:\n" +
                "1. Drag ShipStatsSO onto ShipMotor / ShipDash / ShipHealth components\n" +
                "2. Drag ShipJuiceSettingsSO onto ShipVisualJuice / DashAfterImageSpawner\n" +
                "3. Tweak values in Inspector during Play Mode for live tuning",
                "OK"
            );
        }

        // ════════════════════════════════════════════════════════════════
        //  SHIP STATS SO (Create or Update)
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Ship/Create Ship Stats Asset")]
        public static void CreateOrUpdateShipStats()
        {
            EnsureFolder("Assets/_Data", "Ship");

            string path = $"{SHIP_DATA_DIR}/{STATS_ASSET_NAME}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ShipStatsSO>(path);

            if (existing != null)
            {
                // Asset exists — update new fields while preserving old values
                UpdateShipStatsDefaults(existing);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[ShipFeelAssetCreator] Updated existing: {path}");
                return;
            }

            // Create new asset with all defaults
            var so = ScriptableObject.CreateInstance<ShipStatsSO>();
            var serialized = new SerializedObject(so);

            // Rotation (mass=1, twin-stick 世界方向移动 + 角加速度旋转)
            serialized.FindProperty("_angularAcceleration").floatValue = 800f;
            serialized.FindProperty("_maxRotationSpeed").floatValue = 360f;
            serialized.FindProperty("_angularDrag").floatValue = 0f;

            // Movement (mass=1: F=ma → 加速度=ForwardAcceleration)
            serialized.FindProperty("_forwardAcceleration").floatValue = 20f;
            serialized.FindProperty("_maxSpeed").floatValue = 8f;
            serialized.FindProperty("_linearDrag").floatValue = 3f;

            // Boost (状态切换: GG IsBoostState 对齐)
            serialized.FindProperty("_boostLinearDrag").floatValue = 2.5f;
            serialized.FindProperty("_boostMaxSpeed").floatValue = 9f;
            serialized.FindProperty("_boostAngularAcceleration").floatValue = 400f;
            serialized.FindProperty("_boostDuration").floatValue = 0.2f;
            serialized.FindProperty("_boostCooldown").floatValue = 1.0f;
            serialized.FindProperty("_boostBufferWindow").floatValue = 0.15f;

            // Dash (mass=1: impulse = 速度变化量)
            serialized.FindProperty("_dashImpulse").floatValue = 12f;
            serialized.FindProperty("_dashIFrameDuration").floatValue = 0.15f;
            serialized.FindProperty("_dashCooldown").floatValue = 0.5f;
            serialized.FindProperty("_dashBufferWindow").floatValue = 0.15f;

            // Survival
            serialized.FindProperty("_maxHP").floatValue = 100f;
            serialized.FindProperty("_hitFlashDuration").floatValue = 0.1f;

            // Hit Feedback
            serialized.FindProperty("_hitStopDuration").floatValue = 0.05f;
            serialized.FindProperty("_iFrameDuration").floatValue = 1.0f;
            serialized.FindProperty("_iFrameBlinkInterval").floatValue = 0.1f;
            serialized.FindProperty("_screenShakeBaseIntensity").floatValue = 0.3f;
            serialized.FindProperty("_screenShakeDamageScale").floatValue = 0.01f;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[ShipFeelAssetCreator] Created: {path}");
        }

        /// <summary>
        /// Updates an existing ShipStatsSO asset to fill in new fields that
        /// may have been added after the original asset was created.
        /// Only writes fields that still hold Unity's default zero/null values
        /// (i.e. fields not yet serialized in the .asset file).
        /// </summary>
        private static void UpdateShipStatsDefaults(ShipStatsSO existing)
        {
            var serialized = new SerializedObject(existing);

            // Rotation (mass=1)
            SetIfDefault(serialized, "_angularAcceleration", 800f);
            SetIfDefault(serialized, "_maxRotationSpeed", 360f);
            // angularDrag 保持 0，不用 SetIfDefault（0 就是期望值）

            // Movement (mass=1)
            SetIfDefault(serialized, "_forwardAcceleration", 20f);
            SetIfDefault(serialized, "_maxSpeed", 8f);
            SetIfDefault(serialized, "_linearDrag", 3f);

            // Boost (状态切换模型)
            SetIfDefault(serialized, "_boostLinearDrag", 2.5f);
            SetIfDefault(serialized, "_boostMaxSpeed", 9f);
            SetIfDefault(serialized, "_boostAngularAcceleration", 400f);
            SetIfDefault(serialized, "_boostDuration", 0.2f);
            SetIfDefault(serialized, "_boostCooldown", 1.0f);
            SetIfDefault(serialized, "_boostBufferWindow", 0.15f);

            // Dash (mass=1)
            SetIfDefault(serialized, "_dashImpulse", 12f);
            SetIfDefault(serialized, "_dashIFrameDuration", 0.15f);
            SetIfDefault(serialized, "_dashCooldown", 0.5f);
            SetIfDefault(serialized, "_dashBufferWindow", 0.15f);

            // Hit Feedback
            SetIfDefault(serialized, "_hitStopDuration", 0.05f);
            SetIfDefault(serialized, "_iFrameDuration", 1.0f);
            SetIfDefault(serialized, "_iFrameBlinkInterval", 0.1f, 0.01f);
            SetIfDefault(serialized, "_screenShakeBaseIntensity", 0.3f);
            SetIfDefault(serialized, "_screenShakeDamageScale", 0.01f);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ════════════════════════════════════════════════════════════════
        //  SHIP JUICE SETTINGS SO
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Ship/Create Ship Juice Settings Asset")]
        public static void CreateShipJuiceSettings()
        {
            EnsureFolder("Assets/_Data", "Ship");

            string path = $"{SHIP_DATA_DIR}/{JUICE_ASSET_NAME}.asset";
            if (AssetDatabase.LoadAssetAtPath<ShipJuiceSettingsSO>(path) != null)
            {
                Debug.Log($"[ShipFeelAssetCreator] Already exists: {path}");
                return;
            }

            var so = ScriptableObject.CreateInstance<ShipJuiceSettingsSO>();
            var serialized = new SerializedObject(so);

            // Movement Tilt
            serialized.FindProperty("_moveTiltMaxAngle").floatValue = 15f;
            serialized.FindProperty("_tiltSmoothSpeed").floatValue = 10f;

            // Squash & Stretch
            serialized.FindProperty("_squashStretchIntensity").floatValue = 0.15f;
            serialized.FindProperty("_squashStretchDuration").floatValue = 0.1f;

            // Dash After-Image
            serialized.FindProperty("_dashAfterImageCount").intValue = 3;
            serialized.FindProperty("_afterImageFadeDuration").floatValue = 0.15f;
            serialized.FindProperty("_afterImageAlpha").floatValue = 0.4f;

            // Engine Particles
            serialized.FindProperty("_engineParticleMinSpeed").floatValue = 0.1f;
            serialized.FindProperty("_engineBaseEmissionRate").floatValue = 20f;
            serialized.FindProperty("_engineDashEmissionMultiplier").floatValue = 3f;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[ShipFeelAssetCreator] Created: {path}");
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

        /// <summary>
        /// Sets a float property only if its current value equals the threshold
        /// (default 0), meaning it was never explicitly set in the asset file.
        /// </summary>
        private static void SetIfDefault(SerializedObject so, string propName,
            float desiredValue, float defaultThreshold = 0f)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && Mathf.Approximately(prop.floatValue, defaultThreshold))
            {
                prop.floatValue = desiredValue;
            }
        }

    }
}