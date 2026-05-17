using ProjectArk.Combat.Enemy;
using ProjectArk.Combat.HyperWind;
using ProjectArk.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// Explicit editor entry for assembling the HyperWind Slice D' test arena scene.
    /// </summary>
    public static class HyperWindArenaSceneConfigurator
    {
        private const string ScenePath = "Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity";
        private const string RuntimeRootName = "HyperWindRuntimeRoot";
        private const string PoolManagerName = "HyperWindPoolManager";
        private const string TestShipName = "HyperWind_TestShip";
        private const string PlayerProjectilePath = "Assets/_Data/StarChart/Prefabs/Projectile_Matter.prefab";
        private const string EnemyProjectilePath = "Assets/_Prefabs/Enemies/EnemyProjectile.prefab";

        [MenuItem("ProjectArk/HyperWind/Configure Slice D Test Arena")]
        public static void ConfigureSliceDTestArena()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject runtimeRoot = GameObject.Find(RuntimeRootName);
            if (runtimeRoot == null)
            {
                runtimeRoot = new GameObject(RuntimeRootName);
                Undo.RegisterCreatedObjectUndo(runtimeRoot, "Create HyperWind runtime root");
            }

            EnsurePoolManager();
            ConfigureCycloneSpawner(runtimeRoot);
            ConfigureArenaDirector(runtimeRoot);

            EditorUtility.SetDirty(runtimeRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HyperWindArenaSceneConfigurator] Slice D' test arena configured: pooled test fire, enemy backlash volleys, cyclone lane, and explicit PoolManager are ready.");
        }

        private static void EnsurePoolManager()
        {
            PoolManager existing = Object.FindFirstObjectByType<PoolManager>();
            if (existing != null)
            {
                return;
            }

            var poolManagerObject = new GameObject(PoolManagerName);
            Undo.RegisterCreatedObjectUndo(poolManagerObject, "Create HyperWind PoolManager");
            poolManagerObject.AddComponent<PoolManager>();
        }

        private static void ConfigureCycloneSpawner(GameObject runtimeRoot)
        {
            GroundCycloneSpawner spawner = runtimeRoot.GetComponent<GroundCycloneSpawner>();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<GroundCycloneSpawner>(runtimeRoot);
            }

            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("_spawnArea").rectValue = new Rect(-5f, -5f, 10f, 10f);
            serialized.FindProperty("_spawnInterval").floatValue = 10f;
            serialized.FindProperty("_minSpawnCount").intValue = 1;
            serialized.FindProperty("_maxSpawnCount").intValue = 2;
            serialized.FindProperty("_spawnOnStart").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawner);
        }

        private static void ConfigureArenaDirector(GameObject runtimeRoot)
        {
            HyperWindArenaTestDirector director = runtimeRoot.GetComponent<HyperWindArenaTestDirector>();
            if (director == null)
            {
                director = Undo.AddComponent<HyperWindArenaTestDirector>(runtimeRoot);
            }

            Projectile playerProjectile = LoadPrefabComponent<Projectile>(PlayerProjectilePath);
            EnemyProjectile enemyProjectile = LoadPrefabComponent<EnemyProjectile>(EnemyProjectilePath);
            GameObject testShip = GameObject.Find(TestShipName);

            var serialized = new SerializedObject(director);
            serialized.FindProperty("_arenaBounds").rectValue = new Rect(-12f, -7f, 24f, 14f);
            serialized.FindProperty("_cycloneLane").rectValue = new Rect(-5f, -5f, 10f, 10f);
            serialized.FindProperty("_enablePlayerTestFire").boolValue = true;
            serialized.FindProperty("_playerProjectilePrefab").objectReferenceValue = playerProjectile;
            serialized.FindProperty("_playerFireOrigin").objectReferenceValue = testShip != null ? testShip.transform : null;
            serialized.FindProperty("_playerFireInterval").floatValue = 0.16f;
            serialized.FindProperty("_playerProjectileSpeed").floatValue = 11f;
            serialized.FindProperty("_playerProjectileDamage").floatValue = 10f;
            serialized.FindProperty("_playerProjectileLifetime").floatValue = 4.5f;
            serialized.FindProperty("_enableEnemyVolley").boolValue = true;
            serialized.FindProperty("_enemyProjectilePrefab").objectReferenceValue = enemyProjectile;
            serialized.FindProperty("_enemyTarget").objectReferenceValue = testShip != null ? testShip.transform : null;
            serialized.FindProperty("_enemyFireInterval").floatValue = 1.25f;
            serialized.FindProperty("_enemyProjectileSpeed").floatValue = 7.5f;
            serialized.FindProperty("_enemyProjectileDamage").floatValue = 8f;
            serialized.FindProperty("_enemyProjectileLifetime").floatValue = 5.5f;
            serialized.FindProperty("_createPoolManagerIfMissing").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);

            if (playerProjectile == null || enemyProjectile == null || testShip == null)
            {
                Debug.LogWarning("[HyperWindArenaSceneConfigurator] Arena configured with missing references. Check player projectile prefab, enemy projectile prefab, and HyperWind_TestShip.", director);
            }
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }
    }
}
