using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Pure dialogue state machine that resolves entries, choices, and service exits.
    /// </summary>
    public sealed class DialogueRunner
    {
        private readonly DialogueDatabaseSO _database;
        private readonly DialogueEffectExecutor _effectExecutor;

        private DialogueGraphSO _currentGraph;
        private DialogueContext _currentContext;
        private DialogueNodeData _currentNodeData;

        public DialogueRunner(DialogueDatabaseSO database, DialogueFlagStore flagStore)
        {
            _database = database;
            _effectExecutor = new DialogueEffectExecutor(flagStore);
        }

        public DialogueNodeViewModel CurrentNode { get; private set; }
        public DialogueContext CurrentContext => _currentContext;

        public DialogueNodeViewModel StartDialogue(string ownerId, DialogueContext context)
        {
            CurrentNode = null;
            _currentNodeData = null;
            _currentGraph = null;
            _currentContext = context;

            if (_database == null)
            {
                Debug.LogError("[DialogueRunner] Dialogue database is not assigned.");
                return null;
            }

            if (context == null)
            {
                Debug.LogError("[DialogueRunner] Dialogue context is null.");
                return null;
            }

            if (!_database.TryGetGraphByOwnerId(ownerId, out _currentGraph) || _currentGraph == null)
            {
                Debug.LogError($"[DialogueRunner] Dialogue graph not found for ownerId: {ownerId}");
                return null;
            }

            string entryNodeId = ResolveEntryNodeId(_currentGraph, context);
            if (string.IsNullOrWhiteSpace(entryNodeId))
            {
                Debug.LogError($"[DialogueRunner] Failed to resolve entry node for ownerId: {ownerId}");
                return null;
            }

            if (!TryMoveToNode(entryNodeId))
            {
                return null;
            }

            return CurrentNode;
        }

        public DialogueServiceExit Choose(string choiceId)
        {
            if (_currentNodeData == null || _currentContext == null)
            {
                Debug.LogError("[DialogueRunner] Cannot choose without an active dialogue session.");
                return DialogueServiceExit.None;
            }

            DialogueChoiceData selectedChoice = GetVisibleChoiceById(choiceId);
            if (selectedChoice == null)
            {
                Debug.LogError($"[DialogueRunner] Current node '{_currentNodeData.NodeId}' does not contain a visible choice '{choiceId}'.");
                return DialogueServiceExit.None;
            }

            DialogueServiceExit effectExit = _effectExecutor.Execute(selectedChoice.Effects, _currentContext);
            DialogueServiceExit result = effectExit.HasExit || effectExit.ShouldEndDialogue
                ? effectExit
                : CreateExitFromChoice(selectedChoice);

            if (!string.IsNullOrWhiteSpace(selectedChoice.NextNodeId))
            {
                TryMoveToNode(selectedChoice.NextNodeId);
                return result;
            }

            if (result.HasExit || result.ShouldEndDialogue)
            {
                EndDialogue();
                return result;
            }

            EndDialogue();
            return new DialogueServiceExit(DialogueServiceExitType.None, string.Empty, shouldEndDialogue: true);
        }

        public DialogueServiceExit Continue()
        {
            if (_currentNodeData == null || _currentContext == null)
            {
                Debug.LogError("[DialogueRunner] Cannot continue without an active dialogue session.");
                return DialogueServiceExit.None;
            }

            IReadOnlyList<DialogueChoiceData> choices = _currentNodeData.Choices;
            if (choices != null && choices.Count > 0)
            {
                Debug.LogError($"[DialogueRunner] Node '{_currentNodeData.NodeId}' still has visible choices and cannot use Continue().");
                return DialogueServiceExit.None;
            }

            if (!string.IsNullOrWhiteSpace(_currentNodeData.NextNodeId))
            {
                return TryMoveToNode(_currentNodeData.NextNodeId)
                    ? DialogueServiceExit.None
                    : new DialogueServiceExit(DialogueServiceExitType.None, string.Empty, shouldEndDialogue: true);
            }

            EndDialogue();
            return new DialogueServiceExit(DialogueServiceExitType.None, string.Empty, shouldEndDialogue: true);
        }

        private string ResolveEntryNodeId(DialogueGraphSO graph, DialogueContext context)
        {
            DialogueEntryRuleData bestRule = null;
            IReadOnlyList<DialogueEntryRuleData> rules = graph.EntryRules;

            if (rules != null)
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    DialogueEntryRuleData rule = rules[i];
                    if (rule == null)
                    {
                        continue;
                    }

                    if (!DialogueConditionEvaluator.AreAllSatisfied(rule.Conditions, context))
                    {
                        continue;
                    }

                    if (bestRule == null || rule.Priority > bestRule.Priority)
                    {
                        bestRule = rule;
                    }
                }
            }

            if (bestRule != null)
            {
                return bestRule.EntryNodeId;
            }

            IReadOnlyList<DialogueNodeData> nodes = graph.Nodes;
            return nodes != null && nodes.Count > 0 ? nodes[0].NodeId : string.Empty;
        }

        private bool TryMoveToNode(string nodeId)
        {
            if (_currentGraph == null)
            {
                Debug.LogError("[DialogueRunner] Cannot move node without an active graph.");
                return false;
            }

            if (!_currentGraph.TryGetNodeById(nodeId, out DialogueNodeData node) || node == null)
            {
                Debug.LogError($"[DialogueRunner] Graph '{_currentGraph.GraphId}' is missing node '{nodeId}'.");
                return false;
            }

            _currentNodeData = node;
            CurrentNode = BuildViewModel(node);
            return true;
        }

        private DialogueNodeViewModel BuildViewModel(DialogueNodeData node)
        {
            var visibleChoices = new List<DialogueNodeViewModel.ChoiceViewModel>();
            IReadOnlyList<DialogueChoiceData> choices = node.Choices;
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    DialogueChoiceData choice = choices[i];
                    if (choice == null)
                    {
                        continue;
                    }

                    if (!DialogueConditionEvaluator.AreAllSatisfied(choice.Conditions, _currentContext))
                    {
                        continue;
                    }

                    visibleChoices.Add(new DialogueNodeViewModel.ChoiceViewModel(
                        choice.ChoiceId,
                        choice.RawText,
                        choice.ExitType,
                        choice.ExitPayload));
                }
            }

            return new DialogueNodeViewModel(
                _currentContext?.OwnerId,
                node.NodeId,
                node.NodeType,
                node.SpeakerName,
                node.RawText,
                visibleChoices);
        }

        private DialogueChoiceData GetVisibleChoiceById(string choiceId)
        {
            IReadOnlyList<DialogueChoiceData> choices = _currentNodeData.Choices;
            if (choices == null)
            {
                return null;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                DialogueChoiceData choice = choices[i];
                if (choice == null)
                {
                    continue;
                }

                if (!string.Equals(choice.ChoiceId, choiceId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (!DialogueConditionEvaluator.AreAllSatisfied(choice.Conditions, _currentContext))
                {
                    return null;
                }

                return choice;
            }

            return null;
        }

        private void EndDialogue()
        {
            CurrentNode = null;
            _currentNodeData = null;
            _currentGraph = null;
        }

        private static DialogueServiceExit CreateExitFromChoice(DialogueChoiceData choice)
        {
            if (choice.ExitType == DialogueServiceExitType.None)
            {
                return DialogueServiceExit.None;
            }

            return new DialogueServiceExit(choice.ExitType, choice.ExitPayload);
        }
    }
}
