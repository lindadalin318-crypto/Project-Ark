#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Builds the isolated GGReplica PlayerSkin asset from curated imported sprites.
    /// </summary>
    public static class GGReplicaPlayerSkinAssetBuilder
    {
        private const string PlayerSkinPath = "Assets/_Data/Ship/GGReplicaPlayerSkin.asset";
        private const string SpriteRoot = "Assets/_Art/Ship/GGReplica/Sprites";

        [MenuItem("ProjectArk/Ship/GG Replica/Build Player Skin Asset")]
        public static bool BuildPlayerSkinAsset()
        {
            return BuildPlayerSkinAsset(PlayerSkinPath, SpriteRoot);
        }

        internal static bool BuildPlayerSkinAsset(string playerSkinPath, string spriteRoot)
        {
            if (!TryBuildPacks(spriteRoot, out var packs)) return false;
            if (!TryLoadFixedSprites(spriteRoot, out var fixedSprites)) return false;

            bool createdAsset = false;
            var skin = AssetDatabase.LoadAssetAtPath<GGReplicaPlayerSkinSO>(playerSkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<GGReplicaPlayerSkinSO>();
                AssetDatabase.CreateAsset(skin, playerSkinPath);
                createdAsset = true;
            }

            var so = new SerializedObject(skin);
            so.UpdateIfRequiredOrScript();

            bool canWrite = true;
            canWrite &= SetPacks(so.FindProperty("_stateToSpritesTable"), packs);
            canWrite &= SetObject(so, "_shipSpriteSolidGrabR", fixedSprites.GrabRight);
            canWrite &= SetObject(so, "_shipSpriteSolidGrabL", fixedSprites.GrabLeft);
            canWrite &= SetObject(so, "_shipSpriteBack", fixedSprites.Back);
            canWrite &= SetObject(so, "_reactorSprite", fixedSprites.Reactor);
            canWrite &= SetObject(so, "_eyeSprite", fixedSprites.Eye);
            canWrite &= SetObject(so, "_viewSilhouetteSprite", fixedSprites.ViewSilhouette);
            canWrite &= SetObject(so, "_dodgeSprite", fixedSprites.Dodge);
            canWrite &= SetObject(so, "_dodgeHalfSprite", fixedSprites.DodgeHalf);
            canWrite &= SetColor(so, "_shipHighlightColor", FromHtml("#8B17FF"));
            canWrite &= SetColor(so, "_transitionColor", FromHtml("#AB00FF"));

            if (!canWrite)
            {
                so.Dispose();
                if (createdAsset)
                {
                    AssetDatabase.DeleteAsset(playerSkinPath);
                }

                Debug.LogError($"[GGReplicaPlayerSkinAssetBuilder] Aborted build; {playerSkinPath} was not saved.");
                return false;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            so.Dispose();

            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GGReplicaPlayerSkinAssetBuilder] Built {playerSkinPath}");
            return true;
        }

        private static bool TryBuildPacks(string spriteRoot, out GGReplicaViewSpritePack[] packs)
        {
            var result = new List<GGReplicaViewSpritePack>
            {
                Pack(spriteRoot, GGReplicaViewState.Idle, 0.2f, "Movement_10.png", "Movement_3.png", "Movement_21.png", Vector3.zero),
                Pack(spriteRoot, GGReplicaViewState.Undefined, 0.2f, "Movement_10.png", "Movement_3.png", "Movement_21.png", Vector3.zero),
                Pack(spriteRoot, GGReplicaViewState.Boost, 0.2f, "Boost_2.png", "Boost_16.png", "Boost_8.png", Vector3.zero),
                new GGReplicaViewSpritePack { State = GGReplicaViewState.Dodge, FadeDuration = 0.2f, SpritesOffset = Vector3.zero },
                Pack(spriteRoot, GGReplicaViewState.Aim, 0.2f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero),
                Pack(spriteRoot, GGReplicaViewState.Fire, 0f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero),
                Pack(spriteRoot, GGReplicaViewState.WeaponUseMoment, 0f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero),
                Pack(spriteRoot, GGReplicaViewState.HeavyFire, 0.2f, "Secondary_8.png", "Secondary_0.png", "Secondary_17.png", Vector3.zero),
                Pack(spriteRoot, GGReplicaViewState.HeavyAim, 0.2f, "Secondary_8.png", "Secondary_0.png", "Secondary_17.png", Vector3.zero),
                Pack(spriteRoot, GGReplicaViewState.Grab, 0f, "GrabGun_Base_9.png", "GrabGun_Base_9.png", "GrabGun_Base_8.png", new Vector3(0f, -0.1f, 0f)),
                Pack(spriteRoot, GGReplicaViewState.Heal, 0.2f, "Healing_0.png", "Healing.png", "vfx_dot_001.png", Vector3.zero)
            };

            bool hasMissingSprites = false;
            foreach (var pack in result)
            {
                if (pack.State == GGReplicaViewState.Dodge) continue;
                hasMissingSprites |= pack.SolidSprite == null || pack.LiquidSprite == null || pack.HighlightSprite == null;
            }

            packs = hasMissingSprites ? Array.Empty<GGReplicaViewSpritePack>() : result.ToArray();
            return !hasMissingSprites;
        }

        private static GGReplicaViewSpritePack Pack(
            string spriteRoot,
            GGReplicaViewState state,
            float fadeDuration,
            string solidSprite,
            string liquidSprite,
            string highlightSprite,
            Vector3 spritesOffset)
        {
            return new GGReplicaViewSpritePack
            {
                State = state,
                FadeDuration = fadeDuration,
                SolidSprite = LoadSprite(spriteRoot, solidSprite, required: true),
                LiquidSprite = LoadSprite(spriteRoot, liquidSprite, required: true),
                HighlightSprite = LoadSprite(spriteRoot, highlightSprite, required: true),
                SpritesOffset = spritesOffset
            };
        }

        private static bool TryLoadFixedSprites(string spriteRoot, out FixedSprites fixedSprites)
        {
            fixedSprites = new FixedSprites
            {
                GrabRight = LoadSprite(spriteRoot, "GrabGun_Hand_7.png", required: true),
                GrabLeft = LoadSprite(spriteRoot, "GrabGun_Hand_7.png", required: true),
                Back = LoadSprite(spriteRoot, "GrabGun_Back_3.png", required: true),
                Reactor = LoadSprite(spriteRoot, "reactor.png", required: true),
                Eye = LoadSprite(spriteRoot, "reactor.png", required: true),
                ViewSilhouette = LoadSprite(spriteRoot, "scheme3_tp.png", required: true),
                Dodge = LoadSprite(spriteRoot, "player_test_fire.png", required: true),
                DodgeHalf = LoadSprite(spriteRoot, "SHIP_PLAYER_DODGE_HALF.png", required: true)
            };

            return fixedSprites.GrabRight != null &&
                   fixedSprites.GrabLeft != null &&
                   fixedSprites.Back != null &&
                   fixedSprites.Reactor != null &&
                   fixedSprites.Eye != null &&
                   fixedSprites.ViewSilhouette != null &&
                   fixedSprites.Dodge != null &&
                   fixedSprites.DodgeHalf != null;
        }

        private static Sprite LoadSprite(string spriteRoot, string fileName, bool required)
        {
            string path = $"{spriteRoot}/{fileName}";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null && required)
            {
                Debug.LogError($"[GGReplicaPlayerSkinAssetBuilder] Missing required sprite: {path}");
            }

            return sprite;
        }

        private static bool SetPacks(SerializedProperty property, GGReplicaViewSpritePack[] packs)
        {
            if (property == null)
            {
                Debug.LogError("[GGReplicaPlayerSkinAssetBuilder] Missing _stateToSpritesTable property.");
                return false;
            }

            bool canWrite = true;
            property.arraySize = packs.Length;
            for (int i = 0; i < packs.Length; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                var pack = packs[i];
                canWrite &= TrySetRelativeEnum(element, "State", EnumIndex(pack.State));
                canWrite &= TrySetRelativeFloat(element, "FadeDuration", pack.FadeDuration);
                canWrite &= TrySetRelativeObject(element, "SolidSprite", pack.SolidSprite);
                canWrite &= TrySetRelativeObject(element, "LiquidSprite", pack.LiquidSprite);
                canWrite &= TrySetRelativeObject(element, "HighlightSprite", pack.HighlightSprite);
                canWrite &= TrySetRelativeVector3(element, "SpritesOffset", pack.SpritesOffset);
            }

            return canWrite;
        }

        private static bool TrySetRelativeEnum(SerializedProperty element, string propertyName, int value)
        {
            var property = element.FindPropertyRelative(propertyName);
            if (property == null)
            {
                Debug.LogError($"[GGReplicaPlayerSkinAssetBuilder] Missing relative property {propertyName}.");
                return false;
            }

            property.enumValueIndex = value;
            return true;
        }

        private static bool TrySetRelativeFloat(SerializedProperty element, string propertyName, float value)
        {
            var property = element.FindPropertyRelative(propertyName);
            if (property == null)
            {
                Debug.LogError($"[GGReplicaPlayerSkinAssetBuilder] Missing relative property {propertyName}.");
                return false;
            }

            property.floatValue = value;
            return true;
        }

        private static bool TrySetRelativeObject(SerializedProperty element, string propertyName, UnityEngine.Object value)
        {
            var property = element.FindPropertyRelative(propertyName);
            if (property == null)
            {
                Debug.LogError($"[GGReplicaPlayerSkinAssetBuilder] Missing relative property {propertyName}.");
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private static bool TrySetRelativeVector3(SerializedProperty element, string propertyName, Vector3 value)
        {
            var property = element.FindPropertyRelative(propertyName);
            if (property == null)
            {
                Debug.LogError($"[GGReplicaPlayerSkinAssetBuilder] Missing relative property {propertyName}.");
                return false;
            }

            property.vector3Value = value;
            return true;
        }

        private static int EnumIndex(GGReplicaViewState state)
        {
            var values = (GGReplicaViewState[])Enum.GetValues(typeof(GGReplicaViewState));
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == state)
                {
                    return i;
                }
            }

            return 0;
        }

        private static bool SetObject(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[GGReplicaPlayerSkinAssetBuilder] Missing serialized property {propertyName}.");
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private static bool SetColor(SerializedObject so, string propertyName, Color value)
        {
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[GGReplicaPlayerSkinAssetBuilder] Missing serialized property {propertyName}.");
                return false;
            }

            property.colorValue = value;
            return true;
        }

        private static Color FromHtml(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out var color) ? color : Color.white;
        }

        private struct FixedSprites
        {
            public Sprite GrabRight;
            public Sprite GrabLeft;
            public Sprite Back;
            public Sprite Reactor;
            public Sprite Eye;
            public Sprite ViewSilhouette;
            public Sprite Dodge;
            public Sprite DodgeHalf;
        }
    }
}
#endif
