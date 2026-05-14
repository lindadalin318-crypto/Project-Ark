using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaPlayerViewAdapterTests
    {
        [Test]
        public void ChangeViewState_IntAppliesBoostSprites()
        {
            var rig = CreateRig();
            try
            {
                rig.Adapter.ChangeViewState(1);

                Assert.That(rig.Adapter.CurrentState, Is.EqualTo(GGReplicaViewState.Boost));
                Assert.That(rig.Adapter.GetCurrentSpritePack().State, Is.EqualTo(GGReplicaViewState.Boost));
                Assert.That(rig.SolidRenderer.sprite, Is.SameAs(rig.BoostSolid));
                Assert.That(rig.LiquidRenderer.sprite, Is.SameAs(rig.BoostLiquid));
                Assert.That(rig.HighlightRenderer.sprite, Is.SameAs(rig.BoostHighlight));
                AssertFixedSpritesApplied(rig);
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void ChangeViewState_DodgeDoesNotClearExistingBodySprites()
        {
            var rig = CreateRig();
            try
            {
                rig.Adapter.ChangeViewState(GGReplicaViewState.Idle);

                rig.Adapter.ChangeViewState(2);

                Assert.That(rig.Adapter.CurrentState, Is.EqualTo(GGReplicaViewState.Dodge));
                Assert.That(rig.SolidRenderer.sprite, Is.SameAs(rig.IdleSolid));
                Assert.That(rig.LiquidRenderer.sprite, Is.SameAs(rig.IdleLiquid));
                Assert.That(rig.HighlightRenderer.sprite, Is.SameAs(rig.IdleHighlight));
                Assert.That(rig.DodgeRenderer.enabled, Is.True);
                Assert.That(rig.DodgeHalfRenderer.enabled, Is.True);
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void ChangeViewState_GrabAppliesGrabSpritesOffsetAndHands()
        {
            var rig = CreateRig();
            try
            {
                rig.Adapter.ChangeViewState(7);

                Assert.That(rig.Adapter.CurrentState, Is.EqualTo(GGReplicaViewState.Grab));
                Assert.That(rig.SolidRenderer.sprite, Is.SameAs(rig.GrabSolid));
                Assert.That(rig.LiquidRenderer.sprite, Is.SameAs(rig.GrabLiquid));
                Assert.That(rig.HighlightRenderer.sprite, Is.SameAs(rig.GrabHighlight));
                Assert.That(rig.SpritesRoot.localPosition, Is.EqualTo(new Vector3(0f, -0.1f, 0f)));
                Assert.That(rig.GrabRightRenderer.enabled, Is.True);
                Assert.That(rig.GrabLeftRenderer.enabled, Is.True);
                Assert.That(rig.GrabRightRenderer.sprite, Is.SameAs(rig.GrabHand));
                Assert.That(rig.GrabLeftRenderer.sprite, Is.SameAs(rig.GrabHand));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void ChangeViewState_MissingPackWarnsOrErrorsWithoutChangingState()
        {
            var rig = CreateRig();
            try
            {
                rig.Adapter.ChangeViewState(GGReplicaViewState.Boost);
                var previousPack = rig.Adapter.GetCurrentSpritePack();

                LogAssert.Expect(LogType.Warning, "[GGReplicaPlayerViewAdapter] Missing sprite pack for HeavyFire (5).");
                rig.Adapter.ChangeViewState(GGReplicaViewState.HeavyFire, strict: false);
                Assert.That(rig.Adapter.CurrentState, Is.EqualTo(GGReplicaViewState.Boost));
                Assert.That(rig.Adapter.GetCurrentSpritePack().SolidSprite, Is.SameAs(previousPack.SolidSprite));

                LogAssert.Expect(LogType.Error, "[GGReplicaPlayerViewAdapter] Missing sprite pack for HeavyFire (5).");
                rig.Adapter.ChangeViewState(GGReplicaViewState.HeavyFire, strict: true);
                Assert.That(rig.Adapter.CurrentState, Is.EqualTo(GGReplicaViewState.Boost));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void GetCurrentSpritePack_ReturnsCopyNotMutableInternalPack()
        {
            var rig = CreateRig();
            try
            {
                rig.Adapter.ChangeViewState(GGReplicaViewState.Boost);
                var exposedPack = rig.Adapter.GetCurrentSpritePack();
                exposedPack.SolidSprite = rig.IdleSolid;

                Assert.That(rig.Adapter.GetCurrentSpritePack().SolidSprite, Is.SameAs(rig.BoostSolid));
                Assert.That(rig.SolidRenderer.sprite, Is.SameAs(rig.BoostSolid));
            }
            finally
            {
                rig.Destroy();
            }
        }

        private static TestRig CreateRig()
        {
            var root = new GameObject("GGReplicaPlayerViewAdapterTestRoot");
            var spritesRoot = new GameObject("SpritesRoot").transform;
            spritesRoot.SetParent(root.transform, false);

            var rig = new TestRig
            {
                Root = root,
                SpritesRoot = spritesRoot,
                Skin = ScriptableObject.CreateInstance<GGReplicaPlayerSkinSO>(),
                Adapter = root.AddComponent<GGReplicaPlayerViewAdapter>(),
                SolidRenderer = CreateRenderer(spritesRoot, "Solid"),
                LiquidRenderer = CreateRenderer(spritesRoot, "Liquid"),
                HighlightRenderer = CreateRenderer(spritesRoot, "Highlight"),
                BackRenderer = CreateRenderer(spritesRoot, "Back"),
                GrabRightRenderer = CreateRenderer(spritesRoot, "GrabR"),
                GrabLeftRenderer = CreateRenderer(spritesRoot, "GrabL"),
                CoreRenderer = CreateRenderer(spritesRoot, "Core"),
                EyeRenderer = CreateRenderer(spritesRoot, "Eye"),
                ViewRenderer = CreateRenderer(spritesRoot, "View"),
                DodgeRenderer = CreateRenderer(spritesRoot, "Dodge"),
                DodgeHalfRenderer = CreateRenderer(spritesRoot, "DodgeHalf"),
                IdleSolid = CreateSprite("IdleSolid"),
                IdleLiquid = CreateSprite("IdleLiquid"),
                IdleHighlight = CreateSprite("IdleHighlight"),
                BoostSolid = CreateSprite("BoostSolid"),
                BoostLiquid = CreateSprite("BoostLiquid"),
                BoostHighlight = CreateSprite("BoostHighlight"),
                GrabSolid = CreateSprite("GrabSolid"),
                GrabLiquid = CreateSprite("GrabLiquid"),
                GrabHighlight = CreateSprite("GrabHighlight"),
                GrabHand = CreateSprite("GrabHand"),
                Back = CreateSprite("Back"),
                Reactor = CreateSprite("Reactor"),
                Eye = CreateSprite("Eye"),
                View = CreateSprite("View"),
                Dodge = CreateSprite("Dodge"),
                DodgeHalf = CreateSprite("DodgeHalf")
            };

            rig.CreatedSprites.AddRange(new[]
            {
                rig.IdleSolid, rig.IdleLiquid, rig.IdleHighlight,
                rig.BoostSolid, rig.BoostLiquid, rig.BoostHighlight,
                rig.GrabSolid, rig.GrabLiquid, rig.GrabHighlight,
                rig.GrabHand, rig.Back, rig.Reactor, rig.Eye, rig.View, rig.Dodge, rig.DodgeHalf
            });

            SetPrivateField(rig.Skin, "_shipSpriteSolidGrabR", rig.GrabHand);
            SetPrivateField(rig.Skin, "_shipSpriteSolidGrabL", rig.GrabHand);
            SetPrivateField(rig.Skin, "_shipSpriteBack", rig.Back);
            SetPrivateField(rig.Skin, "_reactorSprite", rig.Reactor);
            SetPrivateField(rig.Skin, "_eyeSprite", rig.Eye);
            SetPrivateField(rig.Skin, "_viewSilhouetteSprite", rig.View);
            SetPrivateField(rig.Skin, "_dodgeSprite", rig.Dodge);
            SetPrivateField(rig.Skin, "_dodgeHalfSprite", rig.DodgeHalf);
            SetPrivateField(rig.Skin, "_shipHighlightColor", Color.magenta);
            SetPrivateField(rig.Skin, "_stateToSpritesTable", new[]
            {
                new GGReplicaViewSpritePack
                {
                    State = GGReplicaViewState.Idle,
                    SolidSprite = rig.IdleSolid,
                    LiquidSprite = rig.IdleLiquid,
                    HighlightSprite = rig.IdleHighlight,
                    SpritesOffset = Vector3.zero
                },
                new GGReplicaViewSpritePack
                {
                    State = GGReplicaViewState.Boost,
                    SolidSprite = rig.BoostSolid,
                    LiquidSprite = rig.BoostLiquid,
                    HighlightSprite = rig.BoostHighlight,
                    SpritesOffset = Vector3.zero
                },
                new GGReplicaViewSpritePack
                {
                    State = GGReplicaViewState.Dodge,
                    FadeDuration = 0.2f,
                    SpritesOffset = Vector3.zero
                },
                new GGReplicaViewSpritePack
                {
                    State = GGReplicaViewState.Grab,
                    SolidSprite = rig.GrabSolid,
                    LiquidSprite = rig.GrabLiquid,
                    HighlightSprite = rig.GrabHighlight,
                    SpritesOffset = new Vector3(0f, -0.1f, 0f)
                }
            });

            SetPrivateField(rig.Adapter, "_skin", rig.Skin);
            SetPrivateField(rig.Adapter, "_spritesRoot", rig.SpritesRoot);
            SetPrivateField(rig.Adapter, "_shipSolidRenderer", rig.SolidRenderer);
            SetPrivateField(rig.Adapter, "_shipLiquidRenderer", rig.LiquidRenderer);
            SetPrivateField(rig.Adapter, "_shipHighlightRenderer", rig.HighlightRenderer);
            SetPrivateField(rig.Adapter, "_shipBackRenderer", rig.BackRenderer);
            SetPrivateField(rig.Adapter, "_shipGrabRightRenderer", rig.GrabRightRenderer);
            SetPrivateField(rig.Adapter, "_shipGrabLeftRenderer", rig.GrabLeftRenderer);
            SetPrivateField(rig.Adapter, "_coreRenderer", rig.CoreRenderer);
            SetPrivateField(rig.Adapter, "_eyeRenderer", rig.EyeRenderer);
            SetPrivateField(rig.Adapter, "_viewSilhouetteRenderer", rig.ViewRenderer);
            SetPrivateField(rig.Adapter, "_dodgeRenderer", rig.DodgeRenderer);
            SetPrivateField(rig.Adapter, "_dodgeHalfRenderer", rig.DodgeHalfRenderer);
            rig.GrabRightRenderer.enabled = false;
            rig.GrabLeftRenderer.enabled = false;
            rig.DodgeRenderer.enabled = false;
            rig.DodgeHalfRenderer.enabled = false;
            return rig;
        }

        private static void AssertFixedSpritesApplied(TestRig rig)
        {
            Assert.That(rig.BackRenderer.sprite, Is.SameAs(rig.Back));
            Assert.That(rig.GrabRightRenderer.sprite, Is.SameAs(rig.GrabHand));
            Assert.That(rig.GrabLeftRenderer.sprite, Is.SameAs(rig.GrabHand));
            Assert.That(rig.CoreRenderer.sprite, Is.SameAs(rig.Reactor));
            Assert.That(rig.EyeRenderer.sprite, Is.SameAs(rig.Eye));
            Assert.That(rig.ViewRenderer.sprite, Is.SameAs(rig.View));
            Assert.That(rig.DodgeRenderer.sprite, Is.SameAs(rig.Dodge));
            Assert.That(rig.DodgeHalfRenderer.sprite, Is.SameAs(rig.DodgeHalf));
            Assert.That(rig.HighlightRenderer.color, Is.EqualTo(Color.magenta));
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<SpriteRenderer>();
        }

        private static Sprite CreateSprite(string name)
        {
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
            sprite.name = name;
            return sprite;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            field!.SetValue(target, value);
        }

        private sealed class TestRig
        {
            public readonly List<Sprite> CreatedSprites = new List<Sprite>();
            public GameObject Root;
            public Transform SpritesRoot;
            public GGReplicaPlayerSkinSO Skin;
            public GGReplicaPlayerViewAdapter Adapter;
            public SpriteRenderer SolidRenderer;
            public SpriteRenderer LiquidRenderer;
            public SpriteRenderer HighlightRenderer;
            public SpriteRenderer BackRenderer;
            public SpriteRenderer GrabRightRenderer;
            public SpriteRenderer GrabLeftRenderer;
            public SpriteRenderer CoreRenderer;
            public SpriteRenderer EyeRenderer;
            public SpriteRenderer ViewRenderer;
            public SpriteRenderer DodgeRenderer;
            public SpriteRenderer DodgeHalfRenderer;
            public Sprite IdleSolid;
            public Sprite IdleLiquid;
            public Sprite IdleHighlight;
            public Sprite BoostSolid;
            public Sprite BoostLiquid;
            public Sprite BoostHighlight;
            public Sprite GrabSolid;
            public Sprite GrabLiquid;
            public Sprite GrabHighlight;
            public Sprite GrabHand;
            public Sprite Back;
            public Sprite Reactor;
            public Sprite Eye;
            public Sprite View;
            public Sprite Dodge;
            public Sprite DodgeHalf;

            public void Destroy()
            {
                Object.DestroyImmediate(Skin);
                Object.DestroyImmediate(Root);
                foreach (var sprite in CreatedSprites)
                {
                    Object.DestroyImmediate(sprite);
                }
            }
        }
    }
}
