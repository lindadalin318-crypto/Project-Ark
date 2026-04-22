using System;
using ProjectArk.Core;
using ProjectArk.SpaceLife.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class GiftUI : MonoBehaviour
    {
        [Header("UI Root")]
        [SerializeField] private GameObject _giftPanel;
        [SerializeField] private CanvasGroup _giftCanvasGroup;

        [Header("UI Elements")]
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private GameObject _itemButtonPrefab;
        [SerializeField] private Text _npcNameText;
        [SerializeField] private Button _closeButton;

        private NPCController _currentNPC;
        private GiftInventory _giftInventory;

        public event Action OnGiftGiven;
        public event Action OnGiftClosed;

        public bool IsVisible => _giftCanvasGroup != null && _giftCanvasGroup.alpha > 0.001f;

        private void Awake()
        {
            EnsurePanelObjectIsActive();
            ResolveCanvasGroup();
            ApplyVisibility(false);
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _giftInventory = ServiceLocator.Get<GiftInventory>();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(CloseUI);
            }
        }

        public void ShowGiftUI(NPCController npc)
        {
            if (npc == null)
            {
                Debug.LogError("[GiftUI] Cannot open gift UI without an NPC context.");
                return;
            }

            _currentNPC = npc;

            if (_npcNameText != null)
            {
                _npcNameText.text = $"送礼物给 {npc.NPCName}";
            }

            ApplyVisibility(true);
            RefreshItems();
        }

        public void GiveGift(NPCController npc, ItemSO gift)
        {
            if (npc == null || gift == null)
            {
                return;
            }

            if (_giftInventory == null)
            {
                Debug.LogError("[GiftUI] GiftInventory service is missing.");
                return;
            }

            int relationshipChange = CalculateGiftValue(npc, gift);
            if (_giftInventory.RemoveItem(gift))
            {
                npc.ChangeRelationship(relationshipChange);
                Debug.Log($"[GiftUI] Gave {gift.ItemName} to {npc.NPCName}. +{relationshipChange} relationship");

                OnGiftGiven?.Invoke();
                CloseUI();
            }
        }

        public void CloseUI()
        {
            bool wasVisible = IsVisible;
            ApplyVisibility(false);
            ClearItems();
            _currentNPC = null;

            if (_npcNameText != null)
            {
                _npcNameText.text = string.Empty;
            }

            if (wasVisible)
            {
                OnGiftClosed?.Invoke();
            }
        }

        private void RefreshItems()
        {
            ClearItems();

            if (_giftInventory == null)
            {
                return;
            }

            foreach (ItemSO item in _giftInventory.Items)
            {
                CreateItemButton(item);
            }
        }

        private void ClearItems()
        {
            if (_itemsContainer == null)
            {
                return;
            }

            for (int i = _itemsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_itemsContainer.GetChild(i).gameObject);
            }
        }

        private void CreateItemButton(ItemSO item)
        {
            if (_itemButtonPrefab == null || _itemsContainer == null || item == null)
            {
                return;
            }

            GameObject buttonObject = Instantiate(_itemButtonPrefab, _itemsContainer);
            Button button = buttonObject.GetComponent<Button>();
            Text buttonText = buttonObject.GetComponentInChildren<Text>();
            Image buttonImage = buttonObject.GetComponent<Image>();

            if (buttonText != null)
            {
                buttonText.text = item.ItemName;
            }

            if (buttonImage != null && item.Icon != null)
            {
                buttonImage.sprite = item.Icon;
            }

            if (button != null)
            {
                button.onClick.AddListener(() => OnItemSelected(item));
            }
        }

        private void OnItemSelected(ItemSO item)
        {
            if (_currentNPC == null || item == null)
            {
                return;
            }

            GiveGift(_currentNPC, item);
        }

        private int CalculateGiftValue(NPCController npc, ItemSO gift)
        {
            int baseValue = gift != null ? gift.BaseGiftValue : 10;

            if (npc.IsLikedGift(gift))
            {
                return baseValue + 10;
            }

            if (npc.IsDislikedGift(gift))
            {
                return -Mathf.Abs(baseValue) - 5;
            }

            return baseValue;
        }

        private void ApplyVisibility(bool isVisible)
        {
            if (_giftCanvasGroup == null)
            {
                return;
            }

            _giftCanvasGroup.alpha = isVisible ? 1f : 0f;
            _giftCanvasGroup.interactable = isVisible;
            _giftCanvasGroup.blocksRaycasts = isVisible;
        }

        private void ResolveCanvasGroup()
        {
            if (_giftCanvasGroup != null)
            {
                return;
            }

            if (_giftPanel != null)
            {
                _giftCanvasGroup = _giftPanel.GetComponent<CanvasGroup>();
            }

            if (_giftCanvasGroup == null)
            {
                _giftCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (_giftCanvasGroup == null)
            {
                Debug.LogError("[GiftUI] Missing CanvasGroup. Assign one on the gift panel root and keep the GameObject active.", this);
            }
        }

        private void EnsurePanelObjectIsActive()
        {
            if (_giftPanel != null && !_giftPanel.activeSelf)
            {
                Debug.LogWarning("[GiftUI] Gift panel GameObject was inactive. Reactivating it so CanvasGroup visibility can work; please save the scene with the panel left active.", this);
                _giftPanel.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            OnGiftGiven = null;
            OnGiftClosed = null;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(CloseUI);
            }

            ServiceLocator.Unregister(this);
        }
    }
}
