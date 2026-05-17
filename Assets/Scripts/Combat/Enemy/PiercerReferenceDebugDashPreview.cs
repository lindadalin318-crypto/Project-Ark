using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// ReferenceOnly light movement preview for Piercer dash timing.
    /// Applies visual offset only; it does not move gameplay colliders, deal damage, or request director tokens.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PiercerReferenceVisual))]
    public sealed class PiercerReferenceDebugDashPreview : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PiercerReferenceVisual _visual;
        [SerializeField] private Transform _previewTarget;

        [Header("Dash Preview")]
        [SerializeField] private bool _enablePreview = true;
        [SerializeField, Min(0f)] private float _dashDistance = 1.4f;
        [SerializeField] private Vector3 _dashDirection = Vector3.right;
        [SerializeField] private bool _useLocalDirection = true;

        private readonly PiercerReferenceDashPreviewSampler _sampler = new PiercerReferenceDashPreviewSampler();
        private Vector3 _baseLocalPosition;
        private Vector3 _baseWorldPosition;

        private void Awake()
        {
            if (_visual == null)
                _visual = GetComponent<PiercerReferenceVisual>();

            if (_previewTarget == null)
                _previewTarget = transform;

            CaptureBasePosition();
            ValidateReferences();
        }

        private void OnEnable()
        {
            CaptureBasePosition();
            ResetPreviewPosition();
        }

        private void OnDisable()
        {
            ResetPreviewPosition();
        }

        private void OnValidate()
        {
            _dashDistance = Mathf.Max(0f, _dashDistance);
        }

        private void LateUpdate()
        {
            if (_visual == null || _previewTarget == null)
                return;

            if (!_enablePreview)
            {
                ResetPreviewPosition();
                return;
            }

            PiercerReferencePhaseSnapshot snapshot = _visual.ResolveCurrentSnapshot();
            Vector3 direction = _sampler.ResolveDirection(_dashDirection, transform.rotation, _useLocalDirection);
            Vector3 offset = _sampler.SampleOffset(snapshot, direction, _dashDistance);

            if (_useLocalDirection)
                _previewTarget.localPosition = _baseLocalPosition + offset;
            else
                _previewTarget.position = _baseWorldPosition + offset;
        }

        /// <summary>
        /// Recaptures the current target position as the reset point for ReferenceOnly preview movement.
        /// </summary>
        public void CaptureBasePosition()
        {
            if (_previewTarget == null)
                return;

            _baseLocalPosition = _previewTarget.localPosition;
            _baseWorldPosition = _previewTarget.position;
        }

        /// <summary>
        /// Resets the preview target to the captured base position.
        /// </summary>
        public void ResetPreviewPosition()
        {
            if (_previewTarget == null)
                return;

            if (_useLocalDirection)
                _previewTarget.localPosition = _baseLocalPosition;
            else
                _previewTarget.position = _baseWorldPosition;
        }

        private void ValidateReferences()
        {
            if (_visual == null)
                Debug.LogError($"[{nameof(PiercerReferenceDebugDashPreview)}] Missing PiercerReferenceVisual on {name}.", this);

            if (_previewTarget == null)
                Debug.LogError($"[{nameof(PiercerReferenceDebugDashPreview)}] Missing preview target on {name}.", this);
        }
    }
}
