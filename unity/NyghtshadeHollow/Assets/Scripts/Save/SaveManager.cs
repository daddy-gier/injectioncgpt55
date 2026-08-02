using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using NyghtshadeHollow.Core;
using NyghtshadeHollow.Faction;

namespace NyghtshadeHollow.Save
{
    [System.Serializable]
    public class SaveData
    {
        public string version = "1.0";
        public string timestamp;
        public Dictionary<string, bool> worldFlags;
        public Dictionary<string, int> factionStandings;
        public int playerReputation;
        public string alertLevel;
        public List<string> completedQuests;
        public List<string> activeQuests;
        public Vector3Save playerPosition;
    }

    [System.Serializable]
    public class Vector3Save
    {
        public float x, y, z;
        public Vector3Save(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    public class SaveManager : MonoBehaviour
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "nyghtshade_save.json");

        public void Save()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var data = new SaveData
            {
                timestamp       = System.DateTime.Now.ToString("o"),
                worldFlags      = gm.PrisonState.GetAllFlags(),
                factionStandings = new Dictionary<string, int>(),
                playerReputation = gm.PrisonState.PlayerReputation,
                alertLevel      = gm.PrisonState.CurrentAlert.ToString(),
                completedQuests = new List<string>(),
                activeQuests    = new List<string>(),
            };

            foreach (var kvp in gm.Factions.GetAllStandings())
                data.factionStandings[kvp.Key.ToString()] = kvp.Value;

            var player = gm.GetPlayer();
            if (player != null)
                data.playerPosition = new Vector3Save(player.transform.position);

            foreach (var q in gm.Quests.GetAllQuests())
            {
                if (q.Status == Quest.QuestStatus.Completed) data.completedQuests.Add(q.ID);
                else if (q.Status == Quest.QuestStatus.Active) data.activeQuests.Add(q.ID);
            }

            File.WriteAllText(SavePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            Debug.Log($"[Save] Saved to {SavePath}");
        }

        public void Load()
        {
            if (!File.Exists(SavePath)) { Debug.Log("[Save] No save file found."); return; }

            try
            {
                var data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(SavePath));
                var gm = GameManager.Instance;
                if (gm == null || data == null) return;

                foreach (var kvp in data.worldFlags ?? new Dictionary<string, bool>())
                    gm.PrisonState.SetFlag(kvp.Key, kvp.Value);

                foreach (var kvp in data.factionStandings ?? new Dictionary<string, int>())
                    if (System.Enum.TryParse<ENHFaction>(kvp.Key, out var faction))
                        gm.Factions.ModifyStanding(faction, kvp.Value - gm.Factions.GetStanding(faction));

                if (data.playerPosition != null)
                    gm.SpawnPlayer(data.playerPosition.ToVector3());

                Debug.Log($"[Save] Loaded from {data.timestamp}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Save] Load failed: {e.Message}");
            }
        }
    }
}
