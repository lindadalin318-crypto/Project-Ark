using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// MVP core/dodge visual module for the isolated GGReplica PlayerView lane.
    /// </summary>
    public sealed class GGReplicaCoreVisualModule : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private Transform _coreTransform;
        [SerializeField] private SpriteRenderer _dodgeRenderer;
        [SerializeField] private SpriteRenderer _dodgeHalfRenderer;
        [SerializeField, Min(1f)] private float _dodgeCoreScale = 1.2f;
        [SerializeField] private Color _dodgeCoreColor = new Color(0.67f, 0f, 1f, 0.85f);

        public void ApplyState(GGReplicaViewState state)
        {
            bool dodging = state == GGReplicaViewState.Dodge;
            SetEnabled(_dodgeRenderer, dodging);
            SetEnabled(_dodgeHalfRenderer, dodging);

            if (_coreTransform != null)
            {
                _coreTransform.localScale = dodging ? Vector3.one * _dodgeCoreScale : Vector3.one;
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.color = dodging ? _dodgeCoreColor : Color.white;
            }
        }

        private static void SetEnabled(SpriteRenderer renderer, bool enabled)
        {
            if (renderer != null) renderer.enabled = enabled;
        }
    }
}
