
using System.Collections.Generic;
using ProjectArk.Core;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectArk.SpaceLife
{
    public class Interactable : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _interactionText = "交互";
        [SerializeField] private float _interactionRange = 2f;
        [SerializeField] private bool _showIndicator = true;
        [SerializeField] private bool _logRangeDetection = true;

        [Header("Indicator")]
        [Tooltip("Optional: assign a pre-created indicator child. If empty, one will be auto-created at Awake.")]
        [SerializeField] private GameObject _indicator;

        [Header("Events")]
        public UnityEvent OnInteract;

        private const float AUTO_INDICATOR_OFFSET_Y = 1.5f;
        private const int AUTO_INDICATOR_TEXTURE_SIZE = 16;
        private const float AUTO_INDICATOR_PIXELS_PER_UNIT = 16f;
        private const float AUTO_INDICATOR_WORLD_SCALE = 0.2f;

        private static readonly HashSet<Interactable> ActiveInteractables = new();
        private static Sprite _autoIndicatorSprite;

        private bool _isInRange;
        private PlayerController2D _cachedPlayer;

        public string InteractionText
        {
            get => _interactionText;
            set => _interactionText = value;
        }

        public static IReadOnlyCollection<Interactable> RegisteredInteractables => ActiveInteractables;

        public float InteractionRange => _interactionRange;
        public bool IsInRange => _isInRange;

        private void Awake()
        {
            EnsureIndicator();
        }

        private void OnEnable()
        {
            ActiveInteractables.Add(this);
        }

        private void OnDisable()
        {
            ActiveInteractables.Remove(this);

            if (_indicator != null)
            {
                _indicator.SetActive(false);
            }

            _isInRange = false;
        }

        private void Start()
        {
            // Try to cache eagerly; if Player2D has not spawned yet
            // (e.g. before entering SpaceLife mode), we will retry lazily in Update.
            _cachedPlayer = ServiceLocator.Get<PlayerController2D>();
        }

        private void Update()
        {
            bool wasInRange = _isInRange;
            CheckPlayerInRange();
            LogRangeDetection(wasInRange);
            UpdateIndicator();
        }

        private void CheckPlayerInRange()
        {
            // Lazy re-resolve: PlayerController2D is spawned on-demand when
            // entering SpaceLife mode. If we cached null at Start() (before
            // spawn) or the cached reference became stale (pool return /
            // exit-reenter SpaceLife), re-query ServiceLocator every frame
            // until we get a valid reference.
            if (_cachedPlayer == null)
            {
                _cachedPlayer = ServiceLocator.Get<PlayerController2D>();
                if (_cachedPlayer == null)
                {
                    _isInRange = false;
                    return;
                }
            }

            float distance = Vector2.Distance(transform.position, _cachedPlayer.transform.position);
            _isInRange = distance <= _interactionRange;
        }

        public virtual void Interact()
        {
            if (!_isInRange) return;

            Debug.Log($"[Interactable] Interacted with {gameObject.name}");
            OnInteract?.Invoke();
        }

        private void LogRangeDetection(bool wasInRange)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_logRangeDetection || wasInRange || !_isInRange || _cachedPlayer == null)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, _cachedPlayer.transform.position);
            Debug.Log(
                $"[Interactable] Detected player '{_cachedPlayer.name}' near '{gameObject.name}' (distance: {distance:F2}, range: {_interactionRange:F2})",
                this);
#endif
        }

        /// <summary>
        /// Ensures an indicator child exists. Created once at Awake, toggled via SetActive.
        /// </summary>
        private void EnsureIndicator()
        {
            if (_indicator != null)
            {
                _indicator.SetActive(false);
                return;
            }

            // Auto-create a simple indicator child
            _indicator = new GameObject("InteractionIndicator");
            _indicator.transform.SetParent(transform, false);
            _indicator.transform.localPosition = Vector3.up * AUTO_INDICATOR_OFFSET_Y;
            _indicator.transform.localScale = Vector3.one * AUTO_INDICATOR_WORLD_SCALE;

            var spriteRenderer = _indicator.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetOrCreateAutoIndicatorSprite();
            spriteRenderer.color = Color.yellow;
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 100;

            _indicator.SetActive(false);
        }

        private static Sprite GetOrCreateAutoIndicatorSprite()
        {
            if (_autoIndicatorSprite != null)
            {
                return _autoIndicatorSprite;
            }

            Texture2D texture = new Texture2D(AUTO_INDICATOR_TEXTURE_SIZE, AUTO_INDICATOR_TEXTURE_SIZE, TextureFormat.RGBA32, false)
            {
                name = "InteractableIndicatorTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[AUTO_INDICATOR_TEXTURE_SIZE * AUTO_INDICATOR_TEXTURE_SIZE];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            _autoIndicatorSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, AUTO_INDICATOR_TEXTURE_SIZE, AUTO_INDICATOR_TEXTURE_SIZE),
                Vector2.one * 0.5f,
                AUTO_INDICATOR_PIXELS_PER_UNIT);
            _autoIndicatorSprite.name = "InteractableIndicatorSprite";
            _autoIndicatorSprite.hideFlags = HideFlags.HideAndDontSave;
            return _autoIndicatorSprite;
        }

        private void UpdateIndicator()
        {
            if (!_showIndicator || _indicator == null) return;

            bool shouldShow = _isInRange;
            if (_indicator.activeSelf != shouldShow)
            {
                _indicator.SetActive(shouldShow);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}

