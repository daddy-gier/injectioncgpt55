using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NyghtshadeHollow.Core;

namespace NyghtshadeHollow.Dialogue
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string text;
        public string[] conditions;
        public string[] consequences;
        public DialogueResponse[] responses;
    }

    [System.Serializable]
    public class DialogueResponse
    {
        public string text;
        public string next_node;
        public string[] consequences;
        public int faction_delta;
        public string faction_target;
    }

    [System.Serializable]
    public class DialogueTree
    {
        public string npc_id;
        public string npc_name;
        public Dictionary<string, DialogueLine> nodes;
    }

    public class DialogueManager : MonoBehaviour
    {
        private Dictionary<string, DialogueTree> _trees = new();
        private DialogueTree _active;
        private string _currentNode;

        public static event System.Action<DialogueLine> OnLineReady;
        public static event System.Action OnDialogueEnded;

        private void Start()
        {
            LoadAllTrees();
        }

        private void LoadAllTrees()
        {
            // Load from Resources/Dialogue folder
            var assets = Resources.LoadAll<TextAsset>("Dialogue");
            foreach (var asset in assets)
            {
                try
                {
                    var tree = JsonConvert.DeserializeObject<DialogueTree>(asset.text);
                    if (tree != null) _trees[tree.npc_id] = tree;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Dialogue] Failed to parse {asset.name}: {e.Message}");
                }
            }
            Debug.Log($"[Dialogue] Loaded {_trees.Count} dialogue trees");
        }

        public void StartDialogue(string treeId, string npcId)
        {
            string key = string.IsNullOrEmpty(treeId) ? npcId : treeId;
            if (!_trees.TryGetValue(key, out _active))
            {
                Debug.LogWarning($"[Dialogue] Tree not found: {key}");
                return;
            }
            _currentNode = "root";
            PresentNode();
        }

        private void PresentNode()
        {
            if (_active == null || !_active.nodes.TryGetValue(_currentNode, out var line))
            {
                EndDialogue();
                return;
            }

            // Check conditions
            if (!CheckConditions(line.conditions))
            {
                EndDialogue();
                return;
            }

            OnLineReady?.Invoke(line);
        }

        public void SelectResponse(int index)
        {
            if (_active == null || !_active.nodes.TryGetValue(_currentNode, out var line)) return;
            if (line.responses == null || index >= line.responses.Length) { EndDialogue(); return; }

            var response = line.responses[index];
            ApplyConsequences(response.consequences);

            if (!string.IsNullOrEmpty(response.faction_target) &&
                System.Enum.TryParse<Faction.ENHFaction>(response.faction_target, out var faction))
            {
                GameManager.Instance?.Factions.ModifyStanding(faction, response.faction_delta);
            }

            if (string.IsNullOrEmpty(response.next_node)) { EndDialogue(); return; }
            _currentNode = response.next_node;
            PresentNode();
        }

        private bool CheckConditions(string[] conditions)
        {
            if (conditions == null) return true;
            var state = GameManager.Instance?.PrisonState;
            if (state == null) return true;
            foreach (var c in conditions)
                if (!state.GetFlag(c)) return false;
            return true;
        }

        private void ApplyConsequences(string[] consequences)
        {
            if (consequences == null) return;
            var state = GameManager.Instance?.PrisonState;
            foreach (var c in consequences)
                state?.SetFlag(c, true);
        }

        private void EndDialogue()
        {
            _active = null;
            _currentNode = null;
            OnDialogueEnded?.Invoke();
        }
    }
}
