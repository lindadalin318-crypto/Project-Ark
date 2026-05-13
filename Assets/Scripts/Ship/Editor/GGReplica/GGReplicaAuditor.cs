#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Read-only auditor for the isolated GGReplica ship experiment.
    /// Verifies required experimental assets exist and the live Ship/SampleScene chain is not polluted.
    /// </summary>
    public static class GGReplicaAuditor
    {
        public enum Severity
        {
            Info,
            Warning,
            Error
        }

        public sealed class AuditResult
        {
            public Severity Severity;
            public string Scope;
            public string Message;
            public UnityEngine.Object TargetObject;
        }

        private const string MenuPath = "ProjectArk/Ship/GG Replica/Audit Replica Isolation";
        private const string LiveShipPath = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string ReplicaShipPath = "Assets/_Prefabs/Ship/Ship_GGReplica.prefab";
        private const string VisualProfilePath = "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset";
        private const string FeelProfilePath = "Assets/_Data/Ship/GGReplicaShipFeelProfile.asset";
        private const string TestScenePath = "Assets/Scenes/GGReplicaShipTest.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ArtRootPath = "Assets/_Art/Ship/GGReplica";

        private static readonly List<AuditResult> _lastResults = new List<AuditResult>();

        public static IReadOnlyList<AuditResult> LastResults => _lastResults;

        [MenuItem(MenuPath)]
        public static void RunAuditMenu()
        {
            RunAudit(logToConsole: true);
        }

        public static IReadOnlyList<AuditResult> RunAudit(bool logToConsole = true)
        {
            _lastResults.Clear();

            var live = AssetDatabase.LoadAssetAtPath<GameObject>(LiveShipPath);
            var replica = AssetDatabase.LoadAssetAtPath<GameObject>(ReplicaShipPath);
            var visualProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipVisualProfileSO>(VisualProfilePath);
            var feelProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipFeelProfileSO>(FeelProfilePath);
            var testScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath);

            ValidateRequiredAssets(live, replica, visualProfile, feelProfile, testScene);
            ValidateLiveShipIsolation(live);
            ValidateReplicaPrefab(replica, visualProfile, feelProfile);
            ValidateSceneIsolation();

            if (logToConsole)
            {
                LogResultsToConsole();
            }

            return _lastResults;
        }

        private static void ValidateRequiredAssets(
            GameObject live,
            GameObject replica,
            GGReplicaShipVisualProfileSO visualProfile,
            GGReplicaShipFeelProfileSO feelProfile,
            SceneAsset testScene)
        {
            if (live == null) AddResult(Severity.Error, "Assets", $"Missing live ship prefab: `{LiveShipPath}`.", null);
            if (replica == null) AddResult(Severity.Error, "Assets", $"Missing replica prefab: `{ReplicaShipPath}`.", null);
            if (visualProfile == null) AddResult(Severity.Error, "Assets", $"Missing visual profile: `{VisualProfilePath}`.", null);
            if (feelProfile == null) AddResult(Severity.Error, "Assets", $"Missing feel profile: `{FeelProfilePath}`.", null);
            if (testScene == null) AddResult(Severity.Error, "Assets", $"Missing A/B test scene: `{TestScenePath}`.", null);
            if (!AssetDatabase.IsValidFolder(ArtRootPath)) AddResult(Severity.Error, "Assets", $"Missing curated art root: `{ArtRootPath}`.", null);
        }

        private static void ValidateLiveShipIsolation(GameObject live)
        {
            if (live == null) return;

            ValidateMissingComponent<GGReplicaShipViewAdapter>(live, "Live Ship.prefab has GGReplicaShipViewAdapter. This violates isolation.");
            ValidateMissingComponent<GGReplicaBoostVfxAdapter>(live, "Live Ship.prefab has GGReplicaBoostVfxAdapter. This violates isolation.");
            ValidateMissingComponent<GGReplicaDashVfxAdapter>(live, "Live Ship.prefab has GGReplicaDashVfxAdapter. This violates isolation.");
            ValidateMissingComponent<GGReplicaShipFeelAdapter>(live, "Live Ship.prefab has GGReplicaShipFeelAdapter. This violates isolation.");
            ValidateMissingComponent<GGReplicaTestSwitcher>(live, "Live Ship.prefab has GGReplicaTestSwitcher. This violates isolation.");
        }

        private static void ValidateReplicaPrefab(
            GameObject replica,
            GGReplicaShipVisualProfileSO visualProfile,
            GGReplicaShipFeelProfileSO feelProfile)
        {
            if (replica == null) return;

            var view = replica.GetComponent<GGReplicaShipViewAdapter>();
            var boost = replica.GetComponent<GGReplicaBoostVfxAdapter>();
            var dash = replica.GetComponent<GGReplicaDashVfxAdapter>();
            var feel = replica.GetComponent<GGReplicaShipFeelAdapter>();
            var audioSource = replica.GetComponent<AudioSource>();
            var rigidbody = replica.GetComponent<Rigidbody2D>();

            ValidateRequiredComponent(view, replica, "Ship_GGReplica missing GGReplicaShipViewAdapter.");
            ValidateRequiredComponent(boost, replica, "Ship_GGReplica missing GGReplicaBoostVfxAdapter.");
            ValidateRequiredComponent(dash, replica, "Ship_GGReplica missing GGReplicaDashVfxAdapter.");
            ValidateRequiredComponent(feel, replica, "Ship_GGReplica missing GGReplicaShipFeelAdapter.");
            ValidateRequiredComponent(audioSource, replica, "Ship_GGReplica missing AudioSource.");
            ValidateRequiredComponent(rigidbody, replica, "Ship_GGReplica missing Rigidbody2D.");

            if (view != null)
            {
                var so = new SerializedObject(view);
                ValidateObjectReference(so, "_profile", visualProfile, "Replica Prefab", "GGReplicaShipViewAdapter._profile is not wired to GGReplica visual profile.", replica);
                ValidateNotNull(so, "_solidRenderer", "Replica Prefab", "GGReplicaShipViewAdapter._solidRenderer is not wired.", replica);
                ValidateNotNull(so, "_liquidRenderer", "Replica Prefab", "GGReplicaShipViewAdapter._liquidRenderer is not wired.", replica);
                ValidateNotNull(so, "_highlightRenderer", "Replica Prefab", "GGReplicaShipViewAdapter._highlightRenderer is not wired.", replica);
                ValidateNotNull(so, "_dodgeGhostRenderer", "Replica Prefab", "GGReplicaShipViewAdapter._dodgeGhostRenderer is not wired.", replica);
            }

            if (boost != null)
            {
                var so = new SerializedObject(boost);
                ValidateObjectReference(so, "_profile", visualProfile, "Replica Prefab", "GGReplicaBoostVfxAdapter._profile is not wired to GGReplica visual profile.", replica);
                ValidateObjectReference(so, "_audioSource", audioSource, "Replica Prefab", "GGReplicaBoostVfxAdapter._audioSource is not wired to root AudioSource.", replica);
                ValidateNotNull(so, "_boost", "Replica Prefab", "GGReplicaBoostVfxAdapter._boost is not wired.", replica);
            }

            if (dash != null)
            {
                var so = new SerializedObject(dash);
                ValidateObjectReference(so, "_profile", visualProfile, "Replica Prefab", "GGReplicaDashVfxAdapter._profile is not wired to GGReplica visual profile.", replica);
                ValidateObjectReference(so, "_audioSource", audioSource, "Replica Prefab", "GGReplicaDashVfxAdapter._audioSource is not wired to root AudioSource.", replica);
                ValidateNotNull(so, "_dash", "Replica Prefab", "GGReplicaDashVfxAdapter._dash is not wired.", replica);
            }

            if (feel != null)
            {
                var so = new SerializedObject(feel);
                ValidateObjectReference(so, "_profile", feelProfile, "Replica Prefab", "GGReplicaShipFeelAdapter._profile is not wired to GGReplica feel profile.", replica);
                ValidateObjectReference(so, "_rigidbody", rigidbody, "Replica Prefab", "GGReplicaShipFeelAdapter._rigidbody is not wired to root Rigidbody2D.", replica);
                ValidateNotNull(so, "_dash", "Replica Prefab", "GGReplicaShipFeelAdapter._dash is not wired.", replica);
                ValidateNotNull(so, "_boost", "Replica Prefab", "GGReplicaShipFeelAdapter._boost is not wired.", replica);
            }
        }

        private static void ValidateSceneIsolation()
        {
            string absoluteSampleScenePath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, SampleScenePath);
            if (!File.Exists(absoluteSampleScenePath)) return;

            string yaml = File.ReadAllText(absoluteSampleScenePath);
            if (yaml.Contains("Ship_GGReplica", StringComparison.Ordinal) || yaml.Contains("GGReplicaTestSwitcher", StringComparison.Ordinal))
            {
                AddResult(Severity.Error, "Scene Isolation", "SampleScene contains GGReplica references. This violates the replica isolation lane.", null);
            }
        }

        private static void ValidateMissingComponent<T>(GameObject prefab, string message) where T : Component
        {
            if (prefab.GetComponent<T>() != null)
            {
                AddResult(Severity.Error, "Live Ship Isolation", message, prefab);
            }
        }

        private static void ValidateRequiredComponent(Component component, UnityEngine.Object target, string message)
        {
            if (component == null)
            {
                AddResult(Severity.Error, "Replica Prefab", message, target);
            }
        }

        private static void ValidateObjectReference(SerializedObject serializedObject, string propertyPath, UnityEngine.Object expectedValue, string scope, string message, UnityEngine.Object target)
        {
            var prop = serializedObject.FindProperty(propertyPath);
            if (prop == null)
            {
                AddResult(Severity.Error, scope, $"Missing serialized property `{propertyPath}`.", target);
                return;
            }

            if (expectedValue == null || prop.objectReferenceValue != expectedValue)
            {
                AddResult(Severity.Error, scope, message, target);
            }
        }

        private static void ValidateNotNull(SerializedObject serializedObject, string propertyPath, string scope, string message, UnityEngine.Object target)
        {
            var prop = serializedObject.FindProperty(propertyPath);
            if (prop == null)
            {
                AddResult(Severity.Error, scope, $"Missing serialized property `{propertyPath}`.", target);
                return;
            }

            if (prop.objectReferenceValue == null)
            {
                AddResult(Severity.Error, scope, message, target);
            }
        }

        private static void AddResult(Severity severity, string scope, string message, UnityEngine.Object targetObject)
        {
            _lastResults.Add(new AuditResult
            {
                Severity = severity,
                Scope = scope,
                Message = message,
                TargetObject = targetObject
            });
        }

        private static void LogResultsToConsole()
        {
            if (_lastResults.Count == 0)
            {
                Debug.Log("[GGReplicaAuditor] PASS: GGReplica is isolated from live Ship.prefab and SampleScene.");
                return;
            }

            foreach (var result in _lastResults)
            {
                string line = $"[GGReplicaAuditor][{result.Scope}][{result.Severity}] {result.Message}";
                switch (result.Severity)
                {
                    case Severity.Error:
                        Debug.LogError(line, result.TargetObject);
                        break;
                    case Severity.Warning:
                        Debug.LogWarning(line, result.TargetObject);
                        break;
                    default:
                        Debug.Log(line, result.TargetObject);
                        break;
                }
            }

            Debug.Log($"[GGReplicaAuditor] Audit complete: {_lastResults.Count(r => r.Severity == Severity.Error)} errors, {_lastResults.Count(r => r.Severity == Severity.Warning)} warnings.");
        }
    }
}
#endif
