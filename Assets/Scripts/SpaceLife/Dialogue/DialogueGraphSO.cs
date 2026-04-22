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
