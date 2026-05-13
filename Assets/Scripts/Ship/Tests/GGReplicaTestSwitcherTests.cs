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
            var switcher = root.AddComponent<GGReplicaTestSwitcher>();
            SetPrivateField(switcher, "_liveShip", live);
            SetPrivateField(switcher, "_replicaShip", replica);

            switcher.SetReplicaActive(true);

            Assert.That(live.activeSelf, Is.False);
            Assert.That(replica.activeSelf, Is.True);

            switcher.SetReplicaActive(false);

            Assert.That(live.activeSelf, Is.True);
            Assert.That(replica.activeSelf, Is.False);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(live);
            Object.DestroyImmediate(replica);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            field.SetValue(target, value);
        }
    }
}
