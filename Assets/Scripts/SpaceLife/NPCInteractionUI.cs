
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class NPCInteractionUI : MonoBehaviour
    {
        public static NPCInteractionUI Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private GameObject _interactionPanel;
        [SerializeField] private Text _npcNameText;
        [SerializeField] private Button _talkButton;
        [SerializeField] private Button _giftButton;
        [SerializeField] private Button _closeButton;

        private NPCController _currentNPC;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
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

            if (_giftButton != null && GiftInventory.Instance != null)
            {
                _giftButton.interactable = GiftInventory.Instance.GetItemCount() > 0;
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
            if (_currentNPC != null && GiftUI.Instance != null)
            {
                GiftUI.Instance.ShowGiftUI(_currentNPC);
                CloseUI();
            }
        }

        public void CloseUI()
        {
            if (_interactionPanel != null)
                _interactionPanel.SetActive(false);

            _currentNPC = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                if (_interactionPanel != null && _interactionPanel.activeSelf)
                {
                    CloseUI();
                }
            }
        }

        private void OnDestroy()
        {
            if (_talkButton != null)
                _talkButton.onClick.RemoveListener(OnTalkClicked);

            if (_giftButton != null)
                _giftButton.onClick.RemoveListener(OnGiftClicked);

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(CloseUI);

            if (Instance == this)
                Instance = null;
        }
    }
}

