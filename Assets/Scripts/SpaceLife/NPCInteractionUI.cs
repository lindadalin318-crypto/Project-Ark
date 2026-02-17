
using ProjectArk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class NPCInteractionUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _interactionPanel;
        [SerializeField] private Text _npcNameText;
        [SerializeField] private Button _talkButton;
        [SerializeField] private Button _giftButton;
        [SerializeField] private Button _closeButton;

        private NPCController _currentNPC;
        private GiftInventory _giftInventory;
        private GiftUI _giftUI;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _giftInventory = ServiceLocator.Get<GiftInventory>();
            _giftUI = ServiceLocator.Get<GiftUI>();

            if (_interactionPanel != null)
                _interactionPanel.SetActive(false);

            if (_talkButton != null)
                _talkButton.onClick.AddListener(OnTalkClicked);

            if (_giftButton != null)
                _giftButton.onClick.AddListener(OnGiftClicked);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(CloseUI);
        }

        public void ShowInteractionUI(NPCController npc)
        {
            if (npc == null) return;

            _currentNPC = npc;

            if (_npcNameText != null)
                _npcNameText.text = npc.NPCName;

            if (_giftButton != null && _giftInventory != null)
            {
                _giftButton.interactable = _giftInventory.GetItemCount() > 0;
            }

            if (_interactionPanel != null)
                _interactionPanel.SetActive(true);
        }

        private void OnTalkClicked()
        {
            if (_currentNPC != null)
            {
                _currentNPC.StartDialogue();
                CloseUI();
            }
        }

        private void OnGiftClicked()
        {
            if (_currentNPC != null && _giftUI != null)
            {
                _giftUI.ShowGiftUI(_currentNPC);
                CloseUI();
            }
        }

        public void CloseUI()
        {
            if (_interactionPanel != null)
                _interactionPanel.SetActive(false);

            _currentNPC = null;
        }

        private void OnDestroy()
        {
            if (_talkButton != null)
                _talkButton.onClick.RemoveListener(OnTalkClicked);

            if (_giftButton != null)
                _giftButton.onClick.RemoveListener(OnGiftClicked);

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(CloseUI);

            ServiceLocator.Unregister(this);
        }
    }
}

