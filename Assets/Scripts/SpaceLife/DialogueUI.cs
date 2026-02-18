
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectArk.Core;
using ProjectArk.SpaceLife.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _speakerNameText;
        [SerializeField] private Text _dialogueText;
        [SerializeField] private Transform _optionsContainer;
        [SerializeField] private GameObject _optionButtonPrefab;

        [Header("Settings")]
        [SerializeField] private float _typewriterSpeed = 0.05f;

        private DialogueLine _currentLine;
        private NPCController _currentNPC;
        private bool _isTyping;
        private CancellationTokenSource _typewriterCts;

        public event Action OnDialogueEnd;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            if (_dialoguePanel != null)
                _dialoguePanel.SetActive(false);
        }

        public void ShowDialogue(DialogueLine line, NPCController npc)
        {
            if (line == null) return;

            _currentLine = line;
            _currentNPC = npc;

            if (_dialoguePanel != null)
                _dialoguePanel.SetActive(true);

            UpdateUI(line);
        }

        private void UpdateUI(DialogueLine line)
        {
            if (_avatarImage != null && line.SpeakerAvatar != null)
            {
                _avatarImage.sprite = line.SpeakerAvatar;
                _avatarImage.gameObject.SetActive(true);
            }
            else if (_avatarImage != null)
            {
                _avatarImage.gameObject.SetActive(false);
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = line.SpeakerName;
            }

            if (_dialogueText != null)
            {
                CancelTypewriter();
                _typewriterCts = new CancellationTokenSource();
                TypeTextAsync(line.Text, _typewriterCts.Token).Forget();
            }

            ClearOptions();

            if (line.Options != null && line.Options.Count > 0)
            {
                foreach (var option in line.Options)
                {
                    CreateOptionButton(option);
                }
            }
            else
            {
                CreateContinueButton();
            }
        }

        private async UniTaskVoid TypeTextAsync(string text, CancellationToken ct)
        {
            _isTyping = true;
            _dialogueText.text = "";

            try
            {
                foreach (char c in text)
                {
                    if (ct.IsCancellationRequested) return;
                    
                    _dialogueText.text += c;
                    await UniTask.Delay(TimeSpan.FromSeconds(_typewriterSpeed), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
            }

            _isTyping = false;
        }

        private void CancelTypewriter()
        {
            if (_typewriterCts != null)
            {
                _typewriterCts.Cancel();
                _typewriterCts.Dispose();
                _typewriterCts = null;
            }
            _isTyping = false;
        }

        private void ClearOptions()
        {
            if (_optionsContainer == null) return;

            foreach (Transform child in _optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateOptionButton(DialogueOption option)
        {
            if (_optionButtonPrefab == null || _optionsContainer == null) return;

            GameObject buttonObj = Instantiate(_optionButtonPrefab, _optionsContainer);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = option.OptionText;
            }

            if (button != null)
            {
                button.onClick.AddListener(() => OnOptionSelected(option));
            }
        }

        private void CreateContinueButton()
        {
            if (_optionButtonPrefab == null || _optionsContainer == null) return;

            GameObject buttonObj = Instantiate(_optionButtonPrefab, _optionsContainer);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = "继续...";
            }

            if (button != null)
            {
                button.onClick.AddListener(CloseDialogue);
            }
        }

        private void OnOptionSelected(DialogueOption option)
        {
            if (_currentNPC != null && option.RelationshipChange != 0)
            {
                _currentNPC.ChangeRelationship(option.RelationshipChange);
            }

            if (option.NextLine != null)
            {
                ShowDialogue(option.NextLine, _currentNPC);
            }
            else
            {
                CloseDialogue();
            }
        }

        public void CloseDialogue()
        {
            CancelTypewriter();

            if (_dialoguePanel != null)
                _dialoguePanel.SetActive(false);

            _currentLine = null;
            _currentNPC = null;

            OnDialogueEnd?.Invoke();
        }

        private void OnDestroy()
        {
            CancelTypewriter();
            OnDialogueEnd = null;
            ServiceLocator.Unregister(this);
        }
    }
}

