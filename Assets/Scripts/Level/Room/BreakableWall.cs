using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Semantic wrapper for secret breakable walls.
    /// Owns suspicious authoring signals and intact/destroyed presentation only,
    /// while delegating damage, persistence, and save integration to <see cref="DestroyableObject"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DestroyableObject))]
    public class BreakableWall : MonoBehaviour
    {
        [Header("Suspicious Signals")]
        [Tooltip("Renderers that should only be visible before the wall is broken, such as cracks or subtle glow.")]
        [SerializeField] private SpriteRenderer[] _suspiciousSignalRenderers;

        [Header("State Presentation")]
        [Tooltip("Objects that should stay active only while the wall is still intact.")]
        [SerializeField] private GameObject[] _intactOnlyObjects;

        [Tooltip("Objects that should become active only after the wall has been destroyed.")]
        [SerializeField] private GameObject[] _destroyedOnlyObjects;

        private DestroyableObject _destroyable;

        private void Awake()
        {
            _destroyable = GetComponent<DestroyableObject>();
            if (_destroyable == null)
            {
                Debug.LogError($"[BreakableWall] {gameObject.name}: Missing DestroyableObject.");
                return;
            }

            ApplyPresentation(_destroyable.IsDestroyed);
        }

        private void OnEnable()
        {
            if (_destroyable == null)
            {
                _destroyable = GetComponent<DestroyableObject>();
            }

            if (_destroyable == null)
            {
                Debug.LogError($"[BreakableWall] {gameObject.name}: Missing DestroyableObject.");
                return;
            }

            _destroyable.OnDestroyed += HandleDestroyed;
            ApplyPresentation(_destroyable.IsDestroyed);
        }

        private void Start()
        {
            ApplyPresentation(_destroyable != null && _destroyable.IsDestroyed);
        }

        private void OnDisable()
        {
            if (_destroyable != null)
            {
                _destroyable.OnDestroyed -= HandleDestroyed;
            }
        }

        private void HandleDestroyed()
        {
            ApplyPresentation(true);
        }

        private void ApplyPresentation(bool isDestroyed)
        {
            SetRendererGroupEnabled(_suspiciousSignalRenderers, !isDestroyed);
            SetGameObjectGroupActive(_intactOnlyObjects, !isDestroyed);
            SetGameObjectGroupActive(_destroyedOnlyObjects, isDestroyed);
        }

        private static void SetRendererGroupEnabled(SpriteRenderer[] renderers, bool enabled)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = enabled;
            }
        }

        private static void SetGameObjectGroupActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            foreach (var target in objects)
            {
                if (target == null)
                {
                    continue;
                }

                target.SetActive(active);
            }
        }
    }
}
