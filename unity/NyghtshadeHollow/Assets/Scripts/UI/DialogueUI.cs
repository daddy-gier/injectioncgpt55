using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NyghtshadeHollow.Dialogue;
using NyghtshadeHollow.Core;

namespace NyghtshadeHollow.UI
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("Layout")]
        public GameObject DialoguePanel;
        public TextMeshProUGUI SpeakerName;
        public TextMeshProUGUI DialogueText;
        public Button[] ResponseButtons; // 5 buttons
        public TextMeshProUGUI[] ResponseLabels;

        [Header("Typewriter")]
        public float TypewriterSpeed = 0.03f;

        private Coroutine _typewriterCoroutine;
        private string _fullText;
        private bool _skipTypewriter;

        private void OnEnable()
        {
            DialogueManager.OnLineReady   += ShowLine;
            DialogueManager.OnDialogueEnded += HidePanel;
        }

        private void OnDisable()
        {
            DialogueManager.OnLineReady   -= ShowLine;
            DialogueManager.OnDialogueEnded -= HidePanel;
        }

        private void Start()
        {
            HidePanel();
            for (int i = 0; i < ResponseButtons.Length; i++)
            {
                int idx = i;
                ResponseButtons[i].onClick.AddListener(() => OnResponseClicked(idx));
            }
        }

        private void ShowLine(DialogueLine line)
        {
            DialoguePanel.SetActive(true);
            SpeakerName.text = line.speaker;

            // Typewriter
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _skipTypewriter = false;
            _fullText = line.text;
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));

            // Response buttons
            int count = line.responses?.Length ?? 0;
            for (int i = 0; i < ResponseButtons.Length; i++)
            {
                bool active = i < count;
                ResponseButtons[i].gameObject.SetActive(active);
                if (active) ResponseLabels[i].text = line.responses[i].text;
            }
        }

        private IEnumerator TypewriterEffect(string text)
        {
            DialogueText.text = "";
            foreach (char c in text)
            {
                if (_skipTypewriter) { DialogueText.text = text; yield break; }
                DialogueText.text += c;
                yield return new WaitForSeconds(TypewriterSpeed);
            }
        }

        private void Update()
        {
            // Skip typewriter on spacebar
            if (Input.GetKeyDown(KeyCode.Space) && _typewriterCoroutine != null)
                _skipTypewriter = true;
        }

        private void OnResponseClicked(int index)
        {
            GameManager.Instance?.Dialogue.SelectResponse(index);
        }

        private void HidePanel()
        {
            DialoguePanel.SetActive(false);
        }
    }
}
