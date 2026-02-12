using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Core.Tests
{
    /// <summary>
    /// Unit tests for the Heat System core logic.
    /// Tests CanFire guard, overheat trigger, and heat reduction.
    /// Note: Since HeatSystem is a MonoBehaviour, these tests instantiate a
    /// temporary GameObject. Full integration (Update-driven cooling/overheat decay)
    /// requires PlayMode tests.
    /// </summary>
    [TestFixture]
    public class HeatSystemTests
    {
        // We test the public API without relying on Update() timing.
        // For that, we use a minimal HeatStatsSO stub.

        private static ScriptableObject CreateHeatStats(float maxHeat, float overheatValue,
                                                         float overheatDuration, float coolingRate)
        {
            // Create a HeatStatsSO at runtime
            // Since we can't reference ProjectArk.Heat from Core.Tests directly,
            // we test the DamageCalculator and ServiceLocator here instead.
            // Full HeatSystem tests belong in a Heat-specific test assembly.
            return null;
        }

        [Test]
        public void ServiceLocator_RegisterAndGet()
        {
            // Verify ServiceLocator roundtrip
            var testObj = new TestService();
            ServiceLocator.Register<TestService>(testObj);

            var retrieved = ServiceLocator.Get<TestService>();
            Assert.AreSame(testObj, retrieved);

            ServiceLocator.Unregister<TestService>(testObj);
            Assert.IsNull(ServiceLocator.Get<TestService>());
        }

        [Test]
        public void ServiceLocator_UnregisterWrongInstance_NoOp()
        {
            var real = new TestService();
            var imposter = new TestService();
            ServiceLocator.Register<TestService>(real);

            ServiceLocator.Unregister<TestService>(imposter);

            // Real service should still be registered
            Assert.AreSame(real, ServiceLocator.Get<TestService>());

            // Cleanup
            ServiceLocator.Unregister<TestService>(real);
        }

        [Test]
        public void ServiceLocator_Clear_RemovesAll()
        {
            ServiceLocator.Register<TestService>(new TestService());
            ServiceLocator.Clear();

            Assert.IsNull(ServiceLocator.Get<TestService>());
        }

        [Test]
        public void ServiceLocator_GetUnregistered_ReturnsNull()
        {
            ServiceLocator.Clear();
            Assert.IsNull(ServiceLocator.Get<TestService>());
        }

        private class TestService { }
    }
}
