using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaTestSwitcherTests
    {
        [Test]
        public void SetReplicaActive_TogglesLiveAndReplicaShips()
        {
            var root = new GameObject("SwitcherRoot");
            var live = new GameObject("LiveShip_A");
            var replica = new GameObject("GGReplicaShip_B");
            try
            {
                var switcher = root.AddComponent<GGReplicaTestSwitcher>();
                SetPrivateField(switcher, "_liveShip", live);
                SetPrivateField(switcher, "_replicaShip", replica);

                switcher.SetReplicaActive(true);

                Assert.That(live.activeSelf, Is.False);
                Assert.That(replica.activeSelf, Is.True);

                switcher.SetReplicaActive(false);

                Assert.That(live.activeSelf, Is.True);
                Assert.That(replica.activeSelf, Is.False);
                Assert.That(switcher.ReplicaActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(live);
                Object.DestroyImmediate(replica);
            }
        }

        [Test]
        public void ForceReplicaViewState_ForwardsOriginalIntToPlayerViewAdapter()
        {
            var rig = CreatePlayerViewRig();
            var switcherRoot = new GameObject("SwitcherRoot");
            try
            {
                var switcher = switcherRoot.AddComponent<GGReplicaTestSwitcher>();
                SetPrivateField(switcher, "_replicaView", rig.Adapter);

                switcher.ForceReplicaViewState(7);

                Assert.That(rig.Adapter.CurrentState, Is.EqualTo(GGReplicaViewState.Grab));
            }
            finally
            {
                Object.DestroyImmediate(switcherRoot);
                rig.Destroy();
            }
        }

        private static PlayerViewRig CreatePlayerViewRig()
        {
            var root = new GameObject("PlayerViewRig");
            var spritesRoot = new GameObject("SpritesRoot").transform;
            spritesRoot.SetParent(root.transform, false);
            var skin = ScriptableObject.CreateInstance<GGReplicaPlayerSkinSO>();
            var adapter = root.AddComponent<GGReplicaPlayerViewAdapter>();
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
            SetPrivateField(skin, "_stateToSpritesTable", new[]
            {
                new GGReplicaViewSpritePack
                {
                    State = GGReplicaViewState.Grab,
                    SolidSprite = sprite,
                    LiquidSprite = sprite,
                    HighlightSprite = sprite,
                    SpritesOffset = new Vector3(0f, -0.1f, 0f)
                }
            });

            SetPrivateField(adapter, "_skin", skin);
            SetPrivateField(adapter, "_spritesRoot", spritesRoot);
            SetPrivateField(adapter, "_shipSolidRenderer", CreateRenderer(spritesRoot, "Solid"));
            SetPrivateField(adapter, "_shipLiquidRenderer", CreateRenderer(spritesRoot, "Liquid"));
            SetPrivateField(adapter, "_shipHighlightRenderer", CreateRenderer(spritesRoot, "Highlight"));
            SetPrivateField(adapter, "_shipBackRenderer", CreateRenderer(spritesRoot, "Back"));
            SetPrivateField(adapter, "_shipGrabRightRenderer", CreateRenderer(spritesRoot, "GrabR"));
            SetPrivateField(adapter, "_shipGrabLeftRenderer", CreateRenderer(spritesRoot, "GrabL"));
            SetPrivateField(adapter, "_coreRenderer", CreateRenderer(spritesRoot, "Core"));
            SetPrivateField(adapter, "_eyeRenderer", CreateRenderer(spritesRoot, "Eye"));
            SetPrivateField(adapter, "_viewSilhouetteRenderer", CreateRenderer(spritesRoot, "View"));
            SetPrivateField(adapter, "_dodgeRenderer", CreateRenderer(spritesRoot, "Dodge"));
            SetPrivateField(adapter, "_dodgeHalfRenderer", CreateRenderer(spritesRoot, "DodgeHalf"));
            return new PlayerViewRig(root, skin, adapter, sprite);
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<SpriteRenderer>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            field!.SetValue(target, value);
        }

        private sealed class PlayerViewRig
        {
            private readonly GameObject _root;
            private readonly GGReplicaPlayerSkinSO _skin;
            private readonly Sprite _sprite;

            public PlayerViewRig(GameObject root, GGReplicaPlayerSkinSO skin, GGReplicaPlayerViewAdapter adapter, Sprite sprite)
            {
                _root = root;
                _skin = skin;
                Adapter = adapter;
                _sprite = sprite;
            }

            public GGReplicaPlayerViewAdapter Adapter { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(_skin);
                Object.DestroyImmediate(_root);
                Object.DestroyImmediate(_sprite);
            }
        }
    }
}
