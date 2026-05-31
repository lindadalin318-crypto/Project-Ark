using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class ShipVisualJuiceTests
    {
        [Test]
        public void SelectLeanSprite_StrongPositiveLateralInputUsesStrongRightFrame()
        {
            Sprite normal = CreateSprite("Normal");
            Sprite[] left = { CreateSprite("LeftLight"), CreateSprite("LeftMedium"), CreateSprite("LeftStrong") };
            Sprite[] right = { CreateSprite("RightLight"), CreateSprite("RightMedium"), CreateSprite("RightStrong") };

            MethodInfo method = typeof(ShipVisualJuice).GetMethod(
                "SelectLeanSprite",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "ShipVisualJuice should expose an internal lean sprite selector for runtime sprite-swap validation.");

            var selected = (Sprite)method.Invoke(null, new object[] { normal, left, right, 0.9f, 0.15f });

            Assert.That(selected, Is.SameAs(right[2]));

            Object.DestroyImmediate(normal.texture);
            Object.DestroyImmediate(normal);
            foreach (Sprite sprite in left)
            {
                Object.DestroyImmediate(sprite.texture);
                Object.DestroyImmediate(sprite);
            }
            foreach (Sprite sprite in right)
            {
                Object.DestroyImmediate(sprite.texture);
                Object.DestroyImmediate(sprite);
            }
        }

        private static Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = name;
            return sprite;
        }
    }
}
