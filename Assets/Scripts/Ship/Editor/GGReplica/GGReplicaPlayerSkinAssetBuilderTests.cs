#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaPlayerSkinAssetBuilderTests
    {
        private const string TestPlayerSkinPath = "Assets/_Data/Ship/GGReplicaPlayerSkin_Test.asset";
        private const string SpriteRoot = "Assets/_Art/Ship/GGReplica/Sprites";

        [SetUp]
        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(TestPlayerSkinPath);
        }

        [Test]
        public void BuildPlayerSkinAsset_CreatesFullStateTableAndFixedFields()
        {
            Assert.That(GGReplicaPlayerSkinAssetBuilder.BuildPlayerSkinAsset(TestPlayerSkinPath, SpriteRoot), Is.True);

            var skin = AssetDatabase.LoadAssetAtPath<GGReplicaPlayerSkinSO>(TestPlayerSkinPath);
            Assert.That(skin, Is.Not.Null);

            AssertPack(skin, GGReplicaViewState.Idle, 0.2f, "Movement_10.png", "Movement_3.png", "Movement_21.png", Vector3.zero);
            AssertPack(skin, GGReplicaViewState.Undefined, 0.2f, "Movement_10.png", "Movement_3.png", "Movement_21.png", Vector3.zero);
            AssertPack(skin, GGReplicaViewState.Boost, 0.2f, "Boost_2.png", "Boost_16.png", "Boost_8.png", Vector3.zero);
            AssertDodgePack(skin);
            AssertPack(skin, GGReplicaViewState.Aim, 0.2f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero);
            AssertPack(skin, GGReplicaViewState.Fire, 0f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero);
            AssertPack(skin, GGReplicaViewState.WeaponUseMoment, 0f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero);
            AssertPack(skin, GGReplicaViewState.HeavyFire, 0.2f, "Secondary_8.png", "Secondary_0.png", "Secondary_17.png", Vector3.zero);
            AssertPack(skin, GGReplicaViewState.HeavyAim, 0.2f, "Secondary_8.png", "Secondary_0.png", "Secondary_17.png", Vector3.zero);
            AssertPack(skin, GGReplicaViewState.Grab, 0f, "GrabGun_Base_9.png", "GrabGun_Base_9.png", "GrabGun_Base_8.png", new Vector3(0f, -0.1f, 0f));
            AssertPack(skin, GGReplicaViewState.Heal, 0.2f, "Healing_0.png", "Healing.png", "vfx_dot_001.png", Vector3.zero);

            AssertSpritePath(skin.ShipSpriteSolidGrabR, "GrabGun_Hand_7.png");
            AssertSpritePath(skin.ShipSpriteSolidGrabL, "GrabGun_Hand_7.png");
            AssertSpritePath(skin.ShipSpriteBack, "GrabGun_Back_3.png");
            AssertSpritePath(skin.ReactorSprite, "reactor.png");
            AssertSpritePath(skin.EyeSprite, "reactor.png");
            AssertSpritePath(skin.ViewSilhouetteSprite, "scheme3_tp.png");
            Assert.That(skin.DodgeSprite, Is.Null, "Old fire test sprite must not be assigned as a Dodge sprite.");
            AssertSpritePath(skin.DodgeHalfSprite, "SHIP_PLAYER_DODGE_HALF.png");
            Assert.That(skin.ShipHighlightColor, Is.EqualTo(FromHtml("#8B17FF")));
            Assert.That(skin.TransitionColor, Is.EqualTo(FromHtml("#AB00FF")));
        }

        private static void AssertPack(
            GGReplicaPlayerSkinSO skin,
            GGReplicaViewState state,
            float fadeDuration,
            string solid,
            string liquid,
            string highlight,
            Vector3 offset)
        {
            Assert.That(skin.TryGetPack(state, out var pack), Is.True, $"Missing pack for {state}.");
            Assert.That(pack.FadeDuration, Is.EqualTo(fadeDuration));
            AssertSpritePath(pack.SolidSprite, solid);
            AssertSpritePath(pack.LiquidSprite, liquid);
            AssertSpritePath(pack.HighlightSprite, highlight);
            Assert.That(pack.SpritesOffset, Is.EqualTo(offset));
        }

        private static void AssertDodgePack(GGReplicaPlayerSkinSO skin)
        {
            Assert.That(skin.TryGetPack(GGReplicaViewState.Dodge, out var dodge), Is.True);
            Assert.That(dodge.FadeDuration, Is.EqualTo(0.2f));
            Assert.That(dodge.SolidSprite, Is.Null);
            Assert.That(dodge.LiquidSprite, Is.Null);
            Assert.That(dodge.HighlightSprite, Is.Null);
            Assert.That(dodge.SpritesOffset, Is.EqualTo(Vector3.zero));
        }

        private static void AssertSpritePath(Sprite sprite, string fileName)
        {
            Assert.That(sprite, Is.Not.Null, $"Missing sprite {fileName}.");
            Assert.That(AssetDatabase.GetAssetPath(sprite), Is.EqualTo($"{SpriteRoot}/{fileName}"));
        }

        private static Color FromHtml(string html)
        {
            Assert.That(ColorUtility.TryParseHtmlString(html, out var color), Is.True);
            return color;
        }
    }
}
#endif
