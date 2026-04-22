using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectArk.Core.Save;
using ProjectArk.SpaceLife.Data;
using UnityEngine;

namespace ProjectArk.SpaceLife.Tests
{
    [TestFixture]
    public class RelationshipManagerPersistenceTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void GetRelationship_ReturnsStartingRelationship_WhenNoSavedValueExists()
        {
            RelationshipManager relationshipManager = CreateRelationshipManager();
            NPCDataSO npcData = CreateNpcData("engineer_hub", startingRelationship: 35);

            relationshipManager.LoadFromSaveData(new PlayerSaveData());

            Assert.AreEqual(35, relationshipManager.GetRelationship(npcData));
        }

        [Test]
        public void LoadFromSaveData_UsesSavedRelationshipValue_ForNpcId()
        {
            RelationshipManager relationshipManager = CreateRelationshipManager();
            NPCDataSO npcData = CreateNpcData("engineer_hub", startingRelationship: 10);
            var saveData = new PlayerSaveData();
            saveData.Progress.RelationshipValues.Add(new RelationshipValueSaveData("engineer_hub", 72));

            relationshipManager.LoadFromSaveData(saveData);

            Assert.AreEqual(72, relationshipManager.GetRelationship(npcData));
        }

        [Test]
        public void ChangeRelationship_WritesUpdatedValue_WhenSavedBackToSaveData()
        {
            RelationshipManager relationshipManager = CreateRelationshipManager();
            NPCDataSO npcData = CreateNpcData("engineer_hub", startingRelationship: 10);
            var saveData = new PlayerSaveData();
            saveData.Progress.RelationshipValues.Add(new RelationshipValueSaveData("engineer_hub", 20));

            relationshipManager.LoadFromSaveData(saveData);
            relationshipManager.ChangeRelationship(npcData, 15);

            var roundTrip = new PlayerSaveData();
            relationshipManager.SaveToSaveData(roundTrip);

            Assert.AreEqual(35, relationshipManager.GetRelationship(npcData));
            Assert.AreEqual(1, roundTrip.Progress.RelationshipValues.Count);
            Assert.AreEqual("engineer_hub", roundTrip.Progress.RelationshipValues[0].NpcId);
            Assert.AreEqual(35, roundTrip.Progress.RelationshipValues[0].Value);
        }

        private RelationshipManager CreateRelationshipManager()
        {
            var gameObject = new GameObject("RelationshipManager_Test");
            _createdObjects.Add(gameObject);
            var relationshipManager = gameObject.AddComponent<RelationshipManager>();
            SetPrivateField(relationshipManager, "_saveSlot", 97);
            return relationshipManager;
        }

        private NPCDataSO CreateNpcData(string npcId, int startingRelationship)
        {
            var npcData = ScriptableObject.CreateInstance<NPCDataSO>();
            _createdObjects.Add(npcData);
            SetPrivateField(npcData, "_npcId", npcId);
            SetPrivateField(npcData, "_npcName", "Engineer");
            SetPrivateField(npcData, "_startingRelationship", startingRelationship);
            return npcData;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var currentType = target.GetType();
            while (currentType != null)
            {
                FieldInfo field = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                currentType = currentType.BaseType;
            }

            Assert.Fail($"Field '{fieldName}' not found on {target.GetType().Name}.");
        }
    }
}
