using UnityEngine;
using NyghtshadeHollow.AI;
using NyghtshadeHollow.Characters;
using NyghtshadeHollow.Core;

namespace NyghtshadeHollow.Prison
{
    // Combines GuardAI with interactable NPC behavior
    [RequireComponent(typeof(GuardAI))]
    public class GuardNPC : MonoBehaviour, IInteractable
    {
        [Header("Guard Identity")]
        public string GuardName;
        public string Rank;
        public string DialogueTreeID;

        [Header("Bribe")]
        public bool CanBeBribed;
        public int BribeAmount = 50;
        public string BribeFlag;

        private GuardAI _ai;

        private void Awake() => _ai = GetComponent<GuardAI>();

        public bool CanInteract(GameObject initiator)
        {
            // Can't talk to a guard who's trying to arrest you
            return _ai.GetComponent<GuardAI>() != null;
        }

        public string GetInteractionLabel() => $"[Guard] {GuardName}";

        public void Interact(GameObject initiator)
        {
            GameManager.Instance?.Dialogue.StartDialogue(DialogueTreeID, GuardName);
        }

        public bool AttemptBribe(int amount)
        {
            if (!CanBeBribed || amount < BribeAmount) return false;
            GameManager.Instance?.Inventory.RemoveItem("SHIV", amount); // placeholder currency
            GameManager.Instance?.PrisonState.SetFlag(BribeFlag, true);
            GameManager.Instance?.PrisonState.ModifyReputation(-5);
            return true;
        }
    }
}
