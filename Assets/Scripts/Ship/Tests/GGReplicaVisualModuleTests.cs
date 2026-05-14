using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaVisualModuleTests
    {
        [Test]
        public void CoreModule_DodgeState_ShowsDodgeAndHalfSilhouettes()
        {
            var root = new GameObject("CoreModuleTestRoot");
            try
            {
                var module = root.AddComponent<GGReplicaCoreVisualModule>();
                var core = CreateRenderer(root.transform, "Core");
                var dodge = CreateRenderer(root.transform, "Dodge");
                var dodgeHalf = CreateRenderer(root.transform, "DodgeHalf");
                var dodgeColor = new Color(0.6f, 0.2f, 1f, 0.8f);
                SetPrivateField(module, "_coreRenderer", core);
                SetPrivateField(module, "_coreTransform", core.transform);
                SetPrivateField(module, "_dodgeRenderer", dodge);
                SetPrivateField(module, "_dodgeHalfRenderer", dodgeHalf);
                SetPrivateField(module, "_dodgeCoreScale", 1.35f);
                SetPrivateField(module, "_dodgeCoreColor", dodgeColor);

                module.ApplyState(GGReplicaViewState.Dodge);

                Assert.That(dodge.enabled, Is.True);
                Assert.That(dodgeHalf.enabled, Is.True);
                Assert.That(core.transform.localScale, Is.EqualTo(Vector3.one * 1.35f));
                Assert.That(core.color, Is.EqualTo(dodgeColor));

                module.ApplyState(GGReplicaViewState.Idle);
                Assert.That(dodge.enabled, Is.False);
                Assert.That(dodgeHalf.enabled, Is.False);
                Assert.That(core.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(core.color, Is.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BoostModule_BoostState_ShowsBoostTrailRoot()
        {
            var root = new GameObject("BoostModuleTestRoot");
            try
            {
                var module = root.AddComponent<GGReplicaBoostVisualModule>();
                var boostVisualRoot = new GameObject("BoostVisualRoot");
                boostVisualRoot.transform.SetParent(root.transform, false);
                boostVisualRoot.SetActive(false);
                SetPrivateField(module, "_boostVisualRoot", boostVisualRoot);

                module.ApplyState(GGReplicaViewState.Boost);
                Assert.That(boostVisualRoot.activeSelf, Is.True);

                module.ApplyState(GGReplicaViewState.Idle);
                Assert.That(boostVisualRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShapeModule_GrabState_ShowsGrabHandsOnlyForGrab()
        {
            var root = new GameObject("ShapeModuleTestRoot");
            try
            {
                var module = root.AddComponent<GGReplicaShapeVisualModule>();
                var grabRight = CreateRenderer(root.transform, "GrabRight");
                var grabLeft = CreateRenderer(root.transform, "GrabLeft");
                grabRight.enabled = false;
                grabLeft.enabled = false;
                SetPrivateField(module, "_grabRightRenderer", grabRight);
                SetPrivateField(module, "_grabLeftRenderer", grabLeft);

                module.ApplyState(GGReplicaViewState.Grab);
                Assert.That(grabRight.enabled, Is.True);
                Assert.That(grabLeft.enabled, Is.True);

                module.ApplyState(GGReplicaViewState.Idle);
                Assert.That(grabRight.enabled, Is.False);
                Assert.That(grabLeft.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MaterialModule_ApplyState_UsesPropertyBlocksWithoutMutatingSharedMaterials()
        {
            var root = new GameObject("MaterialModuleTestRoot");
            var material = new Material(Shader.Find("ProjectArk/GGReplica/PlayerShipHighlight"));
            try
            {
                Assert.That(material.shader, Is.Not.Null, "GGReplica highlight shader must be available for material module tests.");
                material.SetFloat("_Intensity", 8f);
                material.SetColor("_Tint", new Color(0.54509807f, 0.09019608f, 1f, 1f));

                var module = root.AddComponent<GGReplicaMaterialVisualModule>();
                var highlight = CreateRenderer(root.transform, "Highlight");
                var view = CreateRenderer(root.transform, "View");
                var core = CreateRenderer(root.transform, "Core");
                var dodge = CreateRenderer(root.transform, "Dodge");
                var grabRight = CreateRenderer(root.transform, "GrabRight");
                var grabLeft = CreateRenderer(root.transform, "GrabLeft");
                var trail = new GameObject("PlayerLQTrail").AddComponent<TrailRenderer>();
                trail.transform.SetParent(root.transform, false);
                highlight.sharedMaterial = material;

                SetPrivateField(module, "_highlightRenderer", highlight);
                SetPrivateField(module, "_viewRenderer", view);
                SetPrivateField(module, "_coreRenderer", core);
                SetPrivateField(module, "_dodgeRenderer", dodge);
                SetPrivateField(module, "_grabRightRenderer", grabRight);
                SetPrivateField(module, "_grabLeftRenderer", grabLeft);
                SetPrivateField(module, "_playerLqTrail", trail);

                module.ApplyState(GGReplicaViewState.Idle);
                AssertFloatProperty(highlight, "_Intensity", 8f);
                AssertFloatProperty(highlight, "_BoostAmount", 0f);
                AssertFloatProperty(highlight, "_HealAmount", 0f);
                AssertFloatProperty(view, "_SchemeAlpha", 0.45f);
                AssertFloatProperty(trail, "_TrailIntensity", 0f);
                Assert.That(trail.emitting, Is.False);

                module.ApplyState(GGReplicaViewState.Boost);
                AssertFloatProperty(highlight, "_Intensity", 12f);
                AssertFloatProperty(highlight, "_BoostAmount", 1f);
                AssertFloatProperty(view, "_GlitchStrength", 0.55f);
                AssertFloatProperty(trail, "_TrailIntensity", 1f);
                Assert.That(trail.emitting, Is.True);
                Assert.That(material.GetFloat("_Intensity"), Is.EqualTo(8f).Within(0.001f), "Runtime state must not mutate shared material assets.");

                module.ApplyState(GGReplicaViewState.Dodge);
                AssertFloatProperty(highlight, "_Intensity", 10f);
                AssertFloatProperty(dodge, "_Pulse", 1f);
                AssertFloatProperty(core, "_Pulse", 1f);
                AssertFloatProperty(view, "_GlitchStrength", 0.7f);
                Assert.That(trail.emitting, Is.False);

                module.ApplyState(GGReplicaViewState.Grab);
                AssertFloatProperty(grabRight, "_GrabEmphasis", 1f);
                AssertFloatProperty(grabLeft, "_GrabEmphasis", 1f);

                module.ApplyState(GGReplicaViewState.Heal);
                AssertFloatProperty(highlight, "_HealAmount", 1f);
                AssertColorProperty(highlight, "_Tint", new Color(0.35f, 1f, 0.85f, 1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PlayerViewAdapter_ChangeViewState_NotifiesBoostModule()
        {
            var root = new GameObject("AdapterModuleNotifyTestRoot");
            var skin = ScriptableObject.CreateInstance<GGReplicaPlayerSkinSO>();
            var boostSolid = CreateSprite("BoostSolid");
            var boostLiquid = CreateSprite("BoostLiquid");
            var boostHighlight = CreateSprite("BoostHighlight");
            try
            {
                var adapter = root.AddComponent<GGReplicaPlayerViewAdapter>();
                var boostModule = root.AddComponent<GGReplicaBoostVisualModule>();
                var spritesRoot = new GameObject("SpritesRoot").transform;
                spritesRoot.SetParent(root.transform, false);
                var boostVisualRoot = new GameObject("BoostVisualRoot");
                boostVisualRoot.transform.SetParent(root.transform, false);
                boostVisualRoot.SetActive(false);

                SetPrivateField(skin, "_stateToSpritesTable", new[]
                {
                    new GGReplicaViewSpritePack
                    {
                        State = GGReplicaViewState.Boost,
                        SolidSprite = boostSolid,
                        LiquidSprite = boostLiquid,
                        HighlightSprite = boostHighlight
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
                SetPrivateField(adapter, "_boostModule", boostModule);
                SetPrivateField(boostModule, "_boostVisualRoot", boostVisualRoot);

                adapter.ChangeViewState(GGReplicaViewState.Boost);

                Assert.That(boostVisualRoot.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(skin);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(boostSolid);
                Object.DestroyImmediate(boostLiquid);
                Object.DestroyImmediate(boostHighlight);
            }
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

        private static void AssertFloatProperty(Renderer renderer, string propertyName, float expected)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetFloat(propertyName), Is.EqualTo(expected).Within(0.001f));
        }

        private static void AssertColorProperty(Renderer renderer, string propertyName, Color expected)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var actual = block.GetColor(propertyName);
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }
    }
}
