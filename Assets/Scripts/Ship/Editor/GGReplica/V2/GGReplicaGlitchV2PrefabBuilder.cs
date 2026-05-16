#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    public static class GGReplicaGlitchV2PrefabBuilder
    {
        public const string PrefabPath = "Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab";

        private const string RootName = "Ship_GGReplicaV2";
        private const string VisualRootName = "GGGlitchVisualRoot";
        private const string FeelProfilePath = "Assets/_Data/Ship/GGReplicaShipFeelProfile.asset";
        private const string VisualProfilePath = "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset";
        private const string PlayerSkinPath = "Assets/_Data/Ship/GGReplicaPlayerSkin.asset";
        private const string MovementSolidPath = "Assets/_Art/Ship/GGReplica/Sprites/Movement_10.png";
        private const string MovementLiquidPath = "Assets/_Art/Ship/GGReplica/Sprites/Movement_3.png";
        private const string MovementHighlightPath = "Assets/_Art/Ship/GGReplica/Sprites/Movement_21.png";
        private const string BackPath = "Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Back_3.png";
        private const string ReactorPath = "Assets/_Art/Ship/GGReplica/Sprites/reactor.png";
        private const string DodgeGhostPath = "Assets/_Art/Ship/GGReplica/Sprites/player_test_fire.png";
        private const string SchemePath = "Assets/_Art/Ship/GGReplica/Sprites/scheme3_tp.png";

        [MenuItem("ProjectArk/Ship/GG Replica/V2/Build Glitch V2 Prefab")]
        public static void BuildPrefab()
        {
            GGReplicaMaterialAssetBuilder.BuildVisualMaterials();

            var root = new GameObject(RootName);
            try
            {
                var body = root.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.linearDamping = 4f;
                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 0.55f;
                var view = root.AddComponent<GGReplicaGlitchView>();
                var motor = root.AddComponent<GGReplicaGlitchMotor>();
                var input = root.AddComponent<GGReplicaGlitchInputDriver>();
                var audioSource = root.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                var audioFeedback = root.AddComponent<GGReplicaGlitchAudioFeedback>();
                var feelProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipFeelProfileSO>(FeelProfilePath);
                var visualProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipVisualProfileSO>(VisualProfilePath);
                var playerSkin = AssetDatabase.LoadAssetAtPath<GGReplicaPlayerSkinSO>(PlayerSkinPath);
                if (feelProfile == null || visualProfile == null || playerSkin == null)
                {
                    if (feelProfile == null) Debug.LogError($"[GGReplicaGlitchV2PrefabBuilder] Missing feel profile: {FeelProfilePath}");
                    if (visualProfile == null) Debug.LogError($"[GGReplicaGlitchV2PrefabBuilder] Missing visual profile: {VisualProfilePath}");
                    if (playerSkin == null) Debug.LogError($"[GGReplicaGlitchV2PrefabBuilder] Missing player skin: {PlayerSkinPath}");
                    return;
                }

                var visualRoot = CreateChild(root.transform, VisualRootName);
                var bodyLayers = CreateChild(visualRoot, "BodyLayers");
                var solid = CreateSpriteLayer(bodyLayers, "Ship_Sprite_Solid", MovementSolidPath, 0, null);
                var liquid = CreateSpriteLayer(bodyLayers, "Ship_Sprite_Liquid", MovementLiquidPath, 1, null);
                var highlight = CreateSpriteLayer(bodyLayers, "Ship_Sprite_HL", MovementHighlightPath, 2, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                var back = CreateSpriteLayer(bodyLayers, "Ship_Sprite_Back", BackPath, -1, null);
                var core = CreateSpriteLayer(bodyLayers, "Ship_Sprite_Core", ReactorPath, 3, null);
                var dodgeGhost = CreateSpriteLayer(bodyLayers, "Dodge_Sprite", DodgeGhostPath, 4, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                dodgeGhost.enabled = false;
                var viewSilhouette = CreateSpriteLayer(bodyLayers, "View", SchemePath, -10, LoadMaterial(GGReplicaMaterialAssetBuilder.TeleportSchemeMaterialPath));
                viewSilhouette.color = Color.black;

                var coreModule = CreateChild(visualRoot, "CoreModule");
                var boostModule = CreateChild(visualRoot, "BoostModule");
                var lqTrailModule = CreateChild(visualRoot, "LQTrailModule");
                var lqTrailsContainer = CreateChild(visualRoot, "LQTrailsContainer");
                var shapeTrailModule = CreateChild(visualRoot, "ShapeTrailModule");
                var darkTrailModule = CreateChild(visualRoot, "DarkTrailModule");
                var fluxySolver = CreateChild(visualRoot, "FluxySolver");
                var fluxyGrabModule = CreateChild(visualRoot, "FluxyGrabModule");
                var grabModule = CreateChild(visualRoot, "GrabModule");
                var holdModule = CreateChild(visualRoot, "HoldModule");
                var healModule = CreateChild(visualRoot, "HealModule");
                var dodgeModule = CreateChild(visualRoot, "DodgeModule");
                var fireAimModule = CreateChild(visualRoot, "FireAimModule");
                var hitbox = CreateChild(visualRoot, "ShapeShiftStateHitbox");
                hitbox.gameObject.layer = 0;

                var boostLoop = CreateParticle(boostModule, "vfx_boost_trail_loop_enhanced", new Color(0.75f, 0f, 1f, 0.7f), 18f, 0.45f, GGReplicaMaterialAssetBuilder.EngineTrailMaterialPath);
                var boostBurst = CreateParticle(boostModule, "vfx_boost_trail_burst_enhanced", new Color(1f, 0.2f, 1f, 0.9f), 35f, 0.18f, GGReplicaMaterialAssetBuilder.EngineTrailMaterialPath);
                var flameR = CreateParticle(boostModule, "ps_techno_flame_trail_R", new Color(0.9f, 0.1f, 1f, 0.85f), 22f, 0.35f, GGReplicaMaterialAssetBuilder.EngineTrailMaterialPath);
                var flameQuick = CreateParticle(boostModule, "ps_techno_flame_trail_quick", new Color(0.6f, 0.1f, 1f, 0.7f), 28f, 0.22f, GGReplicaMaterialAssetBuilder.EngineTrailMaterialPath);
                var flameStart = CreateParticle(boostModule, "ps_techno_flame_trail_start", new Color(1f, 0.35f, 1f, 0.9f), 40f, 0.2f, GGReplicaMaterialAssetBuilder.EngineTrailMaterialPath);
                var emberTrail = CreateParticle(boostModule, "ps_ember_trail", new Color(0.9f, 0.25f, 1f, 0.65f), 16f, 0.6f);
                var healParticle = CreateParticle(healModule, "ps_glitch_heal", new Color(0.25f, 1f, 0.85f, 0.8f), 20f, 0.45f);
                var healShell = CreateSpriteLayer(healModule, "Healing_0", "Assets/_Art/Ship/GGReplica/Sprites/Healing_0.png", 6, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                var healDot = CreateSpriteLayer(healModule, "vfx_dot_001", "Assets/_Art/Ship/GGReplica/Sprites/vfx_dot_001.png", 7, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                healDot.transform.localScale = Vector3.one * 0.55f;
                var dodgeShell = CreateParticle(dodgeModule, "ps_dodge_shell", new Color(0.75f, 0.15f, 1f, 0.75f), 30f, 0.16f, GGReplicaMaterialAssetBuilder.DodgeParticlesMaterialPath);
                var dodgeOutlineTrail = CreateParticle(shapeTrailModule, "ShapeTrail_Dodge (old outline trail)", new Color(0.95f, 0.2f, 1f, 0.72f), 34f, 0.28f, GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath);
                var dodgeAdditiveTrail = CreateParticle(shapeTrailModule, "AdditiveTrail_Dodge", new Color(1f, 0.68f, 0.05f, 0.82f), 42f, 0.22f, GGReplicaMaterialAssetBuilder.DodgeParticlesMaterialPath);
                var dodgeHalf = CreateSpriteLayer(dodgeModule, "DodgeHalf_Sprite", "Assets/_Art/Ship/GGReplica/Sprites/SHIP_PLAYER_DODGE_HALF.png", 10, LoadMaterial(GGReplicaMaterialAssetBuilder.DodgeParticlesMaterialPath));
                dodgeHalf.enabled = false;
                var additiveCoreDodge = CreateSpriteLayer(dodgeModule, "AdditiveCore_Dodge", ReactorPath, 11, LoadMaterial(GGReplicaMaterialAssetBuilder.DodgeParticlesMaterialPath));
                additiveCoreDodge.enabled = false;
                var oldOutlineDodge = CreateSpriteLayer(dodgeModule, "Dodge_Sprite (used for old outline trail)", DodgeGhostPath, 9, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                oldOutlineDodge.enabled = false;
                var firePrimary = CreateSpriteLayer(fireAimModule, "MainAttackState", "Assets/_Art/Ship/GGReplica/Sprites/Primary_4.png", 8, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                var fireGlow = CreateSpriteLayer(fireAimModule, "MainAttackFireState", "Assets/_Art/Ship/GGReplica/Sprites/Primary_6.png", 9, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                var fireHitboxHint = CreateSpriteLayer(fireAimModule, "MainAttackStateHitbox", "Assets/_Art/Ship/GGReplica/Sprites/Primary.png", 7, LoadMaterial(GGReplicaMaterialAssetBuilder.TeleportSchemeMaterialPath));
                fireHitboxHint.transform.localScale = Vector3.one * 0.8f;
                var fireParticle = CreateParticle(fireAimModule, "GlitchEnergyReadyParticles (weapon once)", new Color(1f, 0.2f, 0.9f, 0.9f), 45f, 0.18f, GGReplicaMaterialAssetBuilder.EngineTrailMaterialPath);
                var fireMain = fireParticle.main;
                fireMain.loop = false;

                var starTrail = CreateTrail(lqTrailsContainer, "startrails", 0.4f, 4f);
                var starTrailLong = CreateTrail(lqTrailsContainer, "startrails_long", 0.75f, 2.5f);
                var darkTrail = CreateTrail(darkTrailModule, "dark_trail", 0.55f, 3f);
                var shapeTrail = CreateTrail(shapeTrailModule, "shape_trail", 0.25f, 2f);
                var fluxyTrail = CreateTrail(lqTrailModule, "fluxy_like_lq_trail", 0.6f, 3.5f, GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath);

                var grabHandsRoot = CreateChild(fluxyGrabModule, "Grab_Hands");
                var grabRight = CreateSpriteLayer(grabModule, "Ship_Sprite_Solid_Grab_R", "Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Hand_7.png", 5, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var grabLeft = CreateSpriteLayer(grabModule, "Ship_Sprite_Solid_Grab_L", "Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Hand_7.png", 5, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                grabRight.transform.localPosition = new Vector3(0.42f, -0.1f, 0f);
                grabLeft.transform.localPosition = new Vector3(-0.42f, -0.1f, 0f);
                grabLeft.flipX = true;
                var fluxyGrabRight = CreateSpriteLayer(grabHandsRoot, "FluxyGrabHolo_R", "Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Hand_7.png", 8, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var fluxyGrabLeft = CreateSpriteLayer(grabHandsRoot, "FluxyGrabHolo_L", "Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Hand_7.png", 8, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                fluxyGrabLeft.flipX = true;
                fluxyGrabRight.enabled = false;
                fluxyGrabLeft.enabled = false;
                var grabThrowPointer = CreateLine(fluxyGrabModule, "GrabThrowPointer", LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var grabLockRing = CreateSpriteLayer(fluxyGrabModule, "GrabLockRing", "Assets/_Art/Ship/GGReplica/Sprites/vfx_dot_001.png", 10, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var grabReleasePulse = CreateSpriteLayer(fluxyGrabModule, "GrabReleasePulse", "Assets/_Art/Ship/GGReplica/Sprites/vfx_dot_001.png", 11, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var grabReleaseBurst = CreateParticle(fluxyGrabModule, "GrabReleaseBurst", new Color(1f, 0.12f, 1f, 0.86f), 80f, 0.12f, GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath);
                var grabReleaseBurstMain = grabReleaseBurst.main;
                grabReleaseBurstMain.loop = false;
                var grabReleaseThrowLine = CreateLine(fluxyGrabModule, "GrabReleaseThrowLine", LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var grabTargetHolo = CreateSpriteLayer(fluxyGrabModule, "GrabTargetHolo", "Assets/_Art/Ship/GGReplica/Sprites/vfx_dot_001.png", 12, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var grabRippableOverlay = CreateSpriteLayer(fluxyGrabModule, "GrabRippableOverlay", "Assets/_Art/Ship/GGReplica/Sprites/scheme3_tp.png", 13, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var holdParticles = CreateParticle(holdModule, "HoldParticles", new Color(0.4f, 1f, 1f, 0.72f), 36f, 0.32f, GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath);
                var holdFieldRing = CreateSpriteLayer(holdModule, "HoldFieldRing", "Assets/_Art/Ship/GGReplica/Sprites/vfx_dot_001.png", 9, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var holdProgress = CreateSpriteLayer(holdModule, "HoldProgress", "Assets/_Art/Ship/GGReplica/Sprites/vfx_dot_001.png", 10, LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                var holdTetherLine = CreateLine(holdModule, "HoldTetherLine", LoadMaterial(GGReplicaMaterialAssetBuilder.FakeFluxyMaterialPath));
                grabLockRing.enabled = false;
                grabReleasePulse.enabled = false;
                grabTargetHolo.enabled = false;
                grabRippableOverlay.enabled = false;
                holdFieldRing.enabled = false;
                holdProgress.enabled = false;

                SetSerialized(motor, "_body", body);
                SetSerialized(motor, "_view", view);
                SetSerialized(motor, "_audioFeedback", audioFeedback);
                SetSerialized(motor, "_feelProfile", feelProfile);
                SetSerialized(audioFeedback, "_profile", visualProfile);
                SetSerialized(audioFeedback, "_audioSource", audioSource);
                SetSerialized(input, "_motor", motor);
                SetSerialized(view, "_visualRoot", visualRoot);
                SetSerialized(view, "_bodyLayersRoot", bodyLayers);
                SetSerialized(view, "_feelProfile", feelProfile);
                SetSerialized(view, "_playerSkin", playerSkin);
                SetSerialized(view, "_boostModuleRoot", boostModule.gameObject);
                SetSerialized(view, "_lqTrailsContainer", lqTrailsContainer.gameObject);
                SetSerialized(view, "_grabModuleRoot", grabModule.gameObject);
                SetSerialized(view, "_fluxyGrabModuleRoot", fluxyGrabModule.gameObject);
                SetSerialized(view, "_holdModuleRoot", holdModule.gameObject);
                SetSerialized(view, "_healModuleRoot", healModule.gameObject);
                SetSerialized(view, "_dodgeModuleRoot", dodgeModule.gameObject);
                SetSerialized(view, "_fireAimModuleRoot", fireAimModule.gameObject);
                SetSerializedArray(view, "_boostParticles", new Object[] { boostLoop, flameR, flameQuick, emberTrail });
                SetSerializedArray(view, "_boostBurstParticles", new Object[] { boostBurst, flameStart });
                SetSerializedArray(view, "_dodgeBurstParticles", new Object[] { dodgeShell });
                SetSerializedArray(view, "_dodgeTrailParticles", new Object[] { dodgeOutlineTrail, dodgeAdditiveTrail });
                SetSerializedArray(view, "_healParticles", new Object[] { healParticle });
                SetSerializedArray(view, "_fireAimParticles", new Object[] { fireParticle });
                SetSerializedArray(view, "_burstParticles", new Object[] { boostBurst, flameStart });
                SetSerializedArray(view, "_trailRenderers", new Object[] { starTrail, starTrailLong, darkTrail, shapeTrail, fluxyTrail });
                SetSerializedArray(view, "_bodyRenderers", new Object[] { solid, liquid, highlight, back, core });
                SetSerialized(view, "_solidRenderer", solid);
                SetSerialized(view, "_liquidRenderer", liquid);
                SetSerialized(view, "_highlightRenderer", highlight);
                SetSerializedArray(view, "_grabRenderers", new Object[] { grabRight, grabLeft });
                SetSerializedArray(view, "_grabFluxyRenderers", new Object[] { fluxyGrabRight, fluxyGrabLeft });
                SetSerialized(view, "_grabThrowPointer", grabThrowPointer);
                SetSerialized(view, "_grabLockRenderer", grabLockRing);
                SetSerialized(view, "_grabReleaseRenderer", grabReleasePulse);
                SetSerializedArray(view, "_grabReleaseParticles", new Object[] { grabReleaseBurst });
                SetSerialized(view, "_grabReleaseThrowLine", grabReleaseThrowLine);
                SetSerialized(view, "_grabTargetRenderer", grabTargetHolo);
                SetSerialized(view, "_grabTargetOverlayRenderer", grabRippableOverlay);
                SetSerializedArray(view, "_holdParticles", new Object[] { holdParticles });
                SetSerialized(view, "_holdFieldRenderer", holdFieldRing);
                SetSerialized(view, "_holdProgressRenderer", holdProgress);
                SetSerialized(view, "_holdTetherLine", holdTetherLine);
                SetSerializedArray(view, "_healRenderers", new Object[] { healShell, healDot });
                SetSerializedArray(view, "_fireAimRenderers", new Object[] { firePrimary, fireGlow, fireHitboxHint });
                SetSerialized(view, "_coreRenderer", core);
                SetSerialized(view, "_dodgeGhostRenderer", oldOutlineDodge);
                SetSerialized(view, "_fluxyTrailRenderer", fluxyTrail);
                SetSerialized(view, "_dodgeHalfRenderer", dodgeHalf);
                SetSerialized(view, "_dodgeAdditiveCoreRenderer", additiveCoreDodge);

                view.ApplyState(GGReplicaGlitchState.Idle);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success)
                {
                    Debug.LogError($"[GGReplicaGlitchV2PrefabBuilder] Failed to save {PrefabPath}");
                    return;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[GGReplicaGlitchV2PrefabBuilder] Built {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static SpriteRenderer CreateSpriteLayer(Transform parent, string name, string spritePath, int sortingOrder, Material material)
        {
            var child = CreateChild(parent, name);
            var renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            renderer.sortingOrder = sortingOrder;
            if (material != null) renderer.sharedMaterial = material;
            return renderer;
        }

        private static ParticleSystem CreateParticle(Transform parent, string name, Color color, float rate, float lifetime)
        {
            return CreateParticle(parent, name, color, rate, lifetime, null);
        }

        private static ParticleSystem CreateParticle(Transform parent, string name, Color color, float rate, float lifetime, string materialPath)
        {
            var child = CreateChild(parent, name);
            var particle = child.gameObject.AddComponent<ParticleSystem>();
            var main = particle.main;
            main.startColor = color;
            main.startLifetime = lifetime;
            main.startSpeed = 1.6f;
            main.startSize = 0.12f;
            main.loop = true;
            main.playOnAwake = false;
            var emission = particle.emission;
            emission.rateOverTime = rate;
            if (!string.IsNullOrEmpty(materialPath))
            {
                var renderer = child.gameObject.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = LoadMaterial(materialPath);
            }

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particle;
        }

        private static TrailRenderer CreateTrail(Transform parent, string name, float time, float width)
        {
            return CreateTrail(parent, name, time, width, GGReplicaMaterialAssetBuilder.PlayerLqTrailMaterialPath);
        }

        private static TrailRenderer CreateTrail(Transform parent, string name, float time, float width, string materialPath)
        {
            var child = CreateChild(parent, name);
            var trail = child.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = LoadMaterial(materialPath);
            trail.time = time;
            trail.widthMultiplier = width;
            trail.minVertexDistance = 0.08f;
            trail.numCapVertices = 8;
            trail.emitting = false;
            return trail;
        }

        private static LineRenderer CreateLine(Transform parent, string name, Material material)
        {
            var child = CreateChild(parent, name);
            var line = child.gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = 0.04f;
            line.endWidth = 0.015f;
            line.numCapVertices = 6;
            line.enabled = false;
            line.SetPosition(0, new Vector3(-0.74f, -0.08f, 0f));
            line.SetPosition(1, new Vector3(0.74f, -0.08f, 0f));
            return line;
        }

        private static Material LoadMaterial(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static void SetSerialized(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            so.Dispose();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerializedArray(Object target, string propertyName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            so.Dispose();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
