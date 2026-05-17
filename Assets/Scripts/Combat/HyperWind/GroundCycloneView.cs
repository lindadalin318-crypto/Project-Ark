using UnityEngine;

namespace ProjectArk.Combat.HyperWind
{
    /// <summary>
    /// Procedural preview view for GroundCyclone. It is intentionally replaceable:
    /// gameplay lives in GroundCyclone, this component only renders visual intent.
    /// </summary>
    [RequireComponent(typeof(GroundCyclone))]
    public sealed class GroundCycloneView : MonoBehaviour
    {
        private const int CIRCLE_SEGMENTS = 96;
        private const int SPIRAL_SEGMENTS = 80;

        [Header("Colors")]
        [SerializeField] private Color _spawnColor = new(0.3f, 0.9f, 1f, 0.85f);
        [SerializeField] private Color _drawColor = new(0.7f, 0.35f, 1f, 0.9f);
        [SerializeField] private Color _orbitColor = new(1f, 0.9f, 0.25f, 0.95f);
        [SerializeField] private Color _burstColor = new(1f, 1f, 1f, 1f);

        [Header("Shape")]
        [SerializeField] [Min(0.01f)] private float _warningWidth = 0.08f;
        [SerializeField] [Min(0.01f)] private float _orbitWidth = 0.06f;
        [SerializeField] [Min(0.01f)] private float _spiralWidth = 0.08f;
        [SerializeField] private int _sortingOrder = 180;

        [Header("Motion")]
        [SerializeField] private float _spinDegreesPerSecond = 240f;
        [SerializeField] [Min(0f)] private float _pulseAmplitude = 0.12f;

        private GroundCyclone _cyclone;
        private LineRenderer _warningRing;
        private LineRenderer _orbitRing;
        private LineRenderer _spiral;
        private Material _lineMaterial;
        private bool _diagnosticsLogged;

        private void Awake()
        {
            _cyclone = GetComponent<GroundCyclone>();
            _lineMaterial = CreateLineMaterial();
            _warningRing = CreateLineRenderer("CycloneWarningRing", _warningWidth);
            _orbitRing = CreateLineRenderer("CycloneOrbitRing", _orbitWidth);
            _spiral = CreateLineRenderer("CycloneSpiral", _spiralWidth);
        }

        private void Update()
        {
            if (_cyclone == null)
            {
                return;
            }

            float progress = _cyclone.PhaseProgress01;
            float spin = Time.time * _spinDegreesPerSecond;

            switch (_cyclone.CurrentPhase)
            {
                case GroundCyclonePhase.Spawn:
                    RenderSpawn(progress, spin);
                    break;
                case GroundCyclonePhase.Draw:
                    RenderDraw(progress, spin);
                    break;
                case GroundCyclonePhase.Burst:
                    RenderBurst(progress, spin);
                    break;
                default:
                    SetAllEnabled(false);
                    break;
            }

            RunVisibilityDiagnosticsOnce();
        }

        private void RenderSpawn(float progress, float spin)
        {
            _warningRing.enabled = true;
            _orbitRing.enabled = false;
            _spiral.enabled = true;

            float radius = Mathf.Lerp(_cyclone.InfluenceRadius * 0.2f, _cyclone.InfluenceRadius, progress);
            Color color = WithAlpha(_spawnColor, Mathf.Lerp(0.25f, _spawnColor.a, progress));
            ConfigureCircle(_warningRing, radius, color, spin);
            ConfigureSpiral(_spiral, radius * 0.9f, _cyclone.OrbitRadius * 0.25f, WithAlpha(_spawnColor, 0.65f), spin);
        }

        private void RenderDraw(float progress, float spin)
        {
            _warningRing.enabled = true;
            _orbitRing.enabled = true;
            _spiral.enabled = true;

            float pulse = 1f + Mathf.Sin(Time.time * 5f) * _pulseAmplitude;
            ConfigureCircle(_warningRing, _cyclone.InfluenceRadius * pulse, WithAlpha(_drawColor, 0.55f), spin);
            ConfigureCircle(_orbitRing, _cyclone.OrbitRadius, _orbitColor, -spin * 0.6f);
            ConfigureSpiral(_spiral, _cyclone.InfluenceRadius * 0.95f, _cyclone.OrbitRadius * 0.35f, _drawColor, spin);
        }

        private void RenderBurst(float progress, float spin)
        {
            _warningRing.enabled = true;
            _orbitRing.enabled = false;
            _spiral.enabled = true;

            float alpha = 1f - progress;
            float radius = Mathf.Lerp(_cyclone.OrbitRadius, _cyclone.InfluenceRadius * 1.35f, progress);
            ConfigureCircle(_warningRing, radius, WithAlpha(_burstColor, alpha), spin);
            ConfigureSpiral(_spiral, radius, _cyclone.OrbitRadius * 0.2f, WithAlpha(_burstColor, alpha * 0.8f), -spin);
        }

        private LineRenderer CreateLineRenderer(string childName, float width)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = CIRCLE_SEGMENTS;
            line.widthMultiplier = width;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.sortingOrder = _sortingOrder;
            line.material = _lineMaterial;
            line.enabled = false;
            return line;
        }

        private static Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            }

            return shader != null ? new Material(shader) : null;
        }

        private void ConfigureCircle(LineRenderer line, float radius, Color color, float rotationDegrees)
        {
            if (line == null)
            {
                return;
            }

            line.loop = true;
            line.positionCount = CIRCLE_SEGMENTS;
            line.startColor = color;
            line.endColor = color;

            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float t = i / (float)CIRCLE_SEGMENTS;
                float angle = t * Mathf.PI * 2f + rotation;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void ConfigureSpiral(LineRenderer line, float outerRadius, float innerRadius, Color color, float rotationDegrees)
        {
            if (line == null)
            {
                return;
            }

            line.loop = false;
            line.positionCount = SPIRAL_SEGMENTS;
            line.startColor = color;
            line.endColor = WithAlpha(color, 0.15f);

            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < SPIRAL_SEGMENTS; i++)
            {
                float t = i / (float)(SPIRAL_SEGMENTS - 1);
                float radius = Mathf.Lerp(innerRadius, outerRadius, t);
                float angle = t * Mathf.PI * 5f + rotation;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void SetAllEnabled(bool enabled)
        {
            if (_warningRing != null) _warningRing.enabled = enabled;
            if (_orbitRing != null) _orbitRing.enabled = enabled;
            if (_spiral != null) _spiral.enabled = enabled;
        }

        private void RunVisibilityDiagnosticsOnce()
        {
            if (_diagnosticsLogged)
            {
                return;
            }

            _diagnosticsLogged = true;
            if (_lineMaterial == null)
            {
                Debug.LogError("[GroundCycloneView] Failed to create line material. Procedural cyclone view will be invisible.", this);
            }

            ValidateLineRenderer(_warningRing, nameof(_warningRing));
            ValidateLineRenderer(_orbitRing, nameof(_orbitRing));
            ValidateLineRenderer(_spiral, nameof(_spiral));
        }

        private void ValidateLineRenderer(LineRenderer line, string label)
        {
            if (line == null)
            {
                Debug.LogError($"[GroundCycloneView] Missing {label} LineRenderer.", this);
                return;
            }

            if (line.positionCount <= 0)
            {
                Debug.LogError($"[GroundCycloneView] {label} has no points and will be invisible.", this);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }
        }
    }
}
