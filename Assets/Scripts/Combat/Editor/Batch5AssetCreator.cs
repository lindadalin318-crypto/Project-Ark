
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// Editor utility to auto-generate all Batch 5 test Prefabs and ScriptableObject assets.
    /// Menu: ProjectArk > Create Batch 5 Test Assets
    /// </summary>
    public static class Batch5AssetCreator
    {
        private const string PrefabRoot = "Assets/_Data/StarChart/Prefabs";
        private const string CoreRoot = "Assets/_Data/StarChart/Cores";
        private const string PrismRoot = "Assets/_Data/StarChart/Prisms";

        [MenuItem("ProjectArk/Create Batch 5 Test Assets")]
        public static void CreateAllAssets()
        {
            EnsureDirectories();

            // --- Step 1: Create Prefabs ---
            var projectilePrefab = CreateProjectilePrefab();
            var laserBeamPrefab = CreateLaserBeamPrefab();
            var echoWavePrefab = CreateEchoWavePrefab();
            var boomerangModPrefab = CreateBoomerangModifierPrefab();
            var slowModPrefab = CreateSlowOnHitModifierPrefab();
            var bounceModPrefab = CreateBounceModifierPrefab();

            // --- Step 2: Create Core SO assets ---
            CreateMatterCore(projectilePrefab);
            CreateLightCore(laserBeamPrefab);
            CreateEchoCore(echoWavePrefab);
            CreateAnomalyCore(projectilePrefab, boomerangModPrefab);

            // --- Step 3: Create Prism SO assets ---
            CreateFractalPrism();
            CreateRheologyPrism();
            CreateTintPrism(slowModPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Batch5AssetCreator] All Batch 5 test assets created successfully!");
            Debug.Log($"  Prefabs: {PrefabRoot}");
            Debug.Log($"  Cores:   {CoreRoot}");
            Debug.Log($"  Prisms:  {PrismRoot}");
        }

        // =====================================================================
        // Prefab Creation
        // =====================================================================

        private static GameObject CreateProjectilePrefab()
        {
            string path = $"{PrefabRoot}/Projectile_Matter.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var go = new GameObject("Projectile_Matter");

            // Rigidbody2D
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Collider (trigger)
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.15f;

            // SpriteRenderer placeholder
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.9f, 0.3f); // Yellowish bullet

            // PoolReference + Projectile
            go.AddComponent<Core.PoolReference>();
            go.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        private static GameObject CreateLaserBeamPrefab()
        {
            string path = $"{PrefabRoot}/LaserBeam_Light.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var go = new GameObject("LaserBeam_Light");

            // LineRenderer
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 0;
            lr.startWidth = 0.15f;
            lr.endWidth = 0.05f;
            lr.startColor = new Color(0.2f, 0.8f, 1f, 1f); // Cyan beam
            lr.endColor = new Color(0.2f, 0.8f, 1f, 0.5f);
            lr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");

            // PoolReference + LaserBeam
            go.AddComponent<Core.PoolReference>();
            go.AddComponent<LaserBeam>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        private static GameObject CreateEchoWavePrefab()
        {
            string path = $"{PrefabRoot}/EchoWave_Echo.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var go = new GameObject("EchoWave_Echo");

            // CircleCollider2D (trigger)
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            // SpriteRenderer placeholder (circular wave visual)
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.3f, 1f, 0.5f); // Purple wave

            // PoolReference + EchoWave
            go.AddComponent<Core.PoolReference>();
            go.AddComponent<EchoWave>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        private static GameObject CreateBoomerangModifierPrefab()
        {
            string path = $"{PrefabRoot}/Modifier_Boomerang.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var go = new GameObject("Modifier_Boomerang");
            go.AddComponent<BoomerangModifier>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        private static GameObject CreateSlowOnHitModifierPrefab()
        {
            string path = $"{PrefabRoot}/Modifier_SlowOnHit.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var go = new GameObject("Modifier_SlowOnHit");
            go.AddComponent<SlowOnHitModifier>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        private static GameObject CreateBounceModifierPrefab()
        {
            string path = $"{PrefabRoot}/Modifier_Bounce.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var go = new GameObject("Modifier_Bounce");
            go.AddComponent<BounceModifier>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"  Created prefab: {path}");
            return prefab;
        }

        // =====================================================================
        // Core SO Creation
        // =====================================================================

        private static void CreateMatterCore(GameObject projectilePrefab)
        {
            string path = $"{CoreRoot}/MatterCore_StandardBullet.asset";
            if (AssetDatabase.LoadAssetAtPath<StarCoreSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<StarCoreSO>();
            SetPrivateField(so, "_displayName", "Standard Bullet");
            SetPrivateField(so, "_description", "A reliable physical projectile. Solid damage, predictable trajectory.");
            SetPrivateField(so, "_slotSize", 1);
            SetPrivateField(so, "_family", CoreFamily.Matter);
            SetPrivateField(so, "_projectilePrefab", projectilePrefab);
            SetPrivateField(so, "_fireRate", 5f);
            SetPrivateField(so, "_baseDamage", 10f);
            SetPrivateField(so, "_projectileSpeed", 20f);
            SetPrivateField(so, "_lifetime", 2f);
            SetPrivateField(so, "_spread", 0f);
            SetPrivateField(so, "_knockback", 1f);
            SetPrivateField(so, "_recoilForce", 0.5f);

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateLightCore(GameObject laserBeamPrefab)
        {
            string path = $"{CoreRoot}/LightCore_BasicLaser.asset";
            if (AssetDatabase.LoadAssetAtPath<StarCoreSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<StarCoreSO>();
            SetPrivateField(so, "_displayName", "Basic Laser");
            SetPrivateField(so, "_description", "Instant-hit light beam. Fast and precise, but no physical presence.");
            SetPrivateField(so, "_slotSize", 1);
            SetPrivateField(so, "_family", CoreFamily.Light);
            SetPrivateField(so, "_projectilePrefab", laserBeamPrefab);
            SetPrivateField(so, "_fireRate", 4f);
            SetPrivateField(so, "_baseDamage", 8f);
            SetPrivateField(so, "_projectileSpeed", 50f);  // Used as range = speed * lifetime
            SetPrivateField(so, "_lifetime", 0.5f);        // Range = 50 * 0.5 = 25 units
            SetPrivateField(so, "_spread", 0f);
            SetPrivateField(so, "_knockback", 0.3f);
            SetPrivateField(so, "_recoilForce", 0.2f);

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateEchoCore(GameObject echoWavePrefab)
        {
            string path = $"{CoreRoot}/EchoCore_BasicWave.asset";
            if (AssetDatabase.LoadAssetAtPath<StarCoreSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<StarCoreSO>();
            SetPrivateField(so, "_displayName", "Basic Shockwave");
            SetPrivateField(so, "_description", "Expanding ring of force. Hits all nearby enemies, passes through walls.");
            SetPrivateField(so, "_slotSize", 1);
            SetPrivateField(so, "_family", CoreFamily.Echo);
            SetPrivateField(so, "_projectilePrefab", echoWavePrefab);
            SetPrivateField(so, "_fireRate", 2f);
            SetPrivateField(so, "_baseDamage", 15f);
            SetPrivateField(so, "_projectileSpeed", 8f);   // Expansion speed
            SetPrivateField(so, "_lifetime", 1.5f);        // Max radius = 8 * 1.5 = 12 units
            SetPrivateField(so, "_spread", 0f);             // 0 = full 360° ring
            SetPrivateField(so, "_knockback", 2f);
            SetPrivateField(so, "_recoilForce", 0f);

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateAnomalyCore(GameObject projectilePrefab, GameObject boomerangModPrefab)
        {
            string path = $"{CoreRoot}/AnomalyCore_Boomerang.asset";
            if (AssetDatabase.LoadAssetAtPath<StarCoreSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<StarCoreSO>();
            SetPrivateField(so, "_displayName", "Boomerang");
            SetPrivateField(so, "_description", "A curved projectile that returns to sender. Hits enemies on both passes.");
            SetPrivateField(so, "_slotSize", 1);
            SetPrivateField(so, "_family", CoreFamily.Anomaly);
            SetPrivateField(so, "_projectilePrefab", projectilePrefab);
            SetPrivateField(so, "_anomalyModifierPrefab", boomerangModPrefab);
            SetPrivateField(so, "_fireRate", 2f);
            SetPrivateField(so, "_baseDamage", 12f);
            SetPrivateField(so, "_projectileSpeed", 15f);
            SetPrivateField(so, "_lifetime", 3f);
            SetPrivateField(so, "_spread", 0f);
            SetPrivateField(so, "_knockback", 1.5f);
            SetPrivateField(so, "_recoilForce", 0.3f);

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"  Created SO: {path}");
        }

        // =====================================================================
        // Prism SO Creation
        // =====================================================================

        private static void CreateFractalPrism()
        {
            string path = $"{PrismRoot}/FractalPrism_TwinSplit.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetPrivateField(so, "_displayName", "Twin Split");
            SetPrivateField(so, "_description", "Fractal prism: +2 projectiles with 15° spread for fan-shaped coverage.");
            SetPrivateField(so, "_slotSize", 1);
            SetPrivateField(so, "_family", PrismFamily.Fractal);
            SetPrivateField(so, "_statModifiers", new StatModifier[]
            {
                new StatModifier
                {
                    Stat = WeaponStatType.ProjectileCount,
                    Operation = ModifierOperation.Add,
                    Value = 2f
                },
                new StatModifier
                {
                    Stat = WeaponStatType.Spread,
                    Operation = ModifierOperation.Add,
                    Value = 15f
                }
            });

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateRheologyPrism()
        {
            string path = $"{PrismRoot}/RheologyPrism_Accelerate.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetPrivateField(so, "_displayName", "Accelerate");
            SetPrivateField(so, "_description", "Rheology prism: 1.5x projectile speed for faster, longer-range shots.");
            SetPrivateField(so, "_slotSize", 1);
            SetPrivateField(so, "_family", PrismFamily.Rheology);
            SetPrivateField(so, "_statModifiers", new StatModifier[]
            {
                new StatModifier
                {
                    Stat = WeaponStatType.ProjectileSpeed,
                    Operation = ModifierOperation.Multiply,
                    Value = 1.5f
                }
            });

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"  Created SO: {path}");
        }

        private static void CreateTintPrism(GameObject slowModPrefab)
        {
            string path = $"{PrismRoot}/TintPrism_FrostSlow.asset";
            if (AssetDatabase.LoadAssetAtPath<PrismSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<PrismSO>();
            SetPrivateField(so, "_displayName", "Frost Slow");
            SetPrivateField(so, "_description", "Tint prism: projectiles apply a slowing frost effect on hit.");
            SetPrivateField(so, "_slotSize", 1);
            SetPrivateField(so, "_family", PrismFamily.Tint);
            SetPrivateField(so, "_statModifiers", new StatModifier[0]);
            SetPrivateField(so, "_projectileModifierPrefab", slowModPrefab);

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"  Created SO: {path}");
        }

        // =====================================================================
        // Utility
        // =====================================================================

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets", "_Data");
            EnsureFolder("Assets/_Data", "StarChart");
            EnsureFolder("Assets/_Data/StarChart", "Prefabs");
            EnsureFolder("Assets/_Data/StarChart", "Cores");
            EnsureFolder("Assets/_Data/StarChart", "Prisms");
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string fullPath = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>
        /// Sets a private serialized field on a ScriptableObject via SerializedObject.
        /// This ensures proper Unity serialization without needing public setters.
        /// </summary>
        private static void SetPrivateField(ScriptableObject target, string fieldName, object value)
        {
            var serializedObj = new SerializedObject(target);
            var prop = serializedObj.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogWarning($"[Batch5AssetCreator] Field '{fieldName}' not found on {target.GetType().Name}");
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
                        element.FindPropertyRelative("Stat").enumValueIndex = (int)modifiers[i].Stat;
                        element.FindPropertyRelative("Operation").enumValueIndex = (int)modifiers[i].Operation;
                        element.FindPropertyRelative("Value").floatValue = modifiers[i].Value;
                    }
                    break;
                default:
                    Debug.LogWarning($"[Batch5AssetCreator] Unsupported type for field '{fieldName}': {value.GetType()}");
                    break;
            }

            serializedObj.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
