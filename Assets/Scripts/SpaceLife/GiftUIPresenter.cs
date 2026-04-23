using System;
using ProjectArk.Core;
using ProjectArk.SpaceLife.Data;
using ProjectArk.SpaceLife.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    /// <summary>
    /// Presentation-layer implementation of <see cref="IGiftPresenter"/>.
    /// Renders the gift inventory as clickable buttons and publishes <see cref="OnGiftFinished"/>
    /// when the interaction terminates (gift given, cancelled, or closed).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Master Plan v1.1 Phase 2 (§6.2.2):</b> replaces the legacy <c>GiftUI</c> class
    /// (deleted). Key contract changes:
    /// </para>
    /// <list type="bullet">
    /// <item>Implements <see cref="IGiftPresenter"/>; primary API takes <c>npcId</c> (string).</item>
    /// <item><see cref="OnGiftFinished"/> fires on both "gift given" and "closed/cancelled"
    ///       (legacy <c>GiftUI</c> split these into <c>OnGiftGiven</c> + <c>OnGiftClosed</c>).</item>
    /// <item>Visibility is controlled via <see cref="CanvasGroup"/>; never <c>SetActive(false)</c>.</item>
    /// </list>
    /// <para>
    /// <b>NPC resolution:</b> the interface takes <c>npcId</c>, but this presenter still needs
    /// <see cref="NPCController"/> / <see cref="NPCDataSO"/> to compute gift relationship bonuses
    /// and display the NPC name. Lookup goes through <see cref="ServiceLocator.Get{T}"/> for
    /// <see cref="RelationshipManager"/> and a scene scan for the matching <see cref="NPCController"/>
    /// keyed by <c>NPCData.NpcId</c>.
    /// </para>
    /// </remarks>
    public class GiftUIPresenter : MonoBehaviour, IGiftPresenter
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
        private string _currentNpcId;
        private GiftInventory _giftInventory;

        /// <inheritdoc />
        public event Action OnGiftFinished;

        /// <summary>
        /// <c>true</c> when the gift panel is currently visible (alpha &gt; 0).
        /// </summary>
        public bool IsVisible => _giftCanvasGroup != null && _giftCanvasGroup.alpha > 0.001f;

        private void Awake()
        {
            ResolveCanvasGroup();
            ApplyVisibility(false);
            ServiceLocator.Register(this);
            ServiceLocator.Register<IGiftPresenter>(this);
        }

        private void Start()
        {
            _giftInventory = ServiceLocator.Get<GiftInventory>();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseButtonClicked);
            }
        }

        /// <inheritdoc />
        public void ShowGiftUI(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                Debug.LogError("[GiftUIPresenter] ShowGiftUI received a null/empty npcId.", this);
                return;
            }

            NPCController npc = ResolveNpcById(npcId);
            if (npc == null)
            {
                Debug.LogError(
                    $"[GiftUIPresenter] No NPCController with NpcId '{npcId}' found in the active scene.",
                    this);
                return;
            }

            OpenForNpc(npc, npcId);
        }

        /// <summary>
        /// Presenter-specific overload that accepts a concrete <see cref="NPCController"/>.
        /// Used by <c>DialogueServiceRouter</c> when routing <c>OpenGift</c> service exits —
        /// the Router already has the NPC in hand and can avoid the scene scan.
        /// </summary>
        public void ShowGiftUI(NPCController npc)
        {
            if (npc == null || npc.NPCData == null)
            {
                Debug.LogError("[GiftUIPresenter] ShowGiftUI(NPCController) received a null NPC / NPCData.", this);
                return;
            }

            OpenForNpc(npc, npc.NPCData.NpcId);
        }

        /// <inheritdoc />
        public void HideGiftUI()
        {
            if (!IsVisible)
            {
                // Idempotent close: ensure state is reset even when already hidden.
                ClearItems();
                _currentNPC = null;
                _currentNpcId = null;
                return;
            }

            CloseInternal(notifyFinished: true);
        }

        private void OpenForNpc(NPCController npc, string npcId)
        {
            _currentNPC = npc;
            _currentNpcId = npcId;

            if (_npcNameText != null)
            {
                _npcNameText.text = $"送礼物给 {npc.NPCName}";
            }

            ApplyVisibility(true);
            RefreshItems();
        }

        private void HandleCloseButtonClicked()
        {
            // Player chose to cancel; still counts as "finished" so Coordinator / Router can
            // release the hub lock.
            CloseInternal(notifyFinished: true);
        }

        private void CloseInternal(bool notifyFinished)
        {
            bool wasVisible = IsVisible;
            ApplyVisibility(false);
            ClearItems();
            _currentNPC = null;
            _currentNpcId = null;

            if (_npcNameText != null)
            {
                _npcNameText.text = string.Empty;
            }

            if (notifyFinished && wasVisible)
            {
                OnGiftFinished?.Invoke();
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

        private void GiveGift(NPCController npc, ItemSO gift)
        {
            if (npc == null || gift == null)
            {
                return;
            }

            if (_giftInventory == null)
            {
                Debug.LogError("[GiftUIPresenter] GiftInventory service is missing.", this);
                return;
            }

            int relationshipChange = CalculateGiftValue(npc, gift);
            if (_giftInventory.RemoveItem(gift))
            {
                npc.ChangeRelationship(relationshipChange);
                Debug.Log(
                    $"[GiftUIPresenter] Gave {gift.ItemName} to {npc.NPCName}. +{relationshipChange} relationship");

                CloseInternal(notifyFinished: true);
            }
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

        private NPCController ResolveNpcById(string npcId)
        {
            // Linear scan is acceptable here: Hub scene holds a handful of NPCs and
            // ShowGiftUI is triggered at most once per interaction. If this ever becomes
            // a hot path, a dedicated NPCRegistry can be introduced without changing the
            // IGiftPresenter contract.
#if UNITY_2023_1_OR_NEWER
            NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
#else
            NPCController[] npcs = FindObjectsOfType<NPCController>();
#endif
            foreach (NPCController npc in npcs)
            {
                if (npc != null &&
                    npc.NPCData != null &&
                    string.Equals(npc.NPCData.NpcId, npcId, StringComparison.Ordinal))
                {
                    return npc;
                }
            }

            return null;
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
                Debug.LogError(
                    "[GiftUIPresenter] Missing CanvasGroup. Assign one on the gift panel root " +
                    "(SpaceLifeUIPrefabBuilder generates it automatically).",
                    this);
            }
        }

        private void OnDestroy()
        {
            OnGiftFinished = null;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
            }

            ServiceLocator.Unregister(this);
            ServiceLocator.Unregister<IGiftPresenter>(this);
        }
    }
}
