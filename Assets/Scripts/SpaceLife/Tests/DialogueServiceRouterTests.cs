using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectArk.SpaceLife.Dialogue;
using UnityEngine;

namespace ProjectArk.SpaceLife.Tests
{
    [TestFixture]
    public class DialogueServiceRouterTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void TryRoute_InvokesIntelEvent_WhenExitRequestsIntel()
        {
            var router = CreateRouter();
            string receivedPayload = null;

            GetPrivateField<DialogueServiceRouter.PayloadEvent>(router, "_onOpenIntel")
                .AddListener(payload => receivedPayload = payload);

            bool routed = router.TryRoute(new DialogueServiceExit(DialogueServiceExitType.OpenIntel, "intel_payload"));

            Assert.IsTrue(routed);
            Assert.AreEqual("intel_payload", receivedPayload);
        }

        [Test]
        public void TryRoute_OpensGiftUi_AndReleasesHubLock_WhenGiftUiCloses()
        {
            var managerRoot = new GameObject("SpaceLifeManager");
            var manager = managerRoot.AddComponent<SpaceLifeManager>();
            SetPrivateField(manager, "_isInSpaceLifeMode", true);
            _createdObjects.Add(managerRoot);

            var giftRoot = new GameObject("GiftUI");
            giftRoot.AddComponent<CanvasGroup>();
            var giftUi = giftRoot.AddComponent<GiftUI>();
            _createdObjects.Add(giftRoot);

            var router = CreateRouter();
            SetPrivateField(router, "_giftUI", giftUi);
            SetPrivateField(router, "_spaceLifeManager", manager);

            var npcRoot = new GameObject("NPC");
            npcRoot.AddComponent<Interactable>();
            var npc = npcRoot.AddComponent<NPCController>();
            _createdObjects.Add(npcRoot);

            bool routed = router.TryRoute(new DialogueServiceExit(DialogueServiceExitType.OpenGift), npc);

            Assert.IsTrue(routed);
            Assert.IsTrue(giftUi.IsVisible);
            Assert.IsTrue(manager.IsHubInteractionLocked);

            giftUi.CloseUI();

            Assert.IsFalse(manager.IsHubInteractionLocked);
        }

        private DialogueServiceRouter CreateRouter()
        {
            var routerGo = new GameObject("DialogueServiceRouter");
            var router = routerGo.AddComponent<DialogueServiceRouter>();
            _createdObjects.Add(routerGo);
            return router;
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Assert.Fail($"Field '{fieldName}' not found on {target.GetType().Name}.");
            }

            return field.GetValue(target) as T;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Assert.Fail($"Field '{fieldName}' not found on {target.GetType().Name}.");
            }

            field.SetValue(target, value);
        }
    }
}
