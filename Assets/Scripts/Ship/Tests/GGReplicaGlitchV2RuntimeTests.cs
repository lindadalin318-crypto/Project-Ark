using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaGlitchV2RuntimeTests
    {
        [Test]
        public void Motor_BoostAndDodge_ChangeVelocityAndStateWithoutButtonDrivenSpriteSwap()
        {
            var root = new GameObject("GGReplicaGlitchV2RuntimeRig");
            try
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                var view = root.AddComponent<GGReplicaGlitchView>();
                var motor = root.AddComponent<GGReplicaGlitchMotor>();
                SetPrivateField(motor, "_body", rb);
                SetPrivateField(motor, "_view", view);

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, false, false, false, false, false), 0.1f);
                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.Move));
                Assert.That(rb.linearVelocity.x, Is.GreaterThan(0f));

                float moveSpeed = rb.linearVelocity.magnitude;
                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, true, false, false, false, false), 0.1f);
                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.BoostHold));
                Assert.That(rb.linearVelocity.magnitude, Is.GreaterThan(moveSpeed));

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.up, false, true, false, false, false), 0.1f);
                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.DodgeBurst));
                Assert.That(rb.linearVelocity.y, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Motor_UsesGGFeelProfileBoostImpulseAndDrag()
        {
            var root = new GameObject("GGReplicaGlitchV2ProfileBoostRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();
            try
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.linearDamping = 4f;
                var view = root.AddComponent<GGReplicaGlitchView>();
                var motor = root.AddComponent<GGReplicaGlitchMotor>();
                SetPrivateField(motor, "_body", rb);
                SetPrivateField(motor, "_view", view);
                SetPrivateField(motor, "_feelProfile", profile);

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, false, false, false, false, false), 0.1f);
                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, true, false, false, false, false), 0.1f);

                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.BoostHold));
                Assert.That(rb.linearVelocity.x, Is.EqualTo(11.2f).Within(0.01f), "GG Glitch boost is sustain speed plus the original start impulse, not an arbitrary high multiplier.");
                Assert.That(rb.linearDamping, Is.EqualTo(profile.AfterBoostDrag).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Motor_UsesGGDodgeForceAndOriginalMinimumDodgeTime()
        {
            var root = new GameObject("GGReplicaGlitchV2ProfileDodgeRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();
            try
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                var view = root.AddComponent<GGReplicaGlitchView>();
                var motor = root.AddComponent<GGReplicaGlitchMotor>();
                SetPrivateField(motor, "_body", rb);
                SetPrivateField(motor, "_view", view);
                SetPrivateField(motor, "_feelProfile", profile);

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.up, false, true, false, false, false), 0.1f);

                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.DodgeBurst));
                Assert.That(rb.linearVelocity.y, Is.EqualTo(profile.DodgeForce).Within(0.01f));

                for (int i = 0; i < 8; i++)
                {
                    InvokePrivate(motor, "FixedUpdate");
                }

                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.DodgeBurst), "Original Glitch dodge keeps the burst state for about 0.225s, longer than the previous 0.16s placeholder.");
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_ApplyState_TogglesOriginalModuleRootsAndTrailStack()
        {
            var root = new GameObject("GGReplicaGlitchV2ViewRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var visualRoot = new GameObject("GGGlitchVisualRoot");
                var boostRoot = new GameObject("BoostModule");
                var trailsRoot = new GameObject("LQTrailsContainer");
                var grabRoot = new GameObject("GrabModule");
                var healRoot = new GameObject("HealModule");
                visualRoot.transform.SetParent(root.transform, false);
                boostRoot.transform.SetParent(visualRoot.transform, false);
                trailsRoot.transform.SetParent(visualRoot.transform, false);
                grabRoot.transform.SetParent(visualRoot.transform, false);
                healRoot.transform.SetParent(visualRoot.transform, false);
                var trailA = new GameObject("startrails").AddComponent<TrailRenderer>();
                var trailB = new GameObject("startrails_long").AddComponent<TrailRenderer>();
                trailA.transform.SetParent(trailsRoot.transform, false);
                trailB.transform.SetParent(trailsRoot.transform, false);
                var particle = new GameObject("vfx_boost_trail_loop_enhanced").AddComponent<ParticleSystem>();
                particle.transform.SetParent(boostRoot.transform, false);

                SetPrivateField(view, "_visualRoot", visualRoot.transform);
                SetPrivateField(view, "_boostModuleRoot", boostRoot);
                SetPrivateField(view, "_lqTrailsContainer", trailsRoot);
                SetPrivateField(view, "_grabModuleRoot", grabRoot);
                SetPrivateField(view, "_healModuleRoot", healRoot);
                SetPrivateField(view, "_boostParticles", new[] { particle });
                SetPrivateField(view, "_trailRenderers", new[] { trailA, trailB });

                view.ApplyState(GGReplicaGlitchState.Idle);
                Assert.That(boostRoot.activeSelf, Is.False);
                Assert.That(trailA.emitting, Is.False);

                view.ApplyState(GGReplicaGlitchState.BoostHold);
                Assert.That(boostRoot.activeSelf, Is.True);
                Assert.That(trailA.emitting, Is.True);
                Assert.That(trailB.emitting, Is.True);

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                Assert.That(grabRoot.activeSelf, Is.True);
                Assert.That(boostRoot.activeSelf, Is.False);

                view.ApplyState(GGReplicaGlitchState.Heal);
                Assert.That(healRoot.activeSelf, Is.True);
                Assert.That(grabRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName} on {target.GetType().Name}.");
            method!.Invoke(target, null);
        }
    }
}
