#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ProjectArk.UI;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// Legacy editor utility to auto-generate all Sheba Star Chart item assets (13 items total).
    /// Hidden from the Unity menu because Sheba content bootstrap is no longer part of the current ProjectArk main workflow.
    ///
    /// Idempotent: already-existing assets are skipped.
    /// After creation, all new SOs are appended to PlayerInventory.asset.
    /// </summary>
    public static class ShebaAssetCreator
    {
        private const string PrefabRoot    = "Assets/_Data/StarChart/Prefabs";
        private const string CoreRoot      = "Assets/_Data/StarChart/Cores";
        private const string PrismRoot     = "Assets/_Data/StarChart/Prisms";
        private const string SailRoot      = "Assets/_Data/StarChart/Sails";
        private const string SatRoot       = "Assets/_Data/StarChart/Satellites";
        private const string PrefabsScene  = "Assets/_Prefabs/StarChart";

        // Existing prefab paths (created by Batch5AssetCreator)
        private const string MatterPrefabPath    = PrefabRoot + "/Projectile_Matter.prefab";
        private const string LaserPrefabPath     = PrefabRoot + "/LaserBeam_Light.prefab";
        private const string EchoPrefabPath      = PrefabRoot + "/EchoWave_Echo.prefab";
        private const string BoomerangModPath    = PrefabRoot + "/Modifier_Boomerang.prefab";
        private const string BounceModPath       = PrefabRoot + "/Modifier_Bounce.prefab";
        private const string SpeedSailPrefabPath = "Assets/_Prefabs/StarChart/SpeedDamageSailBehavior.prefab";

        private static int _created;
        private static int _skipped;

        public static void CreateAllAssets()
        {
            _created = 0;
            _skipped = 0;

            EnsureDirectories();

            // ── Step 1: New Modifier Prefabs ──────────────────────────────────
            var homingModPrefab     = CreateHomingModifierPrefab();
            var minePlacerModPrefab = CreateMinePlacerModifierPrefab();
            var autoTurretPrefab    = CreateAutoTurretPrefab();

            // ── Step 2: Load existing prefabs ────────────────────────────────
            var matterPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>(MatterPrefabPath);
            var laserPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>(LaserPrefabPath);
            var echoPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>(EchoPrefabPath);
            var boomerangMod    = AssetDatabase.LoadAssetAtPath<GameObject>(BoomerangModPath);
            var bounceMod       = AssetDatabase.LoadAssetAtPath<GameObject>(BounceModPath);
            var speedSailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpeedSailPrefabPath);

            // ── Step 3: Star Core SOs (4) ─────────────────────────────────────
            CreateShebaCore_MachineGun(matterPrefab);
            CreateShebaCore_FocusLaser(laserPrefab);
            CreateShebaCore_Shotgun(matterPrefab);
            CreateShebaCore_PulseWave(echoPrefab);

            // ── Step 4: Prism SOs (6) ─────────────────────────────────────────
            CreateShebaP_TwinSplit();
            CreateShebaP_RapidFire();
            CreateShebaP_Bounce(bounceMod);
            CreateShebaP_Boomerang(boomerangMod);
            CreateShebaP_Homing(homingModPrefab);
            CreateShebaP_MinePlacer(minePlacerModPrefab);

            // ── Step 5: Light Sail SOs (2) ────────────────────────────────────
            CreateShebaSail_Standard();
            CreateShebaSail_Scout(speedSailPrefab);

            // ── Step 6: Satellite SO (1) ──────────────────────────────────────
            CreateShebaSat_AutoTurret(autoTurretPrefab);

            // ── Step 7: Append to PlayerInventory ────────────────────────────
            AppendToPlayerInventory();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ShebaAssetCreator] Done — Created: {_created}, Skipped: {_skipped}");
        }

        // =====================================================================
        // Prefab Creation
        // =====================================================================

        private static GameObject CreateHomingModifierPrefab()
        {
            string path = $"{PrefabRoot}/Modifier_Homing.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { _skipped++; return existing; }

            var go = new GameObject("Modifier_Homing");
            go.AddComponent<HomingModifier>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            _created++;
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        private static GameObject CreateMinePlacerModifierPrefab()
        {
            string path = $"{PrefabRoot}/Modifier_MinePlacer.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { _skipped++; return existing; }

            var go = new GameObject("Modifier_MinePlacer");
            go.AddComponent<MinePlacerModifier>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            _created++;
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        private static GameObject CreateAutoTurretPrefab()
        {
            string path = $"{PrefabsScene}/Sat_AutoTurret.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { _skipped++; return existing; }

            var go = new GameObject("Sat_AutoTurret");
            var behavior = go.AddComponent<AutoTurretBehavior>();

            // Set the projectile prefab reference via SerializedObject
            var so = new SerializedObject(behavior);
            var matterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatterPrefabPath);
            if (matterPrefab != null)
                so.FindProperty("_projectilePrefab").objectReferenceValue = matterPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            _created++;
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        // =====================================================================
        // Star Core SO Creation (4)
        // =====================================================================

        private static void CreateShebaCore_MachineGun(GameObject projectilePrefab)
        {
            string path = $"{CoreRoot}/ShebaCore_MachineGun.asset";
            if (AssetDatabase.LoadAssetAtPath<StarCoreSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<StarCoreSO>();
            SetField(so, "_displayName",     "Machine Gun");
            SetField(so, "_description",     "Sheba Core 1001: Rapid-fire physical rounds. Low heat, high volume.");
            SetField(so, "_family",          CoreFamily.Matter);
            SetField(so, "_projectilePrefab", projectilePrefab);
            SetField(so, "_fireRate",        10f);
            SetField(so, "_baseDamage",      6f);
            SetField(so, "_projectileSpeed", 22f);
            SetField(so, "_lifetime",        1.8f);
            SetField(so, "_spread",          3f);
            SetField(so, "_knockback",       0.5f);
            SetField(so, "_recoilForce",     0.3f);
            SetField(so, "_heatCost",        4f);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaCore_FocusLaser(GameObject laserPrefab)
        {
            string path = $"{CoreRoot}/ShebaCore_FocusLaser.asset";
            if (AssetDatabase.LoadAssetAtPath<StarCoreSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<StarCoreSO>();
            SetField(so, "_displayName",     "Focus Laser");
            SetField(so, "_description",     "Sheba Core 1016: Precision light beam. Zero spread, instant hit.");
            SetField(so, "_family",          CoreFamily.Light);
            SetField(so, "_projectilePrefab", laserPrefab);
            SetField(so, "_fireRate",        4f);
            SetField(so, "_baseDamage",      12f);
            SetField(so, "_projectileSpeed", 50f);
            SetField(so, "_lifetime",        0.5f);
            SetField(so, "_spread",          0f);
            SetField(so, "_knockback",       0.3f);
            SetField(so, "_recoilForce",     0.2f);
            SetField(so, "_heatCost",        8f);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaCore_Shotgun(GameObject projectilePrefab)
        {
            string path = $"{CoreRoot}/ShebaCore_Shotgun.asset";
            if (AssetDatabase.LoadAssetAtPath<StarCoreSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<StarCoreSO>();
            SetField(so, "_displayName",     "Storm Scatter");
            SetField(so, "_description",     "Sheba Core 1002: Wide-spread shotgun burst. Devastating at close range.");
            SetField(so, "_family",          CoreFamily.Matter);
            SetField(so, "_projectilePrefab", projectilePrefab);
            SetField(so, "_fireRate",        1.5f);
            SetField(so, "_baseDamage",      8f);
            SetField(so, "_projectileSpeed", 18f);
            SetField(so, "_lifetime",        1.2f);
            SetField(so, "_spread",          30f);
            SetField(so, "_knockback",       1.5f);
            SetField(so, "_recoilForce",     1.2f);
            SetField(so, "_heatCost",        12f);

            // Shotgun fires 5 pellets via ProjectileCount stat modifier on the prism layer.
            // The core itself fires 1; pair with TwinSplit or use SpawnModifier prism for fan.
            // Base count = 1 here; designer adds Fractal prism for multi-pellet.

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaCore_PulseWave(GameObject echoPrefab)
        {
            string path = $"{CoreRoot}/ShebaCore_PulseWave.asset";
            if (AssetDatabase.LoadAssetAtPath<StarCoreSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<StarCoreSO>();
            SetField(so, "_displayName",     "Pulse Wave");
            SetField(so, "_description",     "Sheba Core 1018: Expanding ring of force. Hits all nearby enemies.");
            SetField(so, "_family",          CoreFamily.Echo);
            SetField(so, "_projectilePrefab", echoPrefab);
            SetField(so, "_fireRate",        0.5f);
            SetField(so, "_baseDamage",      14f);
            SetField(so, "_projectileSpeed", 8f);
            SetField(so, "_lifetime",        1.5f);
            SetField(so, "_spread",          0f);
            SetField(so, "_knockback",       2.5f);
            SetField(so, "_recoilForce",     0f);
            SetField(so, "_heatCost",        8f);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        // =====================================================================
        // Prism SO Creation (6)
        // =====================================================================

        private static void CreateShebaP_TwinSplit()
        {
            string path = $"{PrismRoot}/ShebaP_TwinSplit.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetField(so, "_displayName",  "Twin Split");
            SetField(so, "_description",  "Sheba Prism 2001: +2 projectiles with 15° spread.");
            SetField(so, "_family",       PrismFamily.Fractal);
            SetField(so, "_statModifiers", new StatModifier[]
            {
                new StatModifier { Stat = WeaponStatType.ProjectileCount, Operation = ModifierOperation.Add,      Value = 2f  },
                new StatModifier { Stat = WeaponStatType.Spread,          Operation = ModifierOperation.Add,      Value = 15f }
            });

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaP_RapidFire()
        {
            string path = $"{PrismRoot}/ShebaP_RapidFire.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetField(so, "_displayName",  "Rapid Fire");
            SetField(so, "_description",  "Sheba Prism 2006: Fire rate ×1.3 for sustained pressure.");
            SetField(so, "_family",       PrismFamily.Rheology);
            SetField(so, "_statModifiers", new StatModifier[]
            {
                new StatModifier { Stat = WeaponStatType.FireRate, Operation = ModifierOperation.Multiply, Value = 1.3f }
            });

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaP_Bounce(GameObject bounceModPrefab)
        {
            string path = $"{PrismRoot}/ShebaP_Bounce.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetField(so, "_displayName",              "Bounce");
            SetField(so, "_description",              "Sheba Prism 2013: Projectiles bounce off walls up to 3 times.");
            SetField(so, "_family",                   PrismFamily.Rheology);
            SetField(so, "_statModifiers",            new StatModifier[0]);
            SetField(so, "_projectileModifierPrefab", bounceModPrefab);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaP_Boomerang(GameObject boomerangModPrefab)
        {
            string path = $"{PrismRoot}/ShebaP_Boomerang.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetField(so, "_displayName",              "Boomerang");
            SetField(so, "_description",              "Sheba Prism 2021: Projectiles decelerate, reverse, and return to sender.");
            SetField(so, "_family",                   PrismFamily.Rheology);
            SetField(so, "_statModifiers",            new StatModifier[0]);
            SetField(so, "_projectileModifierPrefab", boomerangModPrefab);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaP_Homing(GameObject homingModPrefab)
        {
            string path = $"{PrismRoot}/ShebaP_Homing.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetField(so, "_displayName",              "Homing");
            SetField(so, "_description",              "Sheba Prism 2024: Projectiles steer toward the nearest enemy within a 45° cone.");
            SetField(so, "_family",                   PrismFamily.Tint);
            SetField(so, "_statModifiers",            new StatModifier[0]);
            SetField(so, "_projectileModifierPrefab", homingModPrefab);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaP_MinePlacer(GameObject minePlacerModPrefab)
        {
            string path = $"{PrismRoot}/ShebaP_MinePlacer.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetField(so, "_displayName",              "Mine Placer");
            SetField(so, "_description",              "Sheba Prism 2067: Projectiles stop in place and linger as proximity mines.");
            SetField(so, "_family",                   PrismFamily.Fractal);
            SetField(so, "_statModifiers",            new StatModifier[0]);
            SetField(so, "_projectileModifierPrefab", minePlacerModPrefab);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        // =====================================================================
        // Light Sail SO Creation (2)
        // =====================================================================

        private static void CreateShebaSail_Standard()
        {
            string path = $"{SailRoot}/ShebaSail_Standard.asset";
            if (AssetDatabase.LoadAssetAtPath<LightSailSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<LightSailSO>();
            SetField(so, "_displayName",         "Standard Sail");
            SetField(so, "_description",         "Sheba Sail 3005: No passive effect. The baseline for comparison.");
            SetField(so, "_conditionDescription", "Always active");
            SetField(so, "_effectDescription",   "No effect");
            SetField(so, "_behaviorPrefab",      (GameObject)null);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateShebaSail_Scout(GameObject speedSailPrefab)
        {
            string path = $"{SailRoot}/ShebaSail_Scout.asset";
            if (AssetDatabase.LoadAssetAtPath<LightSailSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<LightSailSO>();
            SetField(so, "_displayName",         "Scout Sail");
            SetField(so, "_description",         "Sheba Sail 3006: The faster you fly, the harder you hit. Speed > 5 grants +8% damage per unit.");
            SetField(so, "_conditionDescription", "Speed > 5 units/s");
            SetField(so, "_effectDescription",   "+8% damage per unit of speed above 5");
            SetField(so, "_behaviorPrefab",      speedSailPrefab);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        // =====================================================================
        // Satellite SO Creation (1)
        // =====================================================================

        private static void CreateShebaSat_AutoTurret(GameObject autoTurretPrefab)
        {
            string path = $"{SatRoot}/ShebaSat_AutoTurret.asset";
            if (AssetDatabase.LoadAssetAtPath<SatelliteSO>(path) != null) { _skipped++; return; }

            var so = ScriptableObject.CreateInstance<SatelliteSO>();
            SetField(so, "_displayName",        "Auto Turret");
            SetField(so, "_description",        "Sheba Satellite 4005: Automatically fires at the nearest enemy every 1.5 seconds.");
            SetField(so, "_triggerDescription", "Nearest enemy within 15 units");
            SetField(so, "_actionDescription",  "Fire one low-damage Matter projectile toward target");
            SetField(so, "_internalCooldown",   1.5f);
            SetField(so, "_behaviorPrefab",     autoTurretPrefab);

            AssetDatabase.CreateAsset(so, path);
            _created++;
            Debug.Log($"  Created SO: {path}");
        }

        // =====================================================================
        // Inventory Append
        // =====================================================================

        private static void AppendToPlayerInventory()
        {
            var guids = AssetDatabase.FindAssets("t:StarChartInventorySO");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[ShebaAssetCreator] PlayerInventory.asset not found — skipping inventory update.");
                return;
            }

            var inventoryPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var inventory = AssetDatabase.LoadAssetAtPath<StarChartInventorySO>(inventoryPath);
            if (inventory == null) return;

            var serialized = new SerializedObject(inventory);
            var listProp = serialized.FindProperty("_ownedItems");

            // Collect all newly created Sheba SOs
            string[] searchPaths = { CoreRoot, PrismRoot, SailRoot, SatRoot };
            string[] typeNames   = { "StarCoreSO", "PrismSO", "LightSailSO", "SatelliteSO" };

            int appended = 0;
            foreach (var searchPath in searchPaths)
            {
                foreach (var typeName in typeNames)
                {
                    var assetGuids = AssetDatabase.FindAssets($"t:{typeName} Sheba", new[] { searchPath });
                    foreach (var guid in assetGuids)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        var asset = AssetDatabase.LoadAssetAtPath<StarChartItemSO>(assetPath);
                        if (asset == null) continue;

                        // Check if already in list
                        bool alreadyIn = false;
                        for (int i = 0; i < listProp.arraySize; i++)
                        {
                            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                            {
                                alreadyIn = true;
                                break;
                            }
                        }

                        if (!alreadyIn)
                        {
                            int idx = listProp.arraySize;
                            listProp.arraySize = idx + 1;
                            listProp.GetArrayElementAtIndex(idx).objectReferenceValue = asset;
                            appended++;
                            Debug.Log($"  Added to inventory: {asset.name}");
                        }
                    }
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[ShebaAssetCreator] Appended {appended} items to PlayerInventory.");
        }

        // =====================================================================
        // Utilities
        // =====================================================================

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets", "_Data");
            EnsureFolder("Assets/_Data", "StarChart");
            EnsureFolder("Assets/_Data/StarChart", "Prefabs");
            EnsureFolder("Assets/_Data/StarChart", "Cores");
            EnsureFolder("Assets/_Data/StarChart", "Prisms");
            EnsureFolder("Assets/_Data/StarChart", "Sails");
            EnsureFolder("Assets/_Data/StarChart", "Satellites");
            EnsureFolder("Assets", "_Prefabs");
            EnsureFolder("Assets/_Prefabs", "StarChart");
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string fullPath = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>
        /// Sets a private serialized field on a ScriptableObject via SerializedObject.
        /// Supports: string, int, float, bool, GameObject, Object, Enum, StatModifier[].
        /// </summary>
        private static void SetField(ScriptableObject target, string fieldName, object value)
        {
            var serializedObj = new SerializedObject(target);
            var prop = serializedObj.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogWarning($"[ShebaAssetCreator] Field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }

            switch (value)
            {
                case string s:
                    prop.stringValue = s;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
                case bool b:
                    prop.boolValue = b;
                    break;
                case GameObject go:
                    prop.objectReferenceValue = go;
                    break;
                case Object obj:
                    prop.objectReferenceValue = obj;
                    break;
                case System.Enum e:
                    prop.enumValueIndex = System.Convert.ToInt32(e);
                    break;
                case StatModifier[] modifiers:
                    prop.arraySize = modifiers.Length;
                    for (int i = 0; i < modifiers.Length; i++)
                    {
                        var element = prop.GetArrayElementAtIndex(i);
                        element.FindPropertyRelative("Stat").enumValueIndex      = (int)modifiers[i].Stat;
                        element.FindPropertyRelative("Operation").enumValueIndex = (int)modifiers[i].Operation;
                        element.FindPropertyRelative("Value").floatValue         = modifiers[i].Value;
                    }
                    break;
                default:
                    Debug.LogWarning($"[ShebaAssetCreator] Unsupported type for field '{fieldName}': {value?.GetType()}");
                    break;
            }

            serializedObj.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
