#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Read-only Ship / VFX audit tool for Phase A authority validation.
    ///
    /// Scope:
    /// - Ship.prefab authority wiring
    /// - BoostTrailRoot.prefab authority wiring
    /// - Scene-only Bloom binding and override whitelist
    /// - Debug takeover risks and legacy / fallback residue
    ///
    /// This validator never modifies assets or scenes.
    /// It exists to make missing references, dirty overrides, and authority drift explicit.
    /// </summary>
    public static class ShipVfxValidator
    {
        public enum Severity
        {
            Info,
            Warning,
            Error
        }

        public sealed class ValidationResult
        {
            public Severity Severity;
            public string Scope;
            public string Message;
            public UnityEngine.Object TargetObject;
        }

        private sealed class CodePatternCheck
        {
            public string AssetPath;
            public string MatchText;
            public Severity Severity;
            public string Scope;
            public string Message;
        }

        private const string MenuPath = "ProjectArk/Ship/VFX/Audit/Run Ship VFX Audit";
        private const string ShipPrefabPath = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string BoostTrailPrefabPath = "Assets/_Prefabs/VFX/BoostTrailRoot.prefab";
        private const string BoostBloomProfilePath = "Assets/Settings/BoostBloomVolumeProfile.asset";
        private const string ExpectedScenePath = "Assets/Scenes/SampleScene.unity";
        private const string VolumeTypeName = "UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime";
        private const string ShipVisualRootName = "ShipVisual";
        private const string LegacyShipVisualRootName = "VisualChild";
        private const string BoostTrailRootName = "BoostTrailRoot";
        private const string BoostBloomVolumeName = "BoostTrailBloomVolume";

        private static readonly string[] ShipVisualRequiredChildren =
        {
            "Ship_Sprite_Back",
            "Ship_Sprite_Liquid",
            "Ship_Sprite_HL",
            "Ship_Sprite_Solid",
            "Ship_Sprite_Core",
            "Dodge_Sprite",
            BoostTrailRootName
        };

        private static readonly string[] BoostTrailRequiredChildren =
        {
            "MainTrail",
            "FlameTrail_R",
            "FlameTrail_B",
            "FlameCore",
            "EmberTrail",
            "EmberSparks",
            "BoostEnergyLayer2",
            "BoostEnergyLayer3",
            "BoostActivationHalo"
        };

        private static readonly HashSet<string> AllowedBoostTrailViewSceneOverrides = new HashSet<string>
        {
            "_boostBloomVolume"
        };

        private static readonly List<ValidationResult> _lastResults = new List<ValidationResult>();

        public static IReadOnlyList<ValidationResult> LastResults => _lastResults;

        [MenuItem(MenuPath)]
        public static void RunAuditMenu()
        {
            RunAudit(showDialog: false);
        }

        public static IReadOnlyList<ValidationResult> RunAudit(bool showDialog = false)
        {
            _lastResults.Clear();

            var shipPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ShipPrefabPath);
            var boostTrailPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BoostTrailPrefabPath);
            var boostBloomProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BoostBloomProfilePath);

            ValidateRequiredAssets(shipPrefabAsset, boostTrailPrefabAsset, boostBloomProfile);
            ValidateShipPrefab(shipPrefabAsset);
            ValidateBoostTrailPrefab(boostTrailPrefabAsset);
            ValidateSceneBloomBinding(boostBloomProfile);
            ValidateStaticAuthorityResidue();

            LogResultsToConsole();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Ship VFX Audit",
                    BuildDialogSummary(),
                    "OK");
            }

            return _lastResults;
        }

        private static void ValidateRequiredAssets(GameObject shipPrefabAsset, GameObject boostTrailPrefabAsset, UnityEngine.Object boostBloomProfile)
        {
            if (shipPrefabAsset == null)
            {
                AddResult(Severity.Error, "Assets", $"缺少现役资产：`{ShipPrefabPath}`。", null);
            }

            if (boostTrailPrefabAsset == null)
            {
                AddResult(Severity.Error, "Assets", $"缺少现役资产：`{BoostTrailPrefabPath}`。", null);
            }

            if (boostBloomProfile == null)
            {
                AddResult(Severity.Error, "Assets", $"缺少场景 Bloom Profile：`{BoostBloomProfilePath}`。", null);
            }
        }

        private static void ValidateShipPrefab(GameObject shipPrefabAsset)
        {
            if (shipPrefabAsset == null)
            {
                return;
            }

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(ShipPrefabPath);
                if (prefabRoot == null)
                {
                    AddResult(Severity.Error, "Ship Prefab", "无法加载 `Ship.prefab` 内容进行审计。", shipPrefabAsset);
                    return;
                }

                Transform shipVisual = prefabRoot.transform.Find(ShipVisualRootName);
                Transform legacyShipVisual = prefabRoot.transform.Find(LegacyShipVisualRootName);

                if (shipVisual == null)
                {
                    if (legacyShipVisual != null)
                    {
                        shipVisual = legacyShipVisual;
                        AddResult(Severity.Warning, "Ship Prefab", "`Ship.prefab` 仍依赖 legacy 视觉根节点 `VisualChild`。建议继续收口到 `ShipVisual`。", shipPrefabAsset);
                    }
                    else
                    {
                        AddResult(Severity.Error, "Ship Prefab", "`Ship.prefab` 缺少视觉根节点 `ShipVisual`。", shipPrefabAsset);
                        return;
                    }
                }

                if (shipVisual != null && legacyShipVisual != null)
                {
                    AddResult(Severity.Warning, "Ship Prefab", "`Ship.prefab` 同时存在 `ShipVisual` 与 legacy alias `VisualChild`，authority 边界仍不干净。", shipPrefabAsset);
                }

                foreach (string childName in ShipVisualRequiredChildren)
                {
                    ValidateDirectChildExistsExactlyOnce(shipVisual, childName, "Ship Prefab", shipPrefabAsset);
                }

                ValidateShipViewWiring(prefabRoot, shipVisual, shipPrefabAsset);
                ValidateShipEngineVfxWiring(prefabRoot, shipVisual, shipPrefabAsset);
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private static void ValidateShipViewWiring(GameObject prefabRoot, Transform shipVisual, GameObject shipPrefabAsset)
        {
            var shipView = prefabRoot.GetComponent<ShipView>();
            if (shipView == null)
            {
                AddResult(Severity.Error, "Ship Prefab", "`Ship.prefab` 缺少 `ShipView` 组件。", shipPrefabAsset);
                return;
            }

            var backRenderer = GetComponentOnDirectChild<SpriteRenderer>(shipVisual, "Ship_Sprite_Back");
            var liquidRenderer = GetComponentOnDirectChild<SpriteRenderer>(shipVisual, "Ship_Sprite_Liquid");
            var hlRenderer = GetComponentOnDirectChild<SpriteRenderer>(shipVisual, "Ship_Sprite_HL");
            var solidRenderer = GetComponentOnDirectChild<SpriteRenderer>(shipVisual, "Ship_Sprite_Solid");
            var coreRenderer = GetComponentOnDirectChild<SpriteRenderer>(shipVisual, "Ship_Sprite_Core");
            var dodgeRenderer = GetComponentOnDirectChild<SpriteRenderer>(shipVisual, "Dodge_Sprite");
            var boostTrailView = GetComponentOnDirectChild<BoostTrailView>(shipVisual, BoostTrailRootName);

            var serializedShipView = new SerializedObject(shipView);
            ValidateObjectReference(serializedShipView, "_backRenderer", backRenderer, "Ship Prefab", "`ShipView._backRenderer` 未正确指向 `Ship_Sprite_Back`。", shipPrefabAsset);
            ValidateObjectReference(serializedShipView, "_liquidRenderer", liquidRenderer, "Ship Prefab", "`ShipView._liquidRenderer` 未正确指向 `Ship_Sprite_Liquid`。", shipPrefabAsset);
            ValidateObjectReference(serializedShipView, "_hlRenderer", hlRenderer, "Ship Prefab", "`ShipView._hlRenderer` 未正确指向 `Ship_Sprite_HL`。", shipPrefabAsset);
            ValidateObjectReference(serializedShipView, "_solidRenderer", solidRenderer, "Ship Prefab", "`ShipView._solidRenderer` 未正确指向 `Ship_Sprite_Solid`。", shipPrefabAsset);
            ValidateObjectReference(serializedShipView, "_coreRenderer", coreRenderer, "Ship Prefab", "`ShipView._coreRenderer` 未正确指向 `Ship_Sprite_Core`。", shipPrefabAsset);
            ValidateObjectReference(serializedShipView, "_boostTrailView", boostTrailView, "Ship Prefab", "`ShipView._boostTrailView` 未正确指向嵌套的 `BoostTrailRoot`。", shipPrefabAsset);
            ValidateObjectReference(serializedShipView, "_dodgeSprite", dodgeRenderer, "Ship Prefab", "`ShipView._dodgeSprite` 未正确指向 `Dodge_Sprite`。", shipPrefabAsset);
            ValidateNonNullReference(serializedShipView, "_normalLiquidSprite", "Ship Prefab", "`ShipView._normalLiquidSprite` 为空。", shipPrefabAsset);
            ValidateNonNullReference(serializedShipView, "_boostLiquidSprite", "Ship Prefab", "`ShipView._boostLiquidSprite` 为空。", shipPrefabAsset);
        }

        private static void ValidateShipEngineVfxWiring(GameObject prefabRoot, Transform shipVisual, GameObject shipPrefabAsset)
        {
            var shipEngineVfx = prefabRoot.GetComponent<ShipEngineVFX>();
            if (shipEngineVfx == null)
            {
                AddResult(Severity.Error, "Ship Prefab", "`Ship.prefab` 缺少 `ShipEngineVFX` 组件。", shipPrefabAsset);
                return;
            }

            var backNode = shipVisual.Find("Ship_Sprite_Back");
            var engineParticles = GetComponentOnDirectChild<ParticleSystem>(backNode, "EngineParticles");
            var serializedEngineVfx = new SerializedObject(shipEngineVfx);
            ValidateObjectReference(serializedEngineVfx, "_engineParticles", engineParticles, "Ship Prefab", "`ShipEngineVFX._engineParticles` 未正确指向 `EngineParticles`。", shipPrefabAsset);
        }

        private static void ValidateBoostTrailPrefab(GameObject boostTrailPrefabAsset)
        {
            if (boostTrailPrefabAsset == null)
            {
                return;
            }

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(BoostTrailPrefabPath);
                if (prefabRoot == null)
                {
                    AddResult(Severity.Error, "BoostTrail Prefab", "无法加载 `BoostTrailRoot.prefab` 内容进行审计。", boostTrailPrefabAsset);
                    return;
                }

                var boostTrailView = prefabRoot.GetComponent<BoostTrailView>();
                if (boostTrailView == null)
                {
                    AddResult(Severity.Error, "BoostTrail Prefab", "`BoostTrailRoot.prefab` 根节点缺少 `BoostTrailView`。", boostTrailPrefabAsset);
                    return;
                }

                var debugManager = prefabRoot.GetComponent<BoostTrailDebugManager>();
                if (debugManager == null)
                {
                    AddResult(Severity.Error, "BoostTrail Prefab", "`BoostTrailRoot.prefab` 根节点缺少 `BoostTrailDebugManager`。", boostTrailPrefabAsset);
                }

                foreach (string childName in BoostTrailRequiredChildren)
                {
                    ValidateDirectChildExistsExactlyOnce(prefabRoot.transform, childName, "BoostTrail Prefab", boostTrailPrefabAsset);
                }

                var mainTrail = GetComponentOnDirectChild<TrailRenderer>(prefabRoot.transform, "MainTrail");
                var flameTrailR = GetComponentOnDirectChild<ParticleSystem>(prefabRoot.transform, "FlameTrail_R");
                var flameTrailB = GetComponentOnDirectChild<ParticleSystem>(prefabRoot.transform, "FlameTrail_B");
                var flameCore = GetComponentOnDirectChild<ParticleSystem>(prefabRoot.transform, "FlameCore");
                var emberTrail = GetComponentOnDirectChild<ParticleSystem>(prefabRoot.transform, "EmberTrail");
                var emberSparks = GetComponentOnDirectChild<ParticleSystem>(prefabRoot.transform, "EmberSparks");
                var energyLayer2 = GetComponentOnDirectChild<SpriteRenderer>(prefabRoot.transform, "BoostEnergyLayer2");
                var energyLayer3 = GetComponentOnDirectChild<SpriteRenderer>(prefabRoot.transform, "BoostEnergyLayer3");
                var activationHalo = GetComponentOnDirectChild<SpriteRenderer>(prefabRoot.transform, "BoostActivationHalo");

                var serializedBoostTrailView = new SerializedObject(boostTrailView);
                ValidateObjectReference(serializedBoostTrailView, "_mainTrail", mainTrail, "BoostTrail Prefab", "`BoostTrailView._mainTrail` 未正确指向 `MainTrail`。", boostTrailPrefabAsset);
                ValidateObjectReference(serializedBoostTrailView, "_flameTrailR", flameTrailR, "BoostTrail Prefab", "`BoostTrailView._flameTrailR` 未正确指向 `FlameTrail_R`。", boostTrailPrefabAsset);
                ValidateObjectReference(serializedBoostTrailView, "_flameTrailB", flameTrailB, "BoostTrail Prefab", "`BoostTrailView._flameTrailB` 未正确指向 `FlameTrail_B`。", boostTrailPrefabAsset);
                ValidateObjectReference(serializedBoostTrailView, "_flameCore", flameCore, "BoostTrail Prefab", "`BoostTrailView._flameCore` 未正确指向 `FlameCore`。", boostTrailPrefabAsset);
                ValidateObjectReference(serializedBoostTrailView, "_emberTrail", emberTrail, "BoostTrail Prefab", "`BoostTrailView._emberTrail` 未正确指向 `EmberTrail`。", boostTrailPrefabAsset);
                ValidateObjectReference(serializedBoostTrailView, "_emberSparks", emberSparks, "BoostTrail Prefab", "`BoostTrailView._emberSparks` 未正确指向 `EmberSparks`。", boostTrailPrefabAsset);
                ValidateObjectReference(serializedBoostTrailView, "_energyLayer2", energyLayer2, "BoostTrail Prefab", "`BoostTrailView._energyLayer2` 未正确指向 `BoostEnergyLayer2`。", boostTrailPrefabAsset);
                ValidateObjectReference(serializedBoostTrailView, "_energyLayer3", energyLayer3, "BoostTrail Prefab", "`BoostTrailView._energyLayer3` 未正确指向 `BoostEnergyLayer3`。", boostTrailPrefabAsset);
                ValidateObjectReference(serializedBoostTrailView, "_activationHalo", activationHalo, "BoostTrail Prefab", "`BoostTrailView._activationHalo` 未正确指向 `BoostActivationHalo`。", boostTrailPrefabAsset);

                var bloomVolumeProperty = serializedBoostTrailView.FindProperty("_boostBloomVolume");
                if (bloomVolumeProperty == null)
                {
                    AddResult(Severity.Error, "BoostTrail Prefab", "`BoostTrailView._boostBloomVolume` 序列化字段不存在，scene-only authority 无法验证。", boostTrailPrefabAsset);
                }
                else if (bloomVolumeProperty.objectReferenceValue != null)
                {
                    AddResult(Severity.Error, "BoostTrail Prefab", "`BoostTrailRoot.prefab` 上的 `_boostBloomVolume` 不应预绑场景对象。", boostTrailPrefabAsset);
                }
                else
                {
                    AddResult(Severity.Info, "BoostTrail Prefab", "`BoostTrailRoot.prefab` 的 `_boostBloomVolume` 保持空值，scene-only 边界干净。", boostTrailPrefabAsset);
                }

                ValidateBoostTrailDebugDefaults(debugManager, boostTrailView, boostTrailPrefabAsset);
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private static void ValidateBoostTrailDebugDefaults(BoostTrailDebugManager debugManager, BoostTrailView expectedView, GameObject boostTrailPrefabAsset)
        {
            if (debugManager == null)
            {
                return;
            }

            var serializedDebugManager = new SerializedObject(debugManager);
            ValidateObjectReference(serializedDebugManager, "_boostTrailView", expectedView, "BoostTrail Prefab", "`BoostTrailDebugManager._boostTrailView` 未正确指向同根 `BoostTrailView`。", boostTrailPrefabAsset);

            var enableInspectorDebug = serializedDebugManager.FindProperty("_enableInspectorDebug");
            if (enableInspectorDebug == null)
            {
                AddResult(Severity.Error, "BoostTrail Debug", "`BoostTrailDebugManager._enableInspectorDebug` 字段不存在。", boostTrailPrefabAsset);
            }
            else if (enableInspectorDebug.boolValue)
            {
                AddResult(Severity.Warning, "BoostTrail Debug", "`BoostTrailDebugManager` 默认启用了 Inspector Debug，存在接管 live chain 风险。", boostTrailPrefabAsset);
            }

            ValidateEnumValue(
                serializedDebugManager,
                "_debugMode",
                (int)BoostTrailDebugManager.DebugMode.ObserveRuntime,
                "BoostTrail Debug",
                "`BoostTrailDebugManager._debugMode` 默认值不是 `ObserveRuntime`。",
                boostTrailPrefabAsset,
                Severity.Warning);

            ValidateEnumValue(
                serializedDebugManager,
                "_soloLayer",
                (int)BoostTrailDebugManager.SoloLayer.None,
                "BoostTrail Debug",
                "`BoostTrailDebugManager._soloLayer` 默认值不是 `None`。",
                boostTrailPrefabAsset,
                Severity.Warning);
        }

        private static void ValidateSceneBloomBinding(UnityEngine.Object expectedProfile)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                AddResult(Severity.Error, "Scene", "当前没有已加载的场景，无法执行 scene-only Bloom 审计。", null);
                return;
            }

            if (!string.Equals(activeScene.path, ExpectedScenePath, StringComparison.Ordinal))
            {
                AddResult(Severity.Info, "Scene", $"当前审计场景是 `{activeScene.path}`，预期主验证场景是 `{ExpectedScenePath}`。", null);
            }

            Type volumeType = Type.GetType(VolumeTypeName);
            if (volumeType == null)
            {
                AddResult(Severity.Error, "Scene Bloom", $"无法解析 Volume 类型：`{VolumeTypeName}`。", null);
                return;
            }

            var sceneBoostViews = FindSceneComponents<BoostTrailView>(activeScene);
            var sceneDebugManagers = FindSceneComponents<BoostTrailDebugManager>(activeScene);
            var namedBloomObjects = FindNamedSceneGameObjects(activeScene, BoostBloomVolumeName);
            var bloomVolumeComponents = new List<Component>();

            for (int i = 0; i < namedBloomObjects.Count; i++)
            {
                var component = namedBloomObjects[i].GetComponent(volumeType);
                if (component == null)
                {
                    AddResult(Severity.Error, "Scene Bloom", $"`{BoostBloomVolumeName}` 缺少 `Volume` 组件。", namedBloomObjects[i]);
                }
                else
                {
                    bloomVolumeComponents.Add(component);
                }
            }

            if (namedBloomObjects.Count == 0)
            {
                AddResult(Severity.Error, "Scene Bloom", $"当前场景缺少 scene-only 对象 `{BoostBloomVolumeName}`。", null);
            }
            else if (namedBloomObjects.Count > 1)
            {
                AddResult(Severity.Error, "Scene Bloom", $"当前场景发现 {namedBloomObjects.Count} 个 `{BoostBloomVolumeName}`，应只保留一个。", namedBloomObjects[0]);
            }

            Component bloomVolume = bloomVolumeComponents.Count > 0 ? bloomVolumeComponents[0] : null;
            if (bloomVolume != null)
            {
                ValidateSceneBloomVolume(bloomVolume, expectedProfile);
            }

            if (sceneBoostViews.Count == 0)
            {
                AddResult(Severity.Warning, "Scene", "当前场景未找到 `BoostTrailView` 实例，无法验证 scene-only Bloom 接线。", null);
            }

            for (int i = 0; i < sceneBoostViews.Count; i++)
            {
                ValidateSceneBoostTrailView(sceneBoostViews[i], bloomVolume);
            }

            for (int i = 0; i < sceneDebugManagers.Count; i++)
            {
                ValidateSceneDebugManager(sceneDebugManagers[i]);
            }
        }

        private static void ValidateSceneBloomVolume(Component bloomVolume, UnityEngine.Object expectedProfile)
        {
            if (bloomVolume == null)
            {
                return;
            }

            var serializedVolume = new SerializedObject(bloomVolume);
            ValidateObjectReferenceProperty(
                serializedVolume,
                "sharedProfile",
                expectedProfile,
                "Scene Bloom",
                "`BoostTrailBloomVolume.sharedProfile` 未正确指向现役 `BoostBloomVolumeProfile.asset`。",
                bloomVolume);

            ValidateFloatProperty(serializedVolume, "weight", 0f, "Scene Bloom", "`BoostTrailBloomVolume.weight` 应为 0。", bloomVolume);
            ValidateFloatProperty(serializedVolume, "priority", 100f, "Scene Bloom", "`BoostTrailBloomVolume.priority` 应为 100。", bloomVolume);
            ValidateBoolProperty(serializedVolume, "m_IsGlobal", true, "Scene Bloom", "`BoostTrailBloomVolume.isGlobal` 应为 true。", bloomVolume);
            ValidateFloatProperty(serializedVolume, "blendDistance", 0f, "Scene Bloom", "`BoostTrailBloomVolume.blendDistance` 应为 0。", bloomVolume);
        }

        private static void ValidateSceneBoostTrailView(BoostTrailView sceneView, Component expectedBloomVolume)
        {
            if (sceneView == null)
            {
                return;
            }

            var serializedView = new SerializedObject(sceneView);
            var bloomVolumeProperty = serializedView.FindProperty("_boostBloomVolume");
            if (bloomVolumeProperty == null)
            {
                AddResult(Severity.Error, "Scene BoostTrailView", $"`{sceneView.name}` 缺少 `_boostBloomVolume` 字段，无法完成 scene-only authority 验证。", sceneView);
            }
            else if (bloomVolumeProperty.objectReferenceValue == null)
            {
                AddResult(Severity.Error, "Scene BoostTrailView", $"`{sceneView.name}` 的 `_boostBloomVolume` 为空。", sceneView);
            }
            else if (expectedBloomVolume != null && bloomVolumeProperty.objectReferenceValue != expectedBloomVolume)
            {
                AddResult(Severity.Error, "Scene BoostTrailView", $"`{sceneView.name}` 的 `_boostBloomVolume` 未指向统一的 `{BoostBloomVolumeName}`。", sceneView);
            }

            var illegalOverridePaths = GetIllegalOverridePaths(sceneView, AllowedBoostTrailViewSceneOverrides);
            if (illegalOverridePaths.Count > 0)
            {
                AddResult(
                    Severity.Error,
                    "Scene Override",
                    $"`{sceneView.name}` 的 `BoostTrailView` 存在非法 scene override：{string.Join(", ", illegalOverridePaths)}。仅允许 `_boostBloomVolume`。",
                    sceneView);
            }
        }

        private static void ValidateSceneDebugManager(BoostTrailDebugManager debugManager)
        {
            if (debugManager == null)
            {
                return;
            }

            var serializedDebugManager = new SerializedObject(debugManager);
            var enableInspectorDebug = serializedDebugManager.FindProperty("_enableInspectorDebug");
            if (enableInspectorDebug != null && enableInspectorDebug.boolValue)
            {
                AddResult(Severity.Warning, "Scene Debug", $"`{debugManager.name}` 的 `BoostTrailDebugManager` 当前启用了 Inspector Debug。", debugManager);
            }

            var illegalOverridePaths = GetIllegalOverridePaths(debugManager, null);
            if (illegalOverridePaths.Count > 0)
            {
                AddResult(
                    Severity.Warning,
                    "Scene Debug",
                    $"`{debugManager.name}` 的 `BoostTrailDebugManager` 存在 scene override：{string.Join(", ", illegalOverridePaths)}。这通常意味着 debug 状态被序列化进场景。",
                    debugManager);
            }
        }

        private static void ValidateStaticAuthorityResidue()
        {
            var codeChecks = new List<CodePatternCheck>
            {
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs",
                    MatchText = "AssetDatabase.FindAssets",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`ShipPrefabRebuilder` 仍包含 `AssetDatabase.FindAssets` 模糊搜索。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs",
                    MatchText = "FindSpriteExactOrByName",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`ShipPrefabRebuilder` 仍包含 `FindSpriteExactOrByName` 名字搜索兜底。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs",
                    MatchText = "LEGACY_VISUAL_CHILD_NAME",
                    Severity = Severity.Info,
                    Scope = "Code Audit",
                    Message = "`ShipPrefabRebuilder` 仍保留 `VisualChild` legacy alias 兼容路径。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs",
                    MatchText = "DODGE_SPRITE_SRC_PATH",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`ShipPrefabRebuilder` 仍依赖外部盘符 `DODGE_SPRITE_SRC_PATH`。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs",
                    MatchText = "FindSpriteExactOrByName",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`BoostTrailPrefabCreator` 仍包含 `FindSpriteExactOrByName` 名字搜索兜底。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs",
                    MatchText = "AssetDatabase.FindAssets",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`MaterialTextureLinker` 仍包含全项目 `FindAssets` 兜底。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs",
                    MatchText = "GameObject.Find(",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`ShipBoostTrailSceneBinder` 仍依赖 `GameObject.Find` 名字查找 scene object。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs",
                    MatchText = "Type.GetType(",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`ShipBoostTrailSceneBinder` 仍依赖字符串 `Type.GetType` 解析 `Volume`。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs",
                    MatchText = "private void Reset()",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`BoostTrailDebugManager` 仍在 `Reset()` 中自动补引用。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs",
                    MatchText = "private void OnValidate()",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`BoostTrailDebugManager` 仍在 `OnValidate()` 中自动补引用。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs",
                    MatchText = "private void Awake()",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`BoostTrailDebugManager` 仍在 `Awake()` 中自动补引用/恢复状态。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs",
                    MatchText = "AutoAssignReferences()",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`BoostTrailDebugManager` 仍保留 `AutoAssignReferences()` 自动补线入口。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs",
                    MatchText = "private void LateUpdate()",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`BoostTrailDebugManager` 仍在 `LateUpdate()` 中持续接管 live chain。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs",
                    MatchText = "FindLiveSceneInstance()",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`BoostTrailDebugManagerEditor` 仍会从 prefab Inspector 代理到 live runtime instance。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/VFX/ShipView.cs",
                    MatchText = "OnBoostStarted += HandleBoostStarted",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`ShipView` 仍保留 `ShipBoost` 事件 fallback，未完全收口到 `ShipStateController`。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/VFX/ShipView.cs",
                    MatchText = "_cachedNormalLiquidSprite",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`ShipView` 仍保留 normal liquid sprite 的运行时 fallback 缓存。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/VFX/ShipView.cs",
                    MatchText = "_dodgeSprite.sprite == null && _solidRenderer != null",
                    Severity = Severity.Warning,
                    Scope = "Code Audit",
                    Message = "`ShipView` 仍在 Dash 鬼影链路中用 `Ship_Sprite_Solid` 兜底替代 `Dodge_Sprite`。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/ShipBuilder.cs",
                    MatchText = "AssetDatabase.FindAssets",
                    Severity = Severity.Info,
                    Scope = "Code Audit",
                    Message = "`ShipBuilder` 仍包含 `FindAssets` bootstrap 搜索，后续应继续和 authority 工具边界解耦。"
                },
                new CodePatternCheck
                {
                    AssetPath = "Assets/Scripts/Ship/Editor/ShipBuilder.cs",
                    MatchText = "VisualChild",
                    Severity = Severity.Info,
                    Scope = "Code Audit",
                    Message = "`ShipBuilder` 仍保留 `VisualChild` 兼容逻辑。"
                }
            };

            for (int i = 0; i < codeChecks.Count; i++)
            {
                ValidateCodePattern(codeChecks[i]);
            }

            ValidateFileExists(
                "Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs",
                Severity.Warning,
                "Code Audit",
                "仓库中仍保留 `ShipBoostDebugMenu.cs`，它属于 legacy / debug-only 候选。"
            );

            ValidateFileExists(
                "Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs",
                Severity.Info,
                "Code Audit",
                "仓库中仍保留 `BoostTrailDebugManagerEditor.cs`，需继续确认它是否仍有保留必要性。"
            );
        }

        private static void ValidateCodePattern(CodePatternCheck check)
        {
            string absolutePath = GetAbsoluteProjectPath(check.AssetPath);
            if (!File.Exists(absolutePath))
            {
                return;
            }

            string content = File.ReadAllText(absolutePath);
            if (!content.Contains(check.MatchText, StringComparison.Ordinal))
            {
                return;
            }

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(check.AssetPath);
            AddResult(check.Severity, check.Scope, check.Message, script);
        }

        private static void ValidateFileExists(string assetPath, Severity severity, string scope, string message)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (script != null)
            {
                AddResult(severity, scope, message, script);
            }
        }

        private static void ValidateDirectChildExistsExactlyOnce(Transform parent, string childName, string scope, UnityEngine.Object target)
        {
            if (parent == null)
            {
                AddResult(Severity.Error, scope, $"无法检查子节点 `{childName}`，因为父节点为空。", target);
                return;
            }

            int count = CountDirectChildrenByName(parent, childName);
            if (count == 0)
            {
                AddResult(Severity.Error, scope, $"缺少必需子节点 `{childName}`。", target);
            }
            else if (count > 1)
            {
                AddResult(Severity.Error, scope, $"子节点 `{childName}` 出现了 {count} 次，应仅存在一个。", target);
            }
        }

        private static void ValidateObjectReference(SerializedObject serializedObject, string propertyPath, UnityEngine.Object expectedValue, string scope, string errorMessage, UnityEngine.Object target)
        {
            var prop = serializedObject.FindProperty(propertyPath);
            if (prop == null)
            {
                AddResult(Severity.Error, scope, $"缺少序列化字段 `{propertyPath}`。", target);
                return;
            }

            if (expectedValue == null)
            {
                AddResult(Severity.Error, scope, $"{errorMessage}（期望目标不存在）", target);
                return;
            }

            if (prop.objectReferenceValue != expectedValue)
            {
                AddResult(Severity.Error, scope, errorMessage, target);
            }
        }

        private static void ValidateNonNullReference(SerializedObject serializedObject, string propertyPath, string scope, string errorMessage, UnityEngine.Object target)
        {
            var prop = serializedObject.FindProperty(propertyPath);
            if (prop == null)
            {
                AddResult(Severity.Error, scope, $"缺少序列化字段 `{propertyPath}`。", target);
                return;
            }

            if (prop.objectReferenceValue == null)
            {
                AddResult(Severity.Error, scope, errorMessage, target);
            }
        }

        private static void ValidateEnumValue(SerializedObject serializedObject, string propertyPath, int expectedValue, string scope, string message, UnityEngine.Object target, Severity severity)
        {
            var prop = serializedObject.FindProperty(propertyPath);
            if (prop == null)
            {
                AddResult(Severity.Error, scope, $"缺少枚举字段 `{propertyPath}`。", target);
                return;
            }

            if (prop.enumValueIndex != expectedValue)
            {
                AddResult(severity, scope, message, target);
            }
        }

        private static List<string> GetIllegalOverridePaths(UnityEngine.Object target, HashSet<string> allowedPaths)
        {
            var illegalPaths = new List<string>();
            if (target == null)
            {
                return illegalPaths;
            }

            var modifications = PrefabUtility.GetPropertyModifications(target);
            if (modifications == null || modifications.Length == 0)
            {
                return illegalPaths;
            }

            var correspondingSource = PrefabUtility.GetCorrespondingObjectFromSource(target);
            if (correspondingSource == null)
            {
                return illegalPaths;
            }

            for (int i = 0; i < modifications.Length; i++)
            {
                var modification = modifications[i];
                if (!IsModificationForSourceTarget(modification, correspondingSource))
                {
                    continue;
                }

                string propertyPath = modification.propertyPath;
                if (string.IsNullOrEmpty(propertyPath) || propertyPath == "m_Script")
                {
                    continue;
                }

                if (allowedPaths != null && allowedPaths.Contains(propertyPath))
                {
                    continue;
                }

                if (!illegalPaths.Contains(propertyPath))
                {
                    illegalPaths.Add(propertyPath);
                }
            }

            return illegalPaths;
        }

        private static bool IsModificationForSourceTarget(PropertyModification modification, UnityEngine.Object correspondingSource)
        {
            if (modification == null || modification.target == null || correspondingSource == null)
            {
                return false;
            }

            if (ReferenceEquals(modification.target, correspondingSource))
            {
                return true;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(modification.target, out string modificationGuid, out long modificationLocalId))
            {
                return false;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(correspondingSource, out string sourceGuid, out long sourceLocalId))
            {
                return false;
            }

            return modificationGuid == sourceGuid && modificationLocalId == sourceLocalId;
        }

        private static T GetComponentOnDirectChild<T>(Transform parent, string childName) where T : Component
        {
            if (parent == null)
            {
                return null;
            }

            var child = parent.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static int CountDirectChildrenByName(Transform parent, string childName)
        {
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == childName)
                {
                    count++;
                }
            }

            return count;
        }

        private static List<T> FindSceneComponents<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
        {
            var results = new List<T>();
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentsInChildren<T>(true);
                for (int j = 0; j < found.Length; j++)
                {
                    results.Add(found[j]);
                }
            }

            return results;
        }

        private static List<GameObject> FindNamedSceneGameObjects(UnityEngine.SceneManagement.Scene scene, string objectName)
        {
            var results = new List<GameObject>();
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == objectName)
                    {
                        results.Add(transforms[j].gameObject);
                    }
                }
            }

            return results;
        }

        private static void ValidateObjectReferenceProperty(SerializedObject serializedObject, string propertyPath, UnityEngine.Object expectedValue, string scope, string errorMessage, UnityEngine.Object target)
        {
            var prop = serializedObject.FindProperty(propertyPath);
            if (prop == null)
            {
                AddResult(Severity.Error, scope, $"缺少序列化字段 `{propertyPath}`。", target);
                return;
            }

            if (expectedValue == null)
            {
                AddResult(Severity.Error, scope, $"{errorMessage}（期望资产不存在）", target);
                return;
            }

            if (prop.objectReferenceValue != expectedValue)
            {
                AddResult(Severity.Error, scope, errorMessage, target);
            }
        }

        private static void ValidateFloatProperty(SerializedObject serializedObject, string propertyPath, float expectedValue, string scope, string errorMessage, UnityEngine.Object target)
        {
            var prop = serializedObject.FindProperty(propertyPath);
            if (prop == null)
            {
                AddResult(Severity.Error, scope, $"缺少浮点字段 `{propertyPath}`。", target);
                return;
            }

            if (!Mathf.Approximately(prop.floatValue, expectedValue))
            {
                AddResult(Severity.Error, scope, $"{errorMessage} 当前值={prop.floatValue:F3}。", target);
            }
        }

        private static void ValidateBoolProperty(SerializedObject serializedObject, string propertyPath, bool expectedValue, string scope, string errorMessage, UnityEngine.Object target)
        {
            var prop = serializedObject.FindProperty(propertyPath);
            if (prop == null)
            {
                AddResult(Severity.Error, scope, $"缺少布尔字段 `{propertyPath}`。", target);
                return;
            }

            if (prop.boolValue != expectedValue)
            {
                AddResult(Severity.Error, scope, errorMessage, target);
            }
        }

        private static void AddResult(Severity severity, string scope, string message, UnityEngine.Object targetObject)
        {
            _lastResults.Add(new ValidationResult
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
                Debug.Log("[ShipVfxValidator] Audit complete: no issues found.");
                return;
            }

            for (int i = 0; i < _lastResults.Count; i++)
            {
                var result = _lastResults[i];
                string line = $"[ShipVfxValidator][{result.Scope}][{result.Severity}] {result.Message}";

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

            Debug.Log($"[ShipVfxValidator] Audit complete: {CountBySeverity(Severity.Error)} errors, {CountBySeverity(Severity.Warning)} warnings, {CountBySeverity(Severity.Info)} info.");
        }

        private static string BuildDialogSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Ship / VFX Audit Complete");
            sb.AppendLine();
            sb.AppendLine($"Errors:   {CountBySeverity(Severity.Error)}");
            sb.AppendLine($"Warnings: {CountBySeverity(Severity.Warning)}");
            sb.AppendLine($"Info:     {CountBySeverity(Severity.Info)}");
            sb.AppendLine();

            if (_lastResults.Count == 0)
            {
                sb.AppendLine("未发现问题。\n");
                return sb.ToString();
            }

            sb.AppendLine("首批问题：");
            int previewCount = Mathf.Min(8, _lastResults.Count);
            for (int i = 0; i < previewCount; i++)
            {
                var result = _lastResults[i];
                sb.AppendLine($"- [{result.Severity}] {result.Message}");
            }

            if (_lastResults.Count > previewCount)
            {
                sb.AppendLine("- ...更多结果请查看 Console");
            }

            return sb.ToString();
        }

        private static int CountBySeverity(Severity severity)
        {
            int count = 0;
            for (int i = 0; i < _lastResults.Count; i++)
            {
                if (_lastResults[i].Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetAbsoluteProjectPath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetPath);
        }
    }
}
#endif
