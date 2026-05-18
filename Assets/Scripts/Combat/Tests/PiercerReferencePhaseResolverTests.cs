using NUnit.Framework;
using ProjectArk.Combat.Enemy;
using UnityEngine;

namespace ProjectArk.Combat.Tests
{
    [TestFixture]
    public class PiercerReferencePhaseResolverTests
    {
        [Test]
        public void Resolve_ReturnsIdle_WhenCurrentStateIsNotChargeState()
        {
            var phase = PiercerReferencePhaseResolver.Resolve(
                currentStateName: "ChaseState",
                elapsedInState: 0.25f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(PiercerReferencePhase.Idle, phase);
        }

        [Test]
        public void Resolve_ReturnsPause_DuringOriginalAIChargePauseWindow()
        {
            var phase = PiercerReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 0.75f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(PiercerReferencePhase.Pause, phase);
        }

        [Test]
        public void Resolve_ReturnsPause_WhenDrivenByPiercerReferenceChargeState()
        {
            var phase = PiercerReferencePhaseResolver.Resolve(
                currentStateName: "PiercerReferenceChargeState",
                elapsedInState: 0.75f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(PiercerReferencePhase.Pause, phase);
        }

        [Test]
        public void Resolve_ReturnsAnticipation_AfterPauseBeforeDash()
        {
            var phase = PiercerReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 1.05f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(PiercerReferencePhase.Anticipation, phase);
        }

        [Test]
        public void Resolve_ReturnsDashing_AfterAnticipationWindow()
        {
            var phase = PiercerReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 1.25f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(PiercerReferencePhase.Dashing, phase);
        }

        [Test]
        public void Resolve_ReturnsRecovery_AfterDashWindow()
        {
            var phase = PiercerReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 1.72f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(PiercerReferencePhase.Recovery, phase);
        }

        [Test]
        public void Resolve_ReturnsIdle_WhenChargeWindowHasExpired()
        {
            var phase = PiercerReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 2f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(PiercerReferencePhase.Idle, phase);
        }

        [Test]
        public void ResolveLooping_WrapsElapsedTimeBackToPauseWindow()
        {
            var phase = PiercerReferencePhaseResolver.ResolveLooping(
                currentStateName: "PiercerReferenceChargeState",
                elapsedInState: 2.25f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f);

            Assert.AreEqual(PiercerReferencePhase.Pause, phase);
        }

        [Test]
        public void ResolveLooping_ReturnsIdle_DuringIdleGapBetweenChargeCycles()
        {
            var phase = PiercerReferencePhaseResolver.ResolveLooping(
                currentStateName: "PiercerReferenceChargeState",
                elapsedInState: 2.05f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f);

            Assert.AreEqual(PiercerReferencePhase.Idle, phase);
        }

        [Test]
        public void Resolve_ClampsNegativeDurationsToZero()
        {
            var phase = PiercerReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 0.01f,
                pauseDuration: -1f,
                anticipationDuration: -0.2f,
                dashDuration: 0.45f,
                recoveryDuration: -0.25f);

            Assert.AreEqual(PiercerReferencePhase.Dashing, phase);
        }

        [Test]
        public void FormatDebugLabel_IncludesPhaseElapsedAndLoopState()
        {
            string label = PiercerReferencePhaseResolver.FormatDebugLabel(
                PiercerReferencePhase.Anticipation,
                1.054f,
                loopPreview: true);

            Assert.AreEqual("Piercer REF | Anticipation | t=1.05s | loop=on", label);
        }

        [Test]
        public void ResolvePhaseProgress_ReturnsNormalizedProgressWithinCurrentWindow()
        {
            float progress = PiercerReferencePhaseResolver.ResolvePhaseProgress(
                currentStateName: "PiercerReferenceChargeState",
                elapsedInState: 1.05f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f,
                loopPreview: true);

            Assert.AreEqual(0.25f, progress, 0.001f);
        }

        [Test]
        public void FormatDetailedDebugLabel_IncludesPhaseProgressPercent()
        {
            string label = PiercerReferencePhaseResolver.FormatDetailedDebugLabel(
                PiercerReferencePhase.Anticipation,
                elapsedInState: 1.054f,
                phaseProgress: 0.25f,
                phaseRemainingTime: 0.15f,
                cycleElapsedTime: 1.05f,
                cycleDuration: 2.2f,
                loopPreview: true);

            Assert.AreEqual("Piercer REF | Anticipation | t=1.05s | p=25% | left=0.15s | cycle=1.05/2.20s | loop=on", label);
        }

        [Test]
        public void ResolvePhaseRemainingTime_ReturnsSecondsLeftWithinCurrentWindow()
        {
            float remainingTime = PiercerReferencePhaseResolver.ResolvePhaseRemainingTime(
                currentStateName: "PiercerReferenceChargeState",
                elapsedInState: 1.05f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f,
                loopPreview: true);

            Assert.AreEqual(0.15f, remainingTime, 0.001f);
        }

        [Test]
        public void ResolvePhaseRemainingTime_UsesWrappedElapsedTimeWhenLoopPreviewIsEnabled()
        {
            float remainingTime = PiercerReferencePhaseResolver.ResolvePhaseRemainingTime(
                currentStateName: "PiercerReferenceChargeState",
                elapsedInState: 2.25f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f,
                loopPreview: true);

            Assert.AreEqual(0.95f, remainingTime, 0.001f);
        }

        [Test]
        public void ResolvePhaseRemainingTime_ReturnsZero_WhenCurrentStateIsNotChargeState()
        {
            float remainingTime = PiercerReferencePhaseResolver.ResolvePhaseRemainingTime(
                currentStateName: "ChaseState",
                elapsedInState: 1.05f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f,
                loopPreview: true);

            Assert.AreEqual(0f, remainingTime);
        }

        [Test]
        public void ResolveCycleElapsedTime_UsesWrappedElapsedTimeWhenLoopPreviewIsEnabled()
        {
            float cycleElapsedTime = PiercerReferencePhaseResolver.ResolveCycleElapsedTime(
                currentStateName: "PiercerReferenceChargeState",
                elapsedInState: 2.25f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f,
                loopPreview: true);

            Assert.AreEqual(0.05f, cycleElapsedTime, 0.001f);
        }

        [Test]
        public void ResolveCycleElapsedTime_ReturnsSafeElapsedTimeWhenLoopPreviewIsDisabled()
        {
            float cycleElapsedTime = PiercerReferencePhaseResolver.ResolveCycleElapsedTime(
                currentStateName: "ChargeState",
                elapsedInState: 2.25f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f,
                loopPreview: false);

            Assert.AreEqual(2.25f, cycleElapsedTime, 0.001f);
        }

        [Test]
        public void ResolveCycleElapsedTime_ReturnsZero_WhenCurrentStateIsNotChargeState()
        {
            float cycleElapsedTime = PiercerReferencePhaseResolver.ResolveCycleElapsedTime(
                currentStateName: "ChaseState",
                elapsedInState: 2.25f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f,
                loopPreview: true);

            Assert.AreEqual(0f, cycleElapsedTime);
        }

        [Test]
        public void ResolveCycleDuration_ReturnsClampedChargeAndIdleGapDuration()
        {
            float cycleDuration = PiercerReferencePhaseResolver.ResolveCycleDuration(
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f);

            Assert.AreEqual(2.2f, cycleDuration, 0.001f);
        }

        [Test]
        public void FormatDetailedDebugLabel_IncludesCycleElapsedAndDuration()
        {
            string label = PiercerReferencePhaseResolver.FormatDetailedDebugLabel(
                PiercerReferencePhase.Pause,
                elapsedInState: 2.254f,
                phaseProgress: 0.05f,
                phaseRemainingTime: 0.95f,
                cycleElapsedTime: 0.05f,
                cycleDuration: 2.2f,
                loopPreview: true);

            Assert.AreEqual("Piercer REF | Pause | t=2.25s | p=5% | left=0.95s | cycle=0.05/2.20s | loop=on", label);
        }

        [Test]
        public void ResolveSnapshot_ReturnsPhaseTimingAndDetailedLabelForHarness()
        {
            PiercerReferencePhaseSnapshot snapshot = PiercerReferencePhaseResolver.ResolveSnapshot(
                currentStateName: "PiercerReferenceChargeState",
                elapsedInState: 2.25f,
                pauseDuration: 1f,
                anticipationDuration: 0.2f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f,
                idleGapDuration: 0.3f,
                loopPreview: true);

            Assert.AreEqual(PiercerReferencePhase.Pause, snapshot.Phase);
            Assert.AreEqual(0.05f, snapshot.PhaseProgress, 0.001f);
            Assert.AreEqual(0.95f, snapshot.PhaseRemainingTime, 0.001f);
            Assert.AreEqual(0.05f, snapshot.CycleElapsedTime, 0.001f);
            Assert.AreEqual(2.2f, snapshot.CycleDuration, 0.001f);
            Assert.AreEqual("Piercer REF | Pause | t=2.25s | p=5% | left=0.95s | cycle=0.05/2.20s | loop=on", snapshot.DetailedLabel);
        }
    }

    [TestFixture]
    public class PiercerReferenceDashPreviewSamplerTests
    {
        [Test]
        public void SampleOffset_ReturnsZero_WhenPhaseIsNotDashing()
        {
            var sampler = new PiercerReferenceDashPreviewSampler();
            var snapshot = new PiercerReferencePhaseSnapshot(PiercerReferencePhase.Anticipation, 0.5f, 0.1f, 1.1f, 2.2f, string.Empty);

            Vector3 offset = sampler.SampleOffset(snapshot, Vector3.right, 2f);

            Assert.That(offset, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void SampleOffset_ReturnsFullDistance_AtEndOfDashingWindow()
        {
            var sampler = new PiercerReferenceDashPreviewSampler();
            var snapshot = new PiercerReferencePhaseSnapshot(PiercerReferencePhase.Dashing, 1f, 0f, 1.65f, 2.2f, string.Empty);

            Vector3 offset = sampler.SampleOffset(snapshot, Vector3.right, 2f);

            Assert.AreEqual(2f, offset.x, 0.001f);
            Assert.AreEqual(0f, offset.y, 0.001f);
            Assert.AreEqual(0f, offset.z, 0.001f);
        }

        [Test]
        public void SampleOffset_ClampsNegativeDistanceToZero()
        {
            var sampler = new PiercerReferenceDashPreviewSampler();
            var snapshot = new PiercerReferencePhaseSnapshot(PiercerReferencePhase.Dashing, 1f, 0f, 1.65f, 2.2f, string.Empty);

            Vector3 offset = sampler.SampleOffset(snapshot, Vector3.right, -2f);

            Assert.That(offset, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ResolveDirection_ReturnsLocalDirectionTransformedByOwner_WhenUseLocalDirectionIsEnabled()
        {
            var sampler = new PiercerReferenceDashPreviewSampler();
            Quaternion ownerRotation = Quaternion.Euler(0f, 0f, 90f);

            Vector3 direction = sampler.ResolveDirection(Vector3.right, ownerRotation, useLocalDirection: true);

            Assert.AreEqual(0f, direction.x, 0.001f);
            Assert.AreEqual(1f, direction.y, 0.001f);
            Assert.AreEqual(0f, direction.z, 0.001f);
        }

        [Test]
        public void ResolveDirection_ReturnsWorldDirectionUnchanged_WhenUseLocalDirectionIsDisabled()
        {
            var sampler = new PiercerReferenceDashPreviewSampler();
            Quaternion ownerRotation = Quaternion.Euler(0f, 0f, 90f);

            Vector3 direction = sampler.ResolveDirection(Vector3.right, ownerRotation, useLocalDirection: false);

            Assert.AreEqual(1f, direction.x, 0.001f);
            Assert.AreEqual(0f, direction.y, 0.001f);
            Assert.AreEqual(0f, direction.z, 0.001f);
        }

        [Test]
        public void FormatReadout_IncludesPhaseOffsetAndDirectionMode()
        {
            var sampler = new PiercerReferenceDashPreviewSampler();
            var snapshot = new PiercerReferencePhaseSnapshot(PiercerReferencePhase.Dashing, 0.5f, 0.2f, 1.42f, 2.2f, "Piercer REF | Dashing");
            Vector3 offset = new Vector3(1.25f, -0.5f, 0f);

            string readout = sampler.FormatReadout(snapshot, offset, previewEnabled: true, useLocalDirection: false);

            StringAssert.Contains("Preview: ON", readout);
            StringAssert.Contains("Mode: World", readout);
            StringAssert.Contains("Phase: Dashing", readout);
            StringAssert.Contains("Progress: 50%", readout);
            StringAssert.Contains("Offset: (1.25, -0.50, 0.00)", readout);
        }
    }
}
