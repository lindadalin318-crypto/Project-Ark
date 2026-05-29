using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class ShipFireVisualsTests
    {
        [Test]
        public void OnWeaponFired_LightsWeaponMountAndCoreThenResetRestoresBaseline()
        {
            var root = new GameObject("ShipFireVisualsTestRoot");
            var weaponMount = CreateRenderer(root.transform, "WeaponMount", new Color(0.2f, 0.3f, 0.4f, 1f));
            var core = CreateRenderer(root.transform, "Core", new Color(0.1f, 0.2f, 0.3f, 0.6f));
            var visuals = root.AddComponent<ShipFireVisuals>();

            SetPrivateField(visuals, "_weaponMountRenderer", weaponMount);
            SetPrivateField(visuals, "_coreRenderer", core);
            visuals.Initialize(weaponMount.color, core.color);

            visuals.OnWeaponFired(Vector2.zero, Vector2.right);

            Assert.That(weaponMount.color.r, Is.GreaterThan(0.2f));
            Assert.That(weaponMount.color.g, Is.GreaterThan(0.3f));
            Assert.That(weaponMount.color.b, Is.GreaterThan(0.4f));
            Assert.That(core.color.a, Is.GreaterThan(0.6f));

            visuals.ResetState();

            Assert.That(weaponMount.color, Is.EqualTo(new Color(0.2f, 0.3f, 0.4f, 1f)));
            Assert.That(core.color, Is.EqualTo(new Color(0.1f, 0.2f, 0.3f, 0.6f)));

            Object.DestroyImmediate(root);
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.color = color;
            return renderer;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
