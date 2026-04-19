using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Combat.Samples
{
    /// <summary>
    /// Standalone sample rig for previewing a fully procedural EchoWave placeholder.
    /// Creates expanding ring visuals at runtime without touching combat prefabs or authored assets.
    /// Safe to delete after evaluation.
    /// </summary>
    public class EchoWaveProceduralPreviewRig : MonoBehaviour
    {
        [Header("Preview Control")]
        [Tooltip("Spawn the first wave automatically when Play Mode starts.")]
        [SerializeField] private bool _playOnStart = true;

        [Tooltip("Keep spawning waves on a fixed interval for quick visual review.")]
        [SerializeField] private bool _loop = true;

        [Tooltip("Seconds between preview waves when Loop is enabled.")]
        [SerializeField] private float _spawnInterval = 0.45f;

        [Tooltip("Optional explicit spawn point. Falls back to this transform when empty.")]
        [SerializeField] private Transform _spawnOrigin;

        [Tooltip("When no explicit Spawn Origin is assigned, move the rig onto the main camera's view center at Play Mode start so the wave is immediately visible in Game View.")]
        [SerializeField] private bool _snapToMainCameraOnPlay = true;

        [Tooltip("World Z used when snapping the preview rig to the main camera view center.")]
        [SerializeField] private float _spawnPlaneZ = 0f;

        [Tooltip("Sorting order for the generated SpriteRenderers.")]
        [SerializeField] private int _sortingOrder = 200;


        [Header("Wave Shape")]
        [Tooltip("Starting radius in world units.")]
        [SerializeField] private float _initialRadius = 0.18f;

        [Tooltip("Radius growth speed in units per second.")]
        [SerializeField] private float _expandSpeed = 1.83f;


        [Tooltip("How long each wave remains alive.")]
        [SerializeField] private float _lifetime = 0.85f;

        [Tooltip("Inner cutout ratio of the ring. Higher = thinner ring.")]
        [SerializeField] [Range(0.05f, 0.95f)] private float _innerFill = 0.20f;

        [Tooltip("Softness of the inner/outer edge. Higher = blurrier ring.")]
        [SerializeField] [Range(0.001f, 0.25f)] private float _edgeSoftness = 0.12f;



        [Tooltip("Procedural texture resolution. 64-128 is enough for placeholder work.")]
        [SerializeField] private int _textureResolution = 128;

        [Header("Visuals")]
        [Tooltip("Base color for the ring. Alpha is multiplied by the lifetime fade curve.")]
        [SerializeField] private Color _waveColor = new(1f, 0.2f, 0.9f, 1f);

        [Tooltip("Low-opacity fill under the ring to make the placeholder wave readable even on busy backgrounds.")]
        [SerializeField] [Range(0f, 0.5f)] private float _fillOpacity = 0.18f;

        [Tooltip("Controls how the ring fades over its lifetime.")]
        [SerializeField] private AnimationCurve _alphaOverLifetime = new(

            new Keyframe(0f, 0.9f),
            new Keyframe(0.15f, 1f),
            new Keyframe(0.8f, 0.35f),
            new Keyframe(1f, 0f));

        [Tooltip("Optional slight pulse layered on top of the normal radius expansion.")]
        [SerializeField] private AnimationCurve _scalePulseOverLifetime = new(
            new Keyframe(0f, 1f),
            new Keyframe(0.25f, 1.05f),
            new Keyframe(1f, 1f));

        [Header("Diagnostics")]
        [Tooltip("Spawn a tiny filled center marker so you can tell whether Game View is rendering the preview at all.")]
        [SerializeField] private bool _showCenterMarker = true;

        [Tooltip("Radius of the center marker in world units.")]
        [SerializeField] private float _centerMarkerRadius = 0.08f;

        [Tooltip("Color of the center marker.")]
        [SerializeField] private Color _centerMarkerColor = new(1f, 0.15f, 0.85f, 0.95f);

        [Tooltip("Log camera / rig / spawn information on Play Mode start.")]
        [SerializeField] private bool _verboseLogging = true;

        private readonly List<ActiveWave> _activeWaves = new();

        private Sprite _ringSprite;
        private Texture2D _ringTexture;
        private Sprite _centerMarkerSprite;
        private Texture2D _centerMarkerTexture;
        private Material _previewMaterial;
        private GameObject _centerMarkerObject;
        private float _spawnTimer;
        private int _waveSerial;

        private bool HasExplicitSpawnOrigin => _spawnOrigin != null && _spawnOrigin != transform;

        private void Reset()
        {
            _spawnOrigin = null;
        }

        private void OnValidate()
        {
            _spawnInterval = Mathf.Max(0.05f, _spawnInterval);
            _initialRadius = Mathf.Max(0.01f, _initialRadius);
            _expandSpeed = Mathf.Max(0.01f, _expandSpeed);
            _lifetime = Mathf.Max(0.05f, _lifetime);
            _textureResolution = Mathf.Clamp(_textureResolution, 32, 512);
            _centerMarkerRadius = Mathf.Max(0.01f, _centerMarkerRadius);
            _fillOpacity = Mathf.Clamp(_fillOpacity, 0f, 0.5f);

        }

        private void Start()
        {
            SnapRigToMainCameraIfNeeded();
            EnsureRuntimeMaterial();
            EnsureRingSprite();
            EnsureCenterMarkerSprite();
            SpawnOrRefreshCenterMarker();
            LogPreviewContext();

            if (_playOnStart)
            {
                SpawnPreviewWave();
            }
        }

        private void Update()
        {
            UpdateCenterMarkerPosition();

            if (_loop)
            {
                _spawnTimer += Time.deltaTime;
                if (_spawnTimer >= _spawnInterval)
                {
                    _spawnTimer = 0f;
                    SpawnPreviewWave();
                }
            }

            for (int i = _activeWaves.Count - 1; i >= 0; i--)
            {
                ActiveWave wave = _activeWaves[i];
                wave.Elapsed += Time.deltaTime;

                float normalizedTime = wave.Elapsed / wave.Lifetime;
                if (normalizedTime >= 1f)
                {
                    DestroyRuntimeObject(wave.GameObject);
                    _activeWaves.RemoveAt(i);
                    continue;
                }

                float currentRadius = wave.InitialRadius + wave.ExpandSpeed * wave.Elapsed;
                float pulseMultiplier = _scalePulseOverLifetime != null ? _scalePulseOverLifetime.Evaluate(normalizedTime) : 1f;
                float diameter = currentRadius * 2f * pulseMultiplier;
                wave.GameObject.transform.localScale = new Vector3(diameter, diameter, 1f);

                Color color = wave.BaseColor;
                float alphaFactor = _alphaOverLifetime != null ? _alphaOverLifetime.Evaluate(normalizedTime) : 1f;
                color.a *= alphaFactor;
                wave.Renderer.color = color;
            }
        }

        [ContextMenu("Spawn Preview Wave")]
        public void SpawnPreviewWave()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[EchoWavePreview] Spawn Preview Wave only works in Play Mode to avoid leaving editor-only scene residue.");
                return;
            }

            EnsureRuntimeMaterial();
            EnsureRingSprite();
            SpawnOrRefreshCenterMarker();

            Transform origin = HasExplicitSpawnOrigin ? _spawnOrigin : transform;

            GameObject waveObject = new($"EchoWavePreview_{_waveSerial++:D3}");
            waveObject.transform.SetParent(transform, worldPositionStays: false);
            waveObject.transform.position = origin.position;
            waveObject.transform.rotation = Quaternion.identity;
            waveObject.transform.localScale = Vector3.one * _initialRadius * 2f;

            var renderer = waveObject.AddComponent<SpriteRenderer>();
            ConfigureRenderer(renderer, _ringSprite, _waveColor, _sortingOrder);

            _activeWaves.Add(new ActiveWave
            {
                GameObject = waveObject,
                Renderer = renderer,
                Elapsed = 0f,
                InitialRadius = _initialRadius,
                ExpandSpeed = _expandSpeed,
                Lifetime = _lifetime,
                BaseColor = _waveColor
            });
        }

        private void ConfigureRenderer(SpriteRenderer renderer, Sprite sprite, Color color, int sortingOrder)
        {
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;

            if (_previewMaterial != null)
            {
                renderer.sharedMaterial = _previewMaterial;
            }
        }

        private void EnsureRingSprite()
        {
            if (_ringSprite != null && _ringTexture != null)
            {
                return;
            }

            _ringTexture = CreateRingTexture(_textureResolution, _innerFill, _edgeSoftness, _fillOpacity);

            _ringSprite = Sprite.Create(
                _ringTexture,
                new Rect(0f, 0f, _ringTexture.width, _ringTexture.height),
                new Vector2(0.5f, 0.5f),
                _ringTexture.width);
            _ringSprite.name = "EchoWaveProceduralRingSprite";
        }

        private void EnsureCenterMarkerSprite()
        {
            if (_centerMarkerSprite != null && _centerMarkerTexture != null)
            {
                return;
            }

            _centerMarkerTexture = CreateFilledCircleTexture(64);
            _centerMarkerSprite = Sprite.Create(
                _centerMarkerTexture,
                new Rect(0f, 0f, _centerMarkerTexture.width, _centerMarkerTexture.height),
                new Vector2(0.5f, 0.5f),
                _centerMarkerTexture.width);
            _centerMarkerSprite.name = "EchoWaveProceduralCenterMarker";
        }

        private void EnsureRuntimeMaterial()
        {
            if (_previewMaterial != null)
            {
                return;
            }

            Shader shader = FindPreviewShader();
            if (shader == null)
            {
                Debug.LogWarning("[EchoWavePreview] Could not find a preview sprite shader. Falling back to SpriteRenderer default material.");
                return;
            }

            _previewMaterial = new Material(shader)
            {
                name = "EchoWavePreview_RuntimeMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };

            _previewMaterial.SetOverrideTag("RenderType", "Transparent");
            _previewMaterial.renderQueue = 3000;

            if (_previewMaterial.HasProperty("_Surface"))
            {
                _previewMaterial.SetFloat("_Surface", 1f);
            }

            if (_previewMaterial.HasProperty("_ZWrite"))
            {
                _previewMaterial.SetFloat("_ZWrite", 0f);
            }
        }

        private static Shader FindPreviewShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Sprites/Default");
        }

        private void SpawnOrRefreshCenterMarker()
        {
            if (!_showCenterMarker)
            {
                if (_centerMarkerObject != null)
                {
                    DestroyRuntimeObject(_centerMarkerObject);
                    _centerMarkerObject = null;
                }

                return;
            }

            EnsureRuntimeMaterial();
            EnsureCenterMarkerSprite();

            if (_centerMarkerObject == null)
            {
                _centerMarkerObject = new GameObject("EchoWavePreview_CenterMarker");
                _centerMarkerObject.transform.SetParent(transform, worldPositionStays: false);
                _centerMarkerObject.transform.rotation = Quaternion.identity;

                var renderer = _centerMarkerObject.AddComponent<SpriteRenderer>();
                ConfigureRenderer(renderer, _centerMarkerSprite, _centerMarkerColor, _sortingOrder + 1);
            }

            _centerMarkerObject.transform.localScale = Vector3.one * (_centerMarkerRadius * 2f);
            UpdateCenterMarkerPosition();

            SpriteRenderer centerRenderer = _centerMarkerObject.GetComponent<SpriteRenderer>();
            if (centerRenderer != null)
            {
                centerRenderer.color = _centerMarkerColor;
            }
        }

        private void UpdateCenterMarkerPosition()
        {
            if (_centerMarkerObject == null)
            {
                return;
            }

            Transform origin = HasExplicitSpawnOrigin ? _spawnOrigin : transform;
            _centerMarkerObject.transform.position = origin.position;
        }

        private void LogPreviewContext()
        {
            if (!_verboseLogging)
            {
                return;
            }

            Camera camera = FindPreferredCamera();
            Transform origin = HasExplicitSpawnOrigin ? _spawnOrigin : transform;
            string cameraName = camera != null ? camera.name : "<none>";
            string cameraPosition = camera != null ? camera.transform.position.ToString("F2") : "<none>";
            string shaderName = _previewMaterial != null ? _previewMaterial.shader.name : "<default material>";

            Debug.Log(
                $"[EchoWavePreview] Ready. Camera={cameraName} CamPos={cameraPosition} RigPos={transform.position:F2} OriginPos={origin.position:F2} " +
                $"Shader={shaderName} Loop={_loop} SpawnInterval={_spawnInterval:F2} Lifetime={_lifetime:F2}");
        }

        private void SnapRigToMainCameraIfNeeded()
        {
            if (HasExplicitSpawnOrigin || !_snapToMainCameraOnPlay)
            {
                return;
            }

            Camera mainCamera = FindPreferredCamera();
            if (mainCamera == null)
            {
                Debug.LogWarning("[EchoWavePreview] No camera found. Preview rig stays at its authored position.");
                return;
            }

            float depthFromCamera = Mathf.Abs(_spawnPlaneZ - mainCamera.transform.position.z);
            Vector3 viewCenter = new(0.5f, 0.5f, depthFromCamera);
            Vector3 worldCenter = mainCamera.ViewportToWorldPoint(viewCenter);
            worldCenter.z = _spawnPlaneZ;
            transform.position = worldCenter;
        }

        private static Camera FindPreferredCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera;
            }

            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].enabled)
                {
                    return cameras[i];
                }
            }

            return null;
        }

        private static Texture2D CreateRingTexture(int resolution, float innerFill, float edgeSoftness, float fillOpacity)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "EchoWaveProceduralRing"
            };

            float center = (resolution - 1) * 0.5f;
            float maxRadius = center;
            float normalizedInner = Mathf.Clamp01(innerFill);
            float normalizedSoftness = Mathf.Clamp(edgeSoftness, 0.001f, 0.49f);
            float innerFadeStart = Mathf.Max(0f, normalizedInner - normalizedSoftness);
            float innerFadeEnd = Mathf.Clamp01(normalizedInner + normalizedSoftness);
            float outerFadeStart = Mathf.Clamp01(1f - normalizedSoftness);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float normalizedDistance = maxRadius > 0.001f ? distance / maxRadius : 0f;

                    float innerMask = SmoothMask(innerFadeStart, innerFadeEnd, normalizedDistance);
                    float outerMask = 1f - SmoothMask(outerFadeStart, 1f, normalizedDistance);
                    float ringAlpha = Mathf.Clamp01(innerMask * outerMask);

                    float fillMask = 1f - SmoothMask(normalizedInner * 0.25f, normalizedInner, normalizedDistance);
                    float fillAlpha = fillMask * Mathf.Lerp(fillOpacity, fillOpacity * 0.35f, normalizedDistance);
                    float alpha = Mathf.Clamp01(Mathf.Max(ringAlpha, fillAlpha));

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static float SmoothMask(float start, float end, float value)
        {
            if (Mathf.Approximately(start, end))
            {
                return value >= end ? 1f : 0f;
            }

            float t = Mathf.InverseLerp(start, end, value);
            return Mathf.SmoothStep(0f, 1f, t);
        }

        private static int CountNonZeroAlpha(Texture2D texture)
        {
            if (texture == null)
            {
                return 0;
            }

            Color32[] pixels = texture.GetPixels32();
            int count = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static Texture2D CreateFilledCircleTexture(int resolution)

        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "EchoWaveProceduralCenterMarkerTexture"
            };

            float center = (resolution - 1) * 0.5f;
            float radius = center;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _activeWaves.Count; i++)
            {
                if (_activeWaves[i].GameObject != null)
                {
                    DestroyRuntimeObject(_activeWaves[i].GameObject);
                }
            }

            _activeWaves.Clear();

            if (_centerMarkerObject != null)
            {
                DestroyRuntimeObject(_centerMarkerObject);
                _centerMarkerObject = null;
            }

            if (_previewMaterial != null)
            {
                DestroyRuntimeObject(_previewMaterial);
                _previewMaterial = null;
            }

            if (_ringSprite != null)
            {
                DestroyRuntimeObject(_ringSprite);
                _ringSprite = null;
            }

            if (_ringTexture != null)
            {
                DestroyRuntimeObject(_ringTexture);
                _ringTexture = null;
            }

            if (_centerMarkerSprite != null)
            {
                DestroyRuntimeObject(_centerMarkerSprite);
                _centerMarkerSprite = null;
            }

            if (_centerMarkerTexture != null)
            {
                DestroyRuntimeObject(_centerMarkerTexture);
                _centerMarkerTexture = null;
            }
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private sealed class ActiveWave
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public float Elapsed;
            public float InitialRadius;
            public float ExpandSpeed;
            public float Lifetime;
            public Color BaseColor;
        }
    }
}
