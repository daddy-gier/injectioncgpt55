using System;

namespace OllamaGptOss
{
    [Serializable]
    public class OllamaGenerateRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [Serializable]
    public class OllamaGenerateResponse
    {
        public string model;
        public string response;
        public bool done;
    }

    [Serializable]
    public class OllamaChatMessage
    {
        public string role;    // "system" | "user" | "assistant"
        public string content;

        public OllamaChatMessage() { }

        public OllamaChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    public class OllamaChatRequest
    {
        public string model;
        public OllamaChatMessage[] messages;
        public bool stream;
    }

    [Serializable]
    public class OllamaChatResponse
    {
        public string model;
        public OllamaChatMessage message;
        public bool done;
    }

    [Serializable]
    public class OllamaTagsResponse
    {
        public OllamaModelTag[] models;
    }

    [Serializable]
    public class OllamaModelTag
    {
        public string name;
        public string model;
        public long size;
    }
}
