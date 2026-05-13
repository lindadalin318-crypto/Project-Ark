#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Builds only the isolated GGReplica ship prefab from the live ship prefab plus
    /// replica-only visual wiring. This tool never saves changes back to Ship.prefab.
    /// </summary>
    public static class GGReplicaPrefabBuilder
    {
        private const string SourceShipPrefabPath = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string TargetPrefabPath = "Assets/_Prefabs/Ship/Ship_GGReplica.prefab";
        private const string VisualProfilePath = "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset";
        private const string FeelProfilePath = "Assets/_Data/Ship/GGReplicaShipFeelProfile.asset";
        private const string TargetRootName = "Ship_GGReplica";
        private const string ShipVisualName = "ShipVisual";

        [MenuItem("ProjectArk/Ship/GG Replica/Build Experimental Prefab")]
        public static void BuildExperimentalPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceShipPrefabPath);
            if (source == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing source prefab: {SourceShipPrefabPath}");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SourceShipPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Failed to load prefab contents: {SourceShipPrefabPath}");
                return;
            }

            try
            {
                root.name = TargetRootName;
                var visualProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipVisualProfileSO>(VisualProfilePath);
                if (visualProfile == null)
                {
                    Debug.LogError($"[GGReplicaPrefabBuilder] Missing visual profile: {VisualProfilePath}");
                }

                var feelProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipFeelProfileSO>(FeelProfilePath);
                if (feelProfile == null)
                {
                    Debug.LogError($"[GGReplicaPrefabBuilder] Missing feel profile: {FeelProfilePath}");
                }

                EnsureAdapter(root, visualProfile);
                EnsureAudioAdapters(root, visualProfile);
                EnsureFeelAdapter(root, feelProfile);
                PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath, out bool success);
                if (!success)
                {
                    Debug.LogError($"[GGReplicaPrefabBuilder] Failed to save {TargetPrefabPath}");
                    return;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[GGReplicaPrefabBuilder] Built {TargetPrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureAdapter(GameObject root, GGReplicaShipVisualProfileSO visualProfile)
        {
            var adapter = root.GetComponent<GGReplicaShipViewAdapter>();
            if (adapter == null)
            {
                adapter = root.AddComponent<GGReplicaShipViewAdapter>();
            }

            var visual = root.transform.Find(ShipVisualName);
            if (visual == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing {ShipVisualName} child on source Ship prefab.");
                return;
            }

            SetSerialized(adapter, "_profile", visualProfile);
            SetSerialized(adapter, "_backRenderer", FindRenderer(visual, "Ship_Sprite_Back"));
            SetSerialized(adapter, "_liquidRenderer", FindRenderer(visual, "Ship_Sprite_Liquid"));
            SetSerialized(adapter, "_highlightRenderer", FindRenderer(visual, "Ship_Sprite_HL"));
            SetSerialized(adapter, "_solidRenderer", FindRenderer(visual, "Ship_Sprite_Solid"));
            SetSerialized(adapter, "_coreRenderer", FindRenderer(visual, "Ship_Sprite_Core"));
            SetSerialized(adapter, "_dodgeGhostRenderer", FindRenderer(visual, "Dodge_Sprite"));
            SetSerialized(adapter, "_stateController", root.GetComponent<ShipStateController>());
            SetSerialized(adapter, "_boost", root.GetComponent<ShipBoost>());
            SetSerialized(adapter, "_dash", root.GetComponent<ShipDash>());
        }

        private static void EnsureAudioAdapters(GameObject root, GGReplicaShipVisualProfileSO visualProfile)
        {
            var audioSource = root.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = root.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            var boostAdapter = root.GetComponent<GGReplicaBoostVfxAdapter>();
            if (boostAdapter == null)
            {
                boostAdapter = root.AddComponent<GGReplicaBoostVfxAdapter>();
            }

            SetSerialized(boostAdapter, "_profile", visualProfile);
            SetSerialized(boostAdapter, "_audioSource", audioSource);
            SetSerialized(boostAdapter, "_boost", root.GetComponent<ShipBoost>());

            var dashAdapter = root.GetComponent<GGReplicaDashVfxAdapter>();
            if (dashAdapter == null)
            {
                dashAdapter = root.AddComponent<GGReplicaDashVfxAdapter>();
            }

            SetSerialized(dashAdapter, "_profile", visualProfile);
            SetSerialized(dashAdapter, "_audioSource", audioSource);
            SetSerialized(dashAdapter, "_dash", root.GetComponent<ShipDash>());
        }

        private static void EnsureFeelAdapter(GameObject root, GGReplicaShipFeelProfileSO feelProfile)
        {
            var feelAdapter = root.GetComponent<GGReplicaShipFeelAdapter>();
            if (feelAdapter == null)
            {
                feelAdapter = root.AddComponent<GGReplicaShipFeelAdapter>();
            }

            SetSerialized(feelAdapter, "_profile", feelProfile);
            SetSerialized(feelAdapter, "_rigidbody", root.GetComponent<Rigidbody2D>());
            SetSerialized(feelAdapter, "_dash", root.GetComponent<ShipDash>());
            SetSerialized(feelAdapter, "_boost", root.GetComponent<ShipBoost>());
        }

        private static SpriteRenderer FindRenderer(Transform root, string childName)
        {
            var child = root.Find(childName);
            if (child == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing child '{childName}' under {root.name}.");
                return null;
            }

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing SpriteRenderer on '{childName}'.");
            }

            return renderer;
        }

        private static void SetSerialized(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing serialized property '{propertyName}' on {target.name}.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
