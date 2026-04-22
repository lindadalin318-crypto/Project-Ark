using System.Linq;
using NUnit.Framework;
using ProjectArk.Core.Save;
using ProjectArk.SpaceLife.Dialogue;

namespace ProjectArk.SpaceLife.Tests
{
    [TestFixture]
    public class DialogueFlagStoreTests
    {
        [Test]
        public void Set_WritesNewFlag()
        {
            var saveData = new PlayerSaveData();
            var flagStore = new DialogueFlagStore(saveData);

            flagStore.Set("met_engineer");

            Assert.IsTrue(flagStore.Get("met_engineer"));
            Assert.AreEqual(1, saveData.Progress.Flags.Count);
        }

        [Test]
        public void Set_OverwritesExistingKeyWithoutDuplicating()
        {
            var saveData = new PlayerSaveData();
            saveData.Progress.Flags.Add(new SaveFlag("met_engineer", false));
            var flagStore = new DialogueFlagStore(saveData);

            flagStore.Set("met_engineer");

            Assert.IsTrue(flagStore.Get("met_engineer"));
            Assert.AreEqual(1, saveData.Progress.Flags.Count(flag => flag.Key == "met_engineer"));
        }

        [Test]
        public void Clear_RemovesFlagAndReadsBackFalse()
        {
            var saveData = new PlayerSaveData();
            var flagStore = new DialogueFlagStore(saveData);
            flagStore.Set("met_engineer");

            flagStore.Clear("met_engineer");

            Assert.IsFalse(flagStore.Get("met_engineer"));
            Assert.AreEqual(0, saveData.Progress.Flags.Count(flag => flag.Key == "met_engineer"));
        }
    }
}
