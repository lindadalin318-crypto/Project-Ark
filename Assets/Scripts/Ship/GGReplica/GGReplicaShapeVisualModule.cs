using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// MVP grab/shape visual module for the isolated GGReplica PlayerView lane.
    /// </summary>
    public sealed class GGReplicaShapeVisualModule : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _grabRightRenderer;
        [SerializeField] private SpriteRenderer _grabLeftRenderer;

        public void ApplyState(GGReplicaViewState state)
        {
            bool grabbing = state == GGReplicaViewState.Grab;
            SetEnabled(_grabRightRenderer, grabbing);
            SetEnabled(_grabLeftRenderer, grabbing);
        }

        private static void SetEnabled(SpriteRenderer renderer, bool enabled)
        {
            if (renderer != null) renderer.enabled = enabled;
        }
    }
}
