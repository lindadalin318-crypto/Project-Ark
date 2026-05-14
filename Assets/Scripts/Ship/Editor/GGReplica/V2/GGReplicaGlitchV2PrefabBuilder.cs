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
                var feelProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipFeelProfileSO>(FeelProfilePath);
                if (feelProfile == null)
                {
                    Debug.LogError($"[GGReplicaGlitchV2PrefabBuilder] Missing feel profile: {FeelProfilePath}");
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
                var healModule = CreateChild(visualRoot, "HealModule");
                var dodgeModule = CreateChild(visualRoot, "DodgeModule");
                var fireAimModule = CreateChild(visualRoot, "FireAimModule");
                var hitbox = CreateChild(visualRoot, "ShapeShiftStateHitbox");
                hitbox.gameObject.layer = 0;

                var boostLoop = CreateParticle(boostModule, "vfx_boost_trail_loop_enhanced", new Color(0.75f, 0f, 1f, 0.7f), 18f, 0.45f);
                var boostBurst = CreateParticle(boostModule, "vfx_boost_trail_burst_enhanced", new Color(1f, 0.2f, 1f, 0.9f), 35f, 0.18f);
                var flameR = CreateParticle(boostModule, "ps_techno_flame_trail_R", new Color(0.9f, 0.1f, 1f, 0.85f), 22f, 0.35f);
                var flameQuick = CreateParticle(boostModule, "ps_techno_flame_trail_quick", new Color(0.6f, 0.1f, 1f, 0.7f), 28f, 0.22f);
                var flameStart = CreateParticle(boostModule, "ps_techno_flame_trail_start", new Color(1f, 0.35f, 1f, 0.9f), 40f, 0.2f);
                var emberTrail = CreateParticle(boostModule, "ps_ember_trail", new Color(0.9f, 0.25f, 1f, 0.65f), 16f, 0.6f);
                CreateParticle(healModule, "ps_glitch_heal", new Color(0.25f, 1f, 0.85f, 0.8f), 20f, 0.45f);
                CreateParticle(dodgeModule, "ps_dodge_shell", new Color(0.75f, 0.15f, 1f, 0.75f), 30f, 0.16f);

                var starTrail = CreateTrail(lqTrailsContainer, "startrails", 0.4f, 4f);
                var starTrailLong = CreateTrail(lqTrailsContainer, "startrails_long", 0.75f, 2.5f);
                var darkTrail = CreateTrail(darkTrailModule, "dark_trail", 0.55f, 3f);
                var shapeTrail = CreateTrail(shapeTrailModule, "shape_trail", 0.25f, 2f);
                var fluxyTrail = CreateTrail(lqTrailModule, "fluxy_like_lq_trail", 0.6f, 3.5f);

                var grabRight = CreateSpriteLayer(grabModule, "Ship_Sprite_Solid_Grab_R", "Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Hand_7.png", 5, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                var grabLeft = CreateSpriteLayer(grabModule, "Ship_Sprite_Solid_Grab_L", "Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Hand_7.png", 5, LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath));
                grabRight.transform.localPosition = new Vector3(0.42f, -0.1f, 0f);
                grabLeft.transform.localPosition = new Vector3(-0.42f, -0.1f, 0f);
                grabLeft.flipX = true;

                SetSerialized(motor, "_body", body);
                SetSerialized(motor, "_view", view);
                SetSerialized(motor, "_feelProfile", feelProfile);
                SetSerialized(input, "_motor", motor);
                SetSerialized(view, "_visualRoot", visualRoot);
                SetSerialized(view, "_boostModuleRoot", boostModule.gameObject);
                SetSerialized(view, "_lqTrailsContainer", lqTrailsContainer.gameObject);
                SetSerialized(view, "_grabModuleRoot", grabModule.gameObject);
                SetSerialized(view, "_healModuleRoot", healModule.gameObject);
                SetSerialized(view, "_dodgeModuleRoot", dodgeModule.gameObject);
                SetSerialized(view, "_fireAimModuleRoot", fireAimModule.gameObject);
                SetSerializedArray(view, "_boostParticles", new Object[] { boostLoop, flameR, flameQuick, emberTrail });
                SetSerializedArray(view, "_burstParticles", new Object[] { boostBurst, flameStart });
                SetSerializedArray(view, "_trailRenderers", new Object[] { starTrail, starTrailLong, darkTrail, shapeTrail, fluxyTrail });
                SetSerializedArray(view, "_bodyRenderers", new Object[] { solid, liquid, highlight, back, core });
                SetSerialized(view, "_coreRenderer", core);
                SetSerialized(view, "_dodgeGhostRenderer", dodgeGhost);

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
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particle;
        }

        private static TrailRenderer CreateTrail(Transform parent, string name, float time, float width)
        {
            var child = CreateChild(parent, name);
            var trail = child.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = LoadMaterial(GGReplicaMaterialAssetBuilder.PlayerLqTrailMaterialPath);
            trail.time = time;
            trail.widthMultiplier = width;
            trail.minVertexDistance = 0.08f;
            trail.numCapVertices = 8;
            trail.emitting = false;
            return trail;
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
