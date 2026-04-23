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

        // ------------------------------------------------------------
        // ValidateDatabase (Master Plan §5.2 P3 — authoring-time aggregate validator)
        // ------------------------------------------------------------

        [Test]
        public void ValidateDatabase_ReturnsEmpty_WhenDatabaseIsFullyValid()
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new[] { CreateEntryRule(entryNodeId: "intro") },
                nodes: new[] { CreateNode(nodeId: "intro") }));

            List<string> errors = database.ValidateDatabase();

            Assert.IsNotNull(errors);
            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void ValidateDatabase_ReportsError_WhenGraphHasNoEntryRules()
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: System.Array.Empty<DialogueEntryRuleData>(),
                nodes: new[] { CreateNode(nodeId: "intro") }));

            List<string> errors = database.ValidateDatabase();

            CollectionAssert.Contains(errors, "[DialogueGraphSO] Graph 'graph_engineer' must declare at least one EntryRule.");
        }

        [Test]
        public void ValidateDatabase_ReportsError_WhenEntryRuleTargetsMissingNode()
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new[] { CreateEntryRule(entryNodeId: "ghost_node") },
                nodes: new[] { CreateNode(nodeId: "intro") }));

            List<string> errors = database.ValidateDatabase();

            CollectionAssert.Contains(errors, "[DialogueGraphSO] Graph 'graph_engineer' EntryRule[0] references missing NodeId: ghost_node.");
        }

        [Test]
        public void ValidateDatabase_ReportsError_WhenNodeNextNodeIdReferencesMissingNode()
        {
            DialogueNodeData danglingNode = CreateNode(nodeId: "intro");
            SetPrivateField(danglingNode, "_nextNodeId", "missing_target");

            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new[] { CreateEntryRule(entryNodeId: "intro") },
                nodes: new[] { danglingNode }));

            List<string> errors = database.ValidateDatabase();

            CollectionAssert.Contains(errors, "[DialogueGraphSO] Graph 'graph_engineer' Node 'intro'.NextNodeId references missing NodeId: missing_target.");
        }

        [Test]
        public void ValidateDatabase_ReportsError_WhenChoiceNextNodeIdReferencesMissingNode()
        {
            DialogueNodeData intro = CreateNode(nodeId: "intro");
            DialogueChoiceData choice = CreateChoice(choiceId: "choice_a", nextNodeId: "missing_branch");
            SetPrivateField(intro, "_choices", new List<DialogueChoiceData> { choice });

            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new[] { CreateEntryRule(entryNodeId: "intro") },
                nodes: new[] { intro }));

            List<string> errors = database.ValidateDatabase();

            CollectionAssert.Contains(errors, "[DialogueGraphSO] Graph 'graph_engineer' Node 'intro' Choice 'choice_a'.NextNodeId references missing NodeId: missing_branch.");
        }

        [Test]
        public void ValidateDatabase_ReportsError_WhenGraphIdIsDuplicated()
        {
            var database = CreateDatabase(
                CreateGraph(graphId: "graph_dup", ownerId: "owner_a",
                    entryRules: new[] { CreateEntryRule("intro_a") },
                    nodes: new[] { CreateNode(nodeId: "intro_a") }),
                CreateGraph(graphId: "graph_dup", ownerId: "owner_b",
                    entryRules: new[] { CreateEntryRule("intro_b") },
                    nodes: new[] { CreateNode(nodeId: "intro_b") }));

            List<string> errors = database.ValidateDatabase();

            CollectionAssert.Contains(errors, "[DialogueDatabaseSO] Duplicate GraphId detected: graph_dup.");
        }

        [Test]
        public void ValidateDatabase_ReportsError_WhenOwnerIdIsDuplicatedAcrossGraphs()
        {
            var database = CreateDatabase(
                CreateGraph(graphId: "graph_a", ownerId: "engineer_hub",
                    entryRules: new[] { CreateEntryRule("intro_a") },
                    nodes: new[] { CreateNode(nodeId: "intro_a") }),
                CreateGraph(graphId: "graph_b", ownerId: "engineer_hub",
                    entryRules: new[] { CreateEntryRule("intro_b") },
                    nodes: new[] { CreateNode(nodeId: "intro_b") }));

            List<string> errors = database.ValidateDatabase();

            CollectionAssert.Contains(errors, "[DialogueDatabaseSO] Duplicate OwnerId detected: engineer_hub.");
        }

        // ------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------

        private DialogueDatabaseSO CreateDatabase(params DialogueGraphSO[] graphs)
        {
            var database = ScriptableObject.CreateInstance<DialogueDatabaseSO>();
            _createdObjects.Add(database);
            SetPrivateField(database, "_graphs", new List<DialogueGraphSO>(graphs));
            return database;
        }

        private DialogueGraphSO CreateGraph(string graphId, string ownerId, params DialogueNodeData[] nodes)
        {
            return CreateGraph(graphId, ownerId, System.Array.Empty<DialogueEntryRuleData>(), nodes);
        }

        private DialogueGraphSO CreateGraph(
            string graphId,
            string ownerId,
            DialogueEntryRuleData[] entryRules,
            DialogueNodeData[] nodes)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraphSO>();
            _createdObjects.Add(graph);

            SetPrivateField(graph, "_graphId", graphId);
            SetPrivateField(graph, "_ownerId", ownerId);
            SetPrivateField(graph, "_ownerType", DialogueOwnerType.Npc);
            SetPrivateField(graph, "_entryRules", new List<DialogueEntryRuleData>(entryRules ?? System.Array.Empty<DialogueEntryRuleData>()));
            SetPrivateField(graph, "_nodes", new List<DialogueNodeData>(nodes ?? System.Array.Empty<DialogueNodeData>()));

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

        private static DialogueEntryRuleData CreateEntryRule(string entryNodeId)
        {
            var rule = new DialogueEntryRuleData();
            SetPrivateField(rule, "_entryNodeId", entryNodeId);
            SetPrivateField(rule, "_priority", 0);
            SetPrivateField(rule, "_conditions", new List<DialogueConditionData>());
            return rule;
        }

        private static DialogueChoiceData CreateChoice(string choiceId, string nextNodeId)
        {
            var choice = new DialogueChoiceData();
            SetPrivateField(choice, "_choiceId", choiceId);
            SetPrivateField(choice, "_rawText", "placeholder_choice");
            SetPrivateField(choice, "_conditions", new List<DialogueConditionData>());
            SetPrivateField(choice, "_nextNodeId", nextNodeId);
            SetPrivateField(choice, "_effects", new List<DialogueEffectData>());
            SetPrivateField(choice, "_exitType", DialogueServiceExitType.None);
            SetPrivateField(choice, "_exitPayload", string.Empty);
            return choice;
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
