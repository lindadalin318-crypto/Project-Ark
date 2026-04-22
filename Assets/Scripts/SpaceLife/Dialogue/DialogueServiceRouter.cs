using System;
using ProjectArk.Core;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectArk.SpaceLife.Dialogue
{
    public class DialogueServiceRouter : MonoBehaviour
    {
        [Serializable]
        public sealed class PayloadEvent : UnityEvent<string>
        {
        }

        [Header("Dependencies")]
        [SerializeField] private GiftUI _giftUI;
        [SerializeField] private SpaceLifeManager _spaceLifeManager;

        [Header("Service Exits")]
        [SerializeField] private PayloadEvent _onOpenUpgrade = new();
        [SerializeField] private PayloadEvent _onOpenIntel = new();
        [SerializeField] private PayloadEvent _onTriggerRelationshipEvent = new();

        private bool _giftUiSubscribed;
        private bool _giftUiLockApplied;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public bool TryRoute(DialogueServiceExit serviceExit, NPCController npcContext = null)
        {
            if (serviceExit == null || !serviceExit.HasExit)
            {
                return false;
            }

            switch (serviceExit.ExitType)
            {
                case DialogueServiceExitType.OpenGift:
                    return RouteGift(npcContext);
                case DialogueServiceExitType.OpenUpgrade:
                    return InvokePayloadEvent(_onOpenUpgrade, serviceExit.Payload, nameof(DialogueServiceExitType.OpenUpgrade));
                case DialogueServiceExitType.OpenIntel:
                    return InvokePayloadEvent(_onOpenIntel, serviceExit.Payload, nameof(DialogueServiceExitType.OpenIntel));
                case DialogueServiceExitType.TriggerRelationshipEvent:
                    return InvokePayloadEvent(_onTriggerRelationshipEvent, serviceExit.Payload, nameof(DialogueServiceExitType.TriggerRelationshipEvent));
                default:
                    Debug.LogError($"[DialogueServiceRouter] Unsupported service exit type: {serviceExit.ExitType}", this);
                    return false;
            }
        }

        private bool RouteGift(NPCController npcContext)
        {
            if (npcContext == null)
            {
                Debug.LogError("[DialogueServiceRouter] OpenGift requires an NPC context.", this);
                return false;
            }

            GiftUI giftUi = ResolveGiftUi();
            if (giftUi == null)
            {
                Debug.LogError("[DialogueServiceRouter] GiftUI is missing and OpenGift cannot be routed.", this);
                return false;
            }

            EnsureGiftUiSubscription(giftUi);
            LockGiftUiInteraction();
            giftUi.ShowGiftUI(npcContext);
            return true;
        }

        private bool InvokePayloadEvent(PayloadEvent payloadEvent, string payload, string exitName)
        {
            payloadEvent?.Invoke(payload);

            if (payloadEvent == null || payloadEvent.GetPersistentEventCount() == 0)
            {
                Debug.Log($"[DialogueServiceRouter] {exitName} requested with payload '{payload}', but no persistent listener is wired yet. This log is the current placeholder service exit.", this);
            }

            return true;
        }

        private void EnsureGiftUiSubscription(GiftUI giftUi)
        {
            if (_giftUiSubscribed || giftUi == null)
            {
                return;
            }

            giftUi.OnGiftClosed += HandleGiftUiClosed;
            _giftUiSubscribed = true;
        }

        private void HandleGiftUiClosed()
        {
            UnlockGiftUiInteraction();
        }

        private void LockGiftUiInteraction()
        {
            SpaceLifeManager manager = ResolveSpaceLifeManager();
            if (_giftUiLockApplied || manager == null)
            {
                return;
            }

            manager.SetHubInteractionLocked(true);
            _giftUiLockApplied = true;
        }

        private void UnlockGiftUiInteraction()
        {
            SpaceLifeManager manager = ResolveSpaceLifeManager();
            if (!_giftUiLockApplied || manager == null)
            {
                return;
            }

            manager.SetHubInteractionLocked(false);
            _giftUiLockApplied = false;
        }

        private GiftUI ResolveGiftUi()
        {
            if (_giftUI == null)
            {
                _giftUI = ServiceLocator.Get<GiftUI>();
            }

            return _giftUI;
        }

        private SpaceLifeManager ResolveSpaceLifeManager()
        {
            if (_spaceLifeManager == null)
            {
                _spaceLifeManager = ServiceLocator.Get<SpaceLifeManager>();
            }

            return _spaceLifeManager;
        }

        private void OnDestroy()
        {
            if (_giftUiSubscribed && _giftUI != null)
            {
                _giftUI.OnGiftClosed -= HandleGiftUiClosed;
            }

            UnlockGiftUiInteraction();
            ServiceLocator.Unregister(this);
        }
    }
}
