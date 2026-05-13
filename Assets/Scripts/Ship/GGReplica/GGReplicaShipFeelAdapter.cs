using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Replica-only feel adapter for GG-inspired Boost/Dodge impulses and drag experiments.
    /// It only mutates runtime Rigidbody2D state on the GGReplica prefab instance.
    /// </summary>
    public class GGReplicaShipFeelAdapter : MonoBehaviour
    {
        [SerializeField] private GGReplicaShipFeelProfileSO _profile;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private ShipDash _dash;
        [SerializeField] private ShipBoost _boost;

        private float _baseDrag;
        private bool _initialized;
        private CancellationTokenSource _dragCts;

        private void Awake()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
            if (_dash == null) _dash = GetComponent<ShipDash>();
            if (_boost == null) _boost = GetComponent<ShipBoost>();

            if (_profile == null)
            {
                Debug.LogError("[GGReplicaShipFeelAdapter] Missing feel profile.", this);
            }

            if (_rigidbody == null)
            {
                Debug.LogError("[GGReplicaShipFeelAdapter] Missing Rigidbody2D.", this);
                return;
            }

            _baseDrag = _rigidbody.linearDamping;
            _initialized = true;
        }

        private void OnEnable()
        {
            if (_dash != null) _dash.OnDashStarted += HandleDashStarted;
            if (_boost != null) _boost.OnBoostStarted += HandleBoostStarted;
            if (_boost != null) _boost.OnBoostEnded += HandleBoostEnded;
        }

        private void OnDisable()
        {
            if (_dash != null) _dash.OnDashStarted -= HandleDashStarted;
            if (_boost != null) _boost.OnBoostStarted -= HandleBoostStarted;
            if (_boost != null) _boost.OnBoostEnded -= HandleBoostEnded;

            CancelDragTween();
            RestoreDrag();
        }

        private void OnDestroy()
        {
            CancelDragTween();
        }

        private void HandleDashStarted(Vector2 direction)
        {
            if (_profile == null || _rigidbody == null) return;

            Vector2 impulseDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : (Vector2)transform.up;
            _rigidbody.AddForce(impulseDirection * _profile.DodgeForceAfterDodge, ForceMode2D.Impulse);
            ApplyTemporaryDrag(_profile.AfterBoostDrag, _profile.SpeedModAfterDodgeTime).Forget();
        }

        private void HandleBoostStarted()
        {
            if (_profile == null || _rigidbody == null) return;

            _rigidbody.AddForce((Vector2)transform.up * _profile.BoostStartImpulse, ForceMode2D.Impulse);
            CancelDragTween();
            _rigidbody.linearDamping = _profile.AfterBoostDrag;
        }

        private void HandleBoostEnded()
        {
            if (_profile == null) return;
            ApplyTemporaryDrag(_baseDrag, _profile.BoostDecayDuration).Forget();
        }

        private async UniTaskVoid ApplyTemporaryDrag(float targetDrag, float duration)
        {
            if (_rigidbody == null) return;

            CancelDragTween();
            _dragCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            CancellationToken token = _dragCts.Token;

            if (duration <= 0f)
            {
                _rigidbody.linearDamping = targetDrag;
                return;
            }

            float start = _rigidbody.linearDamping;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration && _rigidbody != null)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _rigidbody.linearDamping = Mathf.Lerp(start, targetDrag, t);
                    await UniTask.Yield(token);
                }

                if (_rigidbody != null)
                {
                    _rigidbody.linearDamping = targetDrag;
                }
            }
            catch (OperationCanceledException)
            {
                // Lifecycle cancellation is expected when the replica prefab is disabled/destroyed.
            }
        }

        private void RestoreDrag()
        {
            if (_initialized && _rigidbody != null)
            {
                _rigidbody.linearDamping = _baseDrag;
            }
        }

        private void CancelDragTween()
        {
            if (_dragCts == null) return;
            _dragCts.Cancel();
            _dragCts.Dispose();
            _dragCts = null;
        }
    }
}
