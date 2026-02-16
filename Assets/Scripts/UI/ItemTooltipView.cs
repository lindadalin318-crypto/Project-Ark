using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    public class ItemTooltipView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _typeText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private RectTransform _containerRect;

        [Header("Settings")]
        [SerializeField] private float _showDelay = 0.15f;
        [SerializeField] private Vector2 _offset = new Vector2(15, 15);
        [SerializeField] private float _fadeDuration = 0.1f;

        private Coroutine _showCoroutine;
        private CanvasGroup _canvasGroup;
        private bool _isVisible;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            _isVisible = false;
        }

        public void Show(StarChartItemSO item)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);

            _showCoroutine = StartCoroutine(ShowAfterDelay(item));
        }

        public void Hide()
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            if (_isVisible)
                StartCoroutine(HideCoroutine());
        }

        public void UpdatePosition()
        {
            if (!_isVisible) return;

            Vector2 mousePosition = Input.mousePosition;
            Vector2 position = mousePosition + _offset;

            var rect = _containerRect != null ? _containerRect : (RectTransform)transform;
            position.x = Mathf.Min(position.x, Screen.width - rect.rect.width);
            position.y = Mathf.Max(position.y, rect.rect.height);

            rect.position = position;
        }

        private IEnumerator ShowAfterDelay(StarChartItemSO item)
        {
            yield return new WaitForSeconds(_showDelay);
            ShowImmediately(item);
        }

        private void ShowImmediately(StarChartItemSO item)
        {
            PopulateContent(item);
            gameObject.SetActive(true);
            UpdatePosition();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                StartCoroutine(FadeCoroutine(0f, 1f, _fadeDuration));
            }

            _isVisible = true;
        }

        private IEnumerator HideCoroutine()
        {
            if (_canvasGroup != null)
                yield return StartCoroutine(FadeCoroutine(1f, 0f, _fadeDuration));

            gameObject.SetActive(false);
            _isVisible = false;
        }

        private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }
            _canvasGroup.alpha = toAlpha;
        }

        private void PopulateContent(StarChartItemSO item)
        {
            if (_nameText != null)
                _nameText.text = item.DisplayName;

            if (_typeText != null)
                _typeText.text = GetItemTypeText(item.ItemType);

            if (_descriptionText != null)
                _descriptionText.text = item.Description;

            if (_iconImage != null)
            {
                if (item.Icon != null)
                {
                    _iconImage.sprite = item.Icon;
                    _iconImage.color = Color.white;
                }
                else
                {
                    _iconImage.sprite = null;
                    _iconImage.color = InventoryItemView.GetPlaceholderColor(item);
                }
            }
        }

        private string GetItemTypeText(StarChartItemType type)
        {
            return type switch
            {
                StarChartItemType.Core => "Core",
                StarChartItemType.Prism => "Prism",
                StarChartItemType.LightSail => "Light Sail",
                StarChartItemType.Satellite => "Satellite",
                _ => "Unknown"
            };
        }

        private void Update()
        {
            UpdatePosition();
        }
    }
}
