using UnityEngine;
using UnityEngine.AI;
using NyghtshadeHollow.Core;
using NyghtshadeHollow.Faction;
using NyghtshadeHollow.Dialogue;

namespace NyghtshadeHollow.Characters
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCController : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        public string NPCID;
        public string DisplayName;
        public ENHFaction Faction;

        [Header("Behavior")]
        public float DetectionRange = 8f;
        public float SuspicionThreshold = 50f;
        public NPCSchedule Schedule;

        [Header("Dialogue")]
        public string DialogueTreeID;

        private NavMeshAgent _agent;
        private float _suspicion;
        private NPCState _state = NPCState.Idle;

        private enum NPCState { Idle, Patrol, Alerted, Dialogue, Fleeing }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            UpdateSuspicion();
            switch (_state)
            {
                case NPCState.Idle:   DoIdle();   break;
                case NPCState.Patrol: DoPatrol(); break;
                case NPCState.Alerted: DoAlerted(); break;
                case NPCState.Fleeing: DoFlee(); break;
            }
        }

        private void UpdateSuspicion()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool lockdown = gm.PrisonState.IsLockdown;
            _suspicion = Mathf.Clamp(_suspicion - (lockdown ? -5f : 2f) * Time.deltaTime, 0f, 100f);

            if (_suspicion >= SuspicionThreshold && _state != NPCState.Alerted)
            {
                _state = NPCState.Alerted;
                gm.PrisonState.LogIncident(NPCID, 1);
            }
        }

        public void RaiseSuspicion(float amount) => _suspicion += amount;

        private void DoIdle()
        {
            if (Schedule != null)
            {
                var dest = Schedule.GetCurrentDestination(Time.time);
                if (dest.HasValue) { _state = NPCState.Patrol; _agent.SetDestination(dest.Value); }
            }
        }

        private void DoPatrol()
        {
            if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                _state = NPCState.Idle;
        }

        private void DoAlerted()
        {
            var player = GameManager.Instance?.GetPlayer();
            if (player != null) _agent.SetDestination(player.transform.position);
        }

        private void DoFlee()
        {
            var player = GameManager.Instance?.GetPlayer();
            if (player == null) return;
            Vector3 away = transform.position + (transform.position - player.transform.position).normalized * 10f;
            _agent.SetDestination(away);
        }

        // IInteractable
        public bool CanInteract(GameObject initiator) => _state != NPCState.Alerted && _state != NPCState.Fleeing;
        public string GetInteractionLabel() => $"Talk to {DisplayName}";
        public void Interact(GameObject initiator)
        {
            if (!CanInteract(initiator)) return;
            _state = NPCState.Dialogue;
            _agent.isStopped = true;
            transform.LookAt(initiator.transform);
            GameManager.Instance?.Dialogue.StartDialogue(DialogueTreeID, NPCID);
        }

        public void EndDialogue()
        {
            _agent.isStopped = false;
            _state = NPCState.Idle;
        }
    }

    [System.Serializable]
    public class NPCSchedule
    {
        public ScheduleEntry[] Entries;

        public Vector3? GetCurrentDestination(float gameTime)
        {
            if (Entries == null || Entries.Length == 0) return null;
            float hour = (gameTime / 60f) % 24f;
            for (int i = Entries.Length - 1; i >= 0; i--)
                if (hour >= Entries[i].HourStart) return Entries[i].Destination;
            return Entries[0].Destination;
        }
    }

    [System.Serializable]
    public class ScheduleEntry
    {
        public float HourStart;
        public Vector3 Destination;
        public string LocationLabel;
    }
}
