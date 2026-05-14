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
        private const string PlayerSkinPath = "Assets/_Data/Ship/GGReplicaPlayerSkin.asset";
        private const string PlayerShipHlMaterialPath = GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath;
        private const string TeleportSchemeMaterialPath = GGReplicaMaterialAssetBuilder.TeleportSchemeMaterialPath;
        private const string PlayerLqTrailMaterialPath = GGReplicaMaterialAssetBuilder.PlayerLqTrailMaterialPath;
        private const string TargetRootName = "Ship_GGReplica";
        private const string ShipVisualName = "ShipVisual";
        private const string GGPlayerViewRootName = "GGPlayerViewRoot";

        private static bool _hasBuildErrors;

        [MenuItem("ProjectArk/Ship/GG Replica/Build Experimental Prefab")]
        public static void BuildExperimentalPrefab()
        {
            _hasBuildErrors = false;

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceShipPrefabPath);
            if (source == null)
            {
                MarkError($"Missing source prefab: {SourceShipPrefabPath}");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SourceShipPrefabPath);
            if (root == null)
            {
                MarkError($"Failed to load prefab contents: {SourceShipPrefabPath}");
                return;
            }

            try
            {
                root.name = TargetRootName;
                var visualProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipVisualProfileSO>(VisualProfilePath);
                var feelProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipFeelProfileSO>(FeelProfilePath);
                var playerSkin = AssetDatabase.LoadAssetAtPath<GGReplicaPlayerSkinSO>(PlayerSkinPath);
                if (visualProfile == null || feelProfile == null || playerSkin == null)
                {
                    if (visualProfile == null) MarkError($"Missing visual profile: {VisualProfilePath}");
                    if (feelProfile == null) MarkError($"Missing feel profile: {FeelProfilePath}");
                    if (playerSkin == null) MarkError($"Missing player skin: {PlayerSkinPath}");
                    MarkError("Aborted build; required GGReplica data asset is missing.");
                    return;
                }

                GGReplicaMaterialAssetBuilder.BuildVisualMaterials();
                RemoveLegacyViewAdapter(root);
                var viewRoot = EnsureGGPlayerViewRoot(root, playerSkin);
                DisableLiveShipViewLane(root, viewRoot);
                EnsureVisualModules(root, viewRoot);
                EnsurePlayerViewAdapter(root, viewRoot, playerSkin);
                EnsureAudioAdapters(root, visualProfile);
                EnsureFeelAdapter(root, feelProfile);

                if (!ValidateBuildState(root, viewRoot))
                {
                    MarkError("Aborted build; required GGReplica prefab references are not valid.");
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath, out bool success);
                if (!success)
                {
                    MarkError($"Failed to save {TargetPrefabPath}");
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

        private static void RemoveLegacyViewAdapter(GameObject root)
        {
            var legacy = root.GetComponent<GGReplicaShipViewAdapter>();
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy);
            }
        }

        private static Transform EnsureGGPlayerViewRoot(GameObject root, GGReplicaPlayerSkinSO playerSkin)
        {
            var shipVisual = root.transform.Find(ShipVisualName);
            if (shipVisual == null)
            {
                var visualGo = new GameObject(ShipVisualName);
                visualGo.transform.SetParent(root.transform, false);
                shipVisual = visualGo.transform;
                Debug.LogWarning($"[GGReplicaPrefabBuilder] Missing {ShipVisualName}; created it for replica prefab.");
            }

            var viewRoot = shipVisual.Find(GGPlayerViewRootName);
            if (viewRoot == null)
            {
                var viewRootGo = new GameObject(GGPlayerViewRootName);
                viewRootGo.transform.SetParent(shipVisual, false);
                viewRoot = viewRootGo.transform;
            }

            var liquid = EnsureRenderer(viewRoot, "Ship_Sprite_Liquid", 0);
            var highlight = EnsureRenderer(viewRoot, "Ship_Sprite_HL", 1);
            var dodge = EnsureRenderer(viewRoot, "Dodge_Sprite", 2);
            var solid = EnsureRenderer(viewRoot, "Ship_Sprite_Solid", 3);
            var back = EnsureRenderer(viewRoot, "Ship_Sprite_Back", 4);
            var grabR = EnsureRenderer(viewRoot, "Ship_Sprite_Solid_Grab_R", 5);
            var grabL = EnsureRenderer(viewRoot, "Ship_Sprite_Solid_Grab_L", 5);
            var reactor = EnsureRenderer(viewRoot, "Core_Sprite_Reactor", 6);
            var eye = EnsureRenderer(viewRoot, "Core_Sprite_Eye", 7);
            var view = EnsureRenderer(viewRoot, "View", -10);
            var dodgeHalf = EnsureRenderer(viewRoot, "Dodge_Half_Sprite", 9);

            highlight.sharedMaterial = LoadMaterial(PlayerShipHlMaterialPath);
            view.sharedMaterial = LoadMaterial(TeleportSchemeMaterialPath);

            if (playerSkin != null)
            {
                if (playerSkin.TryGetPack(GGReplicaViewState.Idle, out var idlePack))
                {
                    solid.sprite = idlePack.SolidSprite;
                    liquid.sprite = idlePack.LiquidSprite;
                    highlight.sprite = idlePack.HighlightSprite;
                    viewRoot.localPosition = idlePack.SpritesOffset;
                }

                highlight.color = playerSkin.ShipHighlightColor;
                back.sprite = playerSkin.ShipSpriteBack;
                grabR.sprite = playerSkin.ShipSpriteSolidGrabR;
                grabL.sprite = playerSkin.ShipSpriteSolidGrabL;
                reactor.sprite = playerSkin.ReactorSprite;
                eye.sprite = playerSkin.EyeSprite;
                view.sprite = playerSkin.ViewSilhouetteSprite;
                dodge.sprite = playerSkin.DodgeSprite;
                dodgeHalf.sprite = playerSkin.DodgeHalfSprite;
            }

            dodge.enabled = false;
            dodgeHalf.enabled = false;
            grabR.enabled = false;
            grabL.enabled = false;
            return viewRoot;
        }

        private static void DisableLiveShipViewLane(GameObject root, Transform ggPlayerViewRoot)
        {
            var liveShipView = root.GetComponent<ShipView>();
            if (liveShipView != null)
            {
                liveShipView.enabled = false;
                SetSerializedBool(liveShipView, "_enableBoostVFX", false);
                SetSerializedBool(liveShipView, "_enableHitVFX", false);
                SetSerializedBool(liveShipView, "_enableDashVFX", false);
                SetSerializedBool(liveShipView, "_enableJuiceVFX", false);
            }

            var shipVisual = root.transform.Find(ShipVisualName);
            if (shipVisual == null) return;

            foreach (Transform child in shipVisual)
            {
                if (child == ggPlayerViewRoot) continue;
                if (child.name == "GGBoostVisualRoot") continue;

                child.gameObject.SetActive(false);
                EditorUtility.SetDirty(child.gameObject);

                foreach (var renderer in child.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static SpriteRenderer EnsureRenderer(Transform parent, string childName, int sortingOrder)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static void EnsureVisualModules(GameObject root, Transform viewRoot)
        {
            var coreModule = root.GetComponent<GGReplicaCoreVisualModule>();
            if (coreModule == null)
            {
                coreModule = root.AddComponent<GGReplicaCoreVisualModule>();
            }

            var boostModule = root.GetComponent<GGReplicaBoostVisualModule>();
            if (boostModule == null)
            {
                boostModule = root.AddComponent<GGReplicaBoostVisualModule>();
            }

            var shapeModule = root.GetComponent<GGReplicaShapeVisualModule>();
            if (shapeModule == null)
            {
                shapeModule = root.AddComponent<GGReplicaShapeVisualModule>();
            }

            var materialModule = root.GetComponent<GGReplicaMaterialVisualModule>();
            if (materialModule == null)
            {
                materialModule = root.AddComponent<GGReplicaMaterialVisualModule>();
            }

            var highlightRenderer = FindRenderer(viewRoot, "Ship_Sprite_HL");
            var viewRenderer = FindRenderer(viewRoot, "View");
            var coreRenderer = FindRenderer(viewRoot, "Core_Sprite_Reactor");
            var dodgeRenderer = FindRenderer(viewRoot, "Dodge_Sprite");
            var dodgeHalfRenderer = FindRenderer(viewRoot, "Dodge_Half_Sprite");
            var grabRightRenderer = FindRenderer(viewRoot, "Ship_Sprite_Solid_Grab_R");
            var grabLeftRenderer = FindRenderer(viewRoot, "Ship_Sprite_Solid_Grab_L");

            SetSerialized(coreModule, "_coreRenderer", coreRenderer);
            SetSerialized(coreModule, "_coreTransform", coreRenderer != null ? coreRenderer.transform : null);
            SetSerialized(coreModule, "_dodgeRenderer", dodgeRenderer);
            SetSerialized(coreModule, "_dodgeHalfRenderer", dodgeHalfRenderer);

            var legacyBoostVisualRoot = viewRoot.Find("BoostVisualRoot");
            if (legacyBoostVisualRoot != null)
            {
                Object.DestroyImmediate(legacyBoostVisualRoot.gameObject);
            }

            var shipVisual = root.transform.Find(ShipVisualName);
            var boostVisualRoot = shipVisual != null ? shipVisual.Find("GGBoostVisualRoot") : null;
            if (boostVisualRoot == null)
            {
                var boostVisualRootGo = new GameObject("GGBoostVisualRoot");
                boostVisualRootGo.transform.SetParent(shipVisual != null ? shipVisual : root.transform, false);
                boostVisualRoot = boostVisualRootGo.transform;
            }

            var playerLqTrail = EnsurePlayerLqTrail(shipVisual != null ? shipVisual : root.transform);
            boostVisualRoot.gameObject.SetActive(false);
            SetSerialized(boostModule, "_boostVisualRoot", boostVisualRoot.gameObject);
            SetSerialized(shapeModule, "_grabRightRenderer", grabRightRenderer);
            SetSerialized(shapeModule, "_grabLeftRenderer", grabLeftRenderer);
            SetSerialized(materialModule, "_highlightRenderer", highlightRenderer);
            SetSerialized(materialModule, "_viewRenderer", viewRenderer);
            SetSerialized(materialModule, "_coreRenderer", coreRenderer);
            SetSerialized(materialModule, "_dodgeRenderer", dodgeRenderer);
            SetSerialized(materialModule, "_grabRightRenderer", grabRightRenderer);
            SetSerialized(materialModule, "_grabLeftRenderer", grabLeftRenderer);
            SetSerialized(materialModule, "_playerLqTrail", playerLqTrail);
        }

        private static void EnsurePlayerViewAdapter(GameObject root, Transform viewRoot, GGReplicaPlayerSkinSO playerSkin)
        {
            var adapter = root.GetComponent<GGReplicaPlayerViewAdapter>();
            if (adapter == null)
            {
                adapter = root.AddComponent<GGReplicaPlayerViewAdapter>();
            }

            var coreModule = root.GetComponent<GGReplicaCoreVisualModule>();
            var boostModule = root.GetComponent<GGReplicaBoostVisualModule>();
            var shapeModule = root.GetComponent<GGReplicaShapeVisualModule>();
            var materialModule = root.GetComponent<GGReplicaMaterialVisualModule>();

            SetSerialized(adapter, "_skin", playerSkin);
            SetSerialized(adapter, "_spritesRoot", viewRoot);
            SetSerialized(adapter, "_shipLiquidRenderer", FindRenderer(viewRoot, "Ship_Sprite_Liquid"));
            SetSerialized(adapter, "_shipHighlightRenderer", FindRenderer(viewRoot, "Ship_Sprite_HL"));
            SetSerialized(adapter, "_dodgeRenderer", FindRenderer(viewRoot, "Dodge_Sprite"));
            SetSerialized(adapter, "_shipSolidRenderer", FindRenderer(viewRoot, "Ship_Sprite_Solid"));
            SetSerialized(adapter, "_shipBackRenderer", FindRenderer(viewRoot, "Ship_Sprite_Back"));
            SetSerialized(adapter, "_shipGrabRightRenderer", FindRenderer(viewRoot, "Ship_Sprite_Solid_Grab_R"));
            SetSerialized(adapter, "_shipGrabLeftRenderer", FindRenderer(viewRoot, "Ship_Sprite_Solid_Grab_L"));
            SetSerialized(adapter, "_coreRenderer", FindRenderer(viewRoot, "Core_Sprite_Reactor"));
            SetSerialized(adapter, "_eyeRenderer", FindRenderer(viewRoot, "Core_Sprite_Eye"));
            SetSerialized(adapter, "_viewSilhouetteRenderer", FindRenderer(viewRoot, "View"));
            SetSerialized(adapter, "_dodgeHalfRenderer", FindRenderer(viewRoot, "Dodge_Half_Sprite"));
            SetSerialized(adapter, "_coreModule", coreModule);
            SetSerialized(adapter, "_boostModule", boostModule);
            SetSerialized(adapter, "_shapeModule", shapeModule);
            SetSerialized(adapter, "_materialModule", materialModule);
        }

        private static TrailRenderer EnsurePlayerLqTrail(Transform shipVisual)
        {
            var trailRoot = shipVisual.Find("GGPlayerLQTrail");
            if (trailRoot == null)
            {
                var trailGo = new GameObject("GGPlayerLQTrail");
                trailGo.transform.SetParent(shipVisual, false);
                trailRoot = trailGo.transform;
            }

            var trail = trailRoot.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = trailRoot.gameObject.AddComponent<TrailRenderer>();
            }

            trail.time = 0.4f;
            trail.widthMultiplier = 4f;
            trail.emitting = false;
            trail.autodestruct = false;
            trail.minVertexDistance = 0.3f;
            trail.numCapVertices = 32;
            trail.generateLightingData = false;
            trail.sharedMaterial = LoadMaterial(PlayerLqTrailMaterialPath);
            EditorUtility.SetDirty(trail);
            return trail;
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

        private static bool ValidateBuildState(GameObject root, Transform viewRoot)
        {
            if (_hasBuildErrors) return false;
            if (root.GetComponent<GGReplicaShipViewAdapter>() != null)
            {
                MarkError("Legacy GGReplicaShipViewAdapter is still present on rebuilt prefab.");
            }

            RequireComponent<GGReplicaPlayerViewAdapter>(root);
            RequireComponent<GGReplicaCoreVisualModule>(root);
            RequireComponent<GGReplicaBoostVisualModule>(root);
            RequireComponent<GGReplicaShapeVisualModule>(root);
            RequireComponent<GGReplicaMaterialVisualModule>(root);

            foreach (var childName in RequiredViewChildren)
            {
                var child = viewRoot.Find(childName);
                if (child == null)
                {
                    MarkError($"Missing GGPlayerViewRoot/{childName}.");
                    continue;
                }

                if (child.GetComponent<SpriteRenderer>() == null)
                {
                    MarkError($"Missing SpriteRenderer on GGPlayerViewRoot/{childName}.");
                }
            }

            return !_hasBuildErrors;
        }

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

        private static void RequireComponent<T>(GameObject root) where T : Component
        {
            if (root.GetComponent<T>() == null)
            {
                MarkError($"Missing required component {typeof(T).Name}.");
            }
        }

        private static SpriteRenderer FindRenderer(Transform root, string childName)
        {
            var child = root.Find(childName);
            if (child == null)
            {
                MarkError($"Missing child '{childName}' under {root.name}.");
                return null;
            }

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                MarkError($"Missing SpriteRenderer on '{childName}'.");
            }

            return renderer;
        }

        private static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                MarkError($"Missing GGReplica material: {path}");
            }

            return material;
        }

        private static bool SetSerialized(Object target, string propertyName, Object value)
        {
            if (target == null)
            {
                MarkError($"Cannot set '{propertyName}' because target is null.");
                return false;
            }

            if (value == null)
            {
                MarkError($"Cannot set '{propertyName}' on {target.name} because value is null.");
                return false;
            }

            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                so.Dispose();
                MarkError($"Missing serialized property '{propertyName}' on {target.name}.");
                return false;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            so.Dispose();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static bool SetSerializedBool(Object target, string propertyName, bool value)
        {
            if (target == null)
            {
                MarkError($"Cannot set '{propertyName}' because target is null.");
                return false;
            }

            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                so.Dispose();
                MarkError($"Missing serialized property '{propertyName}' on {target.name}.");
                return false;
            }

            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            so.Dispose();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static void MarkError(string message)
        {
            _hasBuildErrors = true;
            Debug.LogError($"[GGReplicaPrefabBuilder] {message}");
        }
    }
}
#endif
