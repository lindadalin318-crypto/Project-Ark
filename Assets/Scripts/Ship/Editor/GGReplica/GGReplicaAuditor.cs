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
        private const string LegacyAudioProfilePath = "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset";
        private const string FeelProfilePath = "Assets/_Data/Ship/GGReplicaShipFeelProfile.asset";
        private const string PlayerSkinPath = "Assets/_Data/Ship/GGReplicaPlayerSkin.asset";
        private const string TestScenePath = "Assets/Scenes/GGReplicaShipTest.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ArtRootPath = "Assets/_Art/Ship/GGReplica";
        private const string PlayerShipHlMaterialPath = GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath;
        private const string TeleportSchemeMaterialPath = GGReplicaMaterialAssetBuilder.TeleportSchemeMaterialPath;
        private const string PlayerLqTrailMaterialPath = GGReplicaMaterialAssetBuilder.PlayerLqTrailMaterialPath;

        private static readonly string[] GgReplicaScriptPaths =
        {
            "Assets/Scripts/Ship/GGReplica/GGReplicaBoostVfxAdapter.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaBoostVisualModule.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaCoreVisualModule.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaDashVfxAdapter.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaPlayerSkinSO.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaMaterialVisualModule.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaShapeVisualModule.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelAdapter.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelProfileSO.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaShipViewAdapter.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaShipVisualProfileSO.cs",
            "Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs"
        };

        private static readonly GGReplicaViewState[] RequiredStates =
        {
            GGReplicaViewState.Idle,
            GGReplicaViewState.Boost,
            GGReplicaViewState.Dodge,
            GGReplicaViewState.Aim,
            GGReplicaViewState.Fire,
            GGReplicaViewState.HeavyFire,
            GGReplicaViewState.HeavyAim,
            GGReplicaViewState.Grab,
            GGReplicaViewState.WeaponUseMoment,
            GGReplicaViewState.Heal,
            GGReplicaViewState.Undefined
        };

        private static readonly string[] RequiredViewChildren =
        {
            "Ship_Sprite_Liquid",
            "Ship_Sprite_HL",
            "Dodge_Sprite",
            "Ship_Sprite_Solid",
            "Ship_Sprite_Back",
            "Ship_Sprite_Solid_Grab_R",
            "Ship_Sprite_Solid_Grab_L",
            "Core_Sprite_Reactor",
            "Core_Sprite_Eye",
            "View",
            "Dodge_Half_Sprite"
        };

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
            var legacyAudioProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipVisualProfileSO>(LegacyAudioProfilePath);
            var feelProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipFeelProfileSO>(FeelProfilePath);
            var playerSkin = AssetDatabase.LoadAssetAtPath<GGReplicaPlayerSkinSO>(PlayerSkinPath);
            var testScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath);

            ValidateRequiredAssets(live, replica, legacyAudioProfile, feelProfile, playerSkin, testScene);
            ValidatePlayerSkin(playerSkin);
            ValidateLiveShipIsolation(live);
            ValidateReplicaPrefab(replica, legacyAudioProfile, feelProfile, playerSkin);
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
            GGReplicaShipVisualProfileSO legacyAudioProfile,
            GGReplicaShipFeelProfileSO feelProfile,
            GGReplicaPlayerSkinSO playerSkin,
            SceneAsset testScene)
        {
            if (live == null) AddResult(Severity.Error, "Assets", $"Missing live ship prefab: `{LiveShipPath}`.", null);
            if (replica == null) AddResult(Severity.Error, "Assets", $"Missing replica prefab: `{ReplicaShipPath}`.", null);
            if (legacyAudioProfile == null) AddResult(Severity.Error, "Assets", $"Missing legacy GGReplica audio profile: `{LegacyAudioProfilePath}`.", null);
            if (feelProfile == null) AddResult(Severity.Error, "Assets", $"Missing feel profile: `{FeelProfilePath}`.", null);
            if (playerSkin == null) AddResult(Severity.Error, "Assets", $"Missing GGReplicaPlayerSkin: `{PlayerSkinPath}`.", null);
            if (testScene == null) AddResult(Severity.Error, "Assets", $"Missing A/B test scene: `{TestScenePath}`.", null);
            if (!AssetDatabase.IsValidFolder(ArtRootPath)) AddResult(Severity.Error, "Assets", $"Missing curated art root: `{ArtRootPath}`.", null);
        }

        private static void ValidatePlayerSkin(GGReplicaPlayerSkinSO playerSkin)
        {
            if (playerSkin == null) return;

            foreach (var state in RequiredStates)
            {
                if (!playerSkin.TryGetPack(state, out var pack))
                {
                    AddResult(Severity.Error, "PlayerSkin", $"GGReplicaPlayerSkin missing ViewState pack: {state} ({(int)state}).", playerSkin);
                    continue;
                }

                if (state == GGReplicaViewState.Dodge)
                {
                    if (pack.SolidSprite != null || pack.LiquidSprite != null || pack.HighlightSprite != null)
                    {
                        AddResult(Severity.Error, "PlayerSkin", "GGReplicaPlayerSkin Dodge pack must keep Solid/Liquid/Highlight sprites null.", playerSkin);
                    }
                }
                else if (pack.SolidSprite == null || pack.LiquidSprite == null || pack.HighlightSprite == null)
                {
                    AddResult(Severity.Error, "PlayerSkin", $"GGReplicaPlayerSkin pack {state} has missing body sprite references.", playerSkin);
                }
            }

            ValidateSprite(playerSkin.ShipSpriteSolidGrabR, "ShipSpriteSolidGrabR", playerSkin);
            ValidateSprite(playerSkin.ShipSpriteSolidGrabL, "ShipSpriteSolidGrabL", playerSkin);
            ValidateSprite(playerSkin.ShipSpriteBack, "ShipSpriteBack", playerSkin);
            ValidateSprite(playerSkin.ReactorSprite, "ReactorSprite", playerSkin);
            ValidateSprite(playerSkin.ViewSilhouetteSprite, "ViewSilhouetteSprite", playerSkin);
            ValidateSprite(playerSkin.DodgeSprite, "DodgeSprite", playerSkin);
            ValidateSprite(playerSkin.DodgeHalfSprite, "DodgeHalfSprite", playerSkin);

            if (playerSkin.EyeSprite == null)
            {
                AddResult(Severity.Error, "PlayerSkin", "GGReplicaPlayerSkin EyeSprite is not assigned.", playerSkin);
            }
            else if (playerSkin.EyeSprite == playerSkin.ReactorSprite)
            {
                AddResult(Severity.Warning, "PlayerSkin", "GGReplicaPlayerSkin EyeSprite temporarily reuses ReactorSprite.", playerSkin);
            }
        }

        private static void ValidateSprite(Sprite sprite, string fieldName, UnityEngine.Object target)
        {
            if (sprite == null)
            {
                AddResult(Severity.Error, "PlayerSkin", $"GGReplicaPlayerSkin {fieldName} is not assigned.", target);
            }
        }

        private static void ValidateLiveShipIsolation(GameObject live)
        {
            if (live == null) return;

            ValidateMissingComponentInChildren<GGReplicaShipViewAdapter>(live, "Live Ship.prefab has GGReplicaShipViewAdapter. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaPlayerViewAdapter>(live, "Live Ship.prefab has GGReplicaPlayerViewAdapter. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaCoreVisualModule>(live, "Live Ship.prefab has GGReplicaCoreVisualModule. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaBoostVisualModule>(live, "Live Ship.prefab has GGReplicaBoostVisualModule. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaShapeVisualModule>(live, "Live Ship.prefab has GGReplicaShapeVisualModule. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaMaterialVisualModule>(live, "Live Ship.prefab has GGReplicaMaterialVisualModule. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaBoostVfxAdapter>(live, "Live Ship.prefab has GGReplicaBoostVfxAdapter. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaDashVfxAdapter>(live, "Live Ship.prefab has GGReplicaDashVfxAdapter. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaShipFeelAdapter>(live, "Live Ship.prefab has GGReplicaShipFeelAdapter. This violates isolation.");
            ValidateMissingComponentInChildren<GGReplicaTestSwitcher>(live, "Live Ship.prefab has GGReplicaTestSwitcher. This violates isolation.");
        }

        private static void ValidateReplicaPrefab(
            GameObject replica,
            GGReplicaShipVisualProfileSO legacyAudioProfile,
            GGReplicaShipFeelProfileSO feelProfile,
            GGReplicaPlayerSkinSO playerSkin)
        {
            if (replica == null) return;

            var legacyView = replica.GetComponent<GGReplicaShipViewAdapter>();
            var playerView = replica.GetComponent<GGReplicaPlayerViewAdapter>();
            var coreModule = replica.GetComponent<GGReplicaCoreVisualModule>();
            var boostModule = replica.GetComponent<GGReplicaBoostVisualModule>();
            var shapeModule = replica.GetComponent<GGReplicaShapeVisualModule>();
            var materialModule = replica.GetComponent<GGReplicaMaterialVisualModule>();
            var boost = replica.GetComponent<GGReplicaBoostVfxAdapter>();
            var dash = replica.GetComponent<GGReplicaDashVfxAdapter>();
            var feel = replica.GetComponent<GGReplicaShipFeelAdapter>();
            var audioSource = replica.GetComponent<AudioSource>();
            var rigidbody = replica.GetComponent<Rigidbody2D>();

            if (legacyView != null) AddResult(Severity.Error, "Replica Prefab", "Ship_GGReplica still has legacy GGReplicaShipViewAdapter.", replica);
            ValidateRequiredComponent(playerView, replica, "Ship_GGReplica missing GGReplicaPlayerViewAdapter.");
            ValidateRequiredComponent(coreModule, replica, "Ship_GGReplica missing GGReplicaCoreVisualModule.");
            ValidateRequiredComponent(boostModule, replica, "Ship_GGReplica missing GGReplicaBoostVisualModule.");
            ValidateRequiredComponent(shapeModule, replica, "Ship_GGReplica missing GGReplicaShapeVisualModule.");
            ValidateRequiredComponent(materialModule, replica, "Ship_GGReplica missing GGReplicaMaterialVisualModule.");
            ValidateRequiredComponent(boost, replica, "Ship_GGReplica missing GGReplicaBoostVfxAdapter.");
            ValidateRequiredComponent(dash, replica, "Ship_GGReplica missing GGReplicaDashVfxAdapter.");
            ValidateRequiredComponent(feel, replica, "Ship_GGReplica missing GGReplicaShipFeelAdapter.");
            ValidateRequiredComponent(audioSource, replica, "Ship_GGReplica missing AudioSource.");
            ValidateRequiredComponent(rigidbody, replica, "Ship_GGReplica missing Rigidbody2D.");

            var viewRoot = replica.transform.Find("ShipVisual/GGPlayerViewRoot");
            if (viewRoot == null)
            {
                AddResult(Severity.Error, "Replica Prefab", "Ship_GGReplica missing GGPlayerViewRoot.", replica);
            }
            else
            {
                if (viewRoot.childCount != RequiredViewChildren.Length)
                {
                    AddResult(Severity.Error, "Replica Prefab", $"GGPlayerViewRoot should contain {RequiredViewChildren.Length} render children but has {viewRoot.childCount}.", replica);
                }

                foreach (var childName in RequiredViewChildren)
                {
                    ValidateViewChild(viewRoot, childName, replica);
                }

                ValidateRendererMaterial(viewRoot, "Ship_Sprite_HL", PlayerShipHlMaterialPath, replica);
                ValidateRendererMaterial(viewRoot, "View", TeleportSchemeMaterialPath, replica);
            }

            ValidateTrailMaterial(replica, PlayerLqTrailMaterialPath);

            if (playerView != null)
            {
                var so = new SerializedObject(playerView);
                ValidateObjectReference(so, "_skin", playerSkin, "Replica Prefab", "GGReplicaPlayerViewAdapter._skin is not wired to GGReplicaPlayerSkin.", replica);
                ValidateObjectReference(so, "_spritesRoot", viewRoot, "Replica Prefab", "GGReplicaPlayerViewAdapter._spritesRoot is not wired to GGPlayerViewRoot.", replica);
                ValidateRendererReference(so, "_shipLiquidRenderer", viewRoot, "Ship_Sprite_Liquid", replica);
                ValidateRendererReference(so, "_shipHighlightRenderer", viewRoot, "Ship_Sprite_HL", replica);
                ValidateRendererReference(so, "_dodgeRenderer", viewRoot, "Dodge_Sprite", replica);
                ValidateRendererReference(so, "_shipSolidRenderer", viewRoot, "Ship_Sprite_Solid", replica);
                ValidateRendererReference(so, "_shipBackRenderer", viewRoot, "Ship_Sprite_Back", replica);
                ValidateRendererReference(so, "_shipGrabRightRenderer", viewRoot, "Ship_Sprite_Solid_Grab_R", replica);
                ValidateRendererReference(so, "_shipGrabLeftRenderer", viewRoot, "Ship_Sprite_Solid_Grab_L", replica);
                ValidateRendererReference(so, "_coreRenderer", viewRoot, "Core_Sprite_Reactor", replica);
                ValidateRendererReference(so, "_eyeRenderer", viewRoot, "Core_Sprite_Eye", replica);
                ValidateRendererReference(so, "_viewSilhouetteRenderer", viewRoot, "View", replica);
                ValidateRendererReference(so, "_dodgeHalfRenderer", viewRoot, "Dodge_Half_Sprite", replica);
                ValidateObjectReference(so, "_coreModule", coreModule, "Replica Prefab", "GGReplicaPlayerViewAdapter._coreModule is not wired.", replica);
                ValidateObjectReference(so, "_boostModule", boostModule, "Replica Prefab", "GGReplicaPlayerViewAdapter._boostModule is not wired.", replica);
                ValidateObjectReference(so, "_shapeModule", shapeModule, "Replica Prefab", "GGReplicaPlayerViewAdapter._shapeModule is not wired.", replica);
                ValidateObjectReference(so, "_materialModule", materialModule, "Replica Prefab", "GGReplicaPlayerViewAdapter._materialModule is not wired.", replica);
                so.Dispose();
            }

            if (boost != null)
            {
                var so = new SerializedObject(boost);
                ValidateObjectReference(so, "_profile", legacyAudioProfile, "Replica Prefab", "GGReplicaBoostVfxAdapter._profile is not wired to legacy GGReplica audio profile.", replica);
                ValidateObjectReference(so, "_audioSource", audioSource, "Replica Prefab", "GGReplicaBoostVfxAdapter._audioSource is not wired to root AudioSource.", replica);
                ValidateNotNull(so, "_boost", "Replica Prefab", "GGReplicaBoostVfxAdapter._boost is not wired.", replica);
                so.Dispose();
            }

            if (dash != null)
            {
                var so = new SerializedObject(dash);
                ValidateObjectReference(so, "_profile", legacyAudioProfile, "Replica Prefab", "GGReplicaDashVfxAdapter._profile is not wired to legacy GGReplica audio profile.", replica);
                ValidateObjectReference(so, "_audioSource", audioSource, "Replica Prefab", "GGReplicaDashVfxAdapter._audioSource is not wired to root AudioSource.", replica);
                ValidateNotNull(so, "_dash", "Replica Prefab", "GGReplicaDashVfxAdapter._dash is not wired.", replica);
                so.Dispose();
            }

            if (feel != null)
            {
                var so = new SerializedObject(feel);
                ValidateObjectReference(so, "_profile", feelProfile, "Replica Prefab", "GGReplicaShipFeelAdapter._profile is not wired to GGReplica feel profile.", replica);
                ValidateObjectReference(so, "_rigidbody", rigidbody, "Replica Prefab", "GGReplicaShipFeelAdapter._rigidbody is not wired to root Rigidbody2D.", replica);
                ValidateNotNull(so, "_dash", "Replica Prefab", "GGReplicaShipFeelAdapter._dash is not wired.", replica);
                ValidateNotNull(so, "_boost", "Replica Prefab", "GGReplicaShipFeelAdapter._boost is not wired.", replica);
                so.Dispose();
            }
        }

        private static void ValidateViewChild(Transform viewRoot, string childName, UnityEngine.Object target)
        {
            var child = viewRoot.Find(childName);
            if (child == null)
            {
                AddResult(Severity.Error, "Replica Prefab", $"Missing required GGPlayerViewRoot child: {childName}.", target);
                return;
            }

            if (child.GetComponent<SpriteRenderer>() == null)
            {
                AddResult(Severity.Error, "Replica Prefab", $"Missing SpriteRenderer on GGPlayerViewRoot child: {childName}.", target);
            }
        }

        private static void ValidateRendererMaterial(Transform viewRoot, string childName, string materialPath, UnityEngine.Object target)
        {
            var renderer = viewRoot.Find(childName)?.GetComponent<SpriteRenderer>();
            var expected = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (expected == null)
            {
                AddResult(Severity.Error, "Replica Prefab", $"Missing expected GGReplica material: {materialPath}.", target);
                return;
            }

            if (renderer == null || renderer.sharedMaterial != expected)
            {
                AddResult(Severity.Error, "Replica Prefab", $"{childName} material must be `{materialPath}`.", target);
            }
        }

        private static void ValidateTrailMaterial(GameObject replica, string materialPath)
        {
            var expected = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (expected == null)
            {
                AddResult(Severity.Error, "Replica Prefab", $"Missing expected GGReplica material: {materialPath}.", replica);
                return;
            }

            var trail = replica.transform.Find("ShipVisual/GGPlayerLQTrail")?.GetComponent<TrailRenderer>();
            if (trail == null || trail.sharedMaterial != expected)
            {
                AddResult(Severity.Error, "Replica Prefab", $"GGPlayerLQTrail material must be `{materialPath}`.", replica);
            }
        }

        private static void ValidateRendererReference(SerializedObject serializedObject, string propertyPath, Transform viewRoot, string childName, UnityEngine.Object target)
        {
            var expected = viewRoot != null ? viewRoot.Find(childName)?.GetComponent<SpriteRenderer>() : null;
            ValidateObjectReference(serializedObject, propertyPath, expected, "Replica Prefab", $"GGReplicaPlayerViewAdapter.{propertyPath} is not wired to GGPlayerViewRoot/{childName}.", target);
        }

        private static void ValidateSceneIsolation()
        {
            string absoluteSampleScenePath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, SampleScenePath);
            if (!File.Exists(absoluteSampleScenePath)) return;

            string yaml = File.ReadAllText(absoluteSampleScenePath);
            var markers = BuildSceneIsolationMarkers();
            foreach (var marker in markers)
            {
                if (string.IsNullOrEmpty(marker)) continue;
                if (yaml.Contains(marker, StringComparison.Ordinal))
                {
                    AddResult(Severity.Error, "Scene Isolation", $"SampleScene contains GGReplica reference marker `{marker}`. This violates the replica isolation lane.", null);
                }
            }
        }

        private static IEnumerable<string> BuildSceneIsolationMarkers()
        {
            yield return "Ship_GGReplica";
            yield return "GGReplicaTestSwitcher";
            yield return "GGReplicaPlayerViewAdapter";
            yield return AssetDatabase.AssetPathToGUID(ReplicaShipPath);

            foreach (var scriptPath in GgReplicaScriptPaths)
            {
                yield return AssetDatabase.AssetPathToGUID(scriptPath);
            }
        }

        private static void ValidateMissingComponentInChildren<T>(GameObject prefab, string message) where T : Component
        {
            if (prefab.GetComponentsInChildren<T>(includeInactive: true).Length > 0)
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
            if (_lastResults.All(result => result.Severity != Severity.Error))
            {
                Debug.Log("[GGReplicaAuditor] PASS: GGReplica is isolated from live Ship.prefab and SampleScene.");
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

            if (_lastResults.Count > 0)
            {
                Debug.Log($"[GGReplicaAuditor] Audit complete: {_lastResults.Count(r => r.Severity == Severity.Error)} errors, {_lastResults.Count(r => r.Severity == Severity.Warning)} warnings.");
            }
        }
    }
}
#endif
