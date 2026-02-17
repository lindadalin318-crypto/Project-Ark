
using System;
using ProjectArk.Core;
using ProjectArk.SpaceLife.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class GiftUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _giftPanel;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private GameObject _itemButtonPrefab;
        [SerializeField] private Text _npcNameText;
        [SerializeField] private Button _closeButton;

        private NPCController _currentNPC;
        private GiftInventory _giftInventory;

        public event Action OnGiftGiven;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _giftInventory = ServiceLocator.Get<GiftInventory>();
            
            if (_giftPanel != null)
                _giftPanel.SetActive(false);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(CloseUI);
        }

        public void ShowGiftUI(NPCController npc)
        {
            if (npc == null) return;

            _currentNPC = npc;

            if (_npcNameText != null)
                _npcNameText.text = $"送礼物给 {npc.NPCName}";

            if (_giftPanel != null)
                _giftPanel.SetActive(true);

            RefreshItems();
        }

        private void RefreshItems()
        {
            ClearItems();

            if (_giftInventory == null) return;

            foreach (var item in _giftInventory.Items)
            {
                CreateItemButton(item);
            }
        }

        private void ClearItems()
        {
            if (_itemsContainer == null) return;

            foreach (Transform child in _itemsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateItemButton(ItemSO item)
        {
            if (_itemButtonPrefab == null || _itemsContainer == null) return;

            GameObject buttonObj = Instantiate(_itemButtonPrefab, _itemsContainer);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            if (buttonText != null)
            {
                buttonText.text = item.itemName;
            }

            if (buttonImage != null && item.icon != null)
            {
                buttonImage.sprite = item.icon;
            }

            if (button != null)
            {
                button.onClick.AddListener(() => OnItemSelected(item));
            }
        }

        private void OnItemSelected(ItemSO item)
        {
            if (_currentNPC == null || item == null) return;

            GiveGift(_currentNPC, item);
        }

        public void GiveGift(NPCController npc, ItemSO gift)
        {
            if (npc == null || gift == null) return;
            if (_giftInventory == null) return;

            int relationshipChange = CalculateGiftValue(npc, gift);

            if (_giftInventory.RemoveItem(gift))
            {
                npc.ChangeRelationship(relationshipChange);
                Debug.Log($"[GiftUI] Gave {gift.itemName} to {npc.NPCName}. +{relationshipChange} relationship");

                OnGiftGiven?.Invoke();
                CloseUI();
            }
        }

        private int CalculateGiftValue(NPCController npc, ItemSO gift)
        {
            int baseValue = gift != null ? gift.baseGiftValue : 10;

            if (npc.IsLikedGift(gift))
            {
                return baseValue + 10;
            }
            else if (npc.IsDislikedGift(gift))
            {
                return -Mathf.Abs(baseValue) - 5;
            }

            return baseValue;
        }

        public void CloseUI()
        {
            if (_giftPanel != null)
                _giftPanel.SetActive(false);

            _currentNPC = null;
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(CloseUI);

            ServiceLocator.Unregister(this);
        }
    }
}

