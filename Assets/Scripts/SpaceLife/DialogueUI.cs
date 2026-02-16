
using System;
using ProjectArk.SpaceLife.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

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

        public event Action OnDialogueEnd;

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
            if (_avatarImage != null && line.speakerAvatar != null)
            {
                _avatarImage.sprite = line.speakerAvatar;
                _avatarImage.gameObject.SetActive(true);
            }
            else if (_avatarImage != null)
            {
                _avatarImage.gameObject.SetActive(false);
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = line.speakerName;
            }

            if (_dialogueText != null)
            {
                StopAllCoroutines();
                StartCoroutine(TypeTextCoroutine(line.text));
            }

            ClearOptions();

            if (line.options != null && line.options.Count > 0)
            {
                foreach (var option in line.options)
                {
                    CreateOptionButton(option);
                }
            }
            else
            {
                CreateContinueButton();
            }
        }

        private System.Collections.IEnumerator TypeTextCoroutine(string text)
        {
            _isTyping = true;
            _dialogueText.text = "";

            foreach (char c in text)
            {
                _dialogueText.text += c;
                yield return new WaitForSeconds(_typewriterSpeed);
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
                buttonText.text = option.optionText;
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
            if (_currentNPC != null && option.relationshipChange != 0)
            {
                _currentNPC.ChangeRelationship(option.relationshipChange);
            }

            if (option.nextLine != null)
            {
                ShowDialogue(option.nextLine, _currentNPC);
            }
            else
            {
                CloseDialogue();
            }
        }

        public void CloseDialogue()
        {
            StopAllCoroutines();
            _isTyping = false;

            if (_dialoguePanel != null)
                _dialoguePanel.SetActive(false);

            _currentLine = null;
            _currentNPC = null;

            OnDialogueEnd?.Invoke();
        }



        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

