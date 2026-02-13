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
                $"  (Tilt + Squash/Stretch + After-Image + Engine VFX)\n\n" +
                "Next steps:\n" +
                "1. Drag ShipStatsSO onto ShipMotor / ShipDash / ShipHealth components\n" +
                "2. Drag ShipJuiceSettingsSO onto ShipVisualJuice / ShipEngineVFX / DashAfterImageSpawner\n" +
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

            // Movement — Base
            serialized.FindProperty("_moveSpeed").floatValue = 12f;
            serialized.FindProperty("_acceleration").floatValue = 45f;
            serialized.FindProperty("_deceleration").floatValue = 25f;

            // Movement — Curves & Feel
            SetAnimationCurve(serialized, "_accelerationCurve",
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            SetAnimationCurve(serialized, "_decelerationCurve",
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            serialized.FindProperty("_sharpTurnAngleThreshold").floatValue = 90f;
            serialized.FindProperty("_sharpTurnSpeedPenalty").floatValue = 0.7f;
            serialized.FindProperty("_initialBoostMultiplier").floatValue = 1.5f;
            serialized.FindProperty("_initialBoostDuration").floatValue = 0.05f;
            serialized.FindProperty("_minMoveSpeedThreshold").floatValue = 0.1f;

            // Aiming
            serialized.FindProperty("_rotationSpeed").floatValue = 720f;

            // Dash
            serialized.FindProperty("_dashSpeed").floatValue = 30f;
            serialized.FindProperty("_dashDuration").floatValue = 0.15f;
            serialized.FindProperty("_dashCooldown").floatValue = 0.3f;
            serialized.FindProperty("_dashBufferWindow").floatValue = 0.15f;
            serialized.FindProperty("_dashExitSpeedRatio").floatValue = 0.5f;
            serialized.FindProperty("_dashIFrames").boolValue = true;

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

            // Movement — Curves & Feel (new fields)
            SetIfDefault(serialized, "_sharpTurnAngleThreshold", 90f);
            SetIfDefault(serialized, "_sharpTurnSpeedPenalty", 0.7f);
            SetIfDefault(serialized, "_initialBoostMultiplier", 1.5f);
            SetIfDefault(serialized, "_initialBoostDuration", 0.05f);
            SetIfDefault(serialized, "_minMoveSpeedThreshold", 0.1f);

            // Ensure curves exist (they default to empty if asset was created before the field existed)
            EnsureCurve(serialized, "_accelerationCurve",
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            EnsureCurve(serialized, "_decelerationCurve",
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

            // Dash (all new)
            SetIfDefault(serialized, "_dashSpeed", 30f);
            SetIfDefault(serialized, "_dashDuration", 0.15f, 0.01f);
            SetIfDefault(serialized, "_dashCooldown", 0.3f);
            SetIfDefault(serialized, "_dashBufferWindow", 0.15f);
            SetIfDefault(serialized, "_dashExitSpeedRatio", 0.5f);

            // _dashIFrames: bool defaults to false; set to true if not yet touched
            var dashIFramesProp = serialized.FindProperty("_dashIFrames");
            if (dashIFramesProp != null && !dashIFramesProp.boolValue)
            {
                dashIFramesProp.boolValue = true;
            }

            // Hit Feedback (all new)
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

        /// <summary>
        /// Sets an AnimationCurve property on a SerializedObject.
        /// </summary>
        private static void SetAnimationCurve(SerializedObject so, string propName,
            AnimationCurve curve)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.animationCurveValue = curve;
            }
        }

        /// <summary>
        /// Ensures an AnimationCurve property has at least 2 keys.
        /// If the curve is empty (0 keys), sets it to the provided default.
        /// </summary>
        private static void EnsureCurve(SerializedObject so, string propName,
            AnimationCurve defaultCurve)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && prop.animationCurveValue.keys.Length < 2)
            {
                prop.animationCurveValue = defaultCurve;
            }
        }
    }
}