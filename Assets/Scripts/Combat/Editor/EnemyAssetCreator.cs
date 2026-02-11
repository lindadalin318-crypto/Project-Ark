using UnityEditor;
using UnityEngine;

namespace ProjectArk.Combat.Enemy.Editor
{
    /// <summary>
    /// Editor utility to create the Rusher enemy EnemyStatsSO asset
    /// and fully-configured Prefab with one click.
    /// Menu: ProjectArk > Create Rusher Enemy Assets
    /// </summary>
    public static class EnemyAssetCreator
    {
        private const string ASSET_DIR = "Assets/_Data/Enemies";
        private const string PREFAB_DIR = "Assets/_Prefabs/Enemies";

        [MenuItem("ProjectArk/Create Rusher Enemy Assets")]
        public static void CreateRusherAssets()
        {
            // ─────────── Step 1: Ensure directories exist ───────────
            EnsureFolder("Assets/_Data", "Enemies");
            EnsureFolder("Assets/_Prefabs", "Enemies");

            // ─────────── Step 2: Create or load EnemyStatsSO ───────────
            string statsPath = $"{ASSET_DIR}/EnemyStats_Rusher.asset";
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);

            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();

                // Identity
                stats.EnemyName = "Rusher";
                stats.EnemyID = "enemy_rusher";

                // Health
                stats.MaxHP = 60f;
                stats.MaxPoise = 30f;

                // Movement
                stats.MoveSpeed = 5f;
                stats.RotationSpeed = 540f;

                // Attack
                stats.AttackDamage = 15f;
                stats.AttackRange = 1.8f;
                stats.AttackCooldown = 0.8f;
                stats.AttackKnockback = 6f;

                // Attack Phases
                stats.TelegraphDuration = 0.3f;
                stats.AttackActiveDuration = 0.15f;
                stats.RecoveryDuration = 0.8f;

                // Perception
                stats.SightRange = 12f;
                stats.SightAngle = 75f;
                stats.HearingRange = 18f;

                // Leash & Memory
                stats.LeashRange = 25f;
                stats.MemoryDuration = 4f;

                // Visuals
                stats.HitFlashDuration = 0.1f;
                stats.BaseColor = new Color(0.9f, 0.2f, 0.2f, 1f);

                AssetDatabase.CreateAsset(stats, statsPath);
                Debug.Log($"[EnemyAssetCreator] Created SO: {statsPath}");
            }
            else
            {
                Debug.Log($"[EnemyAssetCreator] SO already exists: {statsPath}");
            }

            // ─────────── Step 3: Create Prefab ───────────
            string prefabPath = $"{PREFAB_DIR}/Enemy_Rusher.prefab";
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                Debug.Log($"[EnemyAssetCreator] Prefab already exists: {prefabPath}");
            }
            else
            {
                // Build a temporary GameObject with all components configured
                var go = new GameObject("Enemy_Rusher");

                // Set Layer to Enemy (Layer 8)
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0)
                    go.layer = enemyLayer;
                else
                    Debug.LogWarning("[EnemyAssetCreator] 'Enemy' layer not found! Please add it in TagManager.");

                // --- SpriteRenderer ---
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = stats.BaseColor;
                // Use a built-in square sprite as placeholder
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

                // --- Rigidbody2D ---
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                // --- CircleCollider2D ---
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.4f;
                col.isTrigger = false;

                // --- EnemyEntity ---
                var entity = go.AddComponent<EnemyEntity>();

                // --- EnemyPerception ---
                var perception = go.AddComponent<EnemyPerception>();

                // --- EnemyBrain (auto-detects Entity and Perception via RequireComponent) ---
                go.AddComponent<EnemyBrain>();

                // ─────── Wire up SerializeField references via SerializedObject ───────

                // EnemyEntity._stats
                var entitySO = new SerializedObject(entity);
                entitySO.FindProperty("_stats").objectReferenceValue = stats;
                entitySO.ApplyModifiedPropertiesWithoutUndo();

                // EnemyPerception._stats, _playerMask, _obstacleMask
                var perceptionSO = new SerializedObject(perception);
                perceptionSO.FindProperty("_stats").objectReferenceValue = stats;

                // Set _playerMask to "Player" layer (layer 6)
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0)
                    perceptionSO.FindProperty("_playerMask").intValue = 1 << playerLayer;

                // Set _obstacleMask to "Wall" layer (layer 9)
                int wallLayer = LayerMask.NameToLayer("Wall");
                if (wallLayer >= 0)
                    perceptionSO.FindProperty("_obstacleMask").intValue = 1 << wallLayer;

                perceptionSO.ApplyModifiedPropertiesWithoutUndo();

                // ─────── Save as Prefab ───────
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go); // Clean up scene object

                Debug.Log($"<b>[EnemyAssetCreator] Created Prefab: {prefabPath}</b>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ─────────── Step 4: Summary ───────────
            EditorUtility.DisplayDialog(
                "Rusher Enemy Assets Created",
                $"SO: {statsPath}\nPrefab: {prefabPath}\n\n" +
                "The Prefab is fully configured with:\n" +
                "• SpriteRenderer (placeholder knob sprite)\n" +
                "• Rigidbody2D (Dynamic, gravity=0, freeze rotation Z)\n" +
                "• CircleCollider2D (radius=0.4)\n" +
                "• EnemyEntity (stats wired)\n" +
                "• EnemyPerception (stats + masks wired)\n" +
                "• EnemyBrain (auto-wired)\n\n" +
                "Layer: Enemy\n" +
                "Ready to drag into scene for testing!",
                "OK"
            );
        }

        // ════════════════════════════════════════════════════════════════
        //  SHOOTER ENEMY
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Create Shooter Enemy Assets")]
        public static void CreateShooterAssets()
        {
            // ─────────── Step 1: Ensure directories exist ───────────
            EnsureFolder("Assets/_Data", "Enemies");
            EnsureFolder("Assets/_Prefabs", "Enemies");

            // ─────────── Step 2: Create EnemyProjectile Prefab (needed by SO) ───────────
            string projPrefabPath = $"{PREFAB_DIR}/EnemyProjectile.prefab";
            var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPrefabPath);

            if (projPrefab == null)
            {
                projPrefab = CreateEnemyProjectilePrefab(projPrefabPath);
            }
            else
            {
                Debug.Log($"[EnemyAssetCreator] Projectile prefab already exists: {projPrefabPath}");
            }

            // ─────────── Step 3: Create or load EnemyStatsSO ───────────
            string statsPath = $"{ASSET_DIR}/EnemyStats_Shooter.asset";
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);

            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();

                // Identity
                stats.EnemyName = "Shooter";
                stats.EnemyID = "enemy_shooter";

                // Health — fragile, compensated by range
                stats.MaxHP = 40f;
                stats.MaxPoise = 20f;

                // Movement — slower than Rusher
                stats.MoveSpeed = 3.5f;
                stats.RotationSpeed = 360f;

                // Melee Attack (fallback, rarely used)
                stats.AttackDamage = 8f;
                stats.AttackRange = 1.5f;
                stats.AttackCooldown = 1.2f;
                stats.AttackKnockback = 3f;

                // Attack Phases (melee fallback)
                stats.TelegraphDuration = 0.3f;
                stats.AttackActiveDuration = 0.15f;
                stats.RecoveryDuration = 0.6f;

                // Ranged Attack — primary combat mode
                stats.ProjectilePrefab = projPrefab;
                stats.ProjectileSpeed = 10f;
                stats.ProjectileDamage = 8f;
                stats.ProjectileKnockback = 2f;
                stats.ProjectileLifetime = 5f;
                stats.ShotsPerBurst = 3;
                stats.BurstInterval = 0.2f;
                stats.PreferredRange = 10f;
                stats.RetreatRange = 5f;

                // Perception — wider cone, further sight to compensate for range
                stats.SightRange = 16f;
                stats.SightAngle = 90f;
                stats.HearingRange = 20f;

                // Leash & Memory
                stats.LeashRange = 30f;
                stats.MemoryDuration = 5f;

                // Visuals — cold blue tint (vs Rusher's aggressive red)
                stats.HitFlashDuration = 0.1f;
                stats.BaseColor = new Color(0.2f, 0.4f, 0.9f, 1f);

                AssetDatabase.CreateAsset(stats, statsPath);
                Debug.Log($"[EnemyAssetCreator] Created SO: {statsPath}");
            }
            else
            {
                Debug.Log($"[EnemyAssetCreator] SO already exists: {statsPath}");
            }

            // ─────────── Step 4: Create Prefab ───────────
            string prefabPath = $"{PREFAB_DIR}/Enemy_Shooter.prefab";
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                Debug.Log($"[EnemyAssetCreator] Prefab already exists: {prefabPath}");
            }
            else
            {
                var go = new GameObject("Enemy_Shooter");

                // Set Layer to Enemy (Layer 8)
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0)
                    go.layer = enemyLayer;
                else
                    Debug.LogWarning("[EnemyAssetCreator] 'Enemy' layer not found!");

                // --- SpriteRenderer ---
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = stats.BaseColor;
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

                // --- Rigidbody2D ---
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                // --- CircleCollider2D ---
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.35f;
                col.isTrigger = false;

                // --- EnemyEntity ---
                var entity = go.AddComponent<EnemyEntity>();

                // --- EnemyPerception ---
                var perception = go.AddComponent<EnemyPerception>();

                // --- ShooterBrain (inherits from EnemyBrain, satisfies RequireComponent) ---
                go.AddComponent<ShooterBrain>();

                // ─────── Wire up SerializeField references ───────

                // EnemyEntity._stats
                var entitySO = new SerializedObject(entity);
                entitySO.FindProperty("_stats").objectReferenceValue = stats;
                entitySO.ApplyModifiedPropertiesWithoutUndo();

                // EnemyPerception._stats, _playerMask, _obstacleMask
                var perceptionSO = new SerializedObject(perception);
                perceptionSO.FindProperty("_stats").objectReferenceValue = stats;

                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0)
                    perceptionSO.FindProperty("_playerMask").intValue = 1 << playerLayer;

                int wallLayer = LayerMask.NameToLayer("Wall");
                if (wallLayer >= 0)
                    perceptionSO.FindProperty("_obstacleMask").intValue = 1 << wallLayer;

                perceptionSO.ApplyModifiedPropertiesWithoutUndo();

                // ─────── Save as Prefab ───────
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);

                Debug.Log($"<b>[EnemyAssetCreator] Created Prefab: {prefabPath}</b>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ─────────── Step 5: Summary ───────────
            EditorUtility.DisplayDialog(
                "Shooter Enemy Assets Created",
                $"SO: {statsPath}\nPrefab: {prefabPath}\nProjectile: {projPrefabPath}\n\n" +
                "The Prefab is fully configured with:\n" +
                "• SpriteRenderer (blue tint placeholder)\n" +
                "• Rigidbody2D (Dynamic, gravity=0, freeze rotation Z)\n" +
                "• CircleCollider2D (radius=0.35)\n" +
                "• EnemyEntity (stats wired)\n" +
                "• EnemyPerception (stats + masks wired)\n" +
                "• ShooterBrain (ranged HFSM)\n\n" +
                "Behavior: Idle → Chase → Shoot (3-round burst) → Retreat → Return\n" +
                "Layer: Enemy\n\n" +
                "Ready to drag into scene for testing!",
                "OK"
            );
        }

        // ──────────────────── EnemyProjectile Prefab ────────────────────

        /// <summary>
        /// Creates the shared EnemyProjectile prefab used by all shooter-type enemies.
        /// Layer: PlayerProjectile (so it collides with Player).
        /// Has: SpriteRenderer, Rigidbody2D (kinematic-like dynamic), CircleCollider2D (trigger),
        ///      EnemyProjectile component.
        /// </summary>
        private static GameObject CreateEnemyProjectilePrefab(string path)
        {
            var go = new GameObject("EnemyProjectile");

            // Layer: use a dedicated enemy projectile layer, or fall back to Default
            // For now, set to Default — Physics2D matrix handles collision filtering
            // EnemyProjectile.OnTriggerEnter2D already ignores Enemy layer
            go.layer = LayerMask.NameToLayer("Default");

            // --- SpriteRenderer ---
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.3f, 0.1f, 1f); // Orange-red enemy bullet
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.sortingOrder = 5;

            // --- Rigidbody2D ---
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // --- CircleCollider2D (Trigger for OnTriggerEnter2D) ---
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;
            col.isTrigger = true;

            // --- EnemyProjectile ---
            go.AddComponent<EnemyProjectile>();

            // Scale down to bullet size
            go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            Debug.Log($"[EnemyAssetCreator] Created EnemyProjectile prefab: {path}");
            return prefab;
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
