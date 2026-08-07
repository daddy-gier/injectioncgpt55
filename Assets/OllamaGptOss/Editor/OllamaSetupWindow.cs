using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace OllamaGptOss.Editor
{
    /// <summary>
    /// Window > Ollama GPT-OSS > Setup Check — sanity-checks your local
    /// Ollama install and lets you kick off pulling gpt-oss:20b without
    /// leaving the Editor.
    /// </summary>
    public class OllamaSetupWindow : EditorWindow
    {
        private const string DefaultHost = "http://localhost:11434";
        private const string DefaultModel = "gpt-oss:20b";

        private string _status = "Not checked yet. Click \"Check Ollama Server\" below.";
        private UnityWebRequestAsyncOperation _pendingOp;

        [MenuItem("Window/Ollama GPT-OSS/Setup Check")]
        public static void Open()
        {
            GetWindow<OllamaSetupWindow>("Ollama Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Ollama + gpt-oss:20b Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_status, MessageType.Info);

            using (new EditorGUI.DisabledScope(_pendingOp != null))
            {
                if (GUILayout.Button("Check Ollama Server"))
                {
                    CheckServer();
                }
            }

            if (GUILayout.Button("Pull gpt-oss:20b (runs 'ollama pull' in a terminal)"))
            {
                PullModel();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Requires Ollama installed from https://ollama.com", EditorStyles.miniLabel);
        }

        private void CheckServer()
        {
            _status = "Checking...";
            var req = UnityWebRequest.Get($"{DefaultHost}/api/tags");
            req.timeout = 5;
            _pendingOp = req.SendWebRequest();
            EditorApplication.update += PollServerCheck;
        }

        private void PollServerCheck()
        {
            if (_pendingOp == null || !_pendingOp.isDone) return;

            EditorApplication.update -= PollServerCheck;
            UnityWebRequest req = _pendingOp.webRequest;

            _status = req.result == UnityWebRequest.Result.Success
                ? $"Ollama is running at {DefaultHost}.\n\n{req.downloadHandler.text}"
                : $"Could not reach Ollama at {DefaultHost}.\nMake sure it's installed and 'ollama serve' is running.\n\n({req.error})";

            req.Dispose();
            _pendingOp = null;
            Repaint();
        }

        private void PullModel()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = $"pull {DefaultModel}",
                    UseShellExecute = true,
                };
                Process.Start(psi);
                _status = $"Launched 'ollama pull {DefaultModel}' in a separate window — watch it there for progress.";
            }
            catch (System.Exception e)
            {
                _status = $"Couldn't launch ollama pull automatically ({e.Message}).\nRun it manually in a terminal instead:\n  ollama pull {DefaultModel}";
            }
        }
    }
}
