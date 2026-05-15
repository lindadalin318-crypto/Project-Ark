using NUnit.Framework;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Combat.Tests
{
    [TestFixture]
    public class ChargeRusherReferencePhaseResolverTests
    {
        [Test]
        public void Resolve_ReturnsIdle_WhenCurrentStateIsNotChargeState()
        {
            var phase = ChargeRusherReferencePhaseResolver.Resolve(
                currentStateName: "ChaseState",
                elapsedInState: 0.25f,
                telegraphDuration: 0.35f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(ChargeRusherReferencePhase.Idle, phase);
        }

        [Test]
        public void Resolve_ReturnsTelegraph_AtStartOfChargeState()
        {
            var phase = ChargeRusherReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 0.2f,
                telegraphDuration: 0.35f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(ChargeRusherReferencePhase.Telegraph, phase);
        }

        [Test]
        public void Resolve_ReturnsDashing_AfterTelegraphWindow()
        {
            var phase = ChargeRusherReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 0.5f,
                telegraphDuration: 0.35f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(ChargeRusherReferencePhase.Dashing, phase);
        }

        [Test]
        public void Resolve_ReturnsRecovery_AfterDashWindow()
        {
            var phase = ChargeRusherReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 0.9f,
                telegraphDuration: 0.35f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(ChargeRusherReferencePhase.Recovery, phase);
        }

        [Test]
        public void Resolve_ReturnsIdle_WhenChargeWindowHasExpired()
        {
            var phase = ChargeRusherReferencePhaseResolver.Resolve(
                currentStateName: "ChargeState",
                elapsedInState: 1.2f,
                telegraphDuration: 0.35f,
                dashDuration: 0.45f,
                recoveryDuration: 0.25f);

            Assert.AreEqual(ChargeRusherReferencePhase.Idle, phase);
        }
    }

    [TestFixture]
    public class ChargeRusherReferenceAfterimageSamplerTests
    {
        [Test]
        public void ShouldEmit_ReturnsFalse_WhenPhaseIsNotDashing()
        {
            var sampler = new ChargeRusherReferenceAfterimageSampler(0.06f);

            bool shouldEmit = sampler.ShouldEmit(ChargeRusherReferencePhase.Telegraph, 1f);

            Assert.That(shouldEmit, Is.False);
        }

        [Test]
        public void ShouldEmit_ReturnsTrue_OnFirstDashingSample()
        {
            var sampler = new ChargeRusherReferenceAfterimageSampler(0.06f);

            bool shouldEmit = sampler.ShouldEmit(ChargeRusherReferencePhase.Dashing, 1f);

            Assert.That(shouldEmit, Is.True);
        }

        [Test]
        public void ShouldEmit_ThrottlesDashingSamples_UntilIntervalExpires()
        {
            var sampler = new ChargeRusherReferenceAfterimageSampler(0.06f);

            bool first = sampler.ShouldEmit(ChargeRusherReferencePhase.Dashing, 1f);
            bool second = sampler.ShouldEmit(ChargeRusherReferencePhase.Dashing, 1.03f);
            bool third = sampler.ShouldEmit(ChargeRusherReferencePhase.Dashing, 1.061f);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(third, Is.True);
        }

        [Test]
        public void Reset_AllowsNextDashingSampleImmediately()
        {
            var sampler = new ChargeRusherReferenceAfterimageSampler(0.06f);
            sampler.ShouldEmit(ChargeRusherReferencePhase.Dashing, 1f);

            sampler.Reset();
            bool shouldEmit = sampler.ShouldEmit(ChargeRusherReferencePhase.Dashing, 1.01f);

            Assert.That(shouldEmit, Is.True);
        }
    }

    [TestFixture]
    public class ChargeRusherReferenceImpactGateTests
    {
        [Test]
        public void TryTrigger_ReturnsFalse_WhenPhaseIsNotDashing()
        {
            var gate = new ChargeRusherReferenceImpactGate();

            bool triggered = gate.TryTrigger(ChargeRusherReferencePhase.Telegraph);

            Assert.That(triggered, Is.False);
        }

        [Test]
        public void TryTrigger_ReturnsTrue_OnlyOncePerDashingPhase()
        {
            var gate = new ChargeRusherReferenceImpactGate();

            bool first = gate.TryTrigger(ChargeRusherReferencePhase.Dashing);
            bool second = gate.TryTrigger(ChargeRusherReferencePhase.Dashing);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
        }

        [Test]
        public void UpdatePhase_ResetsGate_WhenLeavingDashing()
        {
            var gate = new ChargeRusherReferenceImpactGate();
            gate.TryTrigger(ChargeRusherReferencePhase.Dashing);

            gate.UpdatePhase(ChargeRusherReferencePhase.Recovery);
            gate.UpdatePhase(ChargeRusherReferencePhase.Dashing);
            bool triggered = gate.TryTrigger(ChargeRusherReferencePhase.Dashing);

            Assert.That(triggered, Is.True);
        }
    }
}
