using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaFeelAdapterTests
    {
        [Test]
        public void HandleBoostStarted_AppliesBoostDragAndForwardImpulse()
        {
            var rig = CreateRig();
            rig.Root.SetActive(true);

            InvokePrivate(rig.Adapter, "HandleBoostStarted");

            Assert.That(rig.Rigidbody.linearDamping, Is.EqualTo(rig.Profile.AfterBoostDrag));
            Assert.That(rig.Rigidbody.linearVelocity.y, Is.GreaterThan(0f));
            rig.Destroy();
        }

        [Test]
        public void HandleDashStarted_AppliesAfterDodgeImpulseAndImmediateDrag_WhenDurationIsZero()
        {
            var rig = CreateRig();
            SetPrivateField(rig.Profile, "_speedModAfterDodgeTime", 0f);
            rig.Root.SetActive(true);

            InvokePrivate(rig.Adapter, "HandleDashStarted", Vector2.right);

            Assert.That(rig.Rigidbody.linearVelocity.x, Is.GreaterThan(0f));
            Assert.That(rig.Rigidbody.linearDamping, Is.EqualTo(rig.Profile.AfterBoostDrag));
            rig.Destroy();
        }

        [Test]
        public void OnDisable_RestoresBaseDrag()
        {
            var rig = CreateRig();
            rig.Rigidbody.linearDamping = 1.25f;
            rig.Root.SetActive(true);
            InvokePrivate(rig.Adapter, "HandleBoostStarted");

            rig.Root.SetActive(false);

            Assert.That(rig.Rigidbody.linearDamping, Is.EqualTo(1.25f));
            rig.Destroy();
        }

        private static TestRig CreateRig()
        {
            var root = new GameObject("GGReplicaFeelAdapterTestRoot");
            root.SetActive(false);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 1.25f;

            var rig = new TestRig
            {
                Root = root,
                Rigidbody = rb,
                Profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>(),
                Adapter = root.AddComponent<GGReplicaShipFeelAdapter>()
            };

            SetPrivateField(rig.Adapter, "_profile", rig.Profile);
            SetPrivateField(rig.Adapter, "_rigidbody", rig.Rigidbody);
            return rig;
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
            method.Invoke(target, args);
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
            public Rigidbody2D Rigidbody;
            public GGReplicaShipFeelProfileSO Profile;
            public GGReplicaShipFeelAdapter Adapter;

            public void Destroy()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                UnityEngine.Object.DestroyImmediate(Profile);
            }
        }
    }
}
