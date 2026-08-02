using UnityEngine;
using System.Collections.Generic;
using NyghtshadeHollow.Faction;

namespace NyghtshadeHollow.Quest
{
    public enum QuestStatus { Locked, Available, Active, Completed, Failed }
    public enum QuestType { MainStory, FactionQuest, NPCPersonal, Contraband, Escape, Intel }

    [System.Serializable]
    public class QuestObjective
    {
        public string ID;
        public string Description;
        public bool IsCompleted;
        public bool IsOptional;
        public string RequiredFlag;
    }

    [System.Serializable]
    public class NHQuest
    {
        public string ID;
        public string Title;
        public string Description;
        public QuestType Type;
        public QuestStatus Status;
        public ENHFaction FactionOwner;
        public List<QuestObjective> Objectives = new();
        public string[] UnlockFlags;
        public string[] CompleteFlags;
        public int ReputationReward;
        public int FactionReward;
    }

    public class QuestManager : MonoBehaviour
    {
        private readonly Dictionary<string, NHQuest> _quests = new();

        public static event System.Action<NHQuest> OnQuestAdded;
        public static event System.Action<NHQuest> OnQuestUpdated;
        public static event System.Action<NHQuest> OnQuestCompleted;

        public void RegisterQuest(NHQuest quest)
        {
            _quests[quest.ID] = quest;
            OnQuestAdded?.Invoke(quest);
        }

        public void ActivateQuest(string id)
        {
            if (!_quests.TryGetValue(id, out var q) || q.Status != QuestStatus.Available) return;
            q.Status = QuestStatus.Active;
            OnQuestUpdated?.Invoke(q);
        }

        public void CompleteObjective(string questId, string objectiveId)
        {
            if (!_quests.TryGetValue(questId, out var q)) return;
            var obj = q.Objectives.Find(o => o.ID == objectiveId);
            if (obj == null) return;

            obj.IsCompleted = true;
            OnQuestUpdated?.Invoke(q);

            // Auto-complete if all required objectives done
            if (q.Objectives.TrueForAll(o => o.IsOptional || o.IsCompleted))
                CompleteQuest(questId);
        }

        private void CompleteQuest(string id)
        {
            if (!_quests.TryGetValue(id, out var q)) return;
            q.Status = QuestStatus.Completed;

            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.PrisonState.ModifyReputation(q.ReputationReward);
                gm.Factions.ModifyStanding(q.FactionOwner, q.FactionReward);
                if (q.CompleteFlags != null)
                    foreach (var f in q.CompleteFlags)
                        gm.PrisonState.SetFlag(f, true);
            }
            OnQuestCompleted?.Invoke(q);
        }

        public List<NHQuest> GetActiveQuests() =>
            new(_quests.Values).FindAll(q => q.Status == QuestStatus.Active);

        public List<NHQuest> GetAllQuests() => new(_quests.Values);

        public NHQuest GetQuest(string id) =>
            _quests.TryGetValue(id, out var q) ? q : null;
    }
}
