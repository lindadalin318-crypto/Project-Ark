using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectArk.Core;
using ProjectArk.SpaceLife.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife.Tests
{
    [TestFixture]
    public class DialogueUIPresenterTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();

            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void ShowNode_HidesCloseButton_WhenNodeHasSingleExplicitChoice()
        {
            DialogueUIPresenter presenter = CreatePresenter(out Button closeButton);

            presenter.ShowNode(CreateNode(
                nodeId: "engineer_first_meeting",
                choices: new List<DialogueNodeViewModel.ChoiceViewModel>
                {
                    new("first_meeting_ack", "记住了，我之后再来找你。", DialogueServiceExitType.None, string.Empty)
                }));

            Assert.IsFalse(closeButton.gameObject.activeSelf,
                "Single-choice acknowledgement nodes should not expose the top-right close button, otherwise the player can bypass the authored acknowledgement choice.");
        }

        [Test]
        public void ShowNode_ShowsCloseButton_WhenNodeHasMultipleExplicitChoices()
        {
            DialogueUIPresenter presenter = CreatePresenter(out Button closeButton);

            presenter.ShowNode(CreateNode(
                nodeId: "engineer_small_talk",
                choices: new List<DialogueNodeViewModel.ChoiceViewModel>
                {
                    new("engineer_status", "引擎最近到底哪里最危险？", DialogueServiceExitType.None, string.Empty),
                    new("engineer_gift", "我带了点适合你的东西。", DialogueServiceExitType.OpenGift, "engineer_hub")
                }));

            Assert.IsTrue(closeButton.gameObject.activeSelf,
                "Multi-choice dialogue should keep the close button available so the player can back out without committing to a branch or service exit.");
        }

        [Test]
        public void ShowNode_ReShowsCloseButton_WhenDialogueReturnsToNodeWithoutChoices()
        {
            DialogueUIPresenter presenter = CreatePresenter(out Button closeButton);

            presenter.ShowNode(CreateNode(
                nodeId: "engineer_first_meeting",
                choices: new List<DialogueNodeViewModel.ChoiceViewModel>
                {
                    new("first_meeting_ack", "记住了，我之后再来找你。", DialogueServiceExitType.None, string.Empty)
                }));

            presenter.ShowNode(CreateNode(nodeId: "engineer_status_reply"));

            Assert.IsTrue(closeButton.gameObject.activeSelf,
                "Nodes without explicit choices should restore the close button so utility-only dialogue can still be dismissed.");
        }

        [Test]
        public void ShowNode_ConfiguresChoiceButtonsForReadableLongDialogueOptions()
        {
            DialogueUIPresenter presenter = CreatePresenter(out _);

            presenter.ShowNode(CreateNode(
                nodeId: "engineer_first_meeting",
                choices: new List<DialogueNodeViewModel.ChoiceViewModel>
                {
                    new("first_meeting_ack", "记住了，我之后再来找你。", DialogueServiceExitType.None, string.Empty)
                }));

            RectTransform buttonRect = (RectTransform)GetOptionsContainer(presenter).GetChild(0);
            LayoutElement layoutElement = buttonRect.GetComponent<LayoutElement>();
            Text label = buttonRect.GetComponentInChildren<Text>();
            RectTransform labelRect = label.transform as RectTransform;

            Assert.NotNull(layoutElement,
                "Dialogue choice buttons should expose a LayoutElement so the presenter can guarantee readable button height for long option text.");
            Assert.That(layoutElement.preferredHeight, Is.GreaterThanOrEqualTo(44f));
            Assert.That(label.alignment, Is.EqualTo(TextAnchor.MiddleLeft));
            Assert.That(label.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
            Assert.That(label.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow));
            Assert.NotNull(labelRect);
            Assert.That(labelRect.offsetMin.x, Is.GreaterThan(0f));
            Assert.That(labelRect.offsetMax.x, Is.LessThan(0f));
        }

        [Test]
        public void ShowNode_ExpandsOptionStrip_WhenMultipleChoicesNeedMoreVerticalSpace()
        {
            DialogueUIPresenter presenter = CreatePresenter(out _);
            RectTransform optionsRect = GetOptionsContainer(presenter);
            RectTransform dialogueTextRect = GetDialogueTextRect(presenter);
            float defaultOptionsTop = optionsRect.offsetMax.y;
            float defaultDialogueBottom = dialogueTextRect.offsetMin.y;

            presenter.ShowNode(CreateNode(
                nodeId: "engineer_small_talk",
                choices: new List<DialogueNodeViewModel.ChoiceViewModel>
                {
                    new("engineer_status", "引擎最近到底哪里最危险？", DialogueServiceExitType.None, string.Empty),
                    new("engineer_gift", "我带了点适合你的东西。", DialogueServiceExitType.OpenGift, "engineer_hub")
                }));

            Assert.That(optionsRect.offsetMax.y, Is.GreaterThan(defaultOptionsTop),
                "Multiple dialogue choices should expand the option strip instead of clipping stacked buttons into the default single-row area.");
            Assert.That(dialogueTextRect.offsetMin.y, Is.GreaterThan(defaultDialogueBottom),
                "When the option strip grows, the dialogue body should move up to preserve a gap instead of overlapping the choices.");
        }

        private DialogueUIPresenter CreatePresenter(out Button closeButton)
        {
            var root = new GameObject("DialogueUIPresenterRoot", typeof(RectTransform));
            _createdObjects.Add(root);

            var presenter = root.AddComponent<DialogueUIPresenter>();

            var panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(CanvasGroup));
            panel.transform.SetParent(root.transform, false);
            _createdObjects.Add(panel);

            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(panel.transform, false);
            _createdObjects.Add(avatar);

            var speakerText = CreateTextObject(panel.transform, "SpeakerName");
            var dialogueText = CreateTextObject(panel.transform, "DialogueText");
            ConfigureDialogueTextRect(dialogueText.GetComponent<RectTransform>());

            var options = new GameObject("OptionsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            options.transform.SetParent(panel.transform, false);
            ConfigureOptionsRect(options.GetComponent<RectTransform>());
            VerticalLayoutGroup optionsLayout = options.GetComponent<VerticalLayoutGroup>();
            optionsLayout.spacing = 4f;
            optionsLayout.childForceExpandWidth = true;
            optionsLayout.childForceExpandHeight = false;
            _createdObjects.Add(options);

            var optionButtonPrefab = CreateButtonObject("OptionButtonPrefab");
            optionButtonPrefab.SetActive(false);
            _createdObjects.Add(optionButtonPrefab);

            closeButton = CreateButtonObject("CloseButton").GetComponent<Button>();
            closeButton.transform.SetParent(panel.transform, false);
            _createdObjects.Add(closeButton.gameObject);

            SetPrivateField(presenter, "_dialoguePanel", panel);
            SetPrivateField(presenter, "_dialogueCanvasGroup", panel.GetComponent<CanvasGroup>());
            SetPrivateField(presenter, "_avatarImage", avatar.GetComponent<Image>());
            SetPrivateField(presenter, "_speakerNameText", speakerText.GetComponent<Text>());
            SetPrivateField(presenter, "_dialogueText", dialogueText.GetComponent<Text>());
            SetPrivateField(presenter, "_optionsContainer", options.transform);
            SetPrivateField(presenter, "_optionButtonPrefab", optionButtonPrefab);
            SetPrivateField(presenter, "_closeButton", closeButton);
            SetPrivateField(presenter, "_typewriterSpeed", 0f);

            return presenter;
        }

        private DialogueNodeViewModel CreateNode(string nodeId, List<DialogueNodeViewModel.ChoiceViewModel> choices = null)
        {
            return new DialogueNodeViewModel(
                ownerId: "engineer_hub",
                nodeId: nodeId,
                nodeType: DialogueNodeType.Line,
                speakerName: "工程师",
                text: "placeholder",
                choices: choices ?? new List<DialogueNodeViewModel.ChoiceViewModel>());
        }

        private GameObject CreateTextObject(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = string.Empty;
            _createdObjects.Add(go);
            return go;
        }

        private GameObject CreateButtonObject(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform goRect = go.GetComponent<RectTransform>();
            goRect.anchorMin = new Vector2(0f, 0f);
            goRect.anchorMax = new Vector2(1f, 0f);
            goRect.pivot = new Vector2(0.5f, 0f);
            goRect.sizeDelta = new Vector2(0f, 36f);

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            label.transform.SetParent(go.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = name;
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return go;
        }

        private static void ConfigureDialogueTextRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(120f, 60f);
            rect.offsetMax = new Vector2(-20f, -56f);
        }

        private static void ConfigureOptionsRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(120f, 8f);
            rect.offsetMax = new Vector2(-20f, 56f);
        }

        private static RectTransform GetOptionsContainer(DialogueUIPresenter presenter)
        {
            return GetPrivateFieldValue<Transform>(presenter, "_optionsContainer") as RectTransform;
        }

        private static RectTransform GetDialogueTextRect(DialogueUIPresenter presenter)
        {
            Text dialogueText = GetPrivateFieldValue<Text>(presenter, "_dialogueText");
            return dialogueText.transform as RectTransform;
        }

        private static T GetPrivateFieldValue<T>(object target, string fieldName) where T : class
        {
            var type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.GetValue(target) as T;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Field '{fieldName}' not found on {target.GetType().Name}.");
            return null;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Field '{fieldName}' not found on {target.GetType().Name}.");
        }
    }
}
