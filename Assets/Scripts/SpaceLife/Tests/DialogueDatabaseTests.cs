using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectArk.SpaceLife.Dialogue;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectArk.SpaceLife.Tests
{
    [TestFixture]
    public class DialogueDatabaseTests
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
        public void TryGetGraphByOwnerId_ReturnsGraph_WhenOwnerIdExists()
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                CreateNode(nodeId: "intro")));

            bool found = database.TryGetGraphByOwnerId("engineer_hub", out DialogueGraphSO graph);

            Assert.IsTrue(found);
            Assert.IsNotNull(graph);
            Assert.AreEqual("graph_engineer", graph.GraphId);
        }

        [Test]
        public void TryGetGraphByOwnerId_ReturnsFalseAndLogsError_WhenOwnerIdIsDuplicated()
        {
            var database = CreateDatabase(
                CreateGraph(graphId: "graph_a", ownerId: "engineer_hub", CreateNode(nodeId: "intro_a")),
                CreateGraph(graphId: "graph_b", ownerId: "engineer_hub", CreateNode(nodeId: "intro_b")));

            LogAssert.Expect(LogType.Error, "[DialogueDatabaseSO] Duplicate owner id detected: engineer_hub");

            bool found = database.TryGetGraphByOwnerId("engineer_hub", out DialogueGraphSO graph);

            Assert.IsFalse(found);
            Assert.IsNull(graph);
        }

        [Test]
        public void TryGetGraphByOwnerId_ReturnsFalseAndLogsError_WhenGraphContainsNodeWithoutNodeId()
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                CreateNode(nodeId: string.Empty)));

            LogAssert.Expect(LogType.Error, "[DialogueGraphSO] Graph 'graph_engineer' contains a node with missing NodeId.");

            bool found = database.TryGetGraphByOwnerId("engineer_hub", out DialogueGraphSO graph);

            Assert.IsFalse(found);
            Assert.IsNull(graph);
        }

        private DialogueDatabaseSO CreateDatabase(params DialogueGraphSO[] graphs)
        {
            var database = ScriptableObject.CreateInstance<DialogueDatabaseSO>();
            _createdObjects.Add(database);
            SetPrivateField(database, "_graphs", new List<DialogueGraphSO>(graphs));
            return database;
        }

        private DialogueGraphSO CreateGraph(string graphId, string ownerId, params DialogueNodeData[] nodes)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraphSO>();
            _createdObjects.Add(graph);

            SetPrivateField(graph, "_graphId", graphId);
            SetPrivateField(graph, "_ownerId", ownerId);
            SetPrivateField(graph, "_ownerType", DialogueOwnerType.Npc);
            SetPrivateField(graph, "_entryRules", new List<DialogueEntryRuleData>());
            SetPrivateField(graph, "_nodes", new List<DialogueNodeData>(nodes));

            return graph;
        }

        private static DialogueNodeData CreateNode(string nodeId)
        {
            var node = new DialogueNodeData();
            SetPrivateField(node, "_nodeId", nodeId);
            SetPrivateField(node, "_nodeType", DialogueNodeType.Line);
            SetPrivateField(node, "_rawText", "placeholder");
            SetPrivateField(node, "_speakerName", "Engineer");
            SetPrivateField(node, "_choices", new List<DialogueChoiceData>());
            SetPrivateField(node, "_effects", new List<DialogueEffectData>());
            return node;
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
