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
        public void Motor_AimDirection_RotatesShipTowardAimIndependentlyFromMove()
        {
            var root = new GameObject("GGReplicaGlitchV2AimRig");
            try
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                var view = root.AddComponent<GGReplicaGlitchView>();
                var motor = root.AddComponent<GGReplicaGlitchMotor>();
                SetPrivateField(motor, "_body", rb);
                SetPrivateField(motor, "_view", view);

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.left, false, false, false, false, false, Vector2.right), 0.1f);
                InvokePrivate(motor, "FixedUpdate");

                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.Move));
                Assert.That(rb.linearVelocity.x, Is.LessThan(0f), "Movement should remain world-space WASD, independent from aim.");
                Assert.That(Mathf.DeltaAngle(0f, rb.rotation), Is.LessThan(0f), "GG mouse mode should rotate the ship/sprites toward aim direction instead of leaving the PlayerView static.");
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
        public void Motor_GrabCancelsBoostDragWhenBothAreHeld()
        {
            var root = new GameObject("GGReplicaGlitchV2BoostGrabPriorityRig");
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

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, true, false, false, false, false), 0.1f);
                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.BoostHold));
                Assert.That(rb.linearDamping, Is.EqualTo(profile.AfterBoostDrag).Within(0.001f));

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, true, false, true, false, false), 0.1f);

                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.GrabHold), "Grab should soft-cancel Boost before the sustain loop can visually dominate the hold state.");
                Assert.That(rb.linearDamping, Is.EqualTo(4f).Within(0.001f), "Leaving Boost for Grab must restore base drag; otherwise quick Boost→Grab keeps the slippery boost tail.");
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Motor_DodgeWindow_ResumesHeldGrabAfterInterrupt()
        {
            var root = new GameObject("GGReplicaGlitchV2DodgeResumeGrabRig");
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

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, false, false, true, false, false), 0.1f);
                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.GrabHold));

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.up, false, true, true, false, false), 0.1f);
                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.DodgeBurst));

                for (int i = 0; i < 14; i++)
                {
                    motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.up, false, false, true, false, false), 0.02f);
                    InvokePrivate(motor, "FixedUpdate");
                }

                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.GrabHold), "Held Grab should resume after the Dodge lockout instead of being eaten by the Dodge window.");
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Motor_DodgeWindow_ResumesHeldBoostAfterInterrupt()
        {
            var root = new GameObject("GGReplicaGlitchV2DodgeResumeBoostRig");
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

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, true, false, false, false, false), 0.1f);
                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.BoostHold));

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.up, true, true, false, false, false), 0.1f);
                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.DodgeBurst));

                for (int i = 0; i < 14; i++)
                {
                    motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.up, true, false, false, false, false), 0.02f);
                    InvokePrivate(motor, "FixedUpdate");
                }

                Assert.That(motor.CurrentState, Is.EqualTo(GGReplicaGlitchState.BoostHold), "Held Boost should resume after Dodge so Boost→Dodge→Boost chains keep the original rhythm.");
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
        public void Motor_DodgePressedDuringDodge_RestartsVisualBurstWindow()
        {
            var root = new GameObject("GGReplicaGlitchV2DodgeRetriggerRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();
            try
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                var view = root.AddComponent<GGReplicaGlitchView>();
                var motor = root.AddComponent<GGReplicaGlitchMotor>();
                var dodgeRoot = new GameObject("DodgeModule");
                dodgeRoot.transform.SetParent(root.transform, false);
                var ghost = new GameObject("Dodge_Sprite (used for old outline trail)").AddComponent<SpriteRenderer>();
                ghost.transform.SetParent(dodgeRoot.transform, false);
                var burst = new GameObject("ps_dodge_shell").AddComponent<ParticleSystem>();
                burst.transform.SetParent(dodgeRoot.transform, false);
                SetPrivateField(view, "_feelProfile", profile);
                SetPrivateField(view, "_dodgeModuleRoot", dodgeRoot);
                SetPrivateField(view, "_dodgeGhostRenderer", ghost);
                SetPrivateField(view, "_dodgeBurstParticles", new[] { burst });
                SetPrivateField(motor, "_body", rb);
                SetPrivateField(motor, "_view", view);
                SetPrivateField(motor, "_feelProfile", profile);

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.up, false, true, false, false, false), 0.1f);
                float fullAlpha = ghost.color.a;
                burst.Simulate(0.1f, true, false, false);
                float advancedBurstTime = burst.time;
                InvokePrivate(view, "TickVisuals", profile.DodgeStateDuration * 0.75f);
                float fadedAlpha = ghost.color.a;
                Assert.That(fadedAlpha, Is.LessThan(fullAlpha), "The first Dodge visual window should have started fading before the second press.");
                Assert.That(advancedBurstTime, Is.GreaterThan(0.05f), "Test setup should advance the first Dodge burst before re-triggering.");

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, false, true, false, false, false), 0.1f);

                Assert.That(rb.linearVelocity.x, Is.EqualTo(profile.DodgeForce).Within(0.01f));
                Assert.That(ghost.color.a, Is.GreaterThan(0.6f), "Pressing Dodge during Dodge should restart the original burst window instead of only refreshing physics.");
                Assert.That(burst.isPlaying, Is.True);
                Assert.That(burst.time, Is.LessThan(advancedBurstTime * 0.5f), "The Dodge burst particle should restart from the beginning, not continue the old emission.");
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Motor_StateTransitions_NotifyV2AudioFeedback()
        {
            var root = new GameObject("GGReplicaGlitchV2AudioRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipVisualProfileSO>();
            var boostIgnite = AudioClip.Create("SND_PLAYER_BOOST_IGNITE", 32, 1, 44100, false);
            var boostLoop = AudioClip.Create("SND_PLAYER_BOOST", 32, 1, 44100, false);
            var fire = AudioClip.Create("PLAYER_NORMAL_SHOT", 32, 1, 44100, false);
            var dodge = AudioClip.Create("PLAYER_DODGE", 32, 1, 44100, false);
            var heal = AudioClip.Create("PlayerHealingProgress", 32, 1, 44100, false);
            try
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                var view = root.AddComponent<GGReplicaGlitchView>();
                var audioSource = root.AddComponent<AudioSource>();
                var audioFeedback = root.AddComponent<GGReplicaGlitchAudioFeedback>();
                var motor = root.AddComponent<GGReplicaGlitchMotor>();
                SetPrivateField(profile, "_boostIgniteClip", boostIgnite);
                SetPrivateField(profile, "_boostLoopClip", boostLoop);
                SetPrivateField(profile, "_fireClip", fire);
                SetPrivateField(profile, "_dodgeClip", dodge);
                SetPrivateField(profile, "_healClip", heal);
                Assert.That(profile.HealClip, Is.SameAs(heal));
                SetPrivateField(audioFeedback, "_profile", profile);
                SetPrivateField(audioFeedback, "_audioSource", audioSource);
                SetPrivateField(motor, "_body", rb);
                SetPrivateField(motor, "_view", view);
                SetPrivateField(motor, "_audioFeedback", audioFeedback);

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, true, false, false, false, false), 0.1f);

                Assert.That(GetPrivateField<AudioClip>(audioFeedback, "_lastOneShotClip"), Is.SameAs(boostIgnite));
                Assert.That(audioSource.clip, Is.SameAs(boostLoop));
                Assert.That(audioSource.loop, Is.True);

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.right, false, false, false, false, true), 0.1f);

                Assert.That(GetPrivateField<AudioClip>(audioFeedback, "_lastOneShotClip"), Is.SameAs(fire));
                Assert.That(audioSource.loop, Is.False);
                Assert.That(audioSource.clip, Is.Null);

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.up, false, true, false, false, false), 0.1f);

                Assert.That(GetPrivateField<AudioClip>(audioFeedback, "_lastOneShotClip"), Is.SameAs(dodge));
                Assert.That(audioSource.loop, Is.False);
                Assert.That(audioSource.clip, Is.Null);

                for (int i = 0; i < 20; i++)
                {
                    InvokePrivate(motor, "FixedUpdate");
                }

                motor.ApplyInput(new GGReplicaGlitchInputFrame(Vector2.zero, false, false, false, true, false), 0.1f);

                Assert.That(GetPrivateField<AudioClip>(audioFeedback, "_lastOneShotClip"), Is.SameAs(heal));
                Assert.That(audioSource.loop, Is.False);
                Assert.That(audioSource.clip, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(boostIgnite);
                Object.DestroyImmediate(boostLoop);
                Object.DestroyImmediate(fire);
                Object.DestroyImmediate(dodge);
                Object.DestroyImmediate(heal);
            }
        }

        [Test]
        public void View_StateChanges_ApplyOriginalPlayerSkinSpritePacks()
        {
            var root = new GameObject("GGReplicaGlitchV2SkinRig");
            var skin = ScriptableObject.CreateInstance<GGReplicaPlayerSkinSO>();
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var bodyRoot = new GameObject("BodyLayers").transform;
                bodyRoot.SetParent(root.transform, false);
                var solid = new GameObject("Ship_Sprite_Solid").AddComponent<SpriteRenderer>();
                var liquid = new GameObject("Ship_Sprite_Liquid").AddComponent<SpriteRenderer>();
                var highlight = new GameObject("Ship_Sprite_HL").AddComponent<SpriteRenderer>();
                solid.transform.SetParent(bodyRoot, false);
                liquid.transform.SetParent(bodyRoot, false);
                highlight.transform.SetParent(bodyRoot, false);

                var idleSolid = CreateSprite("Movement_10");
                var idleLiquid = CreateSprite("Movement_3");
                var idleHighlight = CreateSprite("Movement_21");
                var boostSolid = CreateSprite("Boost_2");
                var boostLiquid = CreateSprite("Boost_16");
                var boostHighlight = CreateSprite("Boost_8");
                var fireSolid = CreateSprite("Primary_4");
                var fireLiquid = CreateSprite("Primary");
                var fireHighlight = CreateSprite("Primary_6");
                var healSolid = CreateSprite("Healing_0");
                var healLiquid = CreateSprite("Healing");
                var healHighlight = CreateSprite("vfx_dot_001");
                var grabSolid = CreateSprite("GrabGun_Base_9");
                var grabHighlight = CreateSprite("GrabGun_Base_8");
                SetPrivateField(skin, "_stateToSpritesTable", new[]
                {
                    new GGReplicaViewSpritePack { State = GGReplicaViewState.Idle, SolidSprite = idleSolid, LiquidSprite = idleLiquid, HighlightSprite = idleHighlight, SpritesOffset = Vector3.zero },
                    new GGReplicaViewSpritePack { State = GGReplicaViewState.Boost, SolidSprite = boostSolid, LiquidSprite = boostLiquid, HighlightSprite = boostHighlight, SpritesOffset = Vector3.zero },
                    new GGReplicaViewSpritePack { State = GGReplicaViewState.Dodge, SpritesOffset = Vector3.zero },
                    new GGReplicaViewSpritePack { State = GGReplicaViewState.Fire, SolidSprite = fireSolid, LiquidSprite = fireLiquid, HighlightSprite = fireHighlight, SpritesOffset = Vector3.zero },
                    new GGReplicaViewSpritePack { State = GGReplicaViewState.Heal, SolidSprite = healSolid, LiquidSprite = healLiquid, HighlightSprite = healHighlight, SpritesOffset = Vector3.zero },
                    new GGReplicaViewSpritePack { State = GGReplicaViewState.Grab, SolidSprite = grabSolid, LiquidSprite = grabSolid, HighlightSprite = grabHighlight, SpritesOffset = new Vector3(0f, -0.1f, 0f) }
                });
                SetPrivateField(view, "_playerSkin", skin);
                SetPrivateField(view, "_bodyLayersRoot", bodyRoot);
                SetPrivateField(view, "_solidRenderer", solid);
                SetPrivateField(view, "_liquidRenderer", liquid);
                SetPrivateField(view, "_highlightRenderer", highlight);
                SetPrivateField(view, "_bodyRenderers", new[] { solid, liquid, highlight });

                view.ApplyState(GGReplicaGlitchState.Idle);
                Assert.That(solid.sprite, Is.SameAs(idleSolid));
                Assert.That(liquid.sprite, Is.SameAs(idleLiquid));
                Assert.That(highlight.sprite, Is.SameAs(idleHighlight));

                view.ApplyState(GGReplicaGlitchState.BoostHold);
                Assert.That(solid.sprite, Is.SameAs(boostSolid));
                Assert.That(liquid.sprite, Is.SameAs(boostLiquid));
                Assert.That(highlight.sprite, Is.SameAs(boostHighlight));

                view.ApplyState(GGReplicaGlitchState.DodgeBurst);
                Assert.That(solid.sprite, Is.SameAs(boostSolid), "Original Dodge pack has null body sprites, so V2 must preserve the previous body pack while Dodge modules play.");

                view.ApplyState(GGReplicaGlitchState.FireAim);
                Assert.That(solid.sprite, Is.SameAs(fireSolid));
                Assert.That(liquid.sprite, Is.SameAs(fireLiquid));
                Assert.That(highlight.sprite, Is.SameAs(fireHighlight));

                view.ApplyState(GGReplicaGlitchState.Heal);
                Assert.That(solid.sprite, Is.SameAs(healSolid));
                Assert.That(liquid.sprite, Is.SameAs(healLiquid));
                Assert.That(highlight.sprite, Is.SameAs(healHighlight));

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                Assert.That(solid.sprite, Is.SameAs(grabSolid));
                Assert.That(liquid.sprite, Is.SameAs(grabSolid));
                Assert.That(highlight.sprite, Is.SameAs(grabHighlight));
                Assert.That(bodyRoot.localPosition, Is.EqualTo(new Vector3(0f, -0.1f, 0f)));
            }
            finally
            {
                Object.DestroyImmediate(skin);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_BoostIgnition_PlaysBurstOnlyDuringIgniteWindowThenKeepsSustain()
        {
            var root = new GameObject("GGReplicaGlitchV2BoostTimingRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var boostRoot = new GameObject("BoostModule");
                boostRoot.transform.SetParent(root.transform, false);
                var trailsRoot = new GameObject("LQTrailsContainer");
                trailsRoot.transform.SetParent(root.transform, false);
                var sustain = new GameObject("vfx_boost_trail_loop_enhanced").AddComponent<ParticleSystem>();
                sustain.transform.SetParent(boostRoot.transform, false);
                var burst = new GameObject("vfx_boost_trail_burst_enhanced").AddComponent<ParticleSystem>();
                burst.transform.SetParent(boostRoot.transform, false);

                SetPrivateField(view, "_feelProfile", profile);
                SetPrivateField(view, "_boostModuleRoot", boostRoot);
                SetPrivateField(view, "_lqTrailsContainer", trailsRoot);
                SetPrivateField(view, "_boostParticles", new[] { sustain });
                SetPrivateField(view, "_burstParticles", new[] { burst });

                view.ApplyState(GGReplicaGlitchState.BoostHold);

                Assert.That(sustain.isPlaying, Is.True);
                Assert.That(burst.isPlaying, Is.True, "Boost should have an ignition burst instead of only a sustain loop.");

                InvokePrivate(view, "TickVisuals", profile.BoostIgniteDuration + 0.01f);

                Assert.That(sustain.isPlaying, Is.True, "Sustain particles should keep playing while Boost is held.");
                Assert.That(burst.isPlaying, Is.False, "Ignition burst should stop after the original boost ignite window.");
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_BoostExit_StopsEmittingWithoutClearingLiveParticles()
        {
            var root = new GameObject("GGReplicaGlitchV2BoostStopEmittingRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var boostRoot = new GameObject("BoostModule");
                boostRoot.transform.SetParent(root.transform, false);
                var sustain = new GameObject("vfx_boost_trail_loop_enhanced").AddComponent<ParticleSystem>();
                sustain.transform.SetParent(boostRoot.transform, false);
                var main = sustain.main;
                main.startLifetime = 1f;
                main.startSpeed = 0f;
                var emission = sustain.emission;
                emission.rateOverTime = 80f;

                SetPrivateField(view, "_boostModuleRoot", boostRoot);
                SetPrivateField(view, "_boostParticles", new[] { sustain });

                view.ApplyState(GGReplicaGlitchState.BoostHold);
                sustain.Simulate(0.2f, true, false, false);
                int liveParticles = sustain.particleCount;
                Assert.That(liveParticles, Is.GreaterThan(0), "Test setup should create visible boost sustain particles before stopping.");

                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(sustain.isEmitting, Is.False);
                Assert.That(sustain.particleCount, Is.GreaterThan(0), "Original PlayerViewBoostModule stops emission on BoostEnd; it should not clear the live flame tail instantly.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_BoostInterruptedByGrab_KeepsShortCutoffAfterimage()
        {
            var root = new GameObject("GGReplicaGlitchV2BoostGrabCutoffRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var boostRoot = new GameObject("BoostModule");
                boostRoot.transform.SetParent(root.transform, false);
                var grabRoot = new GameObject("GrabModule");
                grabRoot.transform.SetParent(root.transform, false);
                var sustain = new GameObject("vfx_boost_trail_loop_enhanced").AddComponent<ParticleSystem>();
                sustain.transform.SetParent(boostRoot.transform, false);
                var burst = new GameObject("vfx_boost_trail_burst_enhanced").AddComponent<ParticleSystem>();
                burst.transform.SetParent(boostRoot.transform, false);

                SetPrivateField(view, "_feelProfile", profile);
                SetPrivateField(view, "_boostModuleRoot", boostRoot);
                SetPrivateField(view, "_grabModuleRoot", grabRoot);
                SetPrivateField(view, "_boostParticles", new[] { sustain });
                SetPrivateField(view, "_boostBurstParticles", new[] { burst });

                view.ApplyState(GGReplicaGlitchState.BoostHold);
                InvokePrivate(view, "TickVisuals", profile.BoostIgniteDuration + 0.01f);
                Assert.That(burst.isPlaying, Is.False);

                view.ApplyState(GGReplicaGlitchState.GrabHold);

                Assert.That(grabRoot.activeSelf, Is.True);
                Assert.That(sustain.isPlaying, Is.False, "Boost sustain must stop immediately when Grab takes priority.");
                Assert.That(boostRoot.activeSelf, Is.True, "Boost should leave a short cutoff afterimage instead of disappearing in a hard pop.");
                Assert.That(burst.isPlaying, Is.True, "The cutoff afterimage reuses the boost ignition burst as a one-frame interruption accent.");

                InvokePrivate(view, "TickVisuals", 0.12f);

                Assert.That(boostRoot.activeSelf, Is.False);
                Assert.That(burst.isPlaying, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_GrabInterruptedByDodge_PlaysShortCancelPulseWithoutThrowLine()
        {
            var root = new GameObject("GGReplicaGlitchV2GrabDodgeCancelRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var dodgeRoot = new GameObject("DodgeModule");
                dodgeRoot.transform.SetParent(root.transform, false);
                var holdRoot = new GameObject("HoldModule");
                holdRoot.transform.SetParent(root.transform, false);
                var fluxyGrabRoot = new GameObject("FluxyGrabModule");
                fluxyGrabRoot.transform.SetParent(root.transform, false);
                var releasePulse = new GameObject("GrabReleasePulse").AddComponent<SpriteRenderer>();
                var releaseThrowLine = new GameObject("GrabReleaseThrowLine").AddComponent<LineRenderer>();
                var releaseBurst = new GameObject("GrabReleaseBurst").AddComponent<ParticleSystem>();
                var dodgeGhost = new GameObject("Dodge_Sprite (used for old outline trail)").AddComponent<SpriteRenderer>();
                releasePulse.transform.SetParent(fluxyGrabRoot.transform, false);
                releaseThrowLine.transform.SetParent(fluxyGrabRoot.transform, false);
                releaseBurst.transform.SetParent(fluxyGrabRoot.transform, false);
                dodgeGhost.transform.SetParent(dodgeRoot.transform, false);

                SetPrivateField(view, "_feelProfile", profile);
                SetPrivateField(view, "_dodgeModuleRoot", dodgeRoot);
                SetPrivateField(view, "_holdModuleRoot", holdRoot);
                SetPrivateField(view, "_fluxyGrabModuleRoot", fluxyGrabRoot);
                SetPrivateField(view, "_grabReleaseRenderer", releasePulse);
                SetPrivateField(view, "_grabReleaseThrowLine", releaseThrowLine);
                SetPrivateField(view, "_grabReleaseParticles", new[] { releaseBurst });
                SetPrivateField(view, "_dodgeGhostRenderer", dodgeGhost);

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                InvokePrivate(view, "TickVisuals", 0.24f);
                view.ApplyState(GGReplicaGlitchState.DodgeBurst);

                Assert.That(dodgeRoot.activeSelf, Is.True);
                Assert.That(dodgeGhost.enabled, Is.True, "Dodge should visually win immediately when it interrupts Grab.");
                Assert.That(holdRoot.activeSelf, Is.False, "The maintained hold field should shut off when Dodge cancels Grab.");
                Assert.That(fluxyGrabRoot.activeSelf, Is.True, "Grab cancel should leave a short liquid pulse instead of disappearing instantly.");
                Assert.That(releasePulse.enabled, Is.True);
                Assert.That(releaseBurst.isPlaying, Is.True);
                Assert.That(releaseThrowLine.enabled, Is.False, "A Dodge cancel is not a deliberate throw release, so it should not draw the full throw line.");

                InvokePrivate(view, "TickVisuals", 0.1f);

                Assert.That(fluxyGrabRoot.activeSelf, Is.False, "Cancel pulse should be shorter than the normal Grab release throw.");
                Assert.That(releasePulse.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_GrabHold_ExtendsHandsWithFakeFluxyEmphasisAndRestoresOnExit()
        {
            var root = new GameObject("GGReplicaGlitchV2GrabTimingRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var grabRoot = new GameObject("GrabModule");
                grabRoot.transform.SetParent(root.transform, false);
                var grabRight = new GameObject("Ship_Sprite_Solid_Grab_R").AddComponent<SpriteRenderer>();
                var grabLeft = new GameObject("Ship_Sprite_Solid_Grab_L").AddComponent<SpriteRenderer>();
                grabRight.transform.SetParent(grabRoot.transform, false);
                grabLeft.transform.SetParent(grabRoot.transform, false);
                grabRight.transform.localPosition = new Vector3(0.42f, -0.1f, 0f);
                grabLeft.transform.localPosition = new Vector3(-0.42f, -0.1f, 0f);

                SetPrivateField(view, "_grabModuleRoot", grabRoot);
                SetPrivateField(view, "_grabRenderers", new[] { grabRight, grabLeft });

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                InvokePrivate(view, "TickVisuals", 0.12f);

                Assert.That(grabRight.transform.localPosition.x, Is.GreaterThan(0.5f));
                Assert.That(grabLeft.transform.localPosition.x, Is.LessThan(-0.5f));
                Assert.That(grabRight.transform.localScale.x, Is.GreaterThan(1f));
                Assert.That(grabLeft.transform.localScale.x, Is.GreaterThan(1f));
                Assert.That(grabRight.color.a, Is.GreaterThan(0.95f));

                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(grabRight.transform.localPosition.x, Is.EqualTo(0.42f).Within(0.001f));
                Assert.That(grabLeft.transform.localPosition.x, Is.EqualTo(-0.42f).Within(0.001f));
                Assert.That(grabRight.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_GrabHold_ShowsFluxyGrabHoloAndThrowPointerWithoutMutatingSharedMaterial()
        {
            var root = new GameObject("GGReplicaGlitchV2FluxyGrabRig");
            Material material = null;
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var grabRoot = new GameObject("GrabModule");
                var fluxyGrabRoot = new GameObject("FluxyGrabModule");
                grabRoot.transform.SetParent(root.transform, false);
                fluxyGrabRoot.transform.SetParent(root.transform, false);
                var grabRight = new GameObject("Ship_Sprite_Solid_Grab_R").AddComponent<SpriteRenderer>();
                var grabLeft = new GameObject("Ship_Sprite_Solid_Grab_L").AddComponent<SpriteRenderer>();
                var holoRight = new GameObject("FluxyGrabHolo_R").AddComponent<SpriteRenderer>();
                var holoLeft = new GameObject("FluxyGrabHolo_L").AddComponent<SpriteRenderer>();
                var throwPointer = new GameObject("GrabThrowPointer").AddComponent<LineRenderer>();
                material = new Material(Shader.Find("Sprites/Default"));
                material.SetFloat("_Alpha", 0.62f);
                holoRight.sharedMaterial = material;
                holoLeft.sharedMaterial = material;
                throwPointer.sharedMaterial = material;
                grabRight.transform.SetParent(grabRoot.transform, false);
                grabLeft.transform.SetParent(grabRoot.transform, false);
                holoRight.transform.SetParent(fluxyGrabRoot.transform, false);
                holoLeft.transform.SetParent(fluxyGrabRoot.transform, false);
                throwPointer.transform.SetParent(fluxyGrabRoot.transform, false);
                grabRight.transform.localPosition = new Vector3(0.42f, -0.1f, 0f);
                grabLeft.transform.localPosition = new Vector3(-0.42f, -0.1f, 0f);

                SetPrivateField(view, "_grabModuleRoot", grabRoot);
                SetPrivateField(view, "_fluxyGrabModuleRoot", fluxyGrabRoot);
                SetPrivateField(view, "_grabRenderers", new[] { grabRight, grabLeft });
                SetPrivateField(view, "_grabFluxyRenderers", new[] { holoRight, holoLeft });
                SetPrivateField(view, "_grabThrowPointer", throwPointer);

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                InvokePrivate(view, "TickVisuals", 0.12f);

                Assert.That(fluxyGrabRoot.activeSelf, Is.True);
                Assert.That(holoRight.enabled, Is.True);
                Assert.That(holoLeft.enabled, Is.True);
                Assert.That(holoRight.transform.localScale.x, Is.GreaterThan(1.2f), "Original PlayerViewFluxyGrabModule uses fluxy targets/holograms, not only hand sprites.");
                Assert.That(holoRight.color.a, Is.GreaterThan(0.55f));
                Assert.That(throwPointer.enabled, Is.True);
                Assert.That(throwPointer.positionCount, Is.EqualTo(2));
                AssertFloatProperty(holoRight, "_Alpha", 0.6345f);
                AssertFloatProperty(throwPointer, "_Alpha", 0.522f);
                Assert.That(material.GetFloat("_Alpha"), Is.EqualTo(0.62f).Within(0.001f), "FluxyGrab runtime must not mutate GGReplicaFakeFluxy.mat/shared material.");

                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(fluxyGrabRoot.activeSelf, Is.True, "Release pulse keeps FluxyGrabModule alive briefly after E is released.");

                InvokePrivate(view, "TickVisuals", 0.2f);

                Assert.That(holoRight.enabled, Is.False);
                Assert.That(throwPointer.enabled, Is.False);
                Assert.That(holoRight.transform.localScale, Is.EqualTo(Vector3.one));
                AssertFloatProperty(holoRight, "_Alpha", 0f);
                Assert.That(fluxyGrabRoot.activeSelf, Is.False);
            }
            finally
            {
                if (material != null) Object.DestroyImmediate(material);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_GrabHold_ProgressesSelectLockAndReleaseFeedback()
        {
            var root = new GameObject("GGReplicaGlitchV2GrabPhaseRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var fluxyGrabRoot = new GameObject("FluxyGrabModule");
                fluxyGrabRoot.transform.SetParent(root.transform, false);
                var holo = new GameObject("FluxyGrabHolo_R").AddComponent<SpriteRenderer>();
                var pointer = new GameObject("GrabThrowPointer").AddComponent<LineRenderer>();
                var lockRing = new GameObject("GrabLockRing").AddComponent<SpriteRenderer>();
                var releasePulse = new GameObject("GrabReleasePulse").AddComponent<SpriteRenderer>();
                holo.transform.SetParent(fluxyGrabRoot.transform, false);
                pointer.transform.SetParent(fluxyGrabRoot.transform, false);
                lockRing.transform.SetParent(fluxyGrabRoot.transform, false);
                releasePulse.transform.SetParent(fluxyGrabRoot.transform, false);

                SetPrivateField(view, "_fluxyGrabModuleRoot", fluxyGrabRoot);
                SetPrivateField(view, "_grabFluxyRenderers", new[] { holo });
                SetPrivateField(view, "_grabThrowPointer", pointer);
                SetPrivateField(view, "_grabLockRenderer", lockRing);
                SetPrivateField(view, "_grabReleaseRenderer", releasePulse);

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                InvokePrivate(view, "TickVisuals", 0.06f);

                Assert.That(holo.enabled, Is.True, "OnSelect should show the hologram before lock.");
                Assert.That(lockRing.enabled, Is.False, "Lock ring should wait for the local lock timing instead of appearing instantly.");

                InvokePrivate(view, "TickVisuals", 0.18f);

                Assert.That(lockRing.enabled, Is.True, "OnLock should add a stronger confirmation layer while Grab is held.");
                Assert.That(lockRing.color.a, Is.GreaterThan(0.55f));
                Assert.That(pointer.startWidth, Is.GreaterThan(0.06f));

                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(releasePulse.enabled, Is.True, "OnRelease should leave a short pulse after E is released.");
                Assert.That(fluxyGrabRoot.activeSelf, Is.True);

                InvokePrivate(view, "TickVisuals", 0.2f);

                Assert.That(releasePulse.enabled, Is.False);
                Assert.That(lockRing.enabled, Is.False);
                Assert.That(fluxyGrabRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_GrabRelease_PlaysBurstAndCollapsesThrowLine()
        {
            var root = new GameObject("GGReplicaGlitchV2GrabReleaseBurstRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var fluxyGrabRoot = new GameObject("FluxyGrabModule");
                fluxyGrabRoot.transform.SetParent(root.transform, false);
                var releasePulse = new GameObject("GrabReleasePulse").AddComponent<SpriteRenderer>();
                var releaseBurst = new GameObject("GrabReleaseBurst").AddComponent<ParticleSystem>();
                var releaseThrowLine = new GameObject("GrabReleaseThrowLine").AddComponent<LineRenderer>();
                releasePulse.transform.SetParent(fluxyGrabRoot.transform, false);
                releaseBurst.transform.SetParent(fluxyGrabRoot.transform, false);
                releaseThrowLine.transform.SetParent(fluxyGrabRoot.transform, false);

                SetPrivateField(view, "_fluxyGrabModuleRoot", fluxyGrabRoot);
                SetPrivateField(view, "_grabReleaseRenderer", releasePulse);
                SetPrivateField(view, "_grabReleaseParticles", new[] { releaseBurst });
                SetPrivateField(view, "_grabReleaseThrowLine", releaseThrowLine);

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                InvokePrivate(view, "TickVisuals", 0.24f);
                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(releaseBurst.isPlaying, Is.True, "Original OnRelease should feel like a liquid throw/burst, not only a static pulse.");
                Assert.That(releaseThrowLine.enabled, Is.True);
                Assert.That(releaseThrowLine.positionCount, Is.EqualTo(3));
                Assert.That(releaseThrowLine.startWidth, Is.GreaterThan(releaseThrowLine.endWidth));
                Assert.That(releaseThrowLine.GetPosition(0).x, Is.LessThan(0f));
                Assert.That(releaseThrowLine.GetPosition(2).x, Is.GreaterThan(0f));

                InvokePrivate(view, "TickVisuals", 0.2f);

                Assert.That(releaseBurst.isPlaying, Is.False);
                Assert.That(releaseThrowLine.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_GrabHold_ShowsLocalInteractTargetAndLockOverlay()
        {
            var root = new GameObject("GGReplicaGlitchV2GrabTargetRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var fluxyGrabRoot = new GameObject("FluxyGrabModule");
                fluxyGrabRoot.transform.SetParent(root.transform, false);
                var target = new GameObject("GrabTargetHolo").AddComponent<SpriteRenderer>();
                var overlay = new GameObject("GrabRippableOverlay").AddComponent<SpriteRenderer>();
                var pointer = new GameObject("GrabThrowPointer").AddComponent<LineRenderer>();
                target.transform.SetParent(fluxyGrabRoot.transform, false);
                overlay.transform.SetParent(fluxyGrabRoot.transform, false);
                pointer.transform.SetParent(fluxyGrabRoot.transform, false);

                SetPrivateField(view, "_fluxyGrabModuleRoot", fluxyGrabRoot);
                SetPrivateField(view, "_grabTargetRenderer", target);
                SetPrivateField(view, "_grabTargetOverlayRenderer", overlay);
                SetPrivateField(view, "_grabThrowPointer", pointer);

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                InvokePrivate(view, "TickVisuals", 0.06f);

                Assert.That(target.enabled, Is.True, "OnSelect should give the local placeholder target a visible lock point.");
                Assert.That(overlay.enabled, Is.False);
                Assert.That(target.transform.localPosition.y, Is.GreaterThan(0.9f));
                Assert.That(pointer.GetPosition(1).y, Is.EqualTo(target.transform.localPosition.y).Within(0.001f));

                InvokePrivate(view, "TickVisuals", 0.18f);

                Assert.That(overlay.enabled, Is.True, "OnLock should add a rippable/locked overlay on the local target.");
                Assert.That(overlay.color.a, Is.GreaterThan(0.5f));
                Assert.That(target.transform.localScale.x, Is.GreaterThan(1f));

                view.ApplyState(GGReplicaGlitchState.Idle);
                InvokePrivate(view, "TickVisuals", 0.2f);

                Assert.That(target.enabled, Is.False);
                Assert.That(overlay.enabled, Is.False);
                Assert.That(target.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_GrabHold_ShowsHoldFieldProgressWhileMaintainingTarget()
        {
            var root = new GameObject("GGReplicaGlitchV2HoldFieldRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var holdRoot = new GameObject("HoldModule");
                holdRoot.transform.SetParent(root.transform, false);
                var holdField = new GameObject("HoldFieldRing").AddComponent<SpriteRenderer>();
                var holdProgress = new GameObject("HoldProgress").AddComponent<SpriteRenderer>();
                var holdLine = new GameObject("HoldTetherLine").AddComponent<LineRenderer>();
                var holdParticles = new GameObject("HoldParticles").AddComponent<ParticleSystem>();
                holdField.transform.SetParent(holdRoot.transform, false);
                holdProgress.transform.SetParent(holdRoot.transform, false);
                holdLine.transform.SetParent(holdRoot.transform, false);
                holdParticles.transform.SetParent(holdRoot.transform, false);

                SetPrivateField(view, "_holdModuleRoot", holdRoot);
                SetPrivateField(view, "_holdFieldRenderer", holdField);
                SetPrivateField(view, "_holdProgressRenderer", holdProgress);
                SetPrivateField(view, "_holdTetherLine", holdLine);
                SetPrivateField(view, "_holdParticles", new[] { holdParticles });

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                InvokePrivate(view, "TickVisuals", 0.12f);

                Assert.That(holdRoot.activeSelf, Is.True);
                Assert.That(holdParticles.isPlaying, Is.True, "Original HoldModule/HoldParticles should read as a maintained field while Grab is held.");
                Assert.That(holdField.enabled, Is.True);
                Assert.That(holdProgress.enabled, Is.True);
                Assert.That(holdLine.enabled, Is.True);
                Assert.That(holdLine.positionCount, Is.EqualTo(2));
                Assert.That(holdProgress.color.a, Is.GreaterThan(0.2f));

                InvokePrivate(view, "TickVisuals", 0.42f);

                Assert.That(holdProgress.transform.localScale.x, Is.GreaterThan(holdField.transform.localScale.x), "HoldProgress should grow as the local hold field charges.");

                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(holdParticles.isPlaying, Is.False);
                Assert.That(holdField.enabled, Is.False);
                Assert.That(holdProgress.enabled, Is.False);
                Assert.That(holdLine.enabled, Is.False);
                Assert.That(holdProgress.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_FireAim_ShowsPrimaryAttackLayersAndDedicatedShotParticles()
        {
            var root = new GameObject("GGReplicaGlitchV2FireAimRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var fireRoot = new GameObject("FireAimModule");
                fireRoot.transform.SetParent(root.transform, false);
                var primarySolid = new GameObject("MainAttackState").AddComponent<SpriteRenderer>();
                var primaryHighlight = new GameObject("MainAttackFireState").AddComponent<SpriteRenderer>();
                var hitboxHint = new GameObject("MainAttackStateHitbox").AddComponent<SpriteRenderer>();
                var shotParticle = new GameObject("GlitchEnergyReadyParticles (weapon once)").AddComponent<ParticleSystem>();
                var boostBurst = new GameObject("vfx_boost_trail_burst_enhanced").AddComponent<ParticleSystem>();
                primarySolid.transform.SetParent(fireRoot.transform, false);
                primaryHighlight.transform.SetParent(fireRoot.transform, false);
                hitboxHint.transform.SetParent(fireRoot.transform, false);
                shotParticle.transform.SetParent(fireRoot.transform, false);
                boostBurst.transform.SetParent(root.transform, false);

                SetPrivateField(view, "_fireAimModuleRoot", fireRoot);
                SetPrivateField(view, "_fireAimRenderers", new[] { primarySolid, primaryHighlight, hitboxHint });
                SetPrivateField(view, "_fireAimParticles", new[] { shotParticle });
                SetPrivateField(view, "_burstParticles", new[] { boostBurst });

                view.ApplyState(GGReplicaGlitchState.FireAim);
                InvokePrivate(view, "TickVisuals", 0.08f);

                Assert.That(fireRoot.activeSelf, Is.True);
                Assert.That(primarySolid.enabled, Is.True);
                Assert.That(primaryHighlight.enabled, Is.True);
                Assert.That(hitboxHint.enabled, Is.True);
                Assert.That(primaryHighlight.color.r, Is.GreaterThan(primaryHighlight.color.g));
                Assert.That(primaryHighlight.transform.localScale.x, Is.GreaterThan(1f));
                Assert.That(shotParticle.isPlaying, Is.True);
                Assert.That(boostBurst.isPlaying, Is.False, "FireAim must not reuse Boost ignition burst particles.");

                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(fireRoot.activeSelf, Is.False);
                Assert.That(primarySolid.enabled, Is.False);
                Assert.That(primaryHighlight.enabled, Is.False);
                Assert.That(hitboxHint.enabled, Is.False);
                Assert.That(shotParticle.isPlaying, Is.False);
                Assert.That(primaryHighlight.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_HealHold_PulsesHealSpritesAndRestoresOnExit()
        {
            var root = new GameObject("GGReplicaGlitchV2HealTimingRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var healRoot = new GameObject("HealModule");
                healRoot.transform.SetParent(root.transform, false);
                var healShell = new GameObject("Healing_0").AddComponent<SpriteRenderer>();
                var healDot = new GameObject("vfx_dot_001").AddComponent<SpriteRenderer>();
                var healParticle = new GameObject("ps_glitch_heal").AddComponent<ParticleSystem>();
                var boostBurst = new GameObject("vfx_boost_trail_burst_enhanced").AddComponent<ParticleSystem>();
                healShell.transform.SetParent(healRoot.transform, false);
                healDot.transform.SetParent(healRoot.transform, false);
                healParticle.transform.SetParent(healRoot.transform, false);
                boostBurst.transform.SetParent(root.transform, false);

                SetPrivateField(view, "_healModuleRoot", healRoot);
                SetPrivateField(view, "_healRenderers", new[] { healShell, healDot });
                SetPrivateField(view, "_healParticles", new[] { healParticle });
                SetPrivateField(view, "_burstParticles", new[] { boostBurst });

                view.ApplyState(GGReplicaGlitchState.Heal);
                InvokePrivate(view, "TickVisuals", 0.1f);

                Assert.That(healRoot.activeSelf, Is.True);
                Assert.That(healShell.enabled, Is.True);
                Assert.That(healDot.enabled, Is.True);
                Assert.That(healShell.transform.localScale.x, Is.GreaterThan(1f));
                Assert.That(healDot.color.g, Is.GreaterThan(healDot.color.r));
                Assert.That(healDot.color.a, Is.GreaterThan(0.6f));
                Assert.That(healParticle.isPlaying, Is.True);
                Assert.That(boostBurst.isPlaying, Is.False, "Heal must not reuse Boost ignition burst particles.");

                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(healRoot.activeSelf, Is.False);
                Assert.That(healShell.enabled, Is.False);
                Assert.That(healDot.enabled, Is.False);
                Assert.That(healParticle.isPlaying, Is.False);
                Assert.That(healShell.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_DodgeVisuals_FadeGhostAndStopBurstAfterDodgeWindow()
        {
            var root = new GameObject("GGReplicaGlitchV2DodgeTimingRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var dodgeRoot = new GameObject("DodgeModule");
                dodgeRoot.transform.SetParent(root.transform, false);
                var ghost = new GameObject("Dodge_Sprite (used for old outline trail)").AddComponent<SpriteRenderer>();
                ghost.transform.SetParent(root.transform, false);
                var dodgeHalf = new GameObject("DodgeHalf_Sprite").AddComponent<SpriteRenderer>();
                dodgeHalf.transform.SetParent(dodgeRoot.transform, false);
                var additiveCore = new GameObject("AdditiveCore_Dodge").AddComponent<SpriteRenderer>();
                additiveCore.transform.SetParent(dodgeRoot.transform, false);
                var core = new GameObject("Ship_Sprite_Core").AddComponent<SpriteRenderer>();
                core.transform.SetParent(root.transform, false);
                var burst = new GameObject("ps_dodge_shell").AddComponent<ParticleSystem>();
                burst.transform.SetParent(dodgeRoot.transform, false);

                SetPrivateField(view, "_feelProfile", profile);
                SetPrivateField(view, "_dodgeModuleRoot", dodgeRoot);
                SetPrivateField(view, "_dodgeGhostRenderer", ghost);
                SetPrivateField(view, "_dodgeHalfRenderer", dodgeHalf);
                SetPrivateField(view, "_dodgeAdditiveCoreRenderer", additiveCore);
                SetPrivateField(view, "_coreRenderer", core);
                SetPrivateField(view, "_burstParticles", new[] { burst });

                view.ApplyState(GGReplicaGlitchState.DodgeBurst);

                Assert.That(ghost.enabled, Is.True);
                Assert.That(ghost.color.a, Is.GreaterThan(0.6f));
                Assert.That(dodgeHalf.enabled, Is.True, "Original PlayerViewCoreModule has a dedicated half silhouette / shell layer during Dodge.");
                Assert.That(dodgeHalf.color.a, Is.GreaterThan(0.35f));
                Assert.That(additiveCore.enabled, Is.True, "Original prefab contains AdditiveCore_Dodge; V2 should light it during Dodge.");
                Assert.That(additiveCore.color.a, Is.GreaterThan(0.5f));
                Assert.That(additiveCore.transform.localScale.x, Is.GreaterThan(core.transform.localScale.x));
                Assert.That(core.transform.localScale.x, Is.GreaterThan(1f));
                Assert.That(burst.isPlaying, Is.True);

                InvokePrivate(view, "TickVisuals", profile.DodgeStateDuration + 0.01f);

                Assert.That(ghost.enabled, Is.False, "Dodge ghost should fade out after the original dodge state window.");
                Assert.That(dodgeHalf.enabled, Is.False);
                Assert.That(additiveCore.enabled, Is.False);
                Assert.That(core.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(additiveCore.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(burst.isPlaying, Is.False, "Dodge burst particles should not loop forever.");
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_TrailModules_MoveAndDodgeUseSeparateOriginalModuleLanes()
        {
            var root = new GameObject("GGReplicaGlitchV2TrailModuleSplitRig");
            Material material = null;
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var lqRoot = new GameObject("LQTrailsContainer");
                lqRoot.transform.SetParent(root.transform, false);
                var starTrail = new GameObject("startrails").AddComponent<TrailRenderer>();
                var starTrailLong = new GameObject("startrails_long").AddComponent<TrailRenderer>();
                var darkTrail = new GameObject("dark_trail").AddComponent<TrailRenderer>();
                var shapeTrail = new GameObject("shape_trail").AddComponent<TrailRenderer>();
                var fluxyTrail = new GameObject("fluxy_like_lq_trail").AddComponent<TrailRenderer>();
                starTrail.transform.SetParent(lqRoot.transform, false);
                starTrailLong.transform.SetParent(lqRoot.transform, false);
                darkTrail.transform.SetParent(root.transform, false);
                shapeTrail.transform.SetParent(root.transform, false);
                fluxyTrail.transform.SetParent(root.transform, false);
                material = new Material(Shader.Find("Sprites/Default"));
                material.SetFloat("_Alpha", 0.62f);
                material.SetFloat("_FlowPower", 3.77f);
                material.SetFloat("_NoiseScale", 6f);
                fluxyTrail.sharedMaterial = material;

                SetPrivateField(view, "_lqTrailsContainer", lqRoot);
                SetPrivateField(view, "_lqTrailRenderers", new[] { starTrail, starTrailLong });
                SetPrivateField(view, "_darkTrailRenderers", new[] { darkTrail });
                SetPrivateField(view, "_shapeTrailRenderers", new[] { shapeTrail });
                SetPrivateField(view, "_fluxyTrailRenderer", fluxyTrail);

                view.ApplyState(GGReplicaGlitchState.Move);

                Assert.That(lqRoot.activeSelf, Is.True, "PlayerViewLQTrailModule should own the base movement trail lane.");
                Assert.That(starTrail.emitting, Is.True);
                Assert.That(starTrailLong.emitting, Is.True);
                Assert.That(darkTrail.emitting, Is.False, "DarkTrailModule is a distinct original lane and must not be toggled by the base LQ trail path.");
                Assert.That(shapeTrail.emitting, Is.False, "ShapeTrailModule should not be toggled by Move.");
                Assert.That(fluxyTrail.emitting, Is.False, "FluxyTrailModule should stay off until Dodge-style fluid emphasis.");

                view.ApplyState(GGReplicaGlitchState.DodgeBurst);

                Assert.That(starTrail.emitting, Is.False, "Dodge should not rely on the base LQ trail lane after the modules are split.");
                Assert.That(starTrailLong.emitting, Is.False);
                Assert.That(shapeTrail.emitting, Is.True, "PlayerViewShapeTrailModule owns the shape trail lane during Dodge.");
                Assert.That(fluxyTrail.emitting, Is.True, "PlayerViewFluxyTrailModule owns the fluid Dodge trail lane.");
                Assert.That(darkTrail.emitting, Is.False);
            }
            finally
            {
                if (material != null) Object.DestroyImmediate(material);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_DodgeFluxyTrail_UsesOriginalDodgeIntensityWithoutMutatingSharedMaterial()
        {
            var root = new GameObject("GGReplicaGlitchV2FluxyTrailRig");
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();
            Material material = null;
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var fluxyTrail = new GameObject("fluxy_like_lq_trail").AddComponent<TrailRenderer>();
                fluxyTrail.transform.SetParent(root.transform, false);
                fluxyTrail.time = 0.6f;
                fluxyTrail.widthMultiplier = 3.5f;
                fluxyTrail.startColor = Color.white;
                fluxyTrail.endColor = Color.white;
                material = new Material(Shader.Find("Sprites/Default"));
                material.SetFloat("_Alpha", 0.62f);
                material.SetFloat("_FlowPower", 3.77f);
                material.SetFloat("_NoiseScale", 6f);
                fluxyTrail.sharedMaterial = material;

                SetPrivateField(view, "_feelProfile", profile);
                SetPrivateField(view, "_fluxyTrailRenderer", fluxyTrail);

                view.ApplyState(GGReplicaGlitchState.DodgeBurst);

                Assert.That(fluxyTrail.emitting, Is.True);
                Assert.That(fluxyTrail.widthMultiplier, Is.GreaterThan(5f), "Original PlayerViewFluxyTrailModule grows the fluid target during Dodge.");
                Assert.That(fluxyTrail.time, Is.GreaterThan(0.75f));
                Assert.That(fluxyTrail.transform.localScale.x, Is.GreaterThan(1.1f));
                Assert.That(fluxyTrail.startColor.a, Is.GreaterThan(0.85f));
                AssertFloatProperty(fluxyTrail, "_Alpha", 0.95f);
                Assert.That(ReadFloatProperty(fluxyTrail, "_FlowPower"), Is.GreaterThan(material.GetFloat("_FlowPower")));
                Assert.That(material.GetFloat("_Alpha"), Is.EqualTo(0.62f).Within(0.001f), "Runtime must use MaterialPropertyBlock, not mutate GGReplicaFakeFluxy.mat/shared material.");

                InvokePrivate(view, "TickVisuals", profile.DodgeStateDuration + 0.01f);

                Assert.That(fluxyTrail.emitting, Is.False);
                Assert.That(fluxyTrail.widthMultiplier, Is.EqualTo(3.5f).Within(0.001f));
                Assert.That(fluxyTrail.time, Is.EqualTo(0.6f).Within(0.001f));
                Assert.That(fluxyTrail.transform.localScale, Is.EqualTo(Vector3.one));
                AssertFloatProperty(fluxyTrail, "_Alpha", 0f);
                Assert.That(material.GetFloat("_FlowPower"), Is.EqualTo(3.77f).Within(0.001f));
            }
            finally
            {
                if (material != null) Object.DestroyImmediate(material);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void View_DodgeShapeTrail_StartsOriginalOutlineAndAdditiveParticleTrails()
        {
            var root = new GameObject("GGReplicaGlitchV2DodgeShapeTrailRig");
            try
            {
                var view = root.AddComponent<GGReplicaGlitchView>();
                var dodgeRoot = new GameObject("DodgeModule");
                dodgeRoot.transform.SetParent(root.transform, false);
                var outlineTrail = new GameObject("ShapeTrail_Dodge (old outline trail)").AddComponent<ParticleSystem>();
                outlineTrail.transform.SetParent(dodgeRoot.transform, false);
                var additiveTrail = new GameObject("AdditiveTrail_Dodge").AddComponent<ParticleSystem>();
                additiveTrail.transform.SetParent(dodgeRoot.transform, false);

                SetPrivateField(view, "_dodgeModuleRoot", dodgeRoot);
                SetPrivateField(view, "_dodgeTrailParticles", new[] { outlineTrail, additiveTrail });

                view.ApplyState(GGReplicaGlitchState.DodgeBurst);

                Assert.That(outlineTrail.isPlaying, Is.True, "Original PlayerViewShapeTrailModule starts the old outline trail on Dodge.");
                Assert.That(additiveTrail.isPlaying, Is.True, "Original PlayerViewShapeTrailModule starts the additive core trail on Dodge.");

                view.ApplyState(GGReplicaGlitchState.Idle);

                Assert.That(outlineTrail.isPlaying, Is.False);
                Assert.That(additiveTrail.isPlaying, Is.False);
            }
            finally
            {
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
                SetPrivateField(view, "_lqTrailRenderers", new[] { trailA, trailB });

                view.ApplyState(GGReplicaGlitchState.Idle);
                Assert.That(boostRoot.activeSelf, Is.False);
                Assert.That(trailA.emitting, Is.False);

                view.ApplyState(GGReplicaGlitchState.BoostHold);
                Assert.That(boostRoot.activeSelf, Is.True);
                Assert.That(trailA.emitting, Is.True);
                Assert.That(trailB.emitting, Is.True);

                view.ApplyState(GGReplicaGlitchState.GrabHold);
                Assert.That(grabRoot.activeSelf, Is.True);
                Assert.That(boostRoot.activeSelf, Is.True, "Boost leaves a short cutoff afterimage when Grab interrupts it.");
                InvokePrivate(view, "TickVisuals", 0.12f);
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                InvokePrivate(view, "TickVisuals", 0.01f);
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

        private static Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            sprite.name = name;
            return sprite;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            return field!.GetValue(target) as T;
        }

        private static void AssertFloatProperty(Renderer renderer, string propertyName, float expected)
        {
            Assert.That(ReadFloatProperty(renderer, propertyName), Is.EqualTo(expected).Within(0.001f));
        }

        private static float ReadFloatProperty(Renderer renderer, string propertyName)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetFloat(propertyName);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName} on {target.GetType().Name}.");
            method!.Invoke(target, null);
        }

        private static void InvokePrivate(object target, string methodName, float value)
        {
            var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName} on {target.GetType().Name}.");
            method!.Invoke(target, new object[] { value });
        }
    }
}
