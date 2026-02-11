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

        // ════════════════════════════════════════════════════════════════
        //  ATTACK DATA ASSETS
        // ════════════════════════════════════════════════════════════════

        private const string ATTACK_DIR = "Assets/_Data/Enemies/Attacks";

        [MenuItem("ProjectArk/Create Attack Data Assets (Rusher + Shooter)")]
        public static void CreateAttackDataAssets()
        {
            EnsureFolder("Assets/_Data", "Enemies");
            EnsureFolder("Assets/_Data/Enemies", "Attacks");

            // ─── Rusher Melee ───
            CreateRusherMeleeAttack();

            // ─── Shooter Burst ───
            CreateShooterBurstAttack();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Attack Data Assets Created",
                $"Created in: {ATTACK_DIR}/\n\n" +
                "• RusherMelee.asset (Melee, Circle hitbox)\n" +
                "• ShooterBurst.asset (Projectile, 3-round burst)\n\n" +
                "Next: Assign these to the enemy's EnemyStatsSO Attacks array.",
                "OK"
            );
        }

        private static AttackDataSO CreateRusherMeleeAttack()
        {
            string path = $"{ATTACK_DIR}/RusherMelee.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttackDataSO>(path);
            if (existing != null)
            {
                Debug.Log($"[EnemyAssetCreator] AttackData already exists: {path}");
                return existing;
            }

            var attack = ScriptableObject.CreateInstance<AttackDataSO>();
            attack.AttackName = "Rusher Melee";
            attack.Type = AttackType.Melee;

            // Phases — match legacy Rusher values
            attack.TelegraphDuration = 0.3f;
            attack.ActiveDuration = 0.15f;
            attack.RecoveryDuration = 0.8f;

            // Damage
            attack.Damage = 15f;
            attack.Knockback = 6f;

            // Hitbox
            attack.Shape = HitboxShape.Circle;
            attack.HitboxRadius = 1.8f;
            attack.HitboxOffset = 0.5f;

            // Visual
            attack.TelegraphColor = Color.red;

            AssetDatabase.CreateAsset(attack, path);
            Debug.Log($"[EnemyAssetCreator] Created AttackData: {path}");
            return attack;
        }

        private static AttackDataSO CreateShooterBurstAttack()
        {
            string path = $"{ATTACK_DIR}/ShooterBurst.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttackDataSO>(path);
            if (existing != null)
            {
                Debug.Log($"[EnemyAssetCreator] AttackData already exists: {path}");
                return existing;
            }

            var attack = ScriptableObject.CreateInstance<AttackDataSO>();
            attack.AttackName = "Shooter Burst";
            attack.Type = AttackType.Projectile;

            // Phases — match legacy Shooter values
            attack.TelegraphDuration = 0.3f;
            attack.ActiveDuration = 0f; // Not used for Projectile type (burst handles timing)
            attack.RecoveryDuration = 0.6f;

            // Damage (per projectile)
            attack.Damage = 8f;
            attack.Knockback = 2f;

            // Projectile settings — pick up the prefab if it exists
            var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/EnemyProjectile.prefab");
            attack.ProjectilePrefab = projPrefab;
            attack.ProjectileSpeed = 10f;
            attack.ProjectileKnockback = 2f;
            attack.ProjectileLifetime = 5f;
            attack.ShotsPerBurst = 3;
            attack.BurstInterval = 0.2f;

            // Visual
            attack.TelegraphColor = new Color(1f, 0.6f, 0f, 1f); // Orange

            AssetDatabase.CreateAsset(attack, path);
            Debug.Log($"[EnemyAssetCreator] Created AttackData: {path}");
            return attack;
        }

        // ════════════════════════════════════════════════════════════════
        //  TURRET ENEMY (LASER VARIANT)
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Create Turret Enemy Assets (Laser)")]
        public static void CreateTurretLaserAssets()
        {
            EnsureFolder("Assets/_Data", "Enemies");
            EnsureFolder("Assets/_Data/Enemies", "Attacks");
            EnsureFolder("Assets/_Prefabs", "Enemies");

            // ─── Attack Data: Turret Laser ───
            var laserAttack = CreateTurretLaserAttackData();

            // ─── EnemyStatsSO ───
            string statsPath = $"{ASSET_DIR}/EnemyStats_TurretLaser.asset";
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);

            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();

                stats.EnemyName = "Turret (Laser)";
                stats.EnemyID = "enemy_turret_laser";
                stats.MaxHP = 80f;
                stats.MaxPoise = 40f;
                stats.MoveSpeed = 0f; // Stationary
                stats.RotationSpeed = 180f;
                stats.AttackDamage = 20f;
                stats.AttackRange = 15f;
                stats.AttackCooldown = 2.5f;
                stats.AttackKnockback = 4f;
                stats.TelegraphDuration = 0.8f;
                stats.AttackActiveDuration = 1f;
                stats.RecoveryDuration = 0.5f;
                stats.SightRange = 18f;
                stats.SightAngle = 90f;
                stats.HearingRange = 20f;
                stats.LeashRange = 999f; // Never leash (stationary)
                stats.MemoryDuration = 5f;
                stats.HitFlashDuration = 0.1f;
                stats.BaseColor = new Color(0.8f, 0.8f, 0.2f, 1f); // Yellow
                stats.Attacks = new AttackDataSO[] { laserAttack };

                AssetDatabase.CreateAsset(stats, statsPath);
                Debug.Log($"[EnemyAssetCreator] Created SO: {statsPath}");
            }

            // ─── Prefab ───
            string prefabPath = $"{PREFAB_DIR}/Enemy_TurretLaser.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existing == null)
            {
                var go = CreateTurretGameObject("Enemy_TurretLaser", stats);

                // Add EnemyLaserBeam child (for aim line + beam firing)
                var beamChild = new GameObject("LaserBeam");
                beamChild.transform.SetParent(go.transform);
                beamChild.transform.localPosition = Vector3.zero;
                var lr = beamChild.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 0;
                lr.startWidth = 0.03f;
                lr.endWidth = 0.03f;
                lr.material = null; // Will use default; assign in editor
                beamChild.AddComponent<EnemyLaserBeam>();

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
                Debug.Log($"<b>[EnemyAssetCreator] Created Prefab: {prefabPath}</b>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Turret (Laser) Assets Created",
                $"SO: {statsPath}\nPrefab: {prefabPath}\n" +
                $"AttackData: {ATTACK_DIR}/TurretLaser.asset\n\n" +
                "Components:\n" +
                "• SpriteRenderer (yellow placeholder)\n" +
                "• Rigidbody2D (Kinematic, no movement)\n" +
                "• CircleCollider2D (radius=0.5)\n" +
                "• EnemyEntity + EnemyPerception + TurretBrain\n" +
                "• Child: LaserBeam (LineRenderer + EnemyLaserBeam)\n\n" +
                "Behavior: Scan → Lock (aim line) → Fire Laser → Cooldown",
                "OK"
            );
        }

        // ════════════════════════════════════════════════════════════════
        //  TURRET ENEMY (CANNON VARIANT)
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Create Turret Enemy Assets (Cannon)")]
        public static void CreateTurretCannonAssets()
        {
            EnsureFolder("Assets/_Data", "Enemies");
            EnsureFolder("Assets/_Data/Enemies", "Attacks");
            EnsureFolder("Assets/_Prefabs", "Enemies");

            // Ensure EnemyProjectile prefab exists
            string projPrefabPath = $"{PREFAB_DIR}/EnemyProjectile.prefab";
            var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPrefabPath);
            if (projPrefab == null)
                projPrefab = CreateEnemyProjectilePrefab(projPrefabPath);

            // ─── Attack Data: Turret Cannon ───
            var cannonAttack = CreateTurretCannonAttackData(projPrefab);

            // ─── EnemyStatsSO ───
            string statsPath = $"{ASSET_DIR}/EnemyStats_TurretCannon.asset";
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);

            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();

                stats.EnemyName = "Turret (Cannon)";
                stats.EnemyID = "enemy_turret_cannon";
                stats.MaxHP = 100f;
                stats.MaxPoise = 60f;
                stats.MoveSpeed = 0f; // Stationary
                stats.RotationSpeed = 120f;
                stats.AttackDamage = 30f;
                stats.AttackRange = 12f;
                stats.AttackCooldown = 3f;
                stats.AttackKnockback = 8f;
                stats.TelegraphDuration = 1.0f;
                stats.AttackActiveDuration = 0.1f;
                stats.RecoveryDuration = 0.3f;
                stats.ProjectilePrefab = projPrefab;
                stats.ProjectileSpeed = 14f;
                stats.ProjectileDamage = 30f;
                stats.ProjectileKnockback = 8f;
                stats.ProjectileLifetime = 5f;
                stats.ShotsPerBurst = 1;
                stats.SightRange = 16f;
                stats.SightAngle = 75f;
                stats.HearingRange = 20f;
                stats.LeashRange = 999f;
                stats.MemoryDuration = 5f;
                stats.HitFlashDuration = 0.1f;
                stats.BaseColor = new Color(0.6f, 0.3f, 0.1f, 1f); // Dark orange/brown
                stats.Attacks = new AttackDataSO[] { cannonAttack };

                AssetDatabase.CreateAsset(stats, statsPath);
                Debug.Log($"[EnemyAssetCreator] Created SO: {statsPath}");
            }

            // ─── Prefab ───
            string prefabPath = $"{PREFAB_DIR}/Enemy_TurretCannon.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existing == null)
            {
                var go = CreateTurretGameObject("Enemy_TurretCannon", stats);

                // Add EnemyLaserBeam child for aim line only (visual telegraph)
                var beamChild = new GameObject("AimLine");
                beamChild.transform.SetParent(go.transform);
                beamChild.transform.localPosition = Vector3.zero;
                var lr = beamChild.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 0;
                lr.startWidth = 0.03f;
                lr.endWidth = 0.03f;
                beamChild.AddComponent<EnemyLaserBeam>();

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
                Debug.Log($"<b>[EnemyAssetCreator] Created Prefab: {prefabPath}</b>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Turret (Cannon) Assets Created",
                $"SO: {statsPath}\nPrefab: {prefabPath}\n" +
                $"AttackData: {ATTACK_DIR}/TurretCannon.asset\n\n" +
                "Components:\n" +
                "• SpriteRenderer (dark orange placeholder)\n" +
                "• Rigidbody2D (Kinematic, no movement)\n" +
                "• CircleCollider2D (radius=0.5)\n" +
                "• EnemyEntity + EnemyPerception + TurretBrain\n" +
                "• Child: AimLine (LineRenderer + EnemyLaserBeam)\n\n" +
                "Behavior: Scan → Lock (aim line) → Fire Charged Shot → Cooldown",
                "OK"
            );
        }

        // ──────────────────── Turret Prefab Helper ────────────────────

        private static GameObject CreateTurretGameObject(string name, EnemyStatsSO stats)
        {
            var go = new GameObject(name);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                go.layer = enemyLayer;

            // SpriteRenderer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = stats.BaseColor;
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // Rigidbody2D — Kinematic (turret doesn't move)
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            // CircleCollider2D
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = false;

            // Core components
            var entity = go.AddComponent<EnemyEntity>();
            var perception = go.AddComponent<EnemyPerception>();
            go.AddComponent<TurretBrain>();

            // Wire EnemyEntity._stats
            var entitySO = new SerializedObject(entity);
            entitySO.FindProperty("_stats").objectReferenceValue = stats;
            entitySO.ApplyModifiedPropertiesWithoutUndo();

            // Wire EnemyPerception._stats, masks
            var perceptionSO = new SerializedObject(perception);
            perceptionSO.FindProperty("_stats").objectReferenceValue = stats;

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                perceptionSO.FindProperty("_playerMask").intValue = 1 << playerLayer;

            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0)
                perceptionSO.FindProperty("_obstacleMask").intValue = 1 << wallLayer;

            perceptionSO.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        // ──────────────────── Turret AttackData Helpers ────────────────────

        private static AttackDataSO CreateTurretLaserAttackData()
        {
            string path = $"{ATTACK_DIR}/TurretLaser.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttackDataSO>(path);
            if (existing != null) return existing;

            var attack = ScriptableObject.CreateInstance<AttackDataSO>();
            attack.AttackName = "Turret Laser";
            attack.Type = AttackType.Laser;
            attack.TelegraphDuration = 0.8f;
            attack.ActiveDuration = 1.0f;
            attack.RecoveryDuration = 0.5f;
            attack.Damage = 20f;
            attack.Knockback = 4f;
            attack.LaserRange = 15f;
            attack.LaserDuration = 1.0f;
            attack.LaserWidth = 0.3f;
            attack.TelegraphColor = Color.red;

            AssetDatabase.CreateAsset(attack, path);
            Debug.Log($"[EnemyAssetCreator] Created AttackData: {path}");
            return attack;
        }

        private static AttackDataSO CreateTurretCannonAttackData(GameObject projPrefab)
        {
            string path = $"{ATTACK_DIR}/TurretCannon.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttackDataSO>(path);
            if (existing != null) return existing;

            var attack = ScriptableObject.CreateInstance<AttackDataSO>();
            attack.AttackName = "Turret Cannon";
            attack.Type = AttackType.Projectile;
            attack.TelegraphDuration = 1.0f;
            attack.ActiveDuration = 0.1f;
            attack.RecoveryDuration = 0.3f;
            attack.Damage = 30f;
            attack.Knockback = 8f;
            attack.ProjectilePrefab = projPrefab;
            attack.ProjectileSpeed = 14f;
            attack.ProjectileKnockback = 8f;
            attack.ProjectileLifetime = 5f;
            attack.ShotsPerBurst = 1;
            attack.BurstInterval = 0f;
            attack.TelegraphColor = new Color(1f, 0.4f, 0f, 1f); // Orange

            AssetDatabase.CreateAsset(attack, path);
            Debug.Log($"[EnemyAssetCreator] Created AttackData: {path}");
            return attack;
        }

        // ════════════════════════════════════════════════════════════════
        //  STALKER ENEMY
        // ════════════════════════════════════════════════════════════════

        [MenuItem("ProjectArk/Create Stalker Enemy Assets")]
        public static void CreateStalkerAssets()
        {
            EnsureFolder("Assets/_Data", "Enemies");
            EnsureFolder("Assets/_Data/Enemies", "Attacks");
            EnsureFolder("Assets/_Prefabs", "Enemies");

            // ─── Attack Data: Stalker Backstab ───
            var backstabAttack = CreateStalkerBackstabAttack();

            // ─── EnemyStatsSO ───
            string statsPath = $"{ASSET_DIR}/EnemyStats_Stalker.asset";
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);

            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();

                stats.EnemyName = "Stalker";
                stats.EnemyID = "enemy_stalker";

                // Health — fragile assassin
                stats.MaxHP = 35f;
                stats.MaxPoise = 15f;

                // Movement — very fast
                stats.MoveSpeed = 6.5f;
                stats.RotationSpeed = 720f;

                // Attack (legacy fallback)
                stats.AttackDamage = 25f;
                stats.AttackRange = 1.5f;
                stats.AttackCooldown = 3f;
                stats.AttackKnockback = 4f;

                // Attack Phases (legacy fallback)
                stats.TelegraphDuration = 0.1f;
                stats.AttackActiveDuration = 0.1f;
                stats.RecoveryDuration = 0.3f;

                // Perception — wide awareness
                stats.SightRange = 14f;
                stats.SightAngle = 120f;
                stats.HearingRange = 20f;

                // Leash & Memory
                stats.LeashRange = 25f;
                stats.MemoryDuration = 6f;

                // Visuals — dark purple tint
                stats.HitFlashDuration = 0.1f;
                stats.BaseColor = new Color(0.5f, 0.1f, 0.6f, 1f);

                // Data-driven attacks
                stats.Attacks = new AttackDataSO[] { backstabAttack };

                AssetDatabase.CreateAsset(stats, statsPath);
                Debug.Log($"[EnemyAssetCreator] Created SO: {statsPath}");
            }

            // ─── Prefab ───
            string prefabPath = $"{PREFAB_DIR}/Enemy_Stalker.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existing == null)
            {
                var go = new GameObject("Enemy_Stalker");

                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0)
                    go.layer = enemyLayer;

                // SpriteRenderer
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = stats.BaseColor;
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

                // Rigidbody2D
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                // CircleCollider2D — slightly smaller (agile assassin)
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.3f;
                col.isTrigger = false;

                // Core components
                var entity = go.AddComponent<EnemyEntity>();
                var perception = go.AddComponent<EnemyPerception>();
                go.AddComponent<StalkerBrain>();

                // Wire EnemyEntity._stats
                var entitySO = new SerializedObject(entity);
                entitySO.FindProperty("_stats").objectReferenceValue = stats;
                entitySO.ApplyModifiedPropertiesWithoutUndo();

                // Wire EnemyPerception._stats, masks
                var perceptionSO = new SerializedObject(perception);
                perceptionSO.FindProperty("_stats").objectReferenceValue = stats;

                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0)
                    perceptionSO.FindProperty("_playerMask").intValue = 1 << playerLayer;

                int wallLayer = LayerMask.NameToLayer("Wall");
                if (wallLayer >= 0)
                    perceptionSO.FindProperty("_obstacleMask").intValue = 1 << wallLayer;

                perceptionSO.ApplyModifiedPropertiesWithoutUndo();

                // Save as Prefab
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);

                Debug.Log($"<b>[EnemyAssetCreator] Created Prefab: {prefabPath}</b>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Stalker Enemy Assets Created",
                $"SO: {statsPath}\nPrefab: {prefabPath}\n" +
                $"AttackData: {ATTACK_DIR}/StalkerBackstab.asset\n\n" +
                "Components:\n" +
                "• SpriteRenderer (purple tint, stealth alpha=0.1)\n" +
                "• Rigidbody2D (Dynamic, gravity=0, freeze rotation Z)\n" +
                "• CircleCollider2D (radius=0.3)\n" +
                "• EnemyEntity + EnemyPerception + StalkerBrain\n\n" +
                "Behavior: Stealth → Flank (rear arc) → Strike (backstab) → Disengage → Stealth",
                "OK"
            );
        }

        private static AttackDataSO CreateStalkerBackstabAttack()
        {
            string path = $"{ATTACK_DIR}/StalkerBackstab.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttackDataSO>(path);
            if (existing != null) return existing;

            var attack = ScriptableObject.CreateInstance<AttackDataSO>();
            attack.AttackName = "Stalker Backstab";
            attack.Type = AttackType.Melee;

            // Very short telegraph — assassin speed
            attack.TelegraphDuration = 0.1f;
            attack.ActiveDuration = 0.1f;
            attack.RecoveryDuration = 0.3f;

            // High damage — backstab
            attack.Damage = 25f;
            attack.Knockback = 4f;

            // Hitbox — cone (attack from behind)
            attack.Shape = HitboxShape.Cone;
            attack.HitboxRadius = 1.5f;
            attack.HitboxAngle = 60f;
            attack.HitboxOffset = 0.3f;

            // Visual
            attack.TelegraphColor = new Color(0.8f, 0.2f, 0.8f, 1f); // Purple

            AssetDatabase.CreateAsset(attack, path);
            Debug.Log($"[EnemyAssetCreator] Created AttackData: {path}");
            return attack;
        }

        // ════════════════════════════════════════════════════════════════
        //  ELITE AFFIX ASSETS
        // ════════════════════════════════════════════════════════════════

        private const string AFFIX_DIR = "Assets/_Data/Enemies/Affixes";

        [MenuItem("ProjectArk/Create Elite Affix Assets (5 Affixes)")]
        public static void CreateAffixAssets()
        {
            EnsureFolder("Assets/_Data", "Enemies");
            EnsureFolder("Assets/_Data/Enemies", "Affixes");

            // ─── Berserk ───
            CreateAffixAsset("Berserk", affix =>
            {
                affix.HPMultiplier = 1f;
                affix.DamageMultiplier = 1.5f;
                affix.SpeedMultiplier = 1.3f;
                affix.TintOverride = new Color(1f, 0.2f, 0.2f, 1f); // Red
                affix.ScaleMultiplier = 1.1f;
                affix.Effect = AffixEffect.BerserkOnLowHP;
                affix.EffectValue = 0.3f; // Trigger at 30% HP
                affix.EffectSecondaryValue = 1.5f; // +50% more damage
            });

            // ─── Shielded ───
            CreateAffixAsset("Shielded", affix =>
            {
                affix.HPMultiplier = 2f;
                affix.DamageMultiplier = 1f;
                affix.SpeedMultiplier = 0.9f;
                affix.TintOverride = new Color(0.3f, 0.6f, 1f, 1f); // Blue
                affix.ScaleMultiplier = 1.15f;
                affix.Effect = AffixEffect.ShieldRegen;
                affix.EffectValue = 2f; // 2 HP/sec regen
                affix.AddBehaviorTags = new string[] { "SuperArmor" };
            });

            // ─── Explosive ───
            CreateAffixAsset("Explosive", affix =>
            {
                affix.HPMultiplier = 0.8f;
                affix.DamageMultiplier = 1.2f;
                affix.SpeedMultiplier = 1.1f;
                affix.TintOverride = new Color(1f, 0.5f, 0f, 1f); // Orange
                affix.ScaleMultiplier = 1f;
                affix.Effect = AffixEffect.ExplosiveOnDeath;
                affix.EffectValue = 3f; // 3 unit AoE radius
                affix.EffectSecondaryValue = 20f; // 20 damage AoE
            });

            // ─── Vampiric ───
            CreateAffixAsset("Vampiric", affix =>
            {
                affix.HPMultiplier = 1.2f;
                affix.DamageMultiplier = 1.1f;
                affix.SpeedMultiplier = 1f;
                affix.TintOverride = new Color(0.5f, 0f, 0.3f, 1f); // Dark magenta
                affix.ScaleMultiplier = 1f;
                affix.Effect = AffixEffect.VampiricOnHit;
                affix.EffectValue = 0.2f; // Heal 20% of damage dealt
            });

            // ─── Reflective ───
            CreateAffixAsset("Reflective", affix =>
            {
                affix.HPMultiplier = 1.5f;
                affix.DamageMultiplier = 0.8f;
                affix.SpeedMultiplier = 0.9f;
                affix.TintOverride = new Color(0.8f, 0.8f, 0.8f, 1f); // Silver
                affix.ScaleMultiplier = 1.05f;
                affix.Effect = AffixEffect.ReflectOnHit;
                affix.EffectValue = 0.3f; // Reflect 30% of damage taken
                affix.AddBehaviorTags = new string[] { "CanBlock" };
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Elite Affix Assets Created",
                $"Created in: {AFFIX_DIR}/\n\n" +
                "• Berserk.asset (+50% dmg, +30% spd, berserk at 30% HP)\n" +
                "• Shielded.asset (+100% HP, SuperArmor, shield regen)\n" +
                "• Explosive.asset (AoE on death, 3u radius, 20 dmg)\n" +
                "• Vampiric.asset (+20% HP, heals 20% of dmg dealt)\n" +
                "• Reflective.asset (+50% HP, reflects 30% dmg, CanBlock)\n\n" +
                "Assign to EnemySpawner's _possibleAffixes array to enable elite spawning.",
                "OK"
            );
        }

        private static void CreateAffixAsset(string name, System.Action<EnemyAffixSO> configure)
        {
            string path = $"{AFFIX_DIR}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyAffixSO>(path);
            if (existing != null)
            {
                Debug.Log($"[EnemyAssetCreator] Affix already exists: {path}");
                return;
            }

            var affix = ScriptableObject.CreateInstance<EnemyAffixSO>();
            affix.AffixName = name;
            configure(affix);
            AssetDatabase.CreateAsset(affix, path);
            Debug.Log($"[EnemyAssetCreator] Created Affix: {path}");
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
