using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaVfxAdapterTests
    {
        [Test]
        public void BoostVfxAdapter_OnBoostEvents_StartsAndStopsLoopClip()
        {
            var rig = CreateBoostRig();
            InvokePrivate(rig.BoostAdapter, "OnEnable");

            RaiseEvent(rig.Boost, "OnBoostStarted");

            Assert.That(rig.AudioSource.clip, Is.SameAs(rig.BoostLoopClip));
            Assert.That(rig.AudioSource.loop, Is.True);

            RaiseEvent(rig.Boost, "OnBoostEnded");

            Assert.That(rig.AudioSource.loop, Is.False);
            Assert.That(rig.AudioSource.clip, Is.Null);
            InvokePrivate(rig.BoostAdapter, "OnDisable");
            rig.Destroy();
        }

        [Test]
        public void DashVfxAdapter_OnDashStarted_RecordsDodgeClipForDebug()
        {
            var rig = CreateDashRig();
            InvokePrivate(rig.DashAdapter, "OnEnable");

            RaiseEvent(rig.Dash, "OnDashStarted", Vector2.right);

            Assert.That(GetPrivateField<AudioClip>(rig.DashAdapter, "_lastPlayedClip"), Is.SameAs(rig.DodgeClip));
            InvokePrivate(rig.DashAdapter, "OnDisable");
            rig.Destroy();
        }

        private static BoostRig CreateBoostRig()
        {
            var root = new GameObject("GGReplicaBoostVfxAdapterTestRoot");
            root.SetActive(false);
            var rig = new BoostRig
            {
                Root = root,
                Profile = ScriptableObject.CreateInstance<GGReplicaShipVisualProfileSO>(),
                AudioSource = root.AddComponent<AudioSource>(),
                Boost = root.AddComponent<ShipBoost>(),
                BoostAdapter = root.AddComponent<GGReplicaBoostVfxAdapter>(),
                BoostIgniteClip = AudioClip.Create("BoostIgnite", 32, 1, 44100, false),
                BoostLoopClip = AudioClip.Create("BoostLoop", 32, 1, 44100, false)
            };

            SetPrivateField(rig.Profile, "_boostIgniteClip", rig.BoostIgniteClip);
            SetPrivateField(rig.Profile, "_boostLoopClip", rig.BoostLoopClip);
            SetPrivateField(rig.BoostAdapter, "_profile", rig.Profile);
            SetPrivateField(rig.BoostAdapter, "_audioSource", rig.AudioSource);
            SetPrivateField(rig.BoostAdapter, "_boost", rig.Boost);
            return rig;
        }

        private static DashRig CreateDashRig()
        {
            var root = new GameObject("GGReplicaDashVfxAdapterTestRoot");
            root.SetActive(false);
            var rig = new DashRig
            {
                Root = root,
                Profile = ScriptableObject.CreateInstance<GGReplicaShipVisualProfileSO>(),
                AudioSource = root.AddComponent<AudioSource>(),
                Dash = root.AddComponent<ShipDash>(),
                DashAdapter = root.AddComponent<GGReplicaDashVfxAdapter>(),
                DodgeClip = AudioClip.Create("Dodge", 32, 1, 44100, false)
            };

            SetPrivateField(rig.Profile, "_dodgeClip", rig.DodgeClip);
            SetPrivateField(rig.DashAdapter, "_profile", rig.Profile);
            SetPrivateField(rig.DashAdapter, "_audioSource", rig.AudioSource);
            SetPrivateField(rig.DashAdapter, "_dash", rig.Dash);
            return rig;
        }

        private static void RaiseEvent(object target, string eventName, params object[] args)
        {
            var field = target.GetType().GetField(eventName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing event backing field {eventName}.");
            var callback = field.GetValue(target) as Delegate;
            Assert.That(callback, Is.Not.Null, $"Event {eventName} has no subscribers.");
            callback.DynamicInvoke(args);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
            method.Invoke(target, Array.Empty<object>());
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            return field.GetValue(target) as T;
        }

        private sealed class BoostRig
        {
            public GameObject Root;
            public GGReplicaShipVisualProfileSO Profile;
            public AudioSource AudioSource;
            public ShipBoost Boost;
            public GGReplicaBoostVfxAdapter BoostAdapter;
            public AudioClip BoostIgniteClip;
            public AudioClip BoostLoopClip;

            public void Destroy()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                UnityEngine.Object.DestroyImmediate(Profile);
                UnityEngine.Object.DestroyImmediate(BoostIgniteClip);
                UnityEngine.Object.DestroyImmediate(BoostLoopClip);
            }
        }

        private sealed class DashRig
        {
            public GameObject Root;
            public GGReplicaShipVisualProfileSO Profile;
            public AudioSource AudioSource;
            public ShipDash Dash;
            public GGReplicaDashVfxAdapter DashAdapter;
            public AudioClip DodgeClip;

            public void Destroy()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                UnityEngine.Object.DestroyImmediate(Profile);
                UnityEngine.Object.DestroyImmediate(DodgeClip);
            }
        }
    }
}
