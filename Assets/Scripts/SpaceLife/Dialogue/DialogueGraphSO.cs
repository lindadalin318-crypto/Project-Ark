using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// ScriptableObject-authored dialogue graph for a single owner.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueGraph", menuName = "Project Ark/Space Life/Dialogue Graph")]
    public class DialogueGraphSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _graphId;
        [SerializeField] private string _ownerId;
        [SerializeField] private DialogueOwnerType _ownerType = DialogueOwnerType.Npc;

        [Header("Content")]
        [SerializeField] private List<DialogueEntryRuleData> _entryRules = new();
        [SerializeField] private List<DialogueNodeData> _nodes = new();

        private Dictionary<string, DialogueNodeData> _nodeById;

        public string GraphId => _graphId;
        public string OwnerId => _ownerId;
        public DialogueOwnerType OwnerType => _ownerType;
        public IReadOnlyList<DialogueEntryRuleData> EntryRules => _entryRules;
        public IReadOnlyList<DialogueNodeData> Nodes => _nodes;

        public bool TryGetNodeById(string nodeId, out DialogueNodeData node)
        {
            node = null;
            if (!TryBuildNodeCache(out _))
            {
                return false;
            }

            return _nodeById.TryGetValue(nodeId, out node);
        }

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_graphId))
            {
                error = "[DialogueGraphSO] Graph is missing GraphId.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_ownerId))
            {
                error = $"[DialogueGraphSO] Graph '{_graphId}' is missing OwnerId.";
                return false;
            }

            if (_nodes == null)
            {
                error = $"[DialogueGraphSO] Graph '{_graphId}' has a null node list.";
                return false;
            }

            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _nodes.Count; i++)
            {
                DialogueNodeData node = _nodes[i];
                if (node == null)
                {
                    error = $"[DialogueGraphSO] Graph '{_graphId}' contains a null node entry.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    error = $"[DialogueGraphSO] Graph '{_graphId}' contains a node with missing NodeId.";
                    return false;
                }

                if (!nodeIds.Add(node.NodeId))
                {
                    error = $"[DialogueGraphSO] Graph '{_graphId}' contains duplicate NodeId: {node.NodeId}.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Aggregating authoring-time validator. Collects ALL errors (does not fail-fast) and returns
        /// the total count. Intended for <c>DialogueDatabaseSO.ValidateDatabase</c> and the Editor
        /// <c>ProjectArk &gt; Validate Dialogue Database</c> menu.
        /// Covers: identity (GraphId/OwnerId), node identity (NodeId presence, uniqueness),
        /// entry rule presence &amp; target existence, Choice.NextNodeId target existence,
        /// Node.NextNodeId target existence.
        /// </summary>
        public int ValidateAll(List<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            int startCount = errors.Count;
            string graphLabel = string.IsNullOrWhiteSpace(_graphId) ? name : _graphId;

            if (string.IsNullOrWhiteSpace(_graphId))
            {
                errors.Add($"[DialogueGraphSO] '{name}' is missing GraphId.");
            }

            if (string.IsNullOrWhiteSpace(_ownerId))
            {
                errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' is missing OwnerId.");
            }

            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            if (_nodes == null || _nodes.Count == 0)
            {
                errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' has no nodes.");
            }
            else
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    DialogueNodeData node = _nodes[i];
                    if (node == null)
                    {
                        errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' has a null node at index {i}.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(node.NodeId))
                    {
                        errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' has a node with missing NodeId at index {i}.");
                        continue;
                    }

                    if (!nodeIds.Add(node.NodeId))
                    {
                        errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' has duplicate NodeId: {node.NodeId}.");
                    }
                }
            }

            if (_entryRules == null || _entryRules.Count == 0)
            {
                errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' must declare at least one EntryRule.");
            }
            else
            {
                for (int i = 0; i < _entryRules.Count; i++)
                {
                    DialogueEntryRuleData rule = _entryRules[i];
                    if (rule == null)
                    {
                        errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' has a null EntryRule at index {i}.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(rule.EntryNodeId))
                    {
                        errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' EntryRule[{i}] is missing EntryNodeId.");
                        continue;
                    }

                    if (!nodeIds.Contains(rule.EntryNodeId))
                    {
                        errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' EntryRule[{i}] references missing NodeId: {rule.EntryNodeId}.");
                    }
                }
            }

            if (_nodes != null)
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    DialogueNodeData node = _nodes[i];
                    if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(node.NextNodeId) && !nodeIds.Contains(node.NextNodeId))
                    {
                        errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' Node '{node.NodeId}'.NextNodeId references missing NodeId: {node.NextNodeId}.");
                    }

                    IReadOnlyList<DialogueChoiceData> choices = node.Choices;
                    if (choices == null)
                    {
                        continue;
                    }

                    for (int c = 0; c < choices.Count; c++)
                    {
                        DialogueChoiceData choice = choices[c];
                        if (choice == null)
                        {
                            errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' Node '{node.NodeId}' has a null Choice at index {c}.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(choice.ChoiceId))
                        {
                            errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' Node '{node.NodeId}' Choice[{c}] is missing ChoiceId.");
                        }

                        if (!string.IsNullOrWhiteSpace(choice.NextNodeId) && !nodeIds.Contains(choice.NextNodeId))
                        {
                            errors.Add($"[DialogueGraphSO] Graph '{graphLabel}' Node '{node.NodeId}' Choice '{choice.ChoiceId}'.NextNodeId references missing NodeId: {choice.NextNodeId}.");
                        }
                    }
                }
            }

            return errors.Count - startCount;
        }

        private bool TryBuildNodeCache(out string error)
        {
            if (_nodeById != null)
            {
                error = null;
                return true;
            }

            if (!TryValidate(out error))
            {
                return false;
            }

            _nodeById = new Dictionary<string, DialogueNodeData>(StringComparer.Ordinal);
            for (int i = 0; i < _nodes.Count; i++)
            {
                DialogueNodeData node = _nodes[i];
                _nodeById[node.NodeId] = node;
            }

            return true;
        }

        private void OnEnable()
        {
            _nodeById = null;
        }

        private void OnValidate()
        {
            _nodeById = null;
        }
    }
}
