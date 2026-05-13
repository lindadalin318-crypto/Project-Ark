#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaAuditorTests
    {
        [Test]
        public void RunAudit_ReportsNoErrorsForIsolatedReplicaLane()
        {
            GGReplicaPrefabBuilder.BuildExperimentalPrefab();
            GGReplicaTestSceneBuilder.BuildTestScene();

            var results = GGReplicaAuditor.RunAudit(logToConsole: false);

            Assert.That(results.Any(result => result.Severity == GGReplicaAuditor.Severity.Error), Is.False);
            Assert.That(results.Any(result => result.Message.Contains("Live Ship.prefab has GGReplica")), Is.False);
            Assert.That(results.Any(result => result.Message.Contains("SampleScene")), Is.False);
        }
    }
}
#endif
