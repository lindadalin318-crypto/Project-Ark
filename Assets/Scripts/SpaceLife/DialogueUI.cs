using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectArk.Core;
using ProjectArk.SpaceLife.Data;
using ProjectArk.SpaceLife.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Root")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private CanvasGroup _dialogueCanvasGroup;

        [Header("UI Elements")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _speakerNameText;
        [SerializeField] private Text _dialogueText;
        [SerializeField] private Transform _optionsContainer;
        [SerializeField] private GameObject _optionButtonPrefab;
        [SerializeField] private Button _closeButton;

        [Header("Settings")]
        [SerializeField] private float _typewriterSpeed = 0.05f;

        private DialogueLine _currentLegacyLine;
        private NPCController _currentLegacyNpc;
        private NPCDataSO _currentLegacyNpcData;
        private DialogueNodeViewModel _currentNode;
        private Action<string> _onChoiceSelected;
        private Action _onContinueRequested;
        private Action _onCloseRequested;
        private CancellationTokenSource _typewriterCts;
        private Sprite _currentSpeakerAvatar;

        public event Action OnDialogueEnd;

        public bool IsVisible => _dialogueCanvasGroup != null && _dialogueCanvasGroup.alpha > 0.001f;
        public DialogueNodeViewModel CurrentNode => _currentNode;

        private void Awake()
        {
            EnsurePanelObjectIsActive();
            ResolveCanvasGroup();
            ApplyVisibility(false);

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(CloseDialogue);
            }

            ServiceLocator.Register(this);
        }

        public void PresentNode(
            DialogueNodeViewModel node,
            Action<string> onChoiceSelected,
            Action onContinueRequested = null,
            Action onCloseRequested = null,
            Sprite speakerAvatar = null)
        {
            if (node == null)
            {
                Debug.LogError("[DialogueUI] Cannot present a null node.");
                return;
            }

            _currentNode = node;
            _currentLegacyLine = null;
            _currentLegacyNpc = null;
            _currentLegacyNpcData = null;
            _onChoiceSelected = onChoiceSelected;
            _onContinueRequested = onContinueRequested;
            _onCloseRequested = onCloseRequested;
            _currentSpeakerAvatar = speakerAvatar;

            ApplyVisibility(true);
            UpdatePresentation(node);
        }

        public void ShowDialogue(DialogueLine line, NPCController npc)
        {
            if (line == null)
            {
                Debug.LogWarning("[DialogueUI] Legacy ShowDialogue received a null line.");
                return;
            }

            _currentLegacyLine = line;
            _currentLegacyNpc = npc;
            _currentLegacyNpcData = npc != null ? npc.NPCData : null;

            PresentNode(
                CreateLegacyViewModel(line),
                HandleLegacyChoiceSelected,
                HandleLegacyContinueRequested,
                HandleLegacyCloseRequested,
                line.SpeakerAvatar);
        }

        public void HideDialogue()
        {
            ResetPresentationState(notifyEnd: false);
        }

        public void CloseDialogue()
        {
            if (_onCloseRequested != null)
            {
                _onCloseRequested.Invoke();
                return;
            }

            ResetPresentationState(notifyEnd: true);
        }

        private void UpdatePresentation(DialogueNodeViewModel node)
        {
            UpdateAvatar(_currentSpeakerAvatar);

            if (_speakerNameText != null)
            {
                _speakerNameText.text = string.IsNullOrWhiteSpace(node.SpeakerName)
                    ? string.Empty
                    : node.SpeakerName;
            }

            if (_dialogueText != null)
            {
                CancelTypewriter();
                _typewriterCts = new CancellationTokenSource();
                TypeTextAsync(node.Text ?? string.Empty, _typewriterCts.Token).Forget();
            }

            ClearOptions();

            IReadOnlyList<DialogueNodeViewModel.ChoiceViewModel> choices = node.Choices;
            if (choices != null && choices.Count > 0)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    CreateChoiceButton(choices[i]);
                }

                return;
            }

            if (node.NodeType == DialogueNodeType.Line)
            {
                CreateUtilityButton("继续", HandleContinueRequested);
            }
            else
            {
                CreateUtilityButton("关闭", CloseDialogue);
            }
        }

        private void HandleContinueRequested()
        {
            if (_onContinueRequested != null)
            {
                _onContinueRequested.Invoke();
                return;
            }

            ResetPresentationState(notifyEnd: true);
        }

        private void HandleLegacyChoiceSelected(string choiceId)
        {
            if (_currentLegacyLine == null)
            {
                Debug.LogError("[DialogueUI] Legacy choice selected without an active legacy line.");
                return;
            }

            if (!TryParseLegacyChoiceIndex(choiceId, out int choiceIndex) ||
                _currentLegacyLine.Options == null ||
                choiceIndex < 0 ||
                choiceIndex >= _currentLegacyLine.Options.Count)
            {
                Debug.LogError($"[DialogueUI] Invalid legacy choice id: {choiceId}");
                return;
            }

            DialogueOption option = _currentLegacyLine.Options[choiceIndex];
            if (_currentLegacyNpc != null && option.RelationshipChange != 0)
            {
                _currentLegacyNpc.ChangeRelationship(option.RelationshipChange);
            }

            DialogueLine nextLine = _currentLegacyNpcData != null
                ? _currentLegacyNpcData.GetNodeAt(option.NextLineIndex)
                : null;

            if (nextLine != null)
            {
                ShowDialogue(nextLine, _currentLegacyNpc);
                return;
            }

            ResetPresentationState(notifyEnd: true);
        }

        private void HandleLegacyContinueRequested()
        {
            ResetPresentationState(notifyEnd: true);
        }

        private void HandleLegacyCloseRequested()
        {
            ResetPresentationState(notifyEnd: true);
        }

        private DialogueNodeViewModel CreateLegacyViewModel(DialogueLine line)
        {
            var choices = new List<DialogueNodeViewModel.ChoiceViewModel>();
            if (line.Options != null)
            {
                for (int i = 0; i < line.Options.Count; i++)
                {
                    DialogueOption option = line.Options[i];
                    if (option == null)
                    {
                        continue;
                    }

                    choices.Add(new DialogueNodeViewModel.ChoiceViewModel(
                        $"legacy_{i}",
                        option.OptionText,
                        DialogueServiceExitType.None,
                        string.Empty));
                }
            }

            return new DialogueNodeViewModel(
                _currentLegacyNpcData != null ? _currentLegacyNpcData.NpcId : string.Empty,
                $"legacy_{Guid.NewGuid():N}",
                DialogueNodeType.Line,
                line.SpeakerName,
                line.Text,
                choices);
        }

        private async UniTaskVoid TypeTextAsync(string text, CancellationToken ct)
        {
            if (_dialogueText != null)
            {
                _dialogueText.text = string.Empty;
            }

            try
            {
                foreach (char c in text)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    if (_dialogueText != null)
                    {
                        _dialogueText.text += c;
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(_typewriterSpeed), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelTypewriter()
        {
            if (_typewriterCts != null)
            {
                _typewriterCts.Cancel();
                _typewriterCts.Dispose();
                _typewriterCts = null;
            }
        }

        private void UpdateAvatar(Sprite speakerAvatar)
        {
            if (_avatarImage == null)
            {
                return;
            }

            _avatarImage.sprite = speakerAvatar;
            _avatarImage.enabled = speakerAvatar != null;
        }

        private void ClearOptions()
        {
            if (_optionsContainer == null)
            {
                return;
            }

            for (int i = _optionsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_optionsContainer.GetChild(i).gameObject);
            }
        }

        private void CreateChoiceButton(DialogueNodeViewModel.ChoiceViewModel choice)
        {
            if (choice == null)
            {
                return;
            }

            CreateUtilityButton(choice.Text, () => _onChoiceSelected?.Invoke(choice.ChoiceId));
        }

        private void CreateUtilityButton(string buttonText, Action onClick)
        {
            if (_optionButtonPrefab == null || _optionsContainer == null)
            {
                Debug.LogError("[DialogueUI] Option button prefab or container is missing.");
                return;
            }

            GameObject buttonObject = Instantiate(_optionButtonPrefab, _optionsContainer);
            Button button = buttonObject.GetComponent<Button>();
            Text buttonLabel = buttonObject.GetComponentInChildren<Text>();

            if (buttonLabel != null)
            {
                buttonLabel.text = buttonText;
            }

            if (button == null)
            {
                Debug.LogError("[DialogueUI] Option button prefab is missing a Button component.");
                return;
            }

            button.onClick.AddListener(() => onClick?.Invoke());
        }

        private void ResetPresentationState(bool notifyEnd)
        {
            CancelTypewriter();
            ClearOptions();
            ApplyVisibility(false);

            _currentLegacyLine = null;
            _currentLegacyNpc = null;
            _currentLegacyNpcData = null;
            _currentNode = null;
            _onChoiceSelected = null;
            _onContinueRequested = null;
            _onCloseRequested = null;
            _currentSpeakerAvatar = null;

            if (_speakerNameText != null)
            {
                _speakerNameText.text = string.Empty;
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = string.Empty;
            }

            UpdateAvatar(null);

            if (notifyEnd)
            {
                OnDialogueEnd?.Invoke();
            }
        }

        private void ApplyVisibility(bool isVisible)
        {
            if (_dialogueCanvasGroup == null)
            {
                return;
            }

            _dialogueCanvasGroup.alpha = isVisible ? 1f : 0f;
            _dialogueCanvasGroup.interactable = isVisible;
            _dialogueCanvasGroup.blocksRaycasts = isVisible;
        }

        private void ResolveCanvasGroup()
        {
            if (_dialogueCanvasGroup != null)
            {
                return;
            }

            if (_dialoguePanel != null)
            {
                _dialogueCanvasGroup = _dialoguePanel.GetComponent<CanvasGroup>();
            }

            if (_dialogueCanvasGroup == null)
            {
                _dialogueCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (_dialogueCanvasGroup == null)
            {
                Debug.LogError("[DialogueUI] Missing CanvasGroup. Assign one on the root panel and keep the GameObject active.", this);
            }
        }

        private void EnsurePanelObjectIsActive()
        {
            if (_dialoguePanel != null && !_dialoguePanel.activeSelf)
            {
                Debug.LogWarning("[DialogueUI] Dialogue panel GameObject was inactive. Reactivating it so CanvasGroup visibility can work; please save the scene with the panel left active.", this);
                _dialoguePanel.SetActive(true);
            }
        }

        private static bool TryParseLegacyChoiceIndex(string choiceId, out int choiceIndex)
        {
            choiceIndex = -1;
            if (string.IsNullOrWhiteSpace(choiceId) || !choiceId.StartsWith("legacy_", StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(choiceId.Substring("legacy_".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out choiceIndex);
        }

        private void OnDestroy()
        {
            CancelTypewriter();
            OnDialogueEnd = null;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(CloseDialogue);
            }

            ServiceLocator.Unregister(this);
        }
    }
}
