using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaShipViewAdapterTests
    {
        [Test]
        public void ForceVisualState_AppliesSpritePackAndDodgeGhostVisibility()
        {
            var rig = CreateRig();
            var normalPack = CreatePack(GGReplicaVisualState.Normal, rig.NormalSolid, rig.NormalLiquid, rig.NormalHighlight, Vector3.zero);
            var dodgePack = CreatePack(GGReplicaVisualState.Dodge, rig.DodgeSolid, rig.DodgeLiquid, rig.DodgeHighlight, new Vector3(1f, 2f, 0f));
            SetPrivateField(rig.Profile, "_spritePacks", new[] { normalPack, dodgePack });
            WireAdapter(rig);

            rig.Adapter.ForceVisualState(GGReplicaVisualState.Dodge);

            Assert.That(rig.SolidRenderer.sprite, Is.SameAs(rig.DodgeSolid));
            Assert.That(rig.LiquidRenderer.sprite, Is.SameAs(rig.DodgeLiquid));
            Assert.That(rig.HighlightRenderer.sprite, Is.SameAs(rig.DodgeHighlight));
            Assert.That(rig.SolidRenderer.transform.localPosition, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(rig.LiquidRenderer.transform.localPosition, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(rig.HighlightRenderer.transform.localPosition, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(rig.DodgeGhostRenderer.enabled, Is.True);

            rig.Adapter.ForceVisualState(GGReplicaVisualState.Normal);

            Assert.That(rig.SolidRenderer.sprite, Is.SameAs(rig.NormalSolid));
            Assert.That(rig.DodgeGhostRenderer.enabled, Is.False);
            rig.Destroy();
        }

        [Test]
        public void SetFiring_WhenNotBoostingOrDashing_AppliesFirePack()
        {
            var rig = CreateRig();
            var normalPack = CreatePack(GGReplicaVisualState.Normal, rig.NormalSolid, rig.NormalLiquid, rig.NormalHighlight, Vector3.zero);
            var firePack = CreatePack(GGReplicaVisualState.Fire, rig.FireSolid, rig.FireLiquid, rig.FireHighlight, new Vector3(-1f, 0.5f, 0f));
            SetPrivateField(rig.Profile, "_spritePacks", new[] { normalPack, firePack });
            WireAdapter(rig);

            rig.Adapter.SetFiring(true);

            Assert.That(rig.SolidRenderer.sprite, Is.SameAs(rig.FireSolid));
            Assert.That(rig.LiquidRenderer.sprite, Is.SameAs(rig.FireLiquid));
            Assert.That(rig.HighlightRenderer.sprite, Is.SameAs(rig.FireHighlight));
            Assert.That(rig.SolidRenderer.transform.localPosition, Is.EqualTo(new Vector3(-1f, 0.5f, 0f)));
            Assert.That(rig.DodgeGhostRenderer.enabled, Is.False);
            rig.Destroy();
        }

        private static TestRig CreateRig()
        {
            var root = new GameObject("GGReplicaAdapterTestRoot");
            root.SetActive(false);
            var rig = new TestRig
            {
                Root = root,
                Profile = ScriptableObject.CreateInstance<GGReplicaShipVisualProfileSO>(),
                Adapter = root.AddComponent<GGReplicaShipViewAdapter>(),
                BackRenderer = CreateRenderer(root.transform, "Back"),
                CoreRenderer = CreateRenderer(root.transform, "Core"),
                SolidRenderer = CreateRenderer(root.transform, "Solid"),
                LiquidRenderer = CreateRenderer(root.transform, "Liquid"),
                HighlightRenderer = CreateRenderer(root.transform, "Highlight"),
                DodgeGhostRenderer = CreateRenderer(root.transform, "DodgeGhost"),
                NormalSolid = CreateSprite("NormalSolid"),
                NormalLiquid = CreateSprite("NormalLiquid"),
                NormalHighlight = CreateSprite("NormalHighlight"),
                DodgeSolid = CreateSprite("DodgeSolid"),
                DodgeLiquid = CreateSprite("DodgeLiquid"),
                DodgeHighlight = CreateSprite("DodgeHighlight"),
                FireSolid = CreateSprite("FireSolid"),
                FireLiquid = CreateSprite("FireLiquid"),
                FireHighlight = CreateSprite("FireHighlight")
            };

            return rig;
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<SpriteRenderer>();
        }

        private static Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(1, 1);
            texture.name = $"{name}_Texture";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            sprite.name = name;
            return sprite;
        }

        private static GGReplicaSpritePack CreatePack(
            GGReplicaVisualState state,
            Sprite solid,
            Sprite liquid,
            Sprite highlight,
            Vector3 offset)
        {
            return new GGReplicaSpritePack
            {
                State = state,
                SolidSprite = solid,
                LiquidSprite = liquid,
                HighlightSprite = highlight,
                SpritesOffset = offset
            };
        }

        private static void WireAdapter(TestRig rig)
        {
            SetPrivateField(rig.Adapter, "_profile", rig.Profile);
            SetPrivateField(rig.Adapter, "_backRenderer", rig.BackRenderer);
            SetPrivateField(rig.Adapter, "_coreRenderer", rig.CoreRenderer);
            SetPrivateField(rig.Adapter, "_solidRenderer", rig.SolidRenderer);
            SetPrivateField(rig.Adapter, "_liquidRenderer", rig.LiquidRenderer);
            SetPrivateField(rig.Adapter, "_highlightRenderer", rig.HighlightRenderer);
            SetPrivateField(rig.Adapter, "_dodgeGhostRenderer", rig.DodgeGhostRenderer);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private sealed class TestRig
        {
            public GameObject Root;
            public GGReplicaShipVisualProfileSO Profile;
            public GGReplicaShipViewAdapter Adapter;
            public SpriteRenderer BackRenderer;
            public SpriteRenderer CoreRenderer;
            public SpriteRenderer SolidRenderer;
            public SpriteRenderer LiquidRenderer;
            public SpriteRenderer HighlightRenderer;
            public SpriteRenderer DodgeGhostRenderer;
            public Sprite NormalSolid;
            public Sprite NormalLiquid;
            public Sprite NormalHighlight;
            public Sprite DodgeSolid;
            public Sprite DodgeLiquid;
            public Sprite DodgeHighlight;
            public Sprite FireSolid;
            public Sprite FireLiquid;
            public Sprite FireHighlight;

            public void Destroy()
            {
                UnityEngine.Object.DestroyImmediate(Profile);
                UnityEngine.Object.DestroyImmediate(Root);
                DestroySprite(NormalSolid);
                DestroySprite(NormalLiquid);
                DestroySprite(NormalHighlight);
                DestroySprite(DodgeSolid);
                DestroySprite(DodgeLiquid);
                DestroySprite(DodgeHighlight);
                DestroySprite(FireSolid);
                DestroySprite(FireLiquid);
                DestroySprite(FireHighlight);
            }

            private static void DestroySprite(Sprite sprite)
            {
                if (sprite == null) return;
                var texture = sprite.texture;
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
