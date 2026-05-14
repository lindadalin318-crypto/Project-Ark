using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaPlayerSkinTests
    {
        [Test]
        public void ViewState_IntValues_MatchOriginalGG()
        {
            Assert.That((int)GGReplicaViewState.Idle, Is.EqualTo(0));
            Assert.That((int)GGReplicaViewState.Boost, Is.EqualTo(1));
            Assert.That((int)GGReplicaViewState.Dodge, Is.EqualTo(2));
            Assert.That((int)GGReplicaViewState.Aim, Is.EqualTo(3));
            Assert.That((int)GGReplicaViewState.Fire, Is.EqualTo(4));
            Assert.That((int)GGReplicaViewState.HeavyFire, Is.EqualTo(5));
            Assert.That((int)GGReplicaViewState.HeavyAim, Is.EqualTo(6));
            Assert.That((int)GGReplicaViewState.Grab, Is.EqualTo(7));
            Assert.That((int)GGReplicaViewState.WeaponUseMoment, Is.EqualTo(8));
            Assert.That((int)GGReplicaViewState.Heal, Is.EqualTo(9));
            Assert.That((int)GGReplicaViewState.Undefined, Is.EqualTo(15));
        }

        [Test]
        public void TryGetPack_DodgePack_AllowsNullBodySprites()
        {
            var skin = ScriptableObject.CreateInstance<GGReplicaPlayerSkinSO>();
            SetPrivateField(skin, "_stateToSpritesTable", new[]
            {
                new GGReplicaViewSpritePack { State = GGReplicaViewState.Dodge, FadeDuration = 0.2f }
            });

            Assert.That(skin.TryGetPack(GGReplicaViewState.Dodge, out var pack), Is.True);
            Assert.That(pack.SolidSprite, Is.Null);
            Assert.That(pack.LiquidSprite, Is.Null);
            Assert.That(pack.HighlightSprite, Is.Null);

            Object.DestroyImmediate(skin);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            field!.SetValue(target, value);
        }
    }
}
