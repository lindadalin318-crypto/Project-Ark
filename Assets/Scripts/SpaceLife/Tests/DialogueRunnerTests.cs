using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectArk.Core.Save;
using ProjectArk.SpaceLife.Dialogue;
using UnityEngine;

namespace ProjectArk.SpaceLife.Tests
{
    [TestFixture]
    public class DialogueRunnerTests
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
        public void StartDialogue_SelectsDifferentEntry_WhenWorldStageChanges()
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new List<DialogueEntryRuleData>
                {
                    CreateEntryRule("default_intro", priority: 0),
                    CreateEntryRule("stage_one_intro", priority: 10,
                        CreateCondition(DialogueConditionType.WorldStage, DialogueCompareOp.GreaterOrEqual, 1))
                },
                nodes: new List<DialogueNodeData>
                {
                    CreateNode("default_intro", "Stage 0"),
                    CreateNode("stage_one_intro", "Stage 1")
                }));

            var runner = new DialogueRunner(database, new DialogueFlagStore(new PlayerSaveData()));

            DialogueNodeViewModel stageZeroNode = runner.StartDialogue("engineer_hub", CreateContext("engineer_hub", worldStage: 0));
            DialogueNodeViewModel stageOneNode = runner.StartDialogue("engineer_hub", CreateContext("engineer_hub", worldStage: 1));

            Assert.AreEqual("Stage 0", stageZeroNode.Text);
            Assert.AreEqual("Stage 1", stageOneNode.Text);
        }

        [Test]
        public void StartDialogue_HidesChoice_WhenRelationshipRequirementIsNotMet()
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new List<DialogueEntryRuleData>
                {
                    CreateEntryRule("intro", priority: 0)
                },
                nodes: new List<DialogueNodeData>
                {
                    CreateNode(
                        nodeId: "intro",
                        text: "Intro",
                        choices: new List<DialogueChoiceData>
                        {
                            CreateChoice("ask_basic", "Basic"),
                            CreateChoice(
                                "ask_private",
                                "Private",
                                conditions: new List<DialogueConditionData>
                                {
                                    CreateCondition(DialogueConditionType.RelationshipValue, DialogueCompareOp.GreaterOrEqual, 50)
                                })
                        })
                }));

            var runner = new DialogueRunner(database, new DialogueFlagStore(new PlayerSaveData()));
            DialogueNodeViewModel currentNode = runner.StartDialogue("engineer_hub", CreateContext("engineer_hub", relationshipValue: 20));

            Assert.AreEqual(1, currentNode.Choices.Count);
            Assert.AreEqual("ask_basic", currentNode.Choices[0].ChoiceId);
        }

        [Test]
        public void Choose_MovesToNextNode_WhenChoiceHasNextNodeId()
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new List<DialogueEntryRuleData>
                {
                    CreateEntryRule("intro", priority: 0)
                },
                nodes: new List<DialogueNodeData>
                {
                    CreateNode(
                        nodeId: "intro",
                        text: "Intro",
                        choices: new List<DialogueChoiceData>
                        {
                            CreateChoice("ask_more", "Tell me more", nextNodeId: "followup")
                        }),
                    CreateNode("followup", "Followup")
                }));

            var runner = new DialogueRunner(database, new DialogueFlagStore(new PlayerSaveData()));
            runner.StartDialogue("engineer_hub", CreateContext("engineer_hub"));

            DialogueServiceExit result = runner.Choose("ask_more");

            Assert.AreEqual(DialogueServiceExitType.None, result.ExitType);
            Assert.IsNotNull(runner.CurrentNode);
            Assert.AreEqual("followup", runner.CurrentNode.NodeId);
            Assert.AreEqual("Followup", runner.CurrentNode.Text);
        }

        [TestCase(DialogueServiceExitType.OpenGift)]
        [TestCase(DialogueServiceExitType.OpenIntel)]
        public void Choose_ReturnsServiceExit_WhenChoiceDeclaresExit(DialogueServiceExitType exitType)
        {
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new List<DialogueEntryRuleData>
                {
                    CreateEntryRule("intro", priority: 0)
                },
                nodes: new List<DialogueNodeData>
                {
                    CreateNode(
                        nodeId: "intro",
                        text: "Intro",
                        choices: new List<DialogueChoiceData>
                        {
                            CreateChoice("service", "Open service", exitType: exitType, exitPayload: "payload")
                        })
                }));

            var runner = new DialogueRunner(database, new DialogueFlagStore(new PlayerSaveData()));
            runner.StartDialogue("engineer_hub", CreateContext("engineer_hub"));

            DialogueServiceExit result = runner.Choose("service");

            Assert.AreEqual(exitType, result.ExitType);
            Assert.AreEqual("payload", result.Payload);
        }

        [Test]
        public void Choose_SetFlagChangesLaterEntrySelection_WhenDialogueStartsAgain()
        {
            var saveData = new PlayerSaveData();
            var flagStore = new DialogueFlagStore(saveData);
            var database = CreateDatabase(CreateGraph(
                graphId: "graph_engineer",
                ownerId: "engineer_hub",
                entryRules: new List<DialogueEntryRuleData>
                {
                    CreateEntryRule("default_intro", priority: 0),
                    CreateEntryRule("followup_intro", priority: 10,
                        CreateCondition(DialogueConditionType.FlagPresent, DialogueCompareOp.Equal, 0, "met_engineer"))
                },
                nodes: new List<DialogueNodeData>
                {
                    CreateNode(
                        nodeId: "default_intro",
                        text: "First time",
                        choices: new List<DialogueChoiceData>
                        {
                            CreateChoice(
                                "mark_met",
                                "Mark met",
                                effects: new List<DialogueEffectData>
                                {
                                    CreateEffect(DialogueEffectType.SetFlag, flagKey: "met_engineer"),
                                    CreateEffect(DialogueEffectType.EndDialogue)
                                })
                        }),
                    CreateNode("followup_intro", "Welcome back")
                }));

            var runner = new DialogueRunner(database, flagStore);
            DialogueNodeViewModel firstNode = runner.StartDialogue("engineer_hub", CreateContext("engineer_hub", flagStore: flagStore));
            runner.Choose("mark_met");
            DialogueNodeViewModel secondNode = runner.StartDialogue("engineer_hub", CreateContext("engineer_hub", flagStore: flagStore));

            Assert.AreEqual("First time", firstNode.Text);
            Assert.AreEqual("Welcome back", secondNode.Text);
        }

        private DialogueContext CreateContext(string ownerId, int worldStage = 0, int relationshipValue = 0, DialogueFlagStore flagStore = null)
        {
            return new DialogueContext(ownerId, worldStage, relationshipValue, flagStore?.GetActiveFlagKeys());
        }

        private DialogueDatabaseSO CreateDatabase(params DialogueGraphSO[] graphs)
        {
            var database = ScriptableObject.CreateInstance<DialogueDatabaseSO>();
            _createdObjects.Add(database);
            SetPrivateField(database, "_graphs", new List<DialogueGraphSO>(graphs));
            database.InvalidateCache();
            return database;
        }

        private DialogueGraphSO CreateGraph(string graphId, string ownerId, List<DialogueEntryRuleData> entryRules, List<DialogueNodeData> nodes)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraphSO>();
            _createdObjects.Add(graph);

            SetPrivateField(graph, "_graphId", graphId);
            SetPrivateField(graph, "_ownerId", ownerId);
            SetPrivateField(graph, "_ownerType", DialogueOwnerType.Npc);
            SetPrivateField(graph, "_entryRules", entryRules);
            SetPrivateField(graph, "_nodes", nodes);

            return graph;
        }

        private static DialogueEntryRuleData CreateEntryRule(string entryNodeId, int priority, params DialogueConditionData[] conditions)
        {
            var entryRule = new DialogueEntryRuleData();
            SetPrivateField(entryRule, "_entryNodeId", entryNodeId);
            SetPrivateField(entryRule, "_priority", priority);
            SetPrivateField(entryRule, "_conditions", new List<DialogueConditionData>(conditions));
            return entryRule;
        }

        private static DialogueNodeData CreateNode(
            string nodeId,
            string text,
            string nextNodeId = "",
            List<DialogueChoiceData> choices = null,
            List<DialogueEffectData> effects = null)
        {
            var node = new DialogueNodeData();
            SetPrivateField(node, "_nodeId", nodeId);
            SetPrivateField(node, "_nodeType", DialogueNodeType.Line);
            SetPrivateField(node, "_speakerName", "Engineer");
            SetPrivateField(node, "_rawText", text);
            SetPrivateField(node, "_nextNodeId", nextNodeId);
            SetPrivateField(node, "_choices", choices ?? new List<DialogueChoiceData>());
            SetPrivateField(node, "_effects", effects ?? new List<DialogueEffectData>());
            return node;
        }

        private static DialogueChoiceData CreateChoice(
            string choiceId,
            string text,
            string nextNodeId = "",
            List<DialogueConditionData> conditions = null,
            List<DialogueEffectData> effects = null,
            DialogueServiceExitType exitType = DialogueServiceExitType.None,
            string exitPayload = "")
        {
            var choice = new DialogueChoiceData();
            SetPrivateField(choice, "_choiceId", choiceId);
            SetPrivateField(choice, "_rawText", text);
            SetPrivateField(choice, "_conditions", conditions ?? new List<DialogueConditionData>());
            SetPrivateField(choice, "_nextNodeId", nextNodeId);
            SetPrivateField(choice, "_effects", effects ?? new List<DialogueEffectData>());
            SetPrivateField(choice, "_exitType", exitType);
            SetPrivateField(choice, "_exitPayload", exitPayload);
            return choice;
        }

        private static DialogueConditionData CreateCondition(
            DialogueConditionType type,
            DialogueCompareOp compareOp,
            int intValue,
            string flagKey = "")
        {
            var condition = new DialogueConditionData();
            SetPrivateField(condition, "_conditionType", type);
            SetPrivateField(condition, "_compareOp", compareOp);
            SetPrivateField(condition, "_intValue", intValue);
            SetPrivateField(condition, "_flagKey", flagKey);
            return condition;
        }

        private static DialogueEffectData CreateEffect(
            DialogueEffectType effectType,
            int intValue = 0,
            string flagKey = "",
            DialogueServiceExitType serviceExitType = DialogueServiceExitType.None,
            string payload = "")
        {
            var effect = new DialogueEffectData();
            SetPrivateField(effect, "_effectType", effectType);
            SetPrivateField(effect, "_intValue", intValue);
            SetPrivateField(effect, "_flagKey", flagKey);
            SetPrivateField(effect, "_serviceExitType", serviceExitType);
            SetPrivateField(effect, "_payload", payload);
            return effect;
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
