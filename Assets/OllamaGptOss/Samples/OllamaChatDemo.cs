using System.Collections.Generic;
using UnityEngine;

namespace OllamaGptOss.Samples
{
    /// <summary>
    /// Minimal, dependency-free demo. Add this next to an OllamaClient on
    /// a GameObject, enter Play mode, then either right-click this
    /// component in the Inspector and choose "Send Test Prompt", or call
    /// SendPrompt(...) yourself from your own UI/dialogue code.
    /// </summary>
    [RequireComponent(typeof(OllamaClient))]
    public class OllamaChatDemo : MonoBehaviour
    {
        [TextArea]
        public string testPrompt = "Introduce yourself in one sentence.";

        private OllamaClient _client;

        private readonly List<OllamaChatMessage> _history = new List<OllamaChatMessage>
        {
            new OllamaChatMessage("system", "You are a helpful assistant embedded in a Unity game.")
        };

        private void Awake()
        {
            _client = GetComponent<OllamaClient>();
        }

        [ContextMenu("Send Test Prompt")]
        public void SendTestPrompt()
        {
            SendPrompt(testPrompt);
        }

        public void SendPrompt(string prompt)
        {
            _history.Add(new OllamaChatMessage("user", prompt));
            Debug.Log($"[Ollama] Sending to {_client.model}: {prompt}");

            _client.Chat(_history,
                onComplete: reply =>
                {
                    _history.Add(new OllamaChatMessage("assistant", reply));
                    Debug.Log($"[Ollama] Reply: {reply}");
                },
                onError: err =>
                {
                    Debug.LogError($"[Ollama] Request failed: {err}. Is 'ollama serve' running with gpt-oss:20b pulled?");
                });
        }
    }
}
