using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Database of dialogue graphs keyed by stable owner ids.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Project Ark/Space Life/Dialogue Database")]
    public class DialogueDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<DialogueGraphSO> _graphs = new();

        private Dictionary<string, DialogueGraphSO> _graphByOwnerId;

        public IReadOnlyList<DialogueGraphSO> Graphs => _graphs;

        public bool TryGetGraphByOwnerId(string ownerId, out DialogueGraphSO graph)
        {
            graph = null;
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                Debug.LogError("[DialogueDatabaseSO] Owner id is null or empty.", this);
                return false;
            }

            if (!TryBuildCache())
            {
                return false;
            }

            return _graphByOwnerId.TryGetValue(ownerId, out graph);
        }

        public void InvalidateCache()
        {
            _graphByOwnerId = null;
        }

        private bool TryBuildCache()
        {
            if (_graphByOwnerId != null)
            {
                return true;
            }

            _graphByOwnerId = new Dictionary<string, DialogueGraphSO>(StringComparer.Ordinal);
            if (_graphs == null)
            {
                return true;
            }

            for (int i = 0; i < _graphs.Count; i++)
            {
                DialogueGraphSO graph = _graphs[i];
                if (graph == null)
                {
                    Debug.LogError("[DialogueDatabaseSO] Graph list contains a null entry.", this);
                    _graphByOwnerId = null;
                    return false;
                }

                if (!graph.TryValidate(out string error))
                {
                    Debug.LogError(error, graph);
                    _graphByOwnerId = null;
                    return false;
                }

                if (_graphByOwnerId.ContainsKey(graph.OwnerId))
                {
                    Debug.LogError($"[DialogueDatabaseSO] Duplicate owner id detected: {graph.OwnerId}");
                    _graphByOwnerId = null;
                    return false;
                }

                _graphByOwnerId.Add(graph.OwnerId, graph);
            }

            return true;
        }

        private void OnEnable()
        {
            InvalidateCache();
        }

        private void OnValidate()
        {
            InvalidateCache();
        }
    }
}
