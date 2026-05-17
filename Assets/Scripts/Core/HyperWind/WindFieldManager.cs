using ProjectArk.Core;
using UnityEngine;

namespace ProjectArk.HyperWind
{
    /// <summary>
    /// MVP rectangular wind field used by HyperWind slice D'.
    /// Later slices can replace this with texture/vector-field sampling without changing consumers.
    /// </summary>
    public sealed class WindFieldManager : MonoBehaviour, IWindFieldService
    {
        [System.Serializable]
        private sealed class WindFieldRegion
        {
            [SerializeField] private string _label = "Wind Region";
            [SerializeField] private Rect _worldRect = new Rect(-10f, -6f, 20f, 12f);
            [SerializeField] private Vector2 _direction = Vector2.right;
            [SerializeField] [Min(0f)] private float _baseSpeed = 2f;
            [SerializeField] [Min(0f)] private float _phaseResponse = 1f;

            public WindFieldRegion() { }

            public WindFieldRegion(string label, Rect worldRect, Vector2 direction, float baseSpeed, float phaseResponse)
            {
                _label = label;
                _worldRect = worldRect;
                _direction = direction;
                _baseSpeed = baseSpeed;
                _phaseResponse = phaseResponse;
            }

            public string Label => _label;

            public Rect WorldRect => _worldRect;
            public Vector2 Direction => _direction.sqrMagnitude > 0.0001f ? _direction.normalized : Vector2.right;
            public float BaseSpeed => Mathf.Max(0f, _baseSpeed);
            public float PhaseResponse => Mathf.Max(0f, _phaseResponse);

            public bool Contains(Vector2 worldPosition)
            {
                return _worldRect.Contains(worldPosition);
            }
        }

        [Header("Fallback Wind")]
        [SerializeField] private Vector2 _defaultDirection = Vector2.right;
        [SerializeField] [Min(0f)] private float _defaultBaseSpeed = 0.5f;
        [SerializeField] [Min(0f)] private float _defaultPhaseResponse = 1f;

        [Header("Slice D' Regions")]
        [SerializeField] private WindFieldRegion[] _regions = System.Array.Empty<WindFieldRegion>();

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] [Min(1)] private int _gizmoColumns = 6;
        [SerializeField] [Min(1)] private int _gizmoRows = 4;
        [SerializeField] [Min(0.1f)] private float _gizmoArrowScale = 0.45f;

        private IWindPhaseService _phaseService;

        private void Reset()
        {
            _defaultDirection = Vector2.right;
            _defaultBaseSpeed = 0.25f;
            _defaultPhaseResponse = 1f;
            _regions = new[]
            {
                CreateRegion("Left / Tailwind", new Rect(-30f, -10f, 25f, 20f), Vector2.right, 2.5f),
                CreateRegion("Center / Cyclone Lane", new Rect(-5f, -10f, 10f, 20f), Vector2.right, 0.75f),
                CreateRegion("Right / Headwind", new Rect(5f, -10f, 25f, 20f), Vector2.left, 2.5f)
            };
        }

        private void Awake()
        {
            ServiceLocator.Register<IWindFieldService>(this);
            ServiceLocator.TryGet(out _phaseService);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IWindFieldService>(this);
        }

        public WindSample Sample(Vector2 worldPosition)
        {
            WindFieldRegion region = FindRegion(worldPosition);
            float phaseMultiplier = ResolvePhaseMultiplier(region != null ? region.PhaseResponse : _defaultPhaseResponse);

            if (region != null)
            {
                return new WindSample(region.Direction, region.BaseSpeed, phaseMultiplier);
            }

            Vector2 direction = _defaultDirection.sqrMagnitude > 0.0001f ? _defaultDirection.normalized : Vector2.right;
            return new WindSample(direction, _defaultBaseSpeed, phaseMultiplier);
        }

        private WindFieldRegion FindRegion(Vector2 worldPosition)
        {
            if (_regions == null)
            {
                return null;
            }

            for (int i = _regions.Length - 1; i >= 0; i--)
            {
                WindFieldRegion region = _regions[i];
                if (region != null && region.Contains(worldPosition))
                {
                    return region;
                }
            }

            return null;
        }

        private float ResolvePhaseMultiplier(float phaseResponse)
        {
            if (_phaseService == null && !ServiceLocator.TryGet(out _phaseService))
            {
                return 1f;
            }

            float baseMultiplier = _phaseService != null ? _phaseService.CurrentWindMultiplier : 1f;
            return Mathf.Lerp(1f, baseMultiplier, Mathf.Clamp01(phaseResponse));
        }

        private static WindFieldRegion CreateRegion(string label, Rect rect, Vector2 direction, float baseSpeed)
        {
            return new WindFieldRegion(label, rect, direction, baseSpeed, 1f);
        }


        private void OnValidate()
        {
            _defaultDirection = _defaultDirection.sqrMagnitude > 0.0001f ? _defaultDirection.normalized : Vector2.right;
            _defaultBaseSpeed = Mathf.Max(0f, _defaultBaseSpeed);
            _defaultPhaseResponse = Mathf.Max(0f, _defaultPhaseResponse);
            _gizmoColumns = Mathf.Max(1, _gizmoColumns);
            _gizmoRows = Mathf.Max(1, _gizmoRows);
            _gizmoArrowScale = Mathf.Max(0.1f, _gizmoArrowScale);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos || _regions == null)
            {
                return;
            }

            for (int i = 0; i < _regions.Length; i++)
            {
                DrawRegionGizmos(_regions[i]);
            }
        }

        private void DrawRegionGizmos(WindFieldRegion region)
        {
            if (region == null)
            {
                return;
            }

            Rect rect = region.WorldRect;
            Vector3 center = new(rect.center.x, rect.center.y, 0f);
            Vector3 size = new(rect.width, rect.height, 0f);

            Gizmos.color = new Color(0.25f, 0.75f, 1f, 0.35f);
            Gizmos.DrawWireCube(center, size);

            float stepX = rect.width / _gizmoColumns;
            float stepY = rect.height / _gizmoRows;
            Vector3 direction = region.Direction;
            float arrowLength = Mathf.Max(stepX, stepY) * _gizmoArrowScale;

            Gizmos.color = new Color(0.25f, 0.9f, 1f, 0.85f);
            for (int x = 0; x < _gizmoColumns; x++)
            {
                for (int y = 0; y < _gizmoRows; y++)
                {
                    Vector3 origin = new(rect.xMin + stepX * (x + 0.5f), rect.yMin + stepY * (y + 0.5f), 0f);
                    DrawArrow(origin, direction, arrowLength);
                }
            }
        }

        private static void DrawArrow(Vector3 origin, Vector3 direction, float length)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 normalized = direction.normalized;
            Vector3 end = origin + normalized * length;
            Vector3 side = new Vector3(-normalized.y, normalized.x, 0f) * (length * 0.25f);

            Vector3 back = normalized * (length * 0.3f);

            Gizmos.DrawLine(origin, end);
            Gizmos.DrawLine(end, end - back + side);
            Gizmos.DrawLine(end, end - back - side);
        }
    }
}
