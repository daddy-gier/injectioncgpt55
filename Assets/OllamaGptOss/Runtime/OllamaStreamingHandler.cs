using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace OllamaGptOss
{
    /// <summary>
    /// Reads Ollama's newline-delimited JSON stream and reports each
    /// generated chunk as it arrives, instead of waiting for the full
    /// response. Used by OllamaClient.GenerateStreaming.
    /// </summary>
    public class OllamaStreamingHandler : DownloadHandlerScript
    {
        private readonly Action<string> _onToken;
        private readonly StringBuilder _lineBuffer = new StringBuilder();

        public OllamaStreamingHandler(Action<string> onToken) : base(new byte[4096])
        {
            _onToken = onToken;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0) return false;

            _lineBuffer.Append(Encoding.UTF8.GetString(data, 0, dataLength));

            // Ollama streams one JSON object per line. Peel off complete
            // lines as they arrive; leave any partial line buffered for
            // the next chunk.
            string buffered = _lineBuffer.ToString();
            int searchStart = 0;
            int newlineIndex;
            while ((newlineIndex = buffered.IndexOf('\n', searchStart)) >= 0)
            {
                string line = buffered.Substring(searchStart, newlineIndex - searchStart).Trim();
                searchStart = newlineIndex + 1;

                if (string.IsNullOrEmpty(line)) continue;

                var chunk = JsonUtility.FromJson<OllamaGenerateResponse>(line);
                if (chunk != null && !string.IsNullOrEmpty(chunk.response))
                {
                    _onToken?.Invoke(chunk.response);
                }
            }

            if (searchStart > 0)
            {
                _lineBuffer.Remove(0, searchStart);
            }

            return true;
        }
    }
}
