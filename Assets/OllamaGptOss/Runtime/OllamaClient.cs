using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace OllamaGptOss
{
    /// <summary>
    /// Minimal client for talking to a local Ollama server from Unity.
    /// Defaults to the "gpt-oss:20b" model. Requires Ollama installed and
    /// running on the same machine (or a reachable host) — see the
    /// repo README for setup steps.
    /// </summary>
    public class OllamaClient : MonoBehaviour
    {
        [Tooltip("Base URL of the Ollama server (this is where the default install listens).")]
        public string host = "http://localhost:11434";

        [Tooltip("Ollama model tag to use. Pull it first with: ollama pull gpt-oss:20b")]
        public string model = "gpt-oss:20b";

        [Tooltip("Seconds to wait before giving up. gpt-oss:20b can be slow on the first call while it loads into memory.")]
        public int timeoutSeconds = 120;

        /// <summary>Single-turn prompt completion (non-streaming).</summary>
        public void Generate(string prompt, Action<string> onComplete, Action<string> onError = null)
        {
            StartCoroutine(GenerateRoutine(prompt, onComplete, onError));
        }

        /// <summary>Multi-turn chat completion (non-streaming).</summary>
        public void Chat(List<OllamaChatMessage> messages, Action<string> onComplete, Action<string> onError = null)
        {
            StartCoroutine(ChatRoutine(messages, onComplete, onError));
        }

        /// <summary>Streams response chunks as they're generated — good for a typewriter effect.</summary>
        public void GenerateStreaming(string prompt, Action<string> onToken, Action onDone = null, Action<string> onError = null)
        {
            StartCoroutine(GenerateStreamingRoutine(prompt, onToken, onDone, onError));
        }

        /// <summary>Checks whether the Ollama server is reachable at all.</summary>
        public void IsServerRunning(Action<bool> onResult)
        {
            StartCoroutine(PingRoutine(onResult));
        }

        /// <summary>Checks whether the configured model has already been pulled locally.</summary>
        public void HasModel(Action<bool> onResult)
        {
            StartCoroutine(HasModelRoutine(onResult));
        }

        private IEnumerator GenerateRoutine(string prompt, Action<string> onComplete, Action<string> onError)
        {
            string body = JsonUtility.ToJson(new OllamaGenerateRequest
            {
                model = model,
                prompt = prompt,
                stream = false
            });

            using (var req = BuildJsonRequest($"{host}/api/generate", body))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(req.error);
                    yield break;
                }

                var parsed = JsonUtility.FromJson<OllamaGenerateResponse>(req.downloadHandler.text);
                onComplete?.Invoke(parsed.response);
            }
        }

        private IEnumerator ChatRoutine(List<OllamaChatMessage> messages, Action<string> onComplete, Action<string> onError)
        {
            string body = JsonUtility.ToJson(new OllamaChatRequest
            {
                model = model,
                messages = messages.ToArray(),
                stream = false
            });

            using (var req = BuildJsonRequest($"{host}/api/chat", body))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(req.error);
                    yield break;
                }

                var parsed = JsonUtility.FromJson<OllamaChatResponse>(req.downloadHandler.text);
                onComplete?.Invoke(parsed.message?.content);
            }
        }

        private IEnumerator GenerateStreamingRoutine(string prompt, Action<string> onToken, Action onDone, Action<string> onError)
        {
            string body = JsonUtility.ToJson(new OllamaGenerateRequest
            {
                model = model,
                prompt = prompt,
                stream = true
            });

            using (var req = new UnityWebRequest($"{host}/api/generate", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new OllamaStreamingHandler(onToken);
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = timeoutSeconds;

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(req.error);
                    yield break;
                }

                onDone?.Invoke();
            }
        }

        private IEnumerator PingRoutine(Action<bool> onResult)
        {
            using (var req = UnityWebRequest.Get($"{host}/api/tags"))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();
                onResult?.Invoke(req.result == UnityWebRequest.Result.Success);
            }
        }

        private IEnumerator HasModelRoutine(Action<bool> onResult)
        {
            using (var req = UnityWebRequest.Get($"{host}/api/tags"))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onResult?.Invoke(false);
                    yield break;
                }

                var parsed = JsonUtility.FromJson<OllamaTagsResponse>(req.downloadHandler.text);
                bool found = false;
                if (parsed?.models != null)
                {
                    foreach (var tag in parsed.models)
                    {
                        if (tag.name == model || tag.model == model)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                onResult?.Invoke(found);
            }
        }

        private UnityWebRequest BuildJsonRequest(string url, string jsonBody)
        {
            var req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;
            return req;
        }
    }
}
