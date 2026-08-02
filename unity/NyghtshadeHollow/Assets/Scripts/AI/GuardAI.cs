using UnityEngine;
using UnityEngine.AI;
using NyghtshadeHollow.Core;
using NyghtshadeHollow.Faction;

namespace NyghtshadeHollow.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardAI : MonoBehaviour
    {
        [Header("Guard Config")]
        public string GuardID;
        public string GuardName;
        public GuardRank Rank;
        public Transform[] PatrolPoints;

        [Header("Detection")]
        public float SightRange = 12f;
        public float SightAngle = 120f;
        public float HearingRange = 6f;
        public LayerMask ObstacleLayer;

        [Header("Thresholds")]
        public float SuspicionForAlert = 60f;
        public float SuspicionForArrest = 90f;

        private NavMeshAgent _agent;
        private int _patrolIndex;
        private float _suspicion;
        private GuardState _state = GuardState.Patrol;
        private Vector3 _lastKnownPos;

        public enum GuardRank { Officer, Senior, Captain }
        private enum GuardState { Patrol, Investigate, Alert, Arrest, Return }

        private void Awake() => _agent = GetComponent<NavMeshAgent>();

        private void Update()
        {
            DetectPlayer();
            UpdateState();
        }

        private void DetectPlayer()
        {
            var gm = GameManager.Instance;
            var player = gm?.GetPlayer();
            if (player == null) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            float dist = toPlayer.magnitude;
            float angle = Vector3.Angle(transform.forward, toPlayer);

            bool inSight = dist <= SightRange && angle <= SightAngle * 0.5f &&
                           !Physics.Raycast(transform.position + Vector3.up, toPlayer.normalized, dist, ObstacleLayer);

            bool heard = dist <= HearingRange;

            if (inSight || heard)
            {
                float rate = inSight ? 20f : 8f;
                if (gm.PrisonState.IsLockdown) rate *= 2f;
                _suspicion = Mathf.Min(_suspicion + rate * Time.deltaTime, 100f);
                _lastKnownPos = player.transform.position;
            }
            else
            {
                _suspicion = Mathf.Max(_suspicion - 5f * Time.deltaTime, 0f);
            }
        }

        private void UpdateState()
        {
            var gm = GameManager.Instance;

            if (_suspicion >= SuspicionForArrest && _state != GuardState.Arrest)
            {
                _state = GuardState.Arrest;
                gm?.PrisonState.EscalateAlert();
                gm?.PrisonState.LogIncident(GuardID, 3);
            }
            else if (_suspicion >= SuspicionForAlert && _state == GuardState.Patrol)
            {
                _state = GuardState.Alert;
            }
            else if (_suspicion < 10f && _state == GuardState.Alert)
            {
                _state = GuardState.Investigate;
            }

            switch (_state)
            {
                case GuardState.Patrol:     DoPatrol();     break;
                case GuardState.Investigate: DoInvestigate(); break;
                case GuardState.Alert:      DoAlert();      break;
                case GuardState.Arrest:     DoArrest();     break;
            }
        }

        private void DoPatrol()
        {
            if (_agent.pathPending || _agent.remainingDistance > 0.5f) return;
            _patrolIndex = (_patrolIndex + 1) % PatrolPoints.Length;
            _agent.SetDestination(PatrolPoints[_patrolIndex].position);
        }

        private void DoInvestigate()
        {
            _agent.SetDestination(_lastKnownPos);
            if (!_agent.pathPending && _agent.remainingDistance < 1f)
                _state = GuardState.Patrol;
        }

        private void DoAlert()
        {
            _agent.speed = 5f;
            _agent.SetDestination(_lastKnownPos);
        }

        private void DoArrest()
        {
            var player = GameManager.Instance?.GetPlayer();
            if (player == null) return;
            _agent.SetDestination(player.transform.position);

            if (Vector3.Distance(transform.position, player.transform.position) < 1.5f)
                TriggerArrest();
        }

        private void TriggerArrest()
        {
            // Trigger arrest sequence — UI, punishment, etc.
            GameManager.Instance?.PrisonState.ModifyReputation(-20);
            Debug.Log($"[Guard] {GuardName} arrested the player.");
            _suspicion = 0;
            _state = GuardState.Patrol;
        }
    }
}
